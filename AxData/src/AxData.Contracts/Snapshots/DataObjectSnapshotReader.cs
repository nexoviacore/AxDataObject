using System.Text.Json;

namespace AxData.Contracts.Snapshots;

public static class DataObjectSnapshotReader
{
    public static DataObjectSnapshotV3 ReadCurrent(string payload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);
        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
            throw new JsonException("The DataObject payload must be an object.");
        if (root.TryGetProperty("schemaVersion", out _))
            throw new JsonException("Versioned DataObject payloads are not supported.");
        if (root.TryGetProperty("createdAtUtc", out _) ||
            root.TryGetProperty("lastAccessedAtUtc", out _) ||
            root.TryGetProperty("expiresAtUtc", out _))
            throw new JsonException("Timestamped DataObject payloads are not supported.");
        if (root.TryGetProperty("keyField", out _) || root.TryGetProperty("keyValue", out _))
            throw new JsonException("Business-key locators are not supported in DataObject payloads.");

        return new DataObjectSnapshotV3
        {
            ObjectId = OptionalString(root, "objectId"),
            TenantId = OptionalString(root, "tenantId"),
            TransId = OptionalString(root, "transId"),
            RecordId = OptionalInt64(root, "recordId"),
            InitiatorUserName = OptionalString(root, "initiatorUserName"),
            Status = OptionalString(root, "status"),
            DataContainers = ReadContainers(root),
            Workflow = ReadWorkflow(root)
        };
    }

    private static List<DataContainerSnapshotV3> ReadContainers(JsonElement root)
    {
        if (!root.TryGetProperty("dataContainers", out var containers) || containers.ValueKind != JsonValueKind.Array)
            throw new JsonException("The DataObject payload is missing dataContainers.");

        var result = new List<DataContainerSnapshotV3>();
        var dcIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var container in containers.EnumerateArray())
        {
            var dcId = RequiredString(container, "dcId");
            if (!dcIds.Add(dcId))
                throw new JsonException($"Data container '{dcId}' occurs more than once.");
            if (!container.TryGetProperty("isGrid", out var isGrid) ||
                isGrid.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                throw new JsonException($"Data container '{dcId}' is missing isGrid.");

            var fields = ReadFields(container, dcId);
            var rows = ReadRows(container, dcId, fields.Count);
            result.Add(new DataContainerSnapshotV3(dcId, isGrid.GetBoolean(), fields, rows));
        }
        return result;
    }

    private static List<DataObjectFieldSnapshot> ReadFields(JsonElement container, string dcId)
    {
        if (!container.TryGetProperty("fields", out var fieldsElement) || fieldsElement.ValueKind != JsonValueKind.Array)
            throw new JsonException($"Data container '{dcId}' is missing fields.");
        var fields = fieldsElement.EnumerateArray().Select(field =>
        {
            if (field.ValueKind != JsonValueKind.Array ||
                field.GetArrayLength() != 2 ||
                field[0].ValueKind != JsonValueKind.String ||
                string.IsNullOrWhiteSpace(field[0].GetString()) ||
                field[1].ValueKind != JsonValueKind.Number ||
                !field[1].TryGetInt32(out int flags) ||
                (flags & ~DataObjectFieldSnapshot.KnownFlags) != 0)
            {
                throw new JsonException($"Data container '{dcId}' contains invalid field metadata.");
            }

            return new DataObjectFieldSnapshot(field[0].GetString()!, flags);
        }).ToList();
        if (fields.Select(field => field.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != fields.Count)
            throw new JsonException($"Data container '{dcId}' contains duplicate field names.");
        return fields;
    }

    private static List<DataRowSnapshotV3> ReadRows(JsonElement container, string dcId, int fieldCount)
    {
        if (!container.TryGetProperty("rows", out var rowsElement) || rowsElement.ValueKind != JsonValueKind.Array)
            throw new JsonException($"Data container '{dcId}' is missing rows.");
        var result = new List<DataRowSnapshotV3>();
        var rowIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rowsElement.EnumerateArray())
        {
            var rowId = RequiredString(row, "rowId");
            if (!rowIds.Add(rowId))
                throw new JsonException($"Row '{rowId}' occurs more than once in '{dcId}'.");
            if (!row.TryGetProperty("values", out var valuesElement) || valuesElement.ValueKind != JsonValueKind.Array ||
                valuesElement.GetArrayLength() != fieldCount)
                throw new JsonException($"Row '{rowId}' values do not align with '{dcId}' fields.");

            var values = valuesElement.EnumerateArray().Select(ReadScalar).ToList();
            if (row.TryGetProperty("sourceIds", out _) ||
                row.TryGetProperty("originals", out _) ||
                row.TryGetProperty("originalSourceIds", out _))
            {
                throw new JsonException("Legacy sparse source/original properties are not supported.");
            }

            List<SnapshotScalar>? oldValues = null;
            if (row.TryGetProperty("oldValues", out var oldValuesElement))
            {
                if (oldValuesElement.ValueKind != JsonValueKind.Array ||
                    oldValuesElement.GetArrayLength() != fieldCount)
                {
                    throw new JsonException($"Row '{rowId}' oldValues do not align with '{dcId}' fields.");
                }

                oldValues = oldValuesElement.EnumerateArray().Select(ReadScalar).ToList();
            }

            result.Add(new DataRowSnapshotV3(rowId, OptionalInt64(row, "recordId"), RequiredString(row, "state"),
                values, oldValues));
        }
        return result;
    }

    private static SnapshotScalar ReadScalar(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Null => SnapshotScalar.Null,
        JsonValueKind.String => SnapshotScalar.String(value.GetString()!),
        JsonValueKind.True => SnapshotScalar.Boolean(true),
        JsonValueKind.False => SnapshotScalar.Boolean(false),
        JsonValueKind.Number => SnapshotScalar.Number(value.GetRawText()),
        _ => throw new JsonException("A DataObject field value must be a JSON scalar.")
    };

    private static WorkflowSnapshot? ReadWorkflow(JsonElement root)
    {
        if (!root.TryGetProperty("workflow", out var workflow))
            return null;
        if (workflow.ValueKind != JsonValueKind.Object)
            throw new JsonException("The workflow payload must be an object.");
        if (!workflow.TryGetProperty("approvalLevel", out var level) || !level.TryGetInt32(out var approvalLevel) ||
            !workflow.TryGetProperty("workflowVersion", out var version) || !version.TryGetInt64(out var workflowVersion) ||
            !workflow.TryGetProperty("approvers", out var approversElement) || approversElement.ValueKind != JsonValueKind.Array)
            throw new JsonException("The workflow payload is incomplete.");
        var approvers = approversElement.EnumerateArray().Select(item =>
            item.ValueKind == JsonValueKind.String ? item.GetString()! : throw new JsonException("A workflow approver must be a string.")).ToList();
        return new WorkflowSnapshot(RequiredString(workflow, "workflowInstanceId"), OptionalString(workflow, "currentTaskId"),
            RequiredString(workflow, "initiatorUserName"), RequiredString(workflow, "status"), approvalLevel, approvers,
            workflowVersion, RequiredString(workflow, "pendingFromUtc"));
    }

    private static string RequiredString(JsonElement element, string name) =>
        OptionalString(element, name) is { Length: > 0 } value
            ? value
            : throw new JsonException($"DataObject property '{name}' is required.");

    private static string? OptionalString(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var property))
            return null;
        if (property.ValueKind != JsonValueKind.String)
            throw new JsonException($"DataObject property '{name}' must be a string.");
        return property.GetString();
    }

    private static long? OptionalInt64(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var property))
            return null;
        return property.TryGetInt64(out var value)
            ? value
            : throw new JsonException($"DataObject property '{name}' must be an Int64 value.");
    }
}
