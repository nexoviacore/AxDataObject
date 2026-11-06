using ARMCommon.Helpers;
using ARMCommon.Interface;
using AxData.Contracts.Snapshots;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using StackExchange.Redis;

namespace AxTstructData;

public sealed class AxPutDataObjectStore(
    Utils common,
    IARMLogTraceService traceService) : IAxPutDataObjectStore
{
    private const string SnapshotStreamPrefix = "AXD:SNAPSHOTS";

    private const string ClaimScript = """
        local exists = redis.call('EXISTS', KEYS[1])
        local currentVersion = exists == 1 and redis.call('HGET', KEYS[1], 'version') or false
        if ARGV[1] == '1' then
            if exists == 1 then return 'stale|' .. (currentVersion or '') .. '|-1' end
        elseif exists == 0 or currentVersion ~= ARGV[2] then
            return 'stale|' .. (currentVersion or '') .. '|-1'
        end
        if exists == 1 and redis.call('HEXISTS', KEYS[1], 'saveClaim') == 1 then
            return 'busy|' .. (currentVersion or '') .. '|-1'
        end
        local ttl = exists == 1 and redis.call('PTTL', KEYS[1]) or -2
        redis.call('HMSET', KEYS[1], 'version', ARGV[2], 'payload', ARGV[3], 'saveClaim', ARGV[4])
        redis.call('PERSIST', KEYS[1])
        return 'claimed|' .. ARGV[2] .. '|' .. ttl
        """;

    private const string ReleaseScript = """
        if redis.call('HGET', KEYS[1], 'saveClaim') ~= ARGV[1] then
            local current = redis.call('HGET', KEYS[1], 'version')
            return 'lost|' .. (current or '')
        end
        redis.call('HMSET', KEYS[1], 'version', ARGV[3], 'payload', ARGV[4])
        redis.call('HDEL', KEYS[1], 'saveClaim')
        if ARGV[2] == '1' then
            redis.call('PERSIST', KEYS[1])
            redis.call('XADD', KEYS[2], '*',
                'eventId', ARGV[6], 'tenantId', ARGV[7], 'objectId', ARGV[8],
                'objectVersion', ARGV[3], 'persistedAtUtc', ARGV[9],
                'correlationId', '', 'snapshot', ARGV[4])
        else
            local ttl = tonumber(ARGV[5])
            if ttl and ttl > 0 then redis.call('PEXPIRE', KEYS[1], ttl) end
        end
        return 'released|' .. ARGV[3]
        """;

    private const string FinalizeScript = """
        local current = redis.call('HGET', KEYS[1], 'version')
        if not current or current ~= ARGV[1] then return 'stale|' .. (current or '') end
        if redis.call('HGET', KEYS[1], 'saveClaim') ~= ARGV[2] then return 'lost|' .. current end
        redis.call('HMSET', KEYS[1], 'version', ARGV[3], 'payload', ARGV[4])
        redis.call('HDEL', KEYS[1], 'saveClaim')
        redis.call('PERSIST', KEYS[1])
        redis.call('XADD', KEYS[2], '*',
            'eventId', ARGV[5], 'tenantId', ARGV[6], 'objectId', ARGV[7],
            'objectVersion', ARGV[3], 'persistedAtUtc', ARGV[8],
            'correlationId', '', 'snapshot', ARGV[4])
        if ARGV[9] == '0' then
            redis.call('SET', KEYS[3], ARGV[4])
            redis.call('DEL', KEYS[1])
        end
        return 'finalized|' .. ARGV[3]
        """;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<DataObjectSnapshotV3?> LoadAsync(string tenantId, string objectId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(objectId);

        if (!await common.OpenRedisConnectionAsync())
            throw new InvalidOperationException("Unable to open Redis for DataObject load.");
        string key = BuildKey(tenantId, objectId);
        RedisValue[] values = await common._redisCache.HashGetEntriesAsync(
            key,
            ["version", "payload"]);
        string versionText = values[0];
        string payload = values[1];
        if (string.IsNullOrWhiteSpace(payload))
            return null;

        if (traceService.IsTracingEnabled())
            traceService.LogMessage($"ObjectId DataObject payload for {objectId}", payload);

        DataObjectSnapshotV3 snapshot = DataObjectSnapshotReader.ReadCurrent(payload);
        if (!long.TryParse(versionText, NumberStyles.None, CultureInfo.InvariantCulture, out long storedVersion) ||
            storedVersion != snapshot.Version ||
            !string.Equals(snapshot.TenantId, tenantId, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(snapshot.ObjectId, objectId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The DataObject payload identity or version does not match its tenant-scoped key.");
        }

        return snapshot;
    }

    public async Task<AxPutDataObjectStoreResult> ClaimAsync(
        DataObjectSnapshotV3 savingSnapshot,
        long expectedVersion,
        string claimToken,
        bool createOnly)
    {
        ArgumentNullException.ThrowIfNull(savingSnapshot);
        ArgumentException.ThrowIfNullOrWhiteSpace(claimToken);
        if (expectedVersion < 1 || savingSnapshot.Version != expectedVersion)
            throw new ArgumentOutOfRangeException(nameof(expectedVersion));

        IDatabase database = await GetDatabaseAsync();
        RedisResult result = await database.ScriptEvaluateAsync(
            ClaimScript,
            [BuildKey(savingSnapshot.TenantId!, savingSnapshot.ObjectId!)],
            [createOnly ? 1 : 0, expectedVersion, Serialize(savingSnapshot), claimToken]);
        return ParseClaimResult((string)result!);
    }

    public async Task<AxPutDataObjectStoreResult> ReleaseAsync(AxPutDataObjectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        IDatabase database = await GetDatabaseAsync();
        DataObjectSnapshotV3 rollbackSnapshot = context.RollbackSnapshot
            ?? throw new InvalidOperationException("The DataObject rollback snapshot is unavailable.");
        string originalPayload = Serialize(rollbackSnapshot);
        RedisResult result = await database.ScriptEvaluateAsync(
            ReleaseScript,
            [BuildKey(context.TenantId, context.ObjectId), BuildStreamKey(context.TenantId)],
            [
                context.ClaimToken,
                context.Snapshot is null ? 1 : 0,
                context.Version,
                originalPayload,
                context.PreviousTtlMilliseconds,
                Guid.NewGuid().ToString("N"),
                context.TenantId,
                context.ObjectId,
                DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
            ]);
        return ParseResult((string)result!, AxPutDataObjectStoreStatus.Released);
    }

    public async Task<AxPutDataObjectStoreResult> FinalizeAsync(
        AxPutDataObjectContext context,
        DataObjectSnapshotV3 finalizedSnapshot,
        bool retainDataObject,
        DateTimeOffset persistedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(finalizedSnapshot);
        if (finalizedSnapshot.Version != context.FinalVersion)
            throw new ArgumentException("The finalized DataObject version is invalid.", nameof(finalizedSnapshot));

        IDatabase database = await GetDatabaseAsync();
        string payload = Serialize(finalizedSnapshot);
        RedisResult result = await database.ScriptEvaluateAsync(
            FinalizeScript,
            [
                BuildKey(context.TenantId, context.ObjectId),
                BuildStreamKey(context.TenantId),
                BuildClosedKey(context.TenantId, context.ObjectId)
            ],
            [
                context.Version,
                context.ClaimToken,
                finalizedSnapshot.Version,
                payload,
                Guid.NewGuid().ToString("N"),
                context.TenantId,
                context.ObjectId,
                persistedAtUtc.ToString("O", CultureInfo.InvariantCulture),
                retainDataObject ? 1 : 0
            ]);
        return ParseResult((string)result!, AxPutDataObjectStoreStatus.Finalized);
    }

    internal static string BuildKey(string tenantId, string objectId) =>
        $"AXD:{{{tenantId.Split("~")[0].ToUpperInvariant()}}}:{objectId.ToUpperInvariant()}";

    internal static string BuildClosedKey(string tenantId, string objectId) =>
        $"AXD:CLOSED:{{{tenantId.Split("~")[0].ToUpperInvariant()}}}:{objectId.ToUpperInvariant()}";

    internal static string BuildStreamKey(string tenantId) =>
        $"{SnapshotStreamPrefix}:{{{tenantId.Split("~")[0].ToUpperInvariant()}}}";

    private async Task<IDatabase> GetDatabaseAsync()
    {
        if (!await common.OpenRedisConnectionAsync())
            throw new InvalidOperationException("Unable to open Redis for DataObject lifecycle update.");
        return common._redisCache.Redis.GetDatabase();
    }

    private static string Serialize(DataObjectSnapshotV3 snapshot) =>
        JsonSerializer.Serialize(snapshot, JsonOptions);

    private static AxPutDataObjectStoreResult ParseClaimResult(string result)
    {
        string[] parts = result.Split('|', 3);
        long? version = parts.Length > 1 && long.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out long parsed)
            ? parsed
            : null;
        long ttl = parts.Length > 2 && long.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out long parsedTtl)
            ? parsedTtl
            : -1;
        return parts[0] switch
        {
            "claimed" => new(AxPutDataObjectStoreStatus.Claimed, version, ttl),
            "stale" => new(AxPutDataObjectStoreStatus.StaleVersion, version),
            "busy" => new(AxPutDataObjectStoreStatus.ActiveConflict, version),
            _ => throw new InvalidOperationException("Redis returned an invalid DataObject claim result.")
        };
    }

    private static AxPutDataObjectStoreResult ParseResult(string result, AxPutDataObjectStoreStatus successStatus)
    {
        string[] parts = result.Split('|', 2);
        long? version = parts.Length > 1 && long.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out long parsed)
            ? parsed
            : null;
        return parts[0] switch
        {
            "released" when successStatus == AxPutDataObjectStoreStatus.Released => new(successStatus, version),
            "finalized" when successStatus == AxPutDataObjectStoreStatus.Finalized => new(successStatus, version),
            "stale" => new(AxPutDataObjectStoreStatus.StaleVersion, version),
            "lost" => new(AxPutDataObjectStoreStatus.ClaimLost, version),
            _ => throw new InvalidOperationException("Redis returned an invalid DataObject lifecycle result.")
        };
    }
}
