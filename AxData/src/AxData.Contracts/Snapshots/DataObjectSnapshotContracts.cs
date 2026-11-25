using System.Globalization;

namespace AxData.Contracts.Snapshots;

// The historical V3 suffix is retained as an internal CLR compatibility name for AxPut.
// The Redis payload itself has no schema/version discriminator and only the current shape is readable.
public sealed class DataObjectSnapshotV3
{
    public string? ObjectId { get; init; }
    public string? TenantId { get; init; }
    public string? TransId { get; init; }
    public long? RecordId { get; init; }
    public string? InitiatorUserName { get; init; }
    public string? Status { get; init; }
    public List<DataContainerSnapshotV3>? DataContainers { get; init; }
    public WorkflowSnapshot? Workflow { get; init; }
}

public sealed record DataContainerSnapshotV3(
    string DcId,
    bool IsGrid,
    List<DataObjectFieldSnapshot> Fields,
    List<DataRowSnapshotV3> Rows);

public sealed record DataObjectFieldSnapshot(string Name, int Flags)
{
    public const int ReadOnlyFlag = 1;
    public const int SourceKeyFlag = 2;
    public const int KnownFlags = ReadOnlyFlag | SourceKeyFlag;

    public bool ReadOnly => (Flags & ReadOnlyFlag) != 0;
    public bool SourceKey => (Flags & SourceKeyFlag) != 0;
}

public sealed record DataRowSnapshotV3(
    string RowId,
    long? RecordId,
    string State,
    List<SnapshotScalar> Values,
    List<SnapshotScalar>? OldValues = null);

public enum SnapshotScalarKind
{
    Null,
    String,
    Boolean,
    Number
}

public sealed record SnapshotScalar(SnapshotScalarKind Kind, string? Text)
{
    public static SnapshotScalar Null { get; } = new(SnapshotScalarKind.Null, null);

    public static SnapshotScalar String(string value) =>
        new(SnapshotScalarKind.String, value ?? throw new ArgumentNullException(nameof(value)));

    public static SnapshotScalar Boolean(bool value) =>
        new(SnapshotScalarKind.Boolean, value ? bool.TrueString : bool.FalseString);

    public static SnapshotScalar Number(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        _ = decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _) ||
            double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed) && double.IsFinite(parsed)
            ? true
            : throw new ArgumentException("A snapshot number must be finite and invariant.", nameof(value));
        return new SnapshotScalar(SnapshotScalarKind.Number, value);
    }
}

public sealed record WorkflowSnapshot(
    string WorkflowInstanceId,
    string? CurrentTaskId,
    string InitiatorUserName,
    string Status,
    int ApprovalLevel,
    List<string> Approvers,
    long WorkflowVersion,
    string PendingFromUtc);
