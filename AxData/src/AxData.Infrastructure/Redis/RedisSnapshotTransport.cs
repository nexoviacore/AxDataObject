using System.Globalization;
using AxData.Application.Ports;
using AxData.Infrastructure.Serialization;
using StackExchange.Redis;

namespace AxData.Infrastructure.Redis;

public sealed class RedisSnapshotTransport : ISnapshotTransport
{
    private const string PublishScript = """
        redis.call('XADD', KEYS[1], '*',
            'eventId', ARGV[1],
            'tenantId', ARGV[2],
            'objectId', ARGV[3],
            'persistedAtUtc', ARGV[4],
            'correlationId', ARGV[5],
            'snapshot', ARGV[6])
        return 'stored'
        """;

    private const string DeadLetterScript = """
        redis.call('XADD', KEYS[2], '*',
            'sourceEntryId', ARGV[1],
            'tenantId', ARGV[2],
            'eventId', ARGV[3],
            'objectId', ARGV[4],
            'reasonCode', ARGV[5])
        return redis.call('XACK', KEYS[1], ARGV[6], ARGV[1])
        """;

    private readonly IDatabase _database;
    private readonly AxDataSnapshotSerializer _serializer;
    private readonly AxDataRedisOptions _options;

    public RedisSnapshotTransport(
        IConnectionMultiplexer connectionMultiplexer,
        AxDataSnapshotSerializer serializer,
        AxDataRedisOptions options)
    {
        ArgumentNullException.ThrowIfNull(connectionMultiplexer);
        ArgumentNullException.ThrowIfNull(serializer);
        ArgumentNullException.ThrowIfNull(options);
        if (string.IsNullOrWhiteSpace(options.SnapshotStreamPrefix) ||
            string.IsNullOrWhiteSpace(options.SnapshotDeadLetterPrefix))
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Snapshot Redis options are invalid.");
        }

        var unsupported = connectionMultiplexer.GetEndPoints()
            .Select(endpoint => connectionMultiplexer.GetServer(endpoint))
            .FirstOrDefault(server => server.Version < new Version(6, 2));
        if (unsupported is not null)
        {
            throw new NotSupportedException("Redis 6.2 or later is required for acknowledged-offset stream trimming.");
        }

