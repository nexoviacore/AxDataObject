namespace AxData.Infrastructure.Serialization;

internal sealed class AxDataSnapshotV3
{
    public int SchemaVersion { get; init; }
    public string? ObjectId { get; init; }
    public string? TenantId { get; init; }
    public string? TransId { get; init; }
    public string? KeyField { get; init; }
    public string? KeyValue { get; init; }
    public long? RecordId { get; init; }
    public string? OwnerUserName { get; init; }
    public string? InitiatorUserName { get; init; }
    public long Version { get; init; }
    public StructDefReferenceV1? StructDef { get; init; }
    public string? CreatedAtUtc { get; init; }
    public string? LastAccessedAtUtc { get; init; }
    public string? ExpiresAtUtc { get; init; }
    public string? Status { get; init; }
    public List<DataContainerV3>? DataContainers { get; init; }
    public WorkflowV1? Workflow { get; init; }
    public List<FileReferenceV1>? Files { get; init; }
    public bool AxOpen { get; init; }
}

internal sealed record DataContainerV3(string DcId, bool IsGrid, List<DataRowV3> Rows);

internal sealed record DataRowV3(string RowId, long? RecordId, string State, List<FieldV3> Fields);

internal sealed record FieldV3(
    string Name,
    SnapshotValueV1 Value,
    bool? IsReadOnly = null,
    long? SourceId = null,
    bool? IsDirty = null,
    SnapshotValueV1? OriginalValue = null,
    long? OriginalSourceId = null);
