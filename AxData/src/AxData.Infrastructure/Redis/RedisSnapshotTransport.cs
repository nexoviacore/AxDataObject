using System.Globalization;
using AxData.Application.Ports;
using AxData.Infrastructure.Serialization;
using StackExchange.Redis;

namespace AxData.Infrastructure.Redis;

public sealed class RedisSnapshotTransport : ISnapshotTransport
{
    private const string PublishScript = """
        local exists = redis.call('EXISTS', KEYS[1])
        local currentVersion = false
        if exists == 1 then
            currentVersion = redis.call('HGET', KEYS[1], 'version')
        end
        if exists == 0 or currentVersion ~= ARGV[1] then
            return 'stale|' .. (currentVersion or '')
        end
        redis.call('HMSET', KEYS[1], 'version', ARGV[2], 'payload', ARGV[3])
        redis.call('PERSIST', KEYS[1])
        redis.call('XADD', KEYS[2], '*',
            'eventId', ARGV[4],
            'tenantId', ARGV[5],
            'objectId', ARGV[6],
            'objectVersion', ARGV[2],
            'persistedAtUtc', ARGV[7],
            'correlationId', ARGV[8],
            'snapshot', ARGV[3])
        return 'stored|' .. ARGV[2]
        """;

    private const string MarkDurableScript = """
        local currentVersion = redis.call('HGET', KEYS[1], 'version')
        if currentVersion and tonumber(currentVersion) <= tonumber(ARGV[1]) then
            redis.call('PEXPIRE', KEYS[1], ARGV[2])
        end
        return 1
        """;

    private const string DeadLetterScript = """
        redis.call('XADD', KEYS[2], '*',
            'sourceEntryId', ARGV[1],
            'tenantId', ARGV[2],
            'eventId', ARGV[3],
            'objectId', ARGV[4],
            'objectVersion', ARGV[5],
            'reasonCode', ARGV[6])
        return redis.call('XACK', KEYS[1], ARGV[7], ARGV[1])
        """;

    private readonly IDatabase _database;
    private readonly AxDataSnapshotSerializer _serializer;
    private readonly AxDataRedisOptions _options;
    private readonly long _inactivityTtlMilliseconds;

