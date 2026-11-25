using AxData.Application.Ports;
using AxData.Domain.Model;
using AxData.Infrastructure.Serialization;
using StackExchange.Redis;

namespace AxData.Infrastructure.Redis;

public sealed class RedisAxDataStore : IAxDataStore
{
    private const string CreateOnlyMarker = "create";

    private const string OwnedWriteScript = """
        local exists = redis.call('EXISTS', KEYS[1])
        if ARGV[1] == 'create' then
            if exists == 1 then
                return 'conflict'
            end
        elseif exists == 0 or redis.call('GET', KEYS[2]) ~= ARGV[2] then
            return 'conflict'
        end

        redis.call('HSET', KEYS[1], 'payload', ARGV[3])
        redis.call('PERSIST', KEYS[1])
        return 'stored'
        """;

    private readonly IDatabase _database;
    private readonly AxDataSnapshotSerializer _serializer;

    public RedisAxDataStore(
        IConnectionMultiplexer connectionMultiplexer,
        AxDataSnapshotSerializer serializer)
    {
        ArgumentNullException.ThrowIfNull(connectionMultiplexer);
        ArgumentNullException.ThrowIfNull(serializer);
        _database = connectionMultiplexer.GetDatabase();
        _serializer = serializer;
    }

    /// <summary>Loads and verifies a snapshot stored under its tenant-scoped Redis key.</summary>
    /// <returns>The state, or <see langword="null"/> when no payload exists.</returns>
    public async Task<AxDataState?> LoadAsync(
        string tenantId,
        string objectId,
        CancellationToken cancellationToken)
    {
        var key = BuildKey(tenantId, objectId);
        cancellationToken.ThrowIfCancellationRequested();

        var payload = await _database.HashGetAsync(key, "payload")
            .WaitAsync(cancellationToken);
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

    /// <summary>Checks the durable closed marker for a tenant-scoped object.</summary>
    public async Task<bool> IsClosedAsync(
        string tenantId,
        string objectId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await _database.KeyExistsAsync(BuildClosedKey(tenantId, objectId))
            .WaitAsync(cancellationToken);
    }

    /// <summary>Atomically creates an object or replaces it after verifying the mutation lease token.</summary>
    /// <returns>A stored or conflict status; token mismatches never write state.</returns>
    public async Task<AxDataStoreWriteResult> WriteAsync(
        AxDataState state,
        string? lockToken,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(state);
        var key = BuildKey(state.TenantId, state.ObjectId);
        var payload = _serializer.Serialize(state);
        var mode = lockToken is null ? CreateOnlyMarker : "owned";

        cancellationToken.ThrowIfCancellationRequested();
        var result = await _database.ScriptEvaluateAsync(
                OwnedWriteScript,
                [key, RedisAxDataLock.BuildKey(state.TenantId, state.ObjectId)],
                [mode, lockToken ?? string.Empty, payload])
            .WaitAsync(cancellationToken);

        return (string)result! switch
        {
            "stored" => new AxDataStoreWriteResult(AxDataStoreWriteStatus.Stored),
            "conflict" => new AxDataStoreWriteResult(AxDataStoreWriteStatus.Conflict),
            _ => throw new InvalidOperationException("Redis returned an unknown owned-write result.")
        };
    }

    internal static RedisKey BuildKey(string tenantId, string objectId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(objectId);
        return $"AXD:{{{tenantId.ToUpperInvariant()}}}:{objectId.ToUpperInvariant()}";
    }

    internal static RedisKey BuildClosedKey(string tenantId, string objectId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(objectId);
        return $"AXD:CLOSED:{{{tenantId.ToUpperInvariant()}}}:{objectId.ToUpperInvariant()}";
    }

}