        _database = connectionMultiplexer.GetDatabase();
        _serializer = serializer;
        _options = options;
    }

    public async Task<AxDataStoreWriteResult> PublishAsync(
        SnapshotEnvelope envelope,
        CancellationToken cancellationToken)
    {
        ValidateEnvelope(envelope);
        var payload = _serializer.Serialize(envelope.Snapshot);
        var result = await _database.ScriptEvaluateAsync(
                PublishScript,
                [BuildStreamKey(envelope.TenantId)],
                [
                    envelope.EventId,
                    envelope.TenantId,
                    envelope.ObjectId,
                    envelope.PersistedAtUtc.ToString("O", CultureInfo.InvariantCulture),
                    envelope.CorrelationId,
                    payload
                ])
            .WaitAsync(cancellationToken);

        return (string)result! == "stored"
            ? new AxDataStoreWriteResult(AxDataStoreWriteStatus.Stored)
            : throw new InvalidOperationException("Redis returned an invalid snapshot publication result.");
    }

    public async Task<IReadOnlyList<SnapshotDelivery>> ReadAsync(
        string tenantId,
        string consumerGroup,
        string consumerName,
        int maximumCount,
        TimeSpan pendingIdleTime,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(consumerGroup);
        ArgumentException.ThrowIfNullOrWhiteSpace(consumerName);
        if (maximumCount <= 0 || pendingIdleTime < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumCount));
        }

        var stream = BuildStreamKey(tenantId);
        await EnsureGroupAsync(stream, consumerGroup, cancellationToken);
        var entries = await _database.StreamReadGroupAsync(
                stream,
                consumerGroup,
                consumerName,
                ">",
                maximumCount)
            .WaitAsync(cancellationToken);

        if (entries.Length == 0)
        {
            var pending = await _database.StreamPendingMessagesAsync(
                    stream,
                    consumerGroup,
                    maximumCount,
                    RedisValue.Null,
                    minId: null,
                    maxId: null,
                    minIdleTimeInMs: checked((long)Math.Ceiling(pendingIdleTime.TotalMilliseconds)))
                .WaitAsync(cancellationToken);
            if (pending.Length > 0)
            {
                entries = await _database.StreamClaimAsync(
                        stream,
                        consumerGroup,
                        consumerName,
                        checked((long)Math.Ceiling(pendingIdleTime.TotalMilliseconds)),
                        pending.Select(message => message.MessageId).ToArray())
                    .WaitAsync(cancellationToken);
            }
        }

        var deliveries = new List<SnapshotDelivery>(entries.Length);
        foreach (var entry in entries)
        {
            var pending = await _database.StreamPendingMessagesAsync(
                    stream,
                    consumerGroup,
                    1,
                    consumerName,
                    entry.Id,
                    entry.Id)
                .WaitAsync(cancellationToken);
            var deliveryCount = pending.Length == 0 ? 1 : pending[0].DeliveryCount;
            deliveries.Add(ParseDelivery(tenantId, entry, deliveryCount));
        }

        return deliveries;
    }

    public async Task AcknowledgeAsync(
        SnapshotDelivery delivery,
        string consumerGroup,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(delivery);
        if (delivery.Envelope is null)
        {
            throw new ArgumentException("The delivery has no valid envelope.", nameof(delivery));
        }

        var stream = BuildStreamKey(delivery.TenantId);
        await _database.StreamAcknowledgeAsync(stream, consumerGroup, delivery.StreamEntryId)
            .WaitAsync(cancellationToken);
        await TrimAcknowledgedAsync(stream, consumerGroup, cancellationToken);
    }

    public async Task DeadLetterAsync(
        SnapshotDelivery delivery,
        string consumerGroup,
        string reasonCode,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(delivery);
        ArgumentException.ThrowIfNullOrWhiteSpace(reasonCode);
        var stream = BuildStreamKey(delivery.TenantId);
        var envelope = delivery.Envelope;
        await _database.ScriptEvaluateAsync(
                DeadLetterScript,
                [stream, BuildDeadLetterKey(delivery.TenantId)],
                [
                    delivery.StreamEntryId,
                    delivery.TenantId,
                    envelope?.EventId ?? string.Empty,
                    envelope?.ObjectId ?? string.Empty,
                    reasonCode,
                    consumerGroup
                ])
            .WaitAsync(cancellationToken);
        await TrimAcknowledgedAsync(stream, consumerGroup, cancellationToken);
    }

    private async Task EnsureGroupAsync(RedisKey stream, string consumerGroup, CancellationToken cancellationToken)
    {
        try
        {
            await _database.StreamCreateConsumerGroupAsync(stream, consumerGroup, "0-0", createStream: true)
                .WaitAsync(cancellationToken);
        }
        catch (RedisServerException exception) when (exception.Message.StartsWith("BUSYGROUP", StringComparison.Ordinal))
        {
        }
    }

    private async Task TrimAcknowledgedAsync(
        RedisKey stream,
        string consumerGroup,
        CancellationToken cancellationToken)
    {
        var pending = await _database.StreamPendingAsync(stream, consumerGroup)
            .WaitAsync(cancellationToken);
        RedisValue minimumId;
        if (pending.PendingMessageCount > 0)
        {
            minimumId = pending.LowestPendingMessageId;
        }
        else
        {
            var groups = await _database.StreamGroupInfoAsync(stream)
                .WaitAsync(cancellationToken);
            minimumId = groups.Single(group => string.Equals(group.Name, consumerGroup, StringComparison.Ordinal)).LastDeliveredId;
        }

        if (!minimumId.IsNullOrEmpty)
        {
            await _database.StreamTrimByMinIdAsync(stream, minimumId)
                .WaitAsync(cancellationToken);
        }
    }

    private SnapshotDelivery ParseDelivery(string tenantId, StreamEntry entry, int deliveryCount)
    {
        try
        {
            var values = entry.Values.ToDictionary(value => (string)value.Name!, value => value.Value, StringComparer.Ordinal);
            var payload = (byte[])values["snapshot"]!;
            var state = _serializer.Deserialize(payload);
            var envelope = new SnapshotEnvelope(
                (string)values["eventId"]!,
                (string)values["tenantId"]!,
                (string)values["objectId"]!,
                state,
                DateTimeOffset.Parse((string)values["persistedAtUtc"]!, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                values.TryGetValue("correlationId", out var correlationId) ? (string)correlationId! : string.Empty);
            ValidateEnvelope(envelope);
            if (!string.Equals(envelope.TenantId, tenantId, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Snapshot delivery tenant mismatch.");
            }

            return new SnapshotDelivery(tenantId, entry.Id!, envelope, deliveryCount);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return new SnapshotDelivery(tenantId, entry.Id!, null, deliveryCount, "invalid-envelope");
        }
    }

    private static void ValidateEnvelope(SnapshotEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentException.ThrowIfNullOrWhiteSpace(envelope.EventId);
        ArgumentException.ThrowIfNullOrWhiteSpace(envelope.TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(envelope.ObjectId);
        ArgumentNullException.ThrowIfNull(envelope.Snapshot);
        if (!string.Equals(envelope.TenantId, envelope.Snapshot.TenantId, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(envelope.ObjectId, envelope.Snapshot.ObjectId, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Snapshot envelope metadata does not match its state.", nameof(envelope));
        }
    }

    private RedisKey BuildStreamKey(string tenantId) => $"{_options.SnapshotStreamPrefix}:{{{tenantId.ToUpperInvariant()}}}";

    private RedisKey BuildDeadLetterKey(string tenantId) => $"{_options.SnapshotDeadLetterPrefix}:{{{tenantId.ToUpperInvariant()}}}";

}