    public RedisSnapshotTransport(
        IConnectionMultiplexer connectionMultiplexer,
        AxDataSnapshotSerializer serializer,
        AxDataRedisOptions options)
    {
        ArgumentNullException.ThrowIfNull(connectionMultiplexer);
        ArgumentNullException.ThrowIfNull(serializer);
        ArgumentNullException.ThrowIfNull(options);
        if (options.InactivityTtl <= TimeSpan.Zero ||
            string.IsNullOrWhiteSpace(options.SnapshotStreamPrefix) ||
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
        _inactivityTtlMilliseconds = checked((long)Math.Ceiling(options.InactivityTtl.TotalMilliseconds));
    }

    public async Task<AxDataStoreWriteResult> PublishAsync(
        SnapshotEnvelope envelope,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        ValidateEnvelope(envelope);
        if (expectedVersion < 1 || envelope.ObjectVersion != expectedVersion + 1)
        {
            throw new ArgumentOutOfRangeException(nameof(expectedVersion));
        }

        var payload = _serializer.Serialize(envelope.Snapshot);
        var result = await _database.ScriptEvaluateAsync(
                PublishScript,
                [RedisAxDataStore.BuildKey(envelope.TenantId, envelope.ObjectId), BuildStreamKey(envelope.TenantId)],
                [
                    expectedVersion,
                    envelope.ObjectVersion,
                    payload,
                    envelope.EventId,
                    envelope.TenantId,
                    envelope.ObjectId,
                    envelope.PersistedAtUtc.ToString("O", CultureInfo.InvariantCulture),
                    envelope.CorrelationId
                ])
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);

        return ParseWriteResult((string)result!);
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
        await EnsureGroupAsync(stream, consumerGroup, cancellationToken).ConfigureAwait(false);
        var entries = await _database.StreamReadGroupAsync(
                stream,
                consumerGroup,
                consumerName,
                ">",
                maximumCount)
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);

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
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);
            if (pending.Length > 0)
            {
                entries = await _database.StreamClaimAsync(
                        stream,
                        consumerGroup,
                        consumerName,
                        checked((long)Math.Ceiling(pendingIdleTime.TotalMilliseconds)),
                        pending.Select(message => message.MessageId).ToArray())
                    .WaitAsync(cancellationToken)
                    .ConfigureAwait(false);
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
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);
            var deliveryCount = pending.Length == 0 ? 1 : pending[0].DeliveryCount;
            deliveries.Add(ParseDelivery(tenantId, entry, deliveryCount));
        }

        return deliveries;
    }

    public async Task AcknowledgeAsync(
        SnapshotDelivery delivery,
        long durableVersion,
        string consumerGroup,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(delivery);
        if (delivery.Envelope is null || durableVersion < delivery.Envelope.ObjectVersion)
        {
            throw new ArgumentOutOfRangeException(nameof(durableVersion));
        }

        var stream = BuildStreamKey(delivery.TenantId);
        await _database.StreamAcknowledgeAsync(stream, consumerGroup, delivery.StreamEntryId)
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);
        await _database.ScriptEvaluateAsync(
                MarkDurableScript,
                [RedisAxDataStore.BuildKey(delivery.TenantId, delivery.Envelope.ObjectId)],
                [durableVersion, _inactivityTtlMilliseconds])
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);
        await TrimAcknowledgedAsync(stream, consumerGroup, cancellationToken).ConfigureAwait(false);
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
                    envelope?.ObjectVersion ?? 0,
                    reasonCode,
                    consumerGroup
                ])
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);
        await TrimAcknowledgedAsync(stream, consumerGroup, cancellationToken).ConfigureAwait(false);
    }

    private async Task EnsureGroupAsync(RedisKey stream, string consumerGroup, CancellationToken cancellationToken)
    {
        try
        {
            await _database.StreamCreateConsumerGroupAsync(stream, consumerGroup, "0-0", createStream: true)
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);
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
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);
        RedisValue minimumId;
        if (pending.PendingMessageCount > 0)
        {
            minimumId = pending.LowestPendingMessageId;
        }
        else
        {
            var groups = await _database.StreamGroupInfoAsync(stream)
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);
            minimumId = groups.Single(group => string.Equals(group.Name, consumerGroup, StringComparison.Ordinal)).LastDeliveredId;
        }

        if (!minimumId.IsNullOrEmpty)
        {
            await _database.StreamTrimByMinIdAsync(stream, minimumId)
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);
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
                long.Parse((string)values["objectVersion"]!, CultureInfo.InvariantCulture),
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
        if (envelope.ObjectVersion < 1 || envelope.ObjectVersion != envelope.Snapshot.Version ||
            !string.Equals(envelope.TenantId, envelope.Snapshot.TenantId, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(envelope.ObjectId, envelope.Snapshot.ObjectId, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Snapshot envelope metadata does not match its state.", nameof(envelope));
        }
    }

    private RedisKey BuildStreamKey(string tenantId) => $"{_options.SnapshotStreamPrefix}:{{{tenantId.ToUpperInvariant()}}}";

    private RedisKey BuildDeadLetterKey(string tenantId) => $"{_options.SnapshotDeadLetterPrefix}:{{{tenantId.ToUpperInvariant()}}}";

    private static AxDataStoreWriteResult ParseWriteResult(string result)
    {
        var parts = result.Split('|', 2);
        var currentVersion = parts.Length == 2 && long.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : (long?)null;
        return parts[0] switch
        {
            "stored" => new(AxDataStoreWriteStatus.Stored, currentVersion),
            "stale" => new(AxDataStoreWriteStatus.StaleVersion, currentVersion),
            _ => throw new InvalidOperationException("Redis returned an invalid snapshot publication result.")
        };
    }
}
