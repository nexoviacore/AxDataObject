using System.Globalization;
using AxData.Application.Ports;
using AxData.Domain.Model;
using AxData.Infrastructure.Serialization;
using StackExchange.Redis;

namespace AxData.Infrastructure.Redis;

public sealed class RedisAxDataStore : IAxDataStore
{
    private const string CreateOnlyMarker = "create";

    private const string CompareAndSetScript = """
        local exists = redis.call('EXISTS', KEYS[1])
        local currentVersion = false
        if exists == 1 then
            currentVersion = redis.call('HGET', KEYS[1], 'version')
        end

        if ARGV[1] == 'create' then
            if exists == 1 then
                return 'stale|' .. (currentVersion or '')
            end
        elseif exists == 0 or currentVersion ~= ARGV[1] then
            return 'stale|' .. (currentVersion or '')
        end

        redis.call('HMSET', KEYS[1], 'version', ARGV[2], 'payload', ARGV[3])
        redis.call('PEXPIRE', KEYS[1], ARGV[4])
        return 'stored|' .. ARGV[2]
        """;

    private readonly IDatabase _database;
    private readonly AxDataSnapshotSerializer _serializer;
    private readonly long _inactivityTtlMilliseconds;

    public RedisAxDataStore(
        IConnectionMultiplexer connectionMultiplexer,
        AxDataSnapshotSerializer serializer,
        AxDataRedisOptions options)
    {
        ArgumentNullException.ThrowIfNull(connectionMultiplexer);
        ArgumentNullException.ThrowIfNull(serializer);
        ArgumentNullException.ThrowIfNull(options);

        if (options.InactivityTtl <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "The inactivity TTL must be positive.");
        }

        _database = connectionMultiplexer.GetDatabase();
        _serializer = serializer;
        _inactivityTtlMilliseconds = checked((long)Math.Ceiling(options.InactivityTtl.TotalMilliseconds));
    }

    public async Task<AxDataState?> LoadAsync(
        string tenantId,
        string objectId,
        CancellationToken cancellationToken)
    {
        var key = BuildKey(tenantId, objectId);
        cancellationToken.ThrowIfCancellationRequested();

        var payload = await _database.HashGetAsync(key, "payload")
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);
        if (payload.IsNull)
        {
            return null;
        }

        var state = _serializer.Deserialize((byte[])payload!);
        if (!string.Equals(state.TenantId, tenantId, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(state.ObjectId, objectId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The Redis snapshot identity does not match its tenant-scoped key.");
        }

        return state;
    }

    public async Task<AxDataStoreWriteResult> WriteAsync(
        AxDataState state,
        long? expectedVersion,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (expectedVersion < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(expectedVersion), "Expected version must be positive.");
        }

        var key = BuildKey(state.TenantId, state.ObjectId);
        var payload = _serializer.Serialize(state);
        var expected = expectedVersion?.ToString(CultureInfo.InvariantCulture) ?? CreateOnlyMarker;
        var version = state.Version.ToString(CultureInfo.InvariantCulture);

        cancellationToken.ThrowIfCancellationRequested();
        var result = await _database.ScriptEvaluateAsync(
                CompareAndSetScript,
                [key],
                [expected, version, payload, _inactivityTtlMilliseconds])
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);

        return ParseWriteResult((string)result!);
    }

    internal static RedisKey BuildKey(string tenantId, string objectId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(objectId);
        return $"AXD:{{{tenantId.ToUpperInvariant()}}}:{objectId.ToUpperInvariant()}";
    }

    private static AxDataStoreWriteResult ParseWriteResult(string result)
    {
        var separator = result.IndexOf('|');
        if (separator < 0)
        {
            throw new InvalidOperationException("Redis returned an invalid compare-and-set result.");
        }

        var status = result[..separator];
        var versionText = result[(separator + 1)..];
        var currentVersion = long.TryParse(
            versionText,
            NumberStyles.None,
            CultureInfo.InvariantCulture,
            out var parsedVersion)
            ? parsedVersion
            : (long?)null;

        return status switch
        {
            "stored" => new AxDataStoreWriteResult(AxDataStoreWriteStatus.Stored, currentVersion),
            "stale" => new AxDataStoreWriteResult(AxDataStoreWriteStatus.StaleVersion, currentVersion),
            _ => throw new InvalidOperationException("Redis returned an unknown compare-and-set status.")
        };
    }
}
