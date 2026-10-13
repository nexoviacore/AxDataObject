using AxData.Domain.Model;

namespace AxData.Application.Ports;

public sealed record SnapshotEnvelope(
    string EventId,
    string TenantId,
    string ObjectId,
    long ObjectVersion,
    AxDataState Snapshot,
    DateTimeOffset PersistedAtUtc,
    string CorrelationId = "");

public sealed record SnapshotDelivery(
    string TenantId,
    string StreamEntryId,
    SnapshotEnvelope? Envelope,
    int DeliveryCount,
    string? InvalidReason = null);

public interface ISnapshotTransport
{
    Task<AxDataStoreWriteResult> PublishAsync(
        SnapshotEnvelope envelope,
        long expectedVersion,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<SnapshotDelivery>> ReadAsync(
        string tenantId,
        string consumerGroup,
        string consumerName,
        int maximumCount,
        TimeSpan pendingIdleTime,
        CancellationToken cancellationToken);

    Task AcknowledgeAsync(
        SnapshotDelivery delivery,
        long durableVersion,
        string consumerGroup,
        CancellationToken cancellationToken);

    Task DeadLetterAsync(
        SnapshotDelivery delivery,
        string consumerGroup,
        string reasonCode,
        CancellationToken cancellationToken);
}

public interface IAxDataSnapshotRepository
{
    Task<SnapshotPersistStatus> UpsertAsync(
        SnapshotEnvelope envelope,
        CancellationToken cancellationToken);

    Task<SnapshotReadResult> LoadLatestAsync(
        string tenantId,
        string objectId,
        CancellationToken cancellationToken);
}

public enum SnapshotPersistStatus
{
    Stored,
    Ignored,
    Unavailable
}

public enum SnapshotReadStatus
{
    Found,
    NotFound,
    Invalid,
    Unavailable
}

public sealed record SnapshotReadResult(SnapshotReadStatus Status, AxDataState? State = null)
{
    public static SnapshotReadResult Found(AxDataState state) => new(SnapshotReadStatus.Found, state);
}
