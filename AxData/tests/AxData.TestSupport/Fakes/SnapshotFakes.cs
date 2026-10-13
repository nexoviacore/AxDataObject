using AxData.Application.Ports;
using AxData.Application.Ports.Axpert;
using AxData.Application.Recovery;
using AxData.Domain.Model;

namespace AxData.TestSupport.Fakes;

public sealed class FakeSnapshotRepository : IAxDataSnapshotRepository
{
    private readonly Dictionary<(string TenantId, string ObjectId), AxDataState> _states = new();

    public SnapshotPersistStatus PersistStatus { get; set; } = SnapshotPersistStatus.Stored;

    public Queue<SnapshotPersistStatus> PersistStatuses { get; } = new();

    public SnapshotReadStatus ReadStatus { get; set; } = SnapshotReadStatus.NotFound;

    public int UpsertCount { get; private set; }

    public void Seed(AxDataState state)
    {
        _states[(Normalize(state.TenantId), Normalize(state.ObjectId))] = state;
        ReadStatus = SnapshotReadStatus.Found;
    }

    public Task<SnapshotPersistStatus> UpsertAsync(
        SnapshotEnvelope envelope,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        UpsertCount++;
        var status = PersistStatuses.TryDequeue(out var scripted) ? scripted : PersistStatus;
        if (status == SnapshotPersistStatus.Stored)
        {
            Seed(envelope.Snapshot);
        }

        return Task.FromResult(status);
    }

    public Task<SnapshotReadResult> LoadLatestAsync(
        string tenantId,
        string objectId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (ReadStatus != SnapshotReadStatus.Found)
        {
            return Task.FromResult(new SnapshotReadResult(ReadStatus));
        }

        return Task.FromResult(_states.TryGetValue((Normalize(tenantId), Normalize(objectId)), out var state)
            ? SnapshotReadResult.Found(state)
            : new SnapshotReadResult(SnapshotReadStatus.NotFound));
    }

    private static string Normalize(string value) => value.ToUpperInvariant();
}

public sealed class InMemorySnapshotTransport(IAxDataStore store) : ISnapshotTransport
{
    public List<SnapshotEnvelope> Published { get; } = [];

    public Queue<SnapshotDelivery> Deliveries { get; } = new();

    public List<string> Acknowledged { get; } = [];

    public List<string> DeadLettered { get; } = [];

    public Task<AxDataStoreWriteResult> PublishAsync(
        SnapshotEnvelope envelope,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        Published.Add(envelope);
        return store.WriteAsync(envelope.Snapshot, expectedVersion, cancellationToken);
    }

    public Task<IReadOnlyList<SnapshotDelivery>> ReadAsync(
        string tenantId,
        string consumerGroup,
        string consumerName,
        int maximumCount,
        TimeSpan pendingIdleTime,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = new List<SnapshotDelivery>();
        while (result.Count < maximumCount && Deliveries.TryDequeue(out var delivery))
        {
            result.Add(delivery);
        }

        return Task.FromResult<IReadOnlyList<SnapshotDelivery>>(result);
    }

    public Task AcknowledgeAsync(
        SnapshotDelivery delivery,
        long durableVersion,
        string consumerGroup,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Acknowledged.Add(delivery.StreamEntryId);
        return Task.CompletedTask;
    }

    public Task DeadLetterAsync(
        SnapshotDelivery delivery,
        string consumerGroup,
        string reasonCode,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        DeadLettered.Add(delivery.StreamEntryId);
        return Task.CompletedTask;
    }
}

public static class FakeRecovery
{
    public static AxDataRecoveryService Create(
        IAxDataStore store,
        IAxpertAuthorizationProvider authorizationProvider,
        IStructDefProvider structDefProvider,
        IAxDataSnapshotRepository? repository = null) => new(
            store,
            repository ?? new FakeSnapshotRepository(),
            structDefProvider,
            authorizationProvider);
}
