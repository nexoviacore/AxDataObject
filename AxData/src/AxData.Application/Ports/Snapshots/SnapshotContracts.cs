using AxData.Domain.Model;

namespace AxData.Application.Ports;

public sealed record SnapshotEnvelope(
    string EventId,
    string TenantId,
    string ObjectId,
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
    /// <summary>Publishes an immutable snapshot envelope to its tenant stream.</summary>
    Task<AxDataStoreWriteResult> PublishAsync(
        SnapshotEnvelope envelope,
        CancellationToken cancellationToken);

    /// <summary>Reads new or reclaimable pending deliveries for one tenant consumer group.</summary>
    Task<IReadOnlyList<SnapshotDelivery>> ReadAsync(
        string tenantId,
        string consumerGroup,
        string consumerName,
        int maximumCount,
        TimeSpan pendingIdleTime,
        CancellationToken cancellationToken);

    /// <summary>Acknowledges a successfully handled delivery.</summary>
    Task AcknowledgeAsync(
        SnapshotDelivery delivery,
        string consumerGroup,
        CancellationToken cancellationToken);

    /// <summary>Moves an invalid or terminally failed delivery to the dead-letter stream.</summary>
    Task DeadLetterAsync(
        SnapshotDelivery delivery,
        string consumerGroup,
        string reasonCode,
        CancellationToken cancellationToken);
}

public interface IAxDataSnapshotRepository
{
    /// <summary>Idempotently persists a snapshot when it is newer than the stored version.</summary>
    Task<SnapshotPersistStatus> UpsertAsync(
        SnapshotEnvelope envelope,
        CancellationToken cancellationToken);

    /// <summary>Loads the latest durable snapshot for a tenant-scoped object.</summary>
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
