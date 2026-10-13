namespace AxData.Infrastructure.Redis;

public sealed class AxDataRedisOptions
{
    public required TimeSpan InactivityTtl { get; init; }

    public required TimeSpan LockLeaseDuration { get; init; }

    public string SnapshotStreamPrefix { get; init; } = "AXD:SNAPSHOTS";

    public string SnapshotDeadLetterPrefix { get; init; } = "AXD:SNAPSHOTS:DLQ";
}
