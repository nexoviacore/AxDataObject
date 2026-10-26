using System.Buffers;
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
    public const int CurrentSchemaVersion = 3;

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

        if (options.MaximumFieldInstances <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options.MaximumFieldInstances));
        }

        _options = options;
    }

    public byte[] Serialize(AxDataState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        EnsureStateLimits(state.DataContainers.Values);

        byte[] payload;
        try
        {
            payload = SerializeV3(state);
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
                2 => FromSnapshot(document.RootElement.Deserialize<AxDataSnapshotV2>(JsonOptions)
                    ?? throw new AxDataSnapshotException("The snapshot payload is empty.")),
                CurrentSchemaVersion => FromSnapshot(document.RootElement.Deserialize<AxDataSnapshotV3>(JsonOptions)
                    ?? throw new AxDataSnapshotException("The snapshot payload is empty.")),
                _ => throw new AxDataSnapshotException($"Snapshot schema version '{schemaVersion}' is not supported.")
            };
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

    private byte[] SerializeV3(AxDataState state)
    {
        var fieldCount = state.DataContainers.Values.Sum(container =>
            container.Rows.Sum(row => (long)row.Fields.Count));
        var estimatedBytes = Math.Min(fieldCount * 80L + 4_096L, _options.MaximumSerializedBytes);
        var buffer = new ArrayBufferWriter<byte>((int)estimatedBytes);
        using var writer = new Utf8JsonWriter(buffer);

        writer.WriteStartObject();
        writer.WriteNumber("schemaVersion", CurrentSchemaVersion);
        writer.WriteString("objectId", state.ObjectId);
        writer.WriteString("tenantId", state.TenantId);
        writer.WriteString("transId", state.TransId);
        WriteString(writer, "keyField", state.KeyField);
        WriteString(writer, "keyValue", state.KeyValue);
        WriteNumber(writer, "recordId", state.RecordId);
        writer.WriteString("ownerUserName", state.OwnerUserName);
        writer.WriteString("initiatorUserName", state.InitiatorUserName);
        writer.WriteNumber("version", state.Version);
        writer.WriteStartObject("structDef");
        writer.WriteString("transId", state.TransId);
        writer.WriteString("version", state.StructDefVersion);
        writer.WriteEndObject();
        writer.WriteString("createdAtUtc", FormatTimestamp(state.CreatedAtUtc));
        writer.WriteString("lastAccessedAtUtc", FormatTimestamp(state.LastAccessedAtUtc));
        if (state.ExpiresAtUtc is DateTimeOffset expiresAtUtc)
        {
            writer.WriteString("expiresAtUtc", FormatTimestamp(expiresAtUtc));
        }

        writer.WriteString("status", state.Status.ToString());
        writer.WriteStartArray("dataContainers");
        foreach (var container in state.DataContainers.Values
                     .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                     .ThenBy(item => item.Name, StringComparer.Ordinal))
        {
            writer.WriteStartObject();
            writer.WriteString("dcId", container.Name);
            writer.WriteBoolean("isGrid", container.IsGrid);
            writer.WriteStartArray("rows");
            foreach (var row in container.Rows)
            {
                writer.WriteStartObject();
                writer.WriteString("rowId", row.RowId);
                WriteNumber(writer, "recordId", row.PersistedRecordId);
                writer.WriteString("state", row.State.ToString());
                writer.WriteStartArray("fields");
                foreach (var field in row.Fields
                             .OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
                             .ThenBy(item => item.Key, StringComparer.Ordinal))
                {
                    writer.WriteStartObject();
                    writer.WriteString("name", field.Key);
                    WriteSnapshotValue(writer, "value", field.Value.Value);
                    if (field.Value.IsReadOnly)
                    {
                        writer.WriteBoolean("isReadOnly", true);
                    }

                    WriteNumber(writer, "sourceId", field.Value.SourceId);
                    if (field.Value.IsDirty)
                    {
                        writer.WriteBoolean("isDirty", true);
                        WriteSnapshotValue(writer, "originalValue", field.Value.OriginalValue);
                        WriteNumber(writer, "originalSourceId", field.Value.OriginalSourceId);
                    }

                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        if (state.Workflow is not null)
        {
            writer.WriteStartObject("workflow");
            writer.WriteString("workflowInstanceId", state.Workflow.WorkflowInstanceId);
            WriteString(writer, "currentTaskId", state.Workflow.CurrentTaskId);
            writer.WriteString("initiatorUserName", state.Workflow.InitiatorUserName);
            writer.WriteString("status", state.Workflow.Status.ToString());
            writer.WriteNumber("approvalLevel", state.Workflow.ApprovalLevel);
            writer.WriteStartArray("approvers");
            foreach (var approver in state.Workflow.Approvers)
            {
                writer.WriteStringValue(approver);
            }

            writer.WriteEndArray();
            writer.WriteNumber("workflowVersion", state.Workflow.WorkflowVersion);
            writer.WriteString("pendingFromUtc", FormatTimestamp(state.Workflow.PendingFromUtc));
            writer.WriteEndObject();
        }

        writer.WriteStartArray("files");
        foreach (var file in state.Files
                     .OrderBy(item => item.FileId, StringComparer.Ordinal)
                     .ThenBy(item => item.FileName, StringComparer.Ordinal))
        {
            writer.WriteStartObject();
            writer.WriteString("fileId", file.FileId);
            writer.WriteString("fileName", file.FileName);
            WriteString(writer, "contentType", file.ContentType);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteBoolean("axOpen", state.AxOpen);
        writer.WriteEndObject();
        writer.Flush();
        return buffer.WrittenSpan.ToArray();
    }

    private static void WriteSnapshotValue(Utf8JsonWriter writer, string propertyName, object? value)
    {
        writer.WriteStartObject(propertyName);
        switch (value)
        {
            case null:
                writer.WriteString("kind", "null");
                break;
            case string typed:
                WriteTypedValue("string", typed);
                break;
            case bool typed:
                WriteTypedValue("boolean", typed ? "true" : "false");
                break;
            case byte typed:
                WriteTypedValue("byte", typed.ToString(CultureInfo.InvariantCulture));
                break;
            case short typed:
                WriteTypedValue("int16", typed.ToString(CultureInfo.InvariantCulture));
                break;
            case int typed:
                WriteTypedValue("int32", typed.ToString(CultureInfo.InvariantCulture));
                break;
            case long typed:
                WriteTypedValue("int64", typed.ToString(CultureInfo.InvariantCulture));
                break;
            case float typed:
                WriteTypedValue("single", typed.ToString("R", CultureInfo.InvariantCulture));
                break;
            case double typed:
                WriteTypedValue("double", typed.ToString("R", CultureInfo.InvariantCulture));
                break;
            case decimal typed:
                WriteTypedValue("decimal", typed.ToString(CultureInfo.InvariantCulture));
                break;
            case DateTime typed:
                WriteTypedValue("dateTimeUtc", typed.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
                break;
            case DateTimeOffset typed:
                WriteTypedValue("dateTimeOffsetUtc", FormatTimestamp(typed));
                break;
            case DateOnly typed:
                WriteTypedValue("date", typed.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                break;
            case TimeOnly typed:
                WriteTypedValue("time", typed.ToString("O", CultureInfo.InvariantCulture));
                break;
            case Guid typed:
                WriteTypedValue("guid", typed.ToString("D"));
                break;
            case byte[] typed:
                WriteTypedValue("binary", Convert.ToBase64String(typed));
                break;
            default:
                throw new AxDataSnapshotException(
                    $"Field value type '{value.GetType().FullName}' is not supported in snapshots.");
        }

        writer.WriteEndObject();

        void WriteTypedValue(string kind, string text)
        {
            writer.WriteString("kind", kind);
            writer.WriteString("value", text);
        }
    }

    private static void WriteString(Utf8JsonWriter writer, string propertyName, string? value)
    {
        if (value is not null)
        {
            writer.WriteString(propertyName, value);
        }
    }

    private static void WriteNumber(Utf8JsonWriter writer, string propertyName, long? value)
    {
        if (value is long number)
        {
            writer.WriteNumber(propertyName, number);
        }
    }

    private static AxDataSnapshotV3 ToSnapshot(AxDataState state)
    {
        return new AxDataSnapshotV3
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

    private static DataContainerV3 ToSnapshot(AxDataContainer container)
    {
        var rows = container.Rows
            .Select(row => new DataRowV3(
                row.RowId,
                row.PersistedRecordId,
                row.State.ToString(),
                row.Fields
                    .OrderBy(field => field.Key, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(field => field.Key, StringComparer.Ordinal)
                    .Select(field => ToSnapshot(field.Key, field.Value))
                    .ToList()))
            .ToList();

        return new DataContainerV3(container.Name, container.IsGrid, rows);
    }

    private static FieldV3 ToSnapshot(string name, AxFieldValue field) =>
        field.IsDirty
            ? new FieldV3(
                name,
                ToSnapshotValue(field.Value),
                field.IsReadOnly ? true : null,
                field.SourceId,
                true,
                ToSnapshotValue(field.OriginalValue),
                field.OriginalSourceId)
            : new FieldV3(
                name,
                ToSnapshotValue(field.Value),
                field.IsReadOnly ? true : null,
                field.SourceId);

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
        if (snapshot.SchemaVersion != 2)
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

    private static AxDataState FromSnapshot(AxDataSnapshotV3 snapshot)
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

    private static AxDataContainer FromSnapshot(DataContainerV3 container)
    {
        var rows = (container.Rows ?? throw new AxDataSnapshotException("A data container is missing rows."))
            .Select(row => new AxDataRow(
                Required(row.RowId, "dataContainers.rows.rowId"),
                ParseEnum<AxRowState>(row.State, "dataContainers.rows.state"),
                row.RecordId,
                (row.Fields ?? throw new AxDataSnapshotException("A row is missing fields."))
                    .ToDictionary(
                        field => Required(field.Name, "dataContainers.rows.fields.name"),
                        FromSnapshot,
                        StringComparer.OrdinalIgnoreCase)))
            .ToList();

        return new AxDataContainer(Required(container.DcId, "dataContainers.dcId"), container.IsGrid, rows);
    }

    private static AxFieldValue FromSnapshot(FieldV3 field)
    {
        var value = FromSnapshotValue(field.Value);
        if (field.IsDirty is null or false)
        {
            if (field.OriginalValue is not null || field.OriginalSourceId is not null)
            {
                throw new AxDataSnapshotException("A clean v3 field cannot contain an original value or source ID.");
            }

            return new AxFieldValue(value, field.IsReadOnly is true, field.SourceId);
        }

        if (field.OriginalValue is null)
        {
            throw new AxDataSnapshotException("A dirty v3 field is missing its original value.");
        }

        return AxFieldValue.Rehydrate(
            value,
            FromSnapshotValue(field.OriginalValue),
            true,
            field.IsReadOnly is true,
            field.SourceId,
            field.OriginalSourceId);
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
