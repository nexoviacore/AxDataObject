namespace AxData.Contracts.Snapshots;

public sealed class DataObjectSnapshotV1
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
    public StructDefReferenceSnapshot? StructDef { get; init; }
    public string? CreatedAtUtc { get; init; }
    public string? LastAccessedAtUtc { get; init; }
    public string? ExpiresAtUtc { get; init; }
    public string? Status { get; init; }
    public List<DataContainerSnapshotV1>? DataContainers { get; init; }
    public WorkflowSnapshot? Workflow { get; init; }
    public List<FileReferenceSnapshot>? Files { get; init; }
    public bool AxOpen { get; init; }
}

public sealed class DataObjectSnapshotV2
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
    public StructDefReferenceSnapshot? StructDef { get; init; }
    public string? CreatedAtUtc { get; init; }
    public string? LastAccessedAtUtc { get; init; }
    public string? ExpiresAtUtc { get; init; }
    public string? Status { get; init; }
    public List<DataContainerSnapshotV2>? DataContainers { get; init; }
    public WorkflowSnapshot? Workflow { get; init; }
    public List<FileReferenceSnapshot>? Files { get; init; }
    public bool AxOpen { get; init; }
}

public sealed class DataObjectSnapshotV3
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
    public StructDefReferenceSnapshot? StructDef { get; init; }
    public string? CreatedAtUtc { get; init; }
    public string? LastAccessedAtUtc { get; init; }
    public string? ExpiresAtUtc { get; init; }
    public string? Status { get; init; }
    public List<DataContainerSnapshotV3>? DataContainers { get; init; }
    public WorkflowSnapshot? Workflow { get; init; }
    public List<FileReferenceSnapshot>? Files { get; init; }
    public bool AxOpen { get; init; }
}

public sealed record StructDefReferenceSnapshot(string TransId, string Version);

public sealed record DataContainerSnapshotV1(string Name, bool IsGrid, List<DataRowSnapshotV1> Rows);

public sealed record DataRowSnapshotV1(
    string RowId,
    long? PersistedRecordId,
    string State,
    List<FieldSnapshotV1> Fields);

public sealed record DataContainerSnapshotV2(string DcId, bool IsGrid, List<DataRowSnapshotV2> Rows);

public sealed record DataRowSnapshotV2(
    string RowId,
    long? RecordId,
    string State,
    List<FieldSnapshotV1> Fields);

public sealed record DataContainerSnapshotV3(string DcId, bool IsGrid, List<DataRowSnapshotV3> Rows);

public sealed record DataRowSnapshotV3(
    string RowId,
    long? RecordId,
    string State,
    List<FieldSnapshotV3> Fields);

public sealed record FieldSnapshotV1(
    string Name,
    SnapshotValue Value,
    SnapshotValue OriginalValue,
    bool IsDirty,
    bool IsReadOnly);

public sealed record FieldSnapshotV3(
    string Name,
    SnapshotValue Value,
    bool? IsReadOnly = null,
    long? SourceId = null,
    bool? IsDirty = null,
    SnapshotValue? OriginalValue = null,
    long? OriginalSourceId = null);

public sealed record SnapshotValue(string Kind, string? Value);

public sealed record FileReferenceSnapshot(string FileId, string FileName, string? ContentType);

public sealed record WorkflowSnapshot(
    string WorkflowInstanceId,
    string? CurrentTaskId,
    string InitiatorUserName,
    string Status,
    int ApprovalLevel,
    List<string> Approvers,
    long WorkflowVersion,
    string PendingFromUtc);
