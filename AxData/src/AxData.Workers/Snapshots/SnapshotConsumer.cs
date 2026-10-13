using System.Diagnostics;
using AxData.Application.Ports;

namespace AxData.Workers.Snapshots;

public sealed record SnapshotConsumerOptions
{
    public required string TenantId { get; init; }

    public string ConsumerGroup { get; init; } = "axdata-snapshot-persistence";

    public required string ConsumerName { get; init; }

    public int BatchSize { get; init; } = 25;

    public int MaximumDeliveryAttempts { get; init; } = 5;

    public TimeSpan PendingIdleTime { get; init; } = TimeSpan.FromSeconds(10);

    public TimeSpan InitialRetryDelay { get; init; } = TimeSpan.FromMilliseconds(250);

    public TimeSpan MaximumRetryDelay { get; init; } = TimeSpan.FromSeconds(30);
}

public sealed record SnapshotConsumerRunResult(int Read, int Acknowledged, int DeadLettered);

public sealed class SnapshotConsumer(
    ISnapshotTransport transport,
    IAxDataSnapshotRepository repository,
    SnapshotConsumerOptions options,
    IOperationalEventSink? operationalLog = null)
{
    public async Task<SnapshotConsumerRunResult> RunOnceAsync(CancellationToken cancellationToken)
    {
        ValidateOptions();
        var deliveries = await transport.ReadAsync(
            options.TenantId,
            options.ConsumerGroup,
            options.ConsumerName,
            options.BatchSize,
            options.PendingIdleTime,
            cancellationToken).ConfigureAwait(false);
        var acknowledged = 0;
        var deadLettered = 0;

        foreach (var delivery in deliveries)
        {
            if (delivery.Envelope is null)
            {
                await transport.DeadLetterAsync(
                    delivery,
                    options.ConsumerGroup,
                    delivery.InvalidReason ?? "invalid-envelope",
                    cancellationToken).ConfigureAwait(false);
                deadLettered++;
                Log.Write(new OperationalEvent(
                    "snapshot.deadletter.completed", "worker", "invalid-envelope",
                    OperationalLogLevel.Warning, TenantId: delivery.TenantId,
                    ReasonCode: delivery.InvalidReason ?? "invalid-envelope", Attempt: delivery.DeliveryCount));
                continue;
            }

            var attempt = Math.Max(1, delivery.DeliveryCount);
            while (true)
            {
                var persistStarted = Stopwatch.GetTimestamp();
                var status = await repository.UpsertAsync(delivery.Envelope, cancellationToken).ConfigureAwait(false);
                Log.Write(new OperationalEvent(
                    "snapshot.persistence.completed", "worker", status.ToString().ToLowerInvariant(),
                    status == SnapshotPersistStatus.Unavailable ? OperationalLogLevel.Warning : OperationalLogLevel.Information,
                    CorrelationId: delivery.Envelope.CorrelationId, TenantId: delivery.Envelope.TenantId,
                    TransId: delivery.Envelope.Snapshot.TransId, ObjectId: delivery.Envelope.ObjectId,
                    Version: delivery.Envelope.ObjectVersion, Attempt: attempt,
                    DurationMs: Stopwatch.GetElapsedTime(persistStarted).TotalMilliseconds));
                if (status is SnapshotPersistStatus.Stored or SnapshotPersistStatus.Ignored)
                {
                    await transport.AcknowledgeAsync(
                        delivery,
                        delivery.Envelope.ObjectVersion,
                        options.ConsumerGroup,
                        cancellationToken).ConfigureAwait(false);
                    acknowledged++;
                    Log.Write(new OperationalEvent(
                        "snapshot.ack.completed", "worker", "acknowledged",
                        CorrelationId: delivery.Envelope.CorrelationId, TenantId: delivery.Envelope.TenantId,
                        TransId: delivery.Envelope.Snapshot.TransId, ObjectId: delivery.Envelope.ObjectId,
                        Version: delivery.Envelope.ObjectVersion, Attempt: attempt));
                    break;
                }

                if (attempt >= options.MaximumDeliveryAttempts)
                {
                    await transport.DeadLetterAsync(
                        delivery,
                        options.ConsumerGroup,
                        "snapshot-persistence-failed",
                        cancellationToken).ConfigureAwait(false);
                    deadLettered++;
                    Log.Write(new OperationalEvent(
                        "snapshot.deadletter.completed", "worker", "persistence-failed",
                        OperationalLogLevel.Error, CorrelationId: delivery.Envelope.CorrelationId,
                        TenantId: delivery.Envelope.TenantId, TransId: delivery.Envelope.Snapshot.TransId,
                        ObjectId: delivery.Envelope.ObjectId, Version: delivery.Envelope.ObjectVersion,
                        ReasonCode: "snapshot-persistence-failed", Attempt: attempt));
                    break;
                }

                Log.Write(new OperationalEvent(
                    "snapshot.retry.scheduled", "worker", "retrying",
                    OperationalLogLevel.Warning, OperationalLogDetail.Diagnostic,
                    delivery.Envelope.CorrelationId, delivery.Envelope.TenantId,
                    delivery.Envelope.Snapshot.TransId, delivery.Envelope.ObjectId,
                    delivery.Envelope.ObjectVersion, Attempt: attempt));
                await Task.Delay(GetRetryDelay(attempt), cancellationToken).ConfigureAwait(false);
                attempt++;
            }
        }

        return new SnapshotConsumerRunResult(deliveries.Count, acknowledged, deadLettered);
    }

    private IOperationalEventSink Log => operationalLog ?? NullOperationalEventSink.Instance;

    private TimeSpan GetRetryDelay(int attempt)
    {
        var multiplier = Math.Pow(2, Math.Max(0, attempt - 1));
        var milliseconds = Math.Min(
            options.InitialRetryDelay.TotalMilliseconds * multiplier,
            options.MaximumRetryDelay.TotalMilliseconds);
        return TimeSpan.FromMilliseconds(milliseconds);
    }

    private void ValidateOptions()
    {
        if (string.IsNullOrWhiteSpace(options.TenantId) ||
            string.IsNullOrWhiteSpace(options.ConsumerGroup) ||
            string.IsNullOrWhiteSpace(options.ConsumerName) ||
            options.BatchSize <= 0 ||
            options.MaximumDeliveryAttempts <= 0 ||
            options.PendingIdleTime < TimeSpan.Zero ||
            options.InitialRetryDelay < TimeSpan.Zero ||
            options.MaximumRetryDelay < options.InitialRetryDelay)
        {
            throw new InvalidOperationException("Snapshot consumer options are invalid.");
        }
    }
}
