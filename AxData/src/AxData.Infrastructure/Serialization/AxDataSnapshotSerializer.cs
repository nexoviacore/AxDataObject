using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AxData.Domain.Lifecycle;
using AxData.Domain.Model;

namespace AxData.Infrastructure.Serialization;

public sealed class AxDataSnapshotSerializer
{
    public const int CurrentSchemaVersion = 2;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

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

        _options = options;
    }

    public byte[] Serialize(AxDataState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        EnsureGridRowLimits(state.DataContainers.Values);

        byte[] payload;
        try
        {
            payload = JsonSerializer.SerializeToUtf8Bytes(ToSnapshot(state), JsonOptions);
        }
        catch (AxDataSnapshotException)
        {
            throw;
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            throw new AxDataSnapshotException("The AxData snapshot could not be serialized.", exception);
        }

        EnsurePayloadLimit(payload.Length);
        return payload;
    }

    public AxDataState Deserialize(ReadOnlySpan<byte> payload)
    {
        EnsurePayloadLimit(payload.Length);

        try
        {
            using var document = JsonDocument.Parse(payload.ToArray());
            if (!document.RootElement.TryGetProperty("schemaVersion", out var schemaElement)
                || !schemaElement.TryGetInt32(out var schemaVersion))
            {
                throw new AxDataSnapshotException("The snapshot does not contain a valid schemaVersion.");
            }

            var state = schemaVersion switch
            {
                1 => FromSnapshot(document.RootElement.Deserialize<AxDataSnapshotV1>(JsonOptions)
                    ?? throw new AxDataSnapshotException("The snapshot payload is empty.")),
                CurrentSchemaVersion => FromSnapshot(document.RootElement.Deserialize<AxDataSnapshotV2>(JsonOptions)
                    ?? throw new AxDataSnapshotException("The snapshot payload is empty.")),
                _ => throw new AxDataSnapshotException($"Snapshot schema version '{schemaVersion}' is not supported.")
            };
            EnsureGridRowLimits(state.DataContainers.Values);
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

    private static AxDataSnapshotV2 ToSnapshot(AxDataState state)
    {
        return new AxDataSnapshotV2
        {
            SchemaVersion = CurrentSchemaVersion,
            ObjectId = state.ObjectId,
            TenantId = state.TenantId,
            TransId = state.TransId,
            KeyField = state.KeyField,
            KeyValue = state.KeyValue,
            RecordId = state.RecordId,
            OwnerUserName = state.OwnerUserName,
            InitiatorUserName = state.InitiatorUserName,
            Version = state.Version,
            StructDef = new StructDefReferenceV1(state.TransId, state.StructDefVersion),
            CreatedAtUtc = FormatTimestamp(state.CreatedAtUtc),
            LastAccessedAtUtc = FormatTimestamp(state.LastAccessedAtUtc),
            ExpiresAtUtc = state.ExpiresAtUtc is null ? null : FormatTimestamp(state.ExpiresAtUtc.Value),
            Status = state.Status.ToString(),
            DataContainers = state.DataContainers.Values
                .OrderBy(container => container.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(container => container.Name, StringComparer.Ordinal)
                .Select(ToSnapshot)
                .ToList(),
            Workflow = state.Workflow is null ? null : ToSnapshot(state.Workflow),
            Files = state.Files
                .OrderBy(file => file.FileId, StringComparer.Ordinal)
                .ThenBy(file => file.FileName, StringComparer.Ordinal)
                .Select(file => new FileReferenceV1(file.FileId, file.FileName, file.ContentType))
                .ToList(),
            AxOpen = state.AxOpen
        };
    }

    private static DataContainerV2 ToSnapshot(AxDataContainer container)
    {
        var rows = container.Rows
            .Select(row => new DataRowV2(
                row.RowId,
                row.PersistedRecordId,
                row.State.ToString(),
                row.Fields
                    .OrderBy(field => field.Key, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(field => field.Key, StringComparer.Ordinal)
                    .Select(field => new FieldV1(
                        field.Key,
                        ToSnapshotValue(field.Value.Value),
                        ToSnapshotValue(field.Value.OriginalValue),
                        field.Value.IsDirty,
                        field.Value.IsReadOnly))
                    .ToList()))
            .ToList();

        return new DataContainerV2(container.Name, container.IsGrid, rows);
    }

    private static WorkflowV1 ToSnapshot(PegActiveState workflow)
    {
        return new WorkflowV1(
            workflow.WorkflowInstanceId,
            workflow.CurrentTaskId,
            workflow.InitiatorUserName,
            workflow.Status.ToString(),
            workflow.ApprovalLevel,
            workflow.Approvers.ToList(),
            workflow.WorkflowVersion,
            FormatTimestamp(workflow.PendingFromUtc));
    }

    private static AxDataState FromSnapshot(AxDataSnapshotV1 snapshot)
    {
        if (snapshot.SchemaVersion != 1)
        {
            throw new AxDataSnapshotException($"Snapshot schema version '{snapshot.SchemaVersion}' is not supported.");
        }

        var transId = Required(snapshot.TransId, "transId");
        var structDef = snapshot.StructDef ?? throw new AxDataSnapshotException("The snapshot is missing structDef.");
        if (!string.Equals(transId, Required(structDef.TransId, "structDef.transId"), StringComparison.OrdinalIgnoreCase))
        {
            throw new AxDataSnapshotException("The StructDef reference does not match the snapshot transaction.");
        }

        var objectId = Required(snapshot.ObjectId, "objectId");
        var containers = (snapshot.DataContainers ?? throw new AxDataSnapshotException("The snapshot is missing dataContainers."))
            .Select(container => FromSnapshot(objectId, container))
            .ToList();
        var files = (snapshot.Files ?? throw new AxDataSnapshotException("The snapshot is missing files."))
            .Select(file => new AxFileReference(
                Required(file.FileId, "files.fileId"),
                Required(file.FileName, "files.fileName"),
                file.ContentType))
            .ToList();

        return AxDataState.Rehydrate(
            objectId,
            Required(snapshot.TenantId, "tenantId"),
            transId,
            snapshot.KeyField,
            snapshot.KeyValue,
            snapshot.RecordId,
            Required(snapshot.OwnerUserName, "ownerUserName"),
            Required(snapshot.InitiatorUserName, "initiatorUserName"),
            snapshot.Version,
            Required(structDef.Version, "structDef.version"),
            ParseTimestamp(snapshot.CreatedAtUtc, "createdAtUtc"),
            ParseTimestamp(snapshot.LastAccessedAtUtc, "lastAccessedAtUtc"),
            snapshot.ExpiresAtUtc is null ? null : ParseTimestamp(snapshot.ExpiresAtUtc, "expiresAtUtc"),
            ParseEnum<AxDataLifecycleStatus>(snapshot.Status, "status"),
            containers,
            snapshot.Workflow is null ? null : FromSnapshot(snapshot.Workflow),
            files,
            snapshot.AxOpen);
    }

    private static AxDataState FromSnapshot(AxDataSnapshotV2 snapshot)
    {
        if (snapshot.SchemaVersion != CurrentSchemaVersion)
        {
            throw new AxDataSnapshotException($"Snapshot schema version '{snapshot.SchemaVersion}' is not supported.");
        }

        var transId = Required(snapshot.TransId, "transId");
        var structDef = snapshot.StructDef ?? throw new AxDataSnapshotException("The snapshot is missing structDef.");
        if (!string.Equals(transId, Required(structDef.TransId, "structDef.transId"), StringComparison.OrdinalIgnoreCase))
        {
            throw new AxDataSnapshotException("The StructDef reference does not match the snapshot transaction.");
        }

        var containers = (snapshot.DataContainers ?? throw new AxDataSnapshotException("The snapshot is missing dataContainers."))
            .Select(FromSnapshot)
            .ToList();
        var files = (snapshot.Files ?? throw new AxDataSnapshotException("The snapshot is missing files."))
            .Select(file => new AxFileReference(
                Required(file.FileId, "files.fileId"),
                Required(file.FileName, "files.fileName"),
                file.ContentType))
            .ToList();

        return AxDataState.Rehydrate(
            Required(snapshot.ObjectId, "objectId"),
            Required(snapshot.TenantId, "tenantId"),
            transId,
            snapshot.KeyField,
            snapshot.KeyValue,
            snapshot.RecordId,
            Required(snapshot.OwnerUserName, "ownerUserName"),
            Required(snapshot.InitiatorUserName, "initiatorUserName"),
            snapshot.Version,
            Required(structDef.Version, "structDef.version"),
            ParseTimestamp(snapshot.CreatedAtUtc, "createdAtUtc"),
            ParseTimestamp(snapshot.LastAccessedAtUtc, "lastAccessedAtUtc"),
            snapshot.ExpiresAtUtc is null ? null : ParseTimestamp(snapshot.ExpiresAtUtc, "expiresAtUtc"),
            ParseEnum<AxDataLifecycleStatus>(snapshot.Status, "status"),
            containers,
            snapshot.Workflow is null ? null : FromSnapshot(snapshot.Workflow),
            files,
            snapshot.AxOpen);
    }

    private static AxDataContainer FromSnapshot(string objectId, DataContainerV1 container)
    {
        var rows = (container.Rows ?? throw new AxDataSnapshotException("A data container is missing rows."))
            .Select((row, index) => new AxDataRow(
                LegacyRowId(
                    objectId,
                    Required(container.Name, "dataContainers.name"),
                    Required(row.RowId, "dataContainers.rows.rowId"),
                    index),
                ParseEnum<AxRowState>(row.State, "dataContainers.rows.state"),
                row.PersistedRecordId,
                (row.Fields ?? throw new AxDataSnapshotException("A row is missing fields."))
                    .ToDictionary(
                        field => Required(field.Name, "dataContainers.rows.fields.name"),
                        field => AxFieldValue.Rehydrate(
                            FromSnapshotValue(field.Value),
                            FromSnapshotValue(field.OriginalValue),
                            field.IsDirty,
                            field.IsReadOnly),
                        StringComparer.OrdinalIgnoreCase)))
            .ToList();

        return new AxDataContainer(Required(container.Name, "dataContainers.name"), container.IsGrid, rows);
    }

    private static AxDataContainer FromSnapshot(DataContainerV2 container)
    {
        var rows = (container.Rows ?? throw new AxDataSnapshotException("A data container is missing rows."))
            .Select(row => new AxDataRow(
                Required(row.RowId, "dataContainers.rows.rowId"),
                ParseEnum<AxRowState>(row.State, "dataContainers.rows.state"),
                row.RecordId,
                (row.Fields ?? throw new AxDataSnapshotException("A row is missing fields."))
                    .ToDictionary(
                        field => Required(field.Name, "dataContainers.rows.fields.name"),
                        field => AxFieldValue.Rehydrate(
                            FromSnapshotValue(field.Value),
                            FromSnapshotValue(field.OriginalValue),
                            field.IsDirty,
                            field.IsReadOnly),
                        StringComparer.OrdinalIgnoreCase)))
            .ToList();

        return new AxDataContainer(Required(container.DcId, "dataContainers.dcId"), container.IsGrid, rows);
    }

    private static string LegacyRowId(string objectId, string dcId, string? legacyRowId, int index)
    {
        var input = $"{objectId}\n{dcId}\n{legacyRowId}\n{index}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return $"row_{Convert.ToHexString(hash.AsSpan(0, 16)).ToLowerInvariant()}";
    }

    private static PegActiveState FromSnapshot(WorkflowV1 workflow)
    {
        return new PegActiveState
        {
            WorkflowInstanceId = Required(workflow.WorkflowInstanceId, "workflow.workflowInstanceId"),
            CurrentTaskId = workflow.CurrentTaskId,
            InitiatorUserName = Required(workflow.InitiatorUserName, "workflow.initiatorUserName"),
            Status = ParseEnum<PegWorkflowStatus>(workflow.Status, "workflow.status"),
            ApprovalLevel = workflow.ApprovalLevel,
            Approvers = workflow.Approvers ?? throw new AxDataSnapshotException("The workflow is missing approvers."),
            WorkflowVersion = workflow.WorkflowVersion,
            PendingFromUtc = ParseTimestamp(workflow.PendingFromUtc, "workflow.pendingFromUtc")
        };
    }

    private static SnapshotValueV1 ToSnapshotValue(object? value)
    {
        return value switch
        {
            null => new("null", null),
            string typed => new("string", typed),
            bool typed => new("boolean", typed ? "true" : "false"),
            byte typed => new("byte", typed.ToString(CultureInfo.InvariantCulture)),
            short typed => new("int16", typed.ToString(CultureInfo.InvariantCulture)),
            int typed => new("int32", typed.ToString(CultureInfo.InvariantCulture)),
            long typed => new("int64", typed.ToString(CultureInfo.InvariantCulture)),
            float typed => new("single", typed.ToString("R", CultureInfo.InvariantCulture)),
            double typed => new("double", typed.ToString("R", CultureInfo.InvariantCulture)),
            decimal typed => new("decimal", typed.ToString(CultureInfo.InvariantCulture)),
            DateTime typed => new("dateTimeUtc", typed.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)),
            DateTimeOffset typed => new("dateTimeOffsetUtc", FormatTimestamp(typed)),
            DateOnly typed => new("date", typed.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
            TimeOnly typed => new("time", typed.ToString("O", CultureInfo.InvariantCulture)),
            Guid typed => new("guid", typed.ToString("D")),
            byte[] typed => new("binary", Convert.ToBase64String(typed)),
            _ => throw new AxDataSnapshotException($"Field value type '{value.GetType().FullName}' is not supported in snapshots.")
        };
    }

    private static object? FromSnapshotValue(SnapshotValueV1? value)
    {
        if (value is null)
        {
            throw new AxDataSnapshotException("A field is missing its typed value.");
        }

        var text = value.Value;
        return value.Kind switch
        {
            "null" when text is null => null,
            "string" => text ?? throw MissingValue(value.Kind),
            "boolean" => bool.Parse(text ?? throw MissingValue(value.Kind)),
            "byte" => byte.Parse(text ?? throw MissingValue(value.Kind), CultureInfo.InvariantCulture),
            "int16" => short.Parse(text ?? throw MissingValue(value.Kind), CultureInfo.InvariantCulture),
            "int32" => int.Parse(text ?? throw MissingValue(value.Kind), CultureInfo.InvariantCulture),
            "int64" => long.Parse(text ?? throw MissingValue(value.Kind), CultureInfo.InvariantCulture),
            "single" => float.Parse(text ?? throw MissingValue(value.Kind), CultureInfo.InvariantCulture),
            "double" => double.Parse(text ?? throw MissingValue(value.Kind), CultureInfo.InvariantCulture),
            "decimal" => decimal.Parse(text ?? throw MissingValue(value.Kind), CultureInfo.InvariantCulture),
            "dateTimeUtc" => DateTime.Parse(text ?? throw MissingValue(value.Kind), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            "dateTimeOffsetUtc" => ParseTimestamp(text, "field.value"),
            "date" => DateOnly.ParseExact(text ?? throw MissingValue(value.Kind), "yyyy-MM-dd", CultureInfo.InvariantCulture),
            "time" => TimeOnly.Parse(text ?? throw MissingValue(value.Kind), CultureInfo.InvariantCulture),
            "guid" => Guid.ParseExact(text ?? throw MissingValue(value.Kind), "D"),
            "binary" => Convert.FromBase64String(text ?? throw MissingValue(value.Kind)),
            _ => throw new AxDataSnapshotException($"Snapshot field value kind '{value.Kind}' is not supported.")
        };
    }

    private void EnsureGridRowLimits(IEnumerable<AxDataContainer> containers)
    {
        var oversized = containers.FirstOrDefault(container => container.IsGrid && container.Rows.Count > _options.MaximumGridRows);
        if (oversized is not null)
        {
            throw new AxDataSnapshotException($"Grid '{oversized.Name}' exceeds the maximum row count of {_options.MaximumGridRows}.");
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

    private static DateTimeOffset ParseTimestamp(string? value, string propertyName)
    {
        var parsed = DateTimeOffset.ParseExact(
            Required(value, propertyName),
            "O",
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
        return parsed;
    }

    private static TEnum ParseEnum<TEnum>(string? value, string propertyName)
        where TEnum : struct, Enum
    {
        if (!Enum.TryParse<TEnum>(Required(value, propertyName), ignoreCase: false, out var parsed)
            || !Enum.IsDefined(parsed))
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

    private static AxDataSnapshotException MissingValue(string kind) =>
        new($"Snapshot field value kind '{kind}' requires a value.");
}
