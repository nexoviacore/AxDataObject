namespace AxData.Infrastructure.Serialization;

internal sealed class AxDataSnapshotV1
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
    public List<DataContainerV1>? DataContainers { get; init; }
    public WorkflowV1? Workflow { get; init; }
    public List<FileReferenceV1>? Files { get; init; }
    public bool AxOpen { get; init; }
}

internal sealed record StructDefReferenceV1(string TransId, string Version);

internal sealed record DataContainerV1(string Name, bool IsGrid, List<DataRowV1> Rows);

internal sealed record DataRowV1(string RowId, long? PersistedRecordId, string State, List<FieldV1> Fields);

internal sealed record FieldV1(
    string Name,
    SnapshotValueV1 Value,
    SnapshotValueV1 OriginalValue,
    bool IsDirty,
    bool IsReadOnly);

internal sealed record SnapshotValueV1(string Kind, string? Value);

internal sealed record FileReferenceV1(string FileId, string FileName, string? ContentType);

internal sealed record WorkflowV1(
    string WorkflowInstanceId,
    string? CurrentTaskId,
    string InitiatorUserName,
    string Status,
    int ApprovalLevel,
    List<string> Approvers,
    long WorkflowVersion,
    string PendingFromUtc);
