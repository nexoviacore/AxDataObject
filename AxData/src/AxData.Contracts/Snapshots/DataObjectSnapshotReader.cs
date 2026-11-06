using System.Text.Json;

namespace AxData.Contracts.Snapshots;

public static class DataObjectSnapshotReader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static DataObjectSnapshotV3 ReadCurrent(string payload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);

        using var document = JsonDocument.Parse(payload);
        if (!document.RootElement.TryGetProperty("schemaVersion", out var versionElement) ||
            !versionElement.TryGetInt32(out var schemaVersion))
        {
            throw new JsonException("The DataObject payload is missing schemaVersion.");
        }

        return schemaVersion switch
        {
            1 => FromV1(Deserialize<DataObjectSnapshotV1>(payload)),
            2 => FromV2(Deserialize<DataObjectSnapshotV2>(payload)),
            3 => Deserialize<DataObjectSnapshotV3>(payload),
            _ => throw new JsonException($"DataObject schema version '{schemaVersion}' is not supported.")
        };
    }

    private static T Deserialize<T>(string payload) where T : class =>
        JsonSerializer.Deserialize<T>(payload, JsonOptions)
        ?? throw new JsonException("The DataObject payload is empty.");

    private static DataObjectSnapshotV3 FromV1(DataObjectSnapshotV1 snapshot) => new()
    {
        SchemaVersion = 3,
        ObjectId = snapshot.ObjectId,
        TenantId = snapshot.TenantId,
        TransId = snapshot.TransId,
        KeyField = snapshot.KeyField,
        KeyValue = snapshot.KeyValue,
        RecordId = snapshot.RecordId,
        OwnerUserName = snapshot.OwnerUserName,
        InitiatorUserName = snapshot.InitiatorUserName,
        Version = snapshot.Version,
        StructDef = snapshot.StructDef,
        CreatedAtUtc = snapshot.CreatedAtUtc,
        LastAccessedAtUtc = snapshot.LastAccessedAtUtc,
        ExpiresAtUtc = snapshot.ExpiresAtUtc,
        Status = snapshot.Status,
        DataContainers = snapshot.DataContainers?.Select(container => new DataContainerSnapshotV3(
            container.Name,
            container.IsGrid,
            container.Rows.Select(row => new DataRowSnapshotV3(
                row.RowId,
                row.PersistedRecordId,
                row.State,
                row.Fields.Select(FromV1Field).ToList())).ToList())).ToList(),
        Workflow = snapshot.Workflow,
        Files = snapshot.Files,
        AxOpen = snapshot.AxOpen
    };

    private static DataObjectSnapshotV3 FromV2(DataObjectSnapshotV2 snapshot) => new()
    {
        SchemaVersion = 3,
        ObjectId = snapshot.ObjectId,
        TenantId = snapshot.TenantId,
        TransId = snapshot.TransId,
        KeyField = snapshot.KeyField,
        KeyValue = snapshot.KeyValue,
        RecordId = snapshot.RecordId,
        OwnerUserName = snapshot.OwnerUserName,
        InitiatorUserName = snapshot.InitiatorUserName,
        Version = snapshot.Version,
        StructDef = snapshot.StructDef,
        CreatedAtUtc = snapshot.CreatedAtUtc,
        LastAccessedAtUtc = snapshot.LastAccessedAtUtc,
        ExpiresAtUtc = snapshot.ExpiresAtUtc,
        Status = snapshot.Status,
        DataContainers = snapshot.DataContainers?.Select(container => new DataContainerSnapshotV3(
            container.DcId,
            container.IsGrid,
            container.Rows.Select(row => new DataRowSnapshotV3(
                row.RowId,
                row.RecordId,
                row.State,
                row.Fields.Select(FromV1Field).ToList())).ToList())).ToList(),
        Workflow = snapshot.Workflow,
        Files = snapshot.Files,
        AxOpen = snapshot.AxOpen
    };

    private static FieldSnapshotV3 FromV1Field(FieldSnapshotV1 field) => new(
        field.Name,
        field.Value,
        field.IsReadOnly ? true : null,
        IsDirty: field.IsDirty ? true : null,
        OriginalValue: field.IsDirty ? field.OriginalValue : null);
}
