using AxData.Application.Ports;
using AxData.TestSupport.Fakes;
using AxData.TestSupport.Fixtures;
using AxData.Workers.Snapshots;

namespace AxData.IntegrationTests.Workers;

public sealed class SnapshotConsumerTests
{
    [Fact]
    public async Task Transient_failure_retries_with_backoff_then_acknowledges()
    {
        var repository = new FakeSnapshotRepository();
        repository.PersistStatuses.Enqueue(SnapshotPersistStatus.Unavailable);
        repository.PersistStatuses.Enqueue(SnapshotPersistStatus.Stored);
        var transport = CreateTransport(out var delivery);
        transport.Deliveries.Enqueue(delivery);

        var log = new RecordingOperationalEventSink();
        var result = await CreateConsumer(transport, repository, log).RunOnceAsync(CancellationToken.None);

        Assert.Equal(2, repository.UpsertCount);
        Assert.Equal(1, result.Acknowledged);
        Assert.Contains(delivery.StreamEntryId, transport.Acknowledged);
        Assert.Empty(transport.DeadLettered);
        Assert.Contains(log.Events, item => item.Name == "snapshot.retry.scheduled" && item.CorrelationId == "corr-worker");
        Assert.Contains(log.Events, item => item.Name == "snapshot.ack.completed" && item.CorrelationId == "corr-worker");
    }

    [Fact]
    public async Task Repeated_failure_dead_letters_instead_of_blocking()
    {
        var repository = new FakeSnapshotRepository { PersistStatus = SnapshotPersistStatus.Unavailable };
        var transport = CreateTransport(out var delivery);
        transport.Deliveries.Enqueue(delivery);

        var result = await CreateConsumer(transport, repository).RunOnceAsync(CancellationToken.None);

        Assert.Equal(3, repository.UpsertCount);
        Assert.Equal(1, result.DeadLettered);
        Assert.Contains(delivery.StreamEntryId, transport.DeadLettered);
    }

    [Fact]
    public async Task Poison_envelope_is_dead_lettered_without_sql_attempt()
    {
        var repository = new FakeSnapshotRepository();
        var transport = CreateTransport(out _);
        var poison = new SnapshotDelivery(SampleStructDefFixture.TenantId, "9-0", null, 1, "invalid-envelope");
        transport.Deliveries.Enqueue(poison);

        var result = await CreateConsumer(transport, repository).RunOnceAsync(CancellationToken.None);

        Assert.Equal(0, repository.UpsertCount);
        Assert.Equal(1, result.DeadLettered);
        Assert.Contains(poison.StreamEntryId, transport.DeadLettered);
    }

    private static InMemorySnapshotTransport CreateTransport(out SnapshotDelivery delivery)
    {
        var state = SampleStructDefFixture.CreateState();
        var envelope = new SnapshotEnvelope(
            "event-1",
            state.TenantId,
            state.ObjectId,
            state.Version,
            state,
            DateTimeOffset.UtcNow,
            "corr-worker");
        delivery = new SnapshotDelivery(state.TenantId, "1-0", envelope, 1);
        return new InMemorySnapshotTransport(new InMemoryAxDataStore());
    }

    private static SnapshotConsumer CreateConsumer(
        ISnapshotTransport transport,
        IAxDataSnapshotRepository repository,
        IOperationalEventSink? operationalLog = null) => new(
            transport,
            repository,
            new SnapshotConsumerOptions
            {
                TenantId = SampleStructDefFixture.TenantId,
                ConsumerName = "consumer-1",
                MaximumDeliveryAttempts = 3,
                InitialRetryDelay = TimeSpan.Zero,
                MaximumRetryDelay = TimeSpan.Zero,
                PendingIdleTime = TimeSpan.Zero
            },
            operationalLog);
}
