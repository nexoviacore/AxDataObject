using System.Buffers;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace AxData.Contracts.Snapshots;

public static class DataObjectSnapshotWriter
{
    public static byte[] WriteUtf8(DataObjectSnapshotV3 snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer);
        Write(writer, snapshot);
        writer.Flush();
        return buffer.WrittenSpan.ToArray();
    }

    public static string Write(DataObjectSnapshotV3 snapshot) =>
        Encoding.UTF8.GetString(WriteUtf8(snapshot));

    private static void Write(Utf8JsonWriter writer, DataObjectSnapshotV3 snapshot)
    {
        writer.WriteStartObject();
        WriteString(writer, "objectId", snapshot.ObjectId);
        WriteString(writer, "tenantId", snapshot.TenantId);
        WriteString(writer, "transId", snapshot.TransId);
        WriteNumber(writer, "recordId", snapshot.RecordId);
        WriteString(writer, "initiatorUserName", snapshot.InitiatorUserName);
        WriteString(writer, "status", snapshot.Status);
        writer.WriteStartArray("dataContainers");
        foreach (var container in snapshot.DataContainers ?? [])
        {
            ValidateFields(container.Fields);
            writer.WriteStartObject();
            writer.WriteString("dcId", container.DcId);
            writer.WriteBoolean("isGrid", container.IsGrid);
            writer.WriteStartArray("fields");
            foreach (var field in container.Fields)
            {
                writer.WriteStartArray();
                writer.WriteStringValue(field.Name);
                writer.WriteNumberValue(field.Flags);
                writer.WriteEndArray();
            }
            writer.WriteEndArray();
            writer.WriteStartArray("rows");
            foreach (var row in container.Rows)
            {
                ValidateRow(row, container.Fields);
                writer.WriteStartObject();
                writer.WriteString("rowId", row.RowId);
                WriteNumber(writer, "recordId", row.RecordId);
                writer.WriteString("state", row.State);
                writer.WriteStartArray("values");
                foreach (var value in row.Values)
                    WriteScalar(writer, value);
                writer.WriteEndArray();
                if (row.OldValues is { Count: > 0 })
                {
                    writer.WriteStartArray("oldValues");
                    foreach (var value in row.OldValues)
                        WriteScalar(writer, value);
                    writer.WriteEndArray();
                }
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
        if (snapshot.Workflow is not null)
            WriteWorkflow(writer, snapshot.Workflow);
        writer.WriteEndObject();
    }

    private static void WriteScalar(Utf8JsonWriter writer, SnapshotScalar scalar)
    {
        switch (scalar.Kind)
        {
            case SnapshotScalarKind.Null when scalar.Text is null:
                writer.WriteNullValue();
                break;
            case SnapshotScalarKind.String when scalar.Text is not null:
                writer.WriteStringValue(scalar.Text);
                break;
            case SnapshotScalarKind.Boolean when bool.TryParse(scalar.Text, out var boolean):
                writer.WriteBooleanValue(boolean);
                break;
            case SnapshotScalarKind.Number when scalar.Text is not null:
                writer.WriteRawValue(scalar.Text, skipInputValidation: false);
                break;
            default:
                throw new JsonException("The snapshot contains an invalid scalar value.");
        }
    }

    private static void WriteWorkflow(Utf8JsonWriter writer, WorkflowSnapshot workflow)
    {
        writer.WriteStartObject("workflow");
        writer.WriteString("workflowInstanceId", workflow.WorkflowInstanceId);
        WriteString(writer, "currentTaskId", workflow.CurrentTaskId);
        writer.WriteString("initiatorUserName", workflow.InitiatorUserName);
        writer.WriteString("status", workflow.Status);
        writer.WriteNumber("approvalLevel", workflow.ApprovalLevel);
        writer.WriteStartArray("approvers");
        foreach (var approver in workflow.Approvers)
            writer.WriteStringValue(approver);
        writer.WriteEndArray();
        writer.WriteNumber("workflowVersion", workflow.WorkflowVersion);
        writer.WriteString("pendingFromUtc", workflow.PendingFromUtc);
        writer.WriteEndObject();
    }

    private static void ValidateFields(IReadOnlyList<DataObjectFieldSnapshot> fields)
    {
        if (fields.Any(field => string.IsNullOrWhiteSpace(field.Name) ||
                                (field.Flags & ~DataObjectFieldSnapshot.KnownFlags) != 0) ||
            fields.Select(field => field.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != fields.Count)
        {
            throw new JsonException("A data container contains invalid or duplicate field metadata.");
        }
    }

    private static void ValidateRow(DataRowSnapshotV3 row, IReadOnlyList<DataObjectFieldSnapshot> fields)
    {
        int fieldCount = fields.Count;
        if (row.Values.Count != fieldCount)
            throw new JsonException("A row values array does not align with its fields.");
        if (row.OldValues is not null && row.OldValues.Count != fieldCount)
            throw new JsonException("A row oldValues array does not align with its fields.");
        if (row.OldValues is not null && string.Equals(row.State, "New", StringComparison.OrdinalIgnoreCase))
            throw new JsonException("A new row cannot contain oldValues.");
        for (int ordinal = 0; ordinal < fieldCount; ordinal++)
        {
            ValidateFieldValue(fields[ordinal], row.Values[ordinal]);
            if (row.OldValues is not null)
                ValidateFieldValue(fields[ordinal], row.OldValues[ordinal]);
        }
        if (row.OldValues is not null && row.OldValues.SequenceEqual(row.Values))
            throw new JsonException("A row oldValues array contains no changed field.");
    }

    private static void ValidateFieldValue(DataObjectFieldSnapshot field, SnapshotScalar value)
    {
        if (!field.SourceKey || value.Kind == SnapshotScalarKind.Null && value.Text is null)
            return;
        if (value.Kind != SnapshotScalarKind.String || value.Text is not string text)
            throw new JsonException("A non-null source-key field value must be a packed string.");
        int separator = text.LastIndexOf('~');
        if (separator < 0 ||
            !long.TryParse(text.AsSpan(separator + 1), NumberStyles.None, CultureInfo.InvariantCulture, out long sourceId) ||
            sourceId < 0)
        {
            throw new JsonException("A source-key field must end with a non-negative Int64 source ID.");
        }
    }

    private static void WriteString(Utf8JsonWriter writer, string name, string? value)
    {
        if (value is not null)
            writer.WriteString(name, value);
    }

    private static void WriteNumber(Utf8JsonWriter writer, string name, long? value)
    {
        if (value is not null)
            writer.WriteNumber(name, value.Value);
    }
}
