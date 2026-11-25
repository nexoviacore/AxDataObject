using System.Globalization;
using System.Text;
using System.Text.Json;
using AxData.Contracts.Snapshots;
using AxData.Domain.Lifecycle;
using AxData.Domain.Model;

namespace AxData.Infrastructure.Serialization;

public sealed class AxDataSnapshotSerializer
{
    private readonly AxDataSnapshotOptions _options;

    public AxDataSnapshotSerializer(AxDataSnapshotOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.MaximumSerializedBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options.MaximumSerializedBytes));
        }

        if (options.MaximumGridRows <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options.MaximumGridRows));
        }

        if (options.MaximumFieldInstances <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options.MaximumFieldInstances));
        }

        _options = options;
    }

    /// <summary>Serializes validated state into the current bounded snapshot contract.</summary>
    /// <exception cref="AxDataSnapshotException">The state is unsupported, inconsistent, or exceeds configured limits.</exception>
    public byte[] Serialize(AxDataState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        EnsureStateLimits(state.DataContainers.Values);

        try
        {
            var payload = SerializeCurrent(state);
            EnsurePayloadLimit(payload.Length);
            return payload;
        }
        catch (AxDataSnapshotException)
        {
            throw;
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException or InvalidOperationException)
        {
            throw new AxDataSnapshotException("The AxData snapshot could not be serialized.", exception);
        }
    }

    /// <summary>Reads the current snapshot contract and rehydrates validated domain state.</summary>
    /// <exception cref="AxDataSnapshotException">The payload is malformed, inconsistent, or exceeds configured limits.</exception>
    public AxDataState Deserialize(ReadOnlySpan<byte> payload)
    {
        EnsurePayloadLimit(payload.Length);

        try
        {
            var snapshot = DataObjectSnapshotReader.ReadCurrent(Encoding.UTF8.GetString(payload));
            var state = FromSnapshot(snapshot);
            EnsureStateLimits(state.DataContainers.Values);
            return state;
        }
        catch (AxDataSnapshotException)
        {
            throw;
        }
        catch (Exception exception) when (exception is JsonException or FormatException or ArgumentException or InvalidOperationException)
        {
            throw new AxDataSnapshotException("The AxData snapshot is invalid.", exception);
        }
    }

    private byte[] SerializeCurrent(AxDataState state)
    {
        var snapshot = new DataObjectSnapshotV3
        {
            ObjectId = state.ObjectId,
            TenantId = state.TenantId,
            TransId = state.TransId,
            RecordId = state.RecordId,
            InitiatorUserName = state.InitiatorUserName,
            Status = state.Status.ToString(),
            DataContainers = state.DataContainers.Values
                .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(item => item.Name, StringComparer.Ordinal)
                .Select(ToSnapshot)
                .ToList(),
            Workflow = state.Workflow is not null && IsActiveWorkflow(state.Workflow.Status)
                ? ToSnapshot(state.Workflow)
                : null
        };
        return DataObjectSnapshotWriter.WriteUtf8(snapshot);
    }

    /// <summary>Creates the positional container representation after verifying a common field shape for every row.</summary>
    private static DataContainerSnapshotV3 ToSnapshot(AxDataContainer container)
    {
        string[] fieldNames = container.Fields.Select(field => field.Name).ToArray();
        var canonicalFields = fieldNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var row in container.Rows)
        {
            if (row.Fields.Count != fieldNames.Length || row.Fields.Keys.Any(field => !canonicalFields.Contains(field)))
            {
                throw new AxDataSnapshotException(
                    $"Rows in data container '{container.Name}' do not share one positional field mapping.");
            }

            if (container.Fields.Any(field =>
                    row.Fields[field.Name].IsReadOnly != field.IsReadOnly ||
                    row.Fields[field.Name].IsSourceKey != field.IsSourceKey))
            {
                throw new AxDataSnapshotException(
                    $"Rows in data container '{container.Name}' do not match its field metadata.");
            }
        }

        var fields = container.Fields
            .Select(field => new DataObjectFieldSnapshot(
                field.Name,
                (field.IsReadOnly ? DataObjectFieldSnapshot.ReadOnlyFlag : 0) |
                (field.IsSourceKey ? DataObjectFieldSnapshot.SourceKeyFlag : 0)))
            .ToList();

        var rows = container.Rows.Select(row =>
        {
            var values = fieldNames
                .Select(field => ToSnapshotFieldValue(row.Fields[field].Value, row.Fields[field].SourceId, row.Fields[field].IsSourceKey))
                .ToList();
            List<SnapshotScalar>? oldValues = null;
            if (row.State != AxRowState.New && fieldNames.Any(field => row.Fields[field].IsDirty))
            {
                oldValues = fieldNames
                    .Select(field => row.Fields[field].IsDirty
                        ? ToSnapshotFieldValue(
                            row.Fields[field].OriginalValue,
                            row.Fields[field].OriginalSourceId,
                            row.Fields[field].IsSourceKey)
                        : ToSnapshotFieldValue(
                            row.Fields[field].Value,
                            row.Fields[field].SourceId,
                            row.Fields[field].IsSourceKey))
                    .ToList();
            }
            return new DataRowSnapshotV3(
                row.RowId,
                row.PersistedRecordId,
                row.State.ToString(),
                values,
                oldValues);
        }).ToList();
        return new DataContainerSnapshotV3(container.Name, container.IsGrid, fields, rows);
    }

    private static WorkflowSnapshot ToSnapshot(PegActiveState workflow) => new(
        workflow.WorkflowInstanceId,
        workflow.CurrentTaskId,
        workflow.InitiatorUserName,
        workflow.Status.ToString(),
        workflow.ApprovalLevel,
        workflow.Approvers.ToList(),
        workflow.WorkflowVersion,
        FormatTimestamp(workflow.PendingFromUtc));

    private static SnapshotScalar ToSnapshotScalar(object? value)
    {
        return value switch
        {
            null => SnapshotScalar.Null,
            string typed => SnapshotScalar.String(typed),
            bool typed => SnapshotScalar.Boolean(typed),
            byte typed => SnapshotScalar.Number(typed.ToString(CultureInfo.InvariantCulture)),
            short typed => SnapshotScalar.Number(typed.ToString(CultureInfo.InvariantCulture)),
            int typed => SnapshotScalar.Number(typed.ToString(CultureInfo.InvariantCulture)),
            long typed => SnapshotScalar.Number(typed.ToString(CultureInfo.InvariantCulture)),
            float typed when float.IsFinite(typed) => SnapshotScalar.Number(typed.ToString("R", CultureInfo.InvariantCulture)),
            double typed when double.IsFinite(typed) => SnapshotScalar.Number(typed.ToString("R", CultureInfo.InvariantCulture)),
            decimal typed => SnapshotScalar.Number(typed.ToString(CultureInfo.InvariantCulture)),
            DateTime typed => SnapshotScalar.String(typed.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)),
            DateTimeOffset typed => SnapshotScalar.String(FormatTimestamp(typed)),
            DateOnly typed => SnapshotScalar.String(typed.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
            TimeOnly typed => SnapshotScalar.String(typed.ToString("O", CultureInfo.InvariantCulture)),
            Guid typed => SnapshotScalar.String(typed.ToString("D")),
            byte[] typed => SnapshotScalar.String(Convert.ToBase64String(typed)),
            _ => throw new AxDataSnapshotException(
                $"Field value type '{value.GetType().FullName}' is not supported in snapshots.")
        };
    }

    private static SnapshotScalar ToSnapshotFieldValue(object? value, long? sourceId, bool sourceKey)
    {
        if (!sourceKey)
        {
            if (sourceId is not null)
                throw new AxDataSnapshotException("A non-source-key field contains a source ID.");
            return ToSnapshotScalar(value);
        }

        if (value is null)
            return SnapshotScalar.Null;
        string text = Convert.ToString(value, CultureInfo.InvariantCulture)
            ?? throw new AxDataSnapshotException("A source-key field value could not be converted to text.");
        return SnapshotScalar.String($"{text}~{sourceId ?? 0}");
    }

    private static AxDataState FromSnapshot(DataObjectSnapshotV3 snapshot)
    {
        var containers = (snapshot.DataContainers ?? throw new AxDataSnapshotException("The snapshot is missing dataContainers."))
            .Select(FromSnapshot)
            .ToList();
        var workflow = snapshot.Workflow is null ? null : FromSnapshot(snapshot.Workflow);

        return AxDataState.Rehydrate(
            Required(snapshot.ObjectId, "objectId"),
            Required(snapshot.TenantId, "tenantId"),
            Required(snapshot.TransId, "transId"),
            snapshot.RecordId,
            Required(snapshot.InitiatorUserName, "initiatorUserName"),
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch,
            ParseEnum<AxDataLifecycleStatus>(snapshot.Status, "status"),
            containers,
            workflow,
            [],
            axOpen: false);
    }

    private static AxDataContainer FromSnapshot(DataContainerSnapshotV3 container)
    {
        var fields = container.Fields ?? throw new AxDataSnapshotException("A data container is missing fields.");
        var rows = (container.Rows ?? throw new AxDataSnapshotException("A data container is missing rows."))
            .Select(row => FromSnapshot(fields, row))
            .ToList();
        var fieldMetadata = fields.Select(field => new AxDataFieldMetadata(
            Required(field.Name, "dataContainers.fields.name"),
            field.ReadOnly,
            field.SourceKey));
        return new AxDataContainer(
            Required(container.DcId, "dataContainers.dcId"),
            container.IsGrid,
            rows,
            fieldMetadata);
    }

    /// <summary>Rehydrates one positional row while validating value alignment and old-value invariants.</summary>
    private static AxDataRow FromSnapshot(IReadOnlyList<DataObjectFieldSnapshot> fields, DataRowSnapshotV3 row)
    {
        if (row.Values.Count != fields.Count)
        {
            throw new AxDataSnapshotException("A row values array does not align with its data container fields.");
        }

        if (row.OldValues is not null && row.OldValues.Count != fields.Count)
            throw new AxDataSnapshotException("A row oldValues array does not align with its data container fields.");
        if (row.State == "New" && row.OldValues is not null)
            throw new AxDataSnapshotException("A new row cannot contain oldValues.");

        var values = new Dictionary<string, AxFieldValue>(fields.Count, StringComparer.OrdinalIgnoreCase);
        var hasDirtyField = false;
        for (var ordinal = 0; ordinal < fields.Count; ordinal++)
        {
            DataObjectFieldSnapshot fieldMetadata = fields[ordinal];
            var name = Required(fieldMetadata.Name, "dataContainers.fields.name");
            var current = FromSnapshotFieldValue(row.Values[ordinal], fieldMetadata.SourceKey);
            var original = row.OldValues is null
                ? current
                : FromSnapshotFieldValue(row.OldValues[ordinal], fieldMetadata.SourceKey);
            bool isDirty = !ValuesEqual(current.Value, original.Value) || current.SourceId != original.SourceId;
            hasDirtyField |= isDirty;
            AxFieldValue field = AxFieldValue.Rehydrate(
                current.Value,
                original.Value,
                isDirty,
                fieldMetadata.ReadOnly,
                current.SourceId,
                original.SourceId,
                fieldMetadata.SourceKey);

            if (!values.TryAdd(name, field))
            {
                throw new AxDataSnapshotException($"Field '{name}' occurs more than once in a data container mapping.");
            }
        }

        if (row.OldValues is not null && !hasDirtyField)
            throw new AxDataSnapshotException("A row oldValues array contains no changed field.");

        return new AxDataRow(
            Required(row.RowId, "dataContainers.rows.rowId"),
            ParseEnum<AxRowState>(row.State, "dataContainers.rows.state"),
            row.RecordId,
            values);
    }

    private static PegActiveState FromSnapshot(WorkflowSnapshot workflow)
    {
        var status = ParseEnum<PegWorkflowStatus>(workflow.Status, "workflow.status");
        if (!IsActiveWorkflow(status))
        {
            throw new AxDataSnapshotException("The snapshot contains inactive workflow state.");
        }

        return new PegActiveState
        {
            WorkflowInstanceId = Required(workflow.WorkflowInstanceId, "workflow.workflowInstanceId"),
            CurrentTaskId = workflow.CurrentTaskId,
            InitiatorUserName = Required(workflow.InitiatorUserName, "workflow.initiatorUserName"),
            Status = status,
            ApprovalLevel = workflow.ApprovalLevel,
            Approvers = workflow.Approvers ?? throw new AxDataSnapshotException("The workflow is missing approvers."),
            WorkflowVersion = workflow.WorkflowVersion,
            PendingFromUtc = ParseTimestamp(workflow.PendingFromUtc, "workflow.pendingFromUtc")
        };
    }

    private static object? FromSnapshotValue(SnapshotScalar value)
    {
        if (value is null)
        {
            throw new AxDataSnapshotException("A snapshot scalar value is missing.");
        }

        return value.Kind switch
        {
            SnapshotScalarKind.Null when value.Text is null => null,
            SnapshotScalarKind.String => value.Text ?? throw MissingValue(value.Kind),
            SnapshotScalarKind.Boolean => bool.Parse(value.Text ?? throw MissingValue(value.Kind)),
            SnapshotScalarKind.Number => decimal.Parse(
                value.Text ?? throw MissingValue(value.Kind),
                NumberStyles.Float,
                CultureInfo.InvariantCulture),
            _ => throw new AxDataSnapshotException($"Snapshot scalar kind '{value.Kind}' is not supported.")
        };
    }

    private static DecodedFieldValue FromSnapshotFieldValue(SnapshotScalar value, bool sourceKey)
    {
        object? decoded = FromSnapshotValue(value);
        if (!sourceKey)
            return new DecodedFieldValue(decoded, null);
        if (decoded is null)
            return new DecodedFieldValue(null, null);
        if (decoded is not string text)
            throw new AxDataSnapshotException("A non-null source-key field value must be a string.");

        int separator = text.LastIndexOf('~');
        if (separator < 0 ||
            !long.TryParse(text.AsSpan(separator + 1), NumberStyles.None, CultureInfo.InvariantCulture, out long sourceId) ||
            sourceId < 0)
        {
            throw new AxDataSnapshotException("A source-key field must end with a non-negative Int64 source ID.");
        }

        return new DecodedFieldValue(text[..separator], sourceId == 0 ? null : sourceId);
    }

    private static bool IsActiveWorkflow(PegWorkflowStatus status) =>
        status is PegWorkflowStatus.Pending or PegWorkflowStatus.FinalApprovalSaveFailed;

    private static bool ValuesEqual(object? left, object? right) =>
        left is byte[] leftBytes && right is byte[] rightBytes
            ? leftBytes.AsSpan().SequenceEqual(rightBytes)
            : Equals(left, right);

    private void EnsureStateLimits(IEnumerable<AxDataContainer> containers)
    {
        var materialized = containers as IReadOnlyCollection<AxDataContainer> ?? containers.ToArray();
        var oversized = materialized.FirstOrDefault(container => container.IsGrid && container.Rows.Count > _options.MaximumGridRows);
        if (oversized is not null)
        {
            throw new AxDataSnapshotException($"Grid '{oversized.Name}' exceeds the maximum row count of {_options.MaximumGridRows}.");
        }

        var fieldCount = materialized.Sum(container => container.Rows.Sum(row => (long)row.Fields.Count));
        if (fieldCount > _options.MaximumFieldInstances)
        {
            throw new AxDataSnapshotException(
                $"Snapshot field count exceeds the maximum of {_options.MaximumFieldInstances}.");
        }
    }

    private void EnsurePayloadLimit(int byteCount)
    {
        if (byteCount > _options.MaximumSerializedBytes)
        {
            throw new AxDataSnapshotException($"Snapshot size exceeds the maximum of {_options.MaximumSerializedBytes} bytes.");
        }
    }

    private static string FormatTimestamp(DateTimeOffset value) =>
        value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);

    private static DateTimeOffset ParseTimestamp(string? value, string propertyName) =>
        DateTimeOffset.ParseExact(
            Required(value, propertyName),
            "O",
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

    private static TEnum ParseEnum<TEnum>(string? value, string propertyName)
        where TEnum : struct, Enum
    {
        if (!Enum.TryParse<TEnum>(Required(value, propertyName), ignoreCase: false, out var parsed) ||
            !Enum.IsDefined(parsed))
        {
            throw new AxDataSnapshotException($"Snapshot property '{propertyName}' has an invalid value.");
        }

        return parsed;
    }

    private static string Required(string? value, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new AxDataSnapshotException($"Snapshot property '{propertyName}' is required.");
        }

        return value;
    }

    private static AxDataSnapshotException MissingValue(SnapshotScalarKind kind) =>
        new($"Snapshot scalar kind '{kind}' requires a value.");

    private sealed record DecodedFieldValue(object? Value, long? SourceId);
}
