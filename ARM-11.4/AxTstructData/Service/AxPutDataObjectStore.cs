using ARMCommon.Helpers;
using ARMCommon.Interface;
using AxData.Contracts.Snapshots;
using System.Globalization;
using StackExchange.Redis;

namespace AxTstructData;

public sealed class AxPutDataObjectStore(
    Utils common,
    IARMLogTraceService traceService) : IAxPutDataObjectStore
{
    private const string SnapshotStreamPrefix = "AXD:SNAPSHOTS";

    private const string ClaimScript = """
        local exists = redis.call('EXISTS', KEYS[1])
        if ARGV[1] == '1' then
            if exists == 1 then return 'busy' end
        elseif exists == 0 then
            return 'busy'
        end
        if ARGV[1] == '0' and redis.call('HGET', KEYS[1], 'payload') ~= ARGV[4] then
            return 'busy'
        end
        if redis.call('EXISTS', KEYS[2]) == 1 or
           (exists == 1 and redis.call('HEXISTS', KEYS[1], 'saveClaim') == 1) then
            return 'busy'
        end
        redis.call('HMSET', KEYS[1], 'payload', ARGV[2], 'saveClaim', ARGV[3])
        redis.call('PERSIST', KEYS[1])
        return 'claimed'
        """;

    private const string ReleaseScript = """
        if redis.call('HGET', KEYS[1], 'saveClaim') ~= ARGV[1] then
            return 'lost'
        end
        redis.call('HSET', KEYS[1], 'payload', ARGV[3])
        redis.call('HDEL', KEYS[1], 'saveClaim')
        redis.call('PERSIST', KEYS[1])
        if ARGV[2] == '1' then
            redis.call('XADD', KEYS[2], '*',
                'eventId', ARGV[4], 'tenantId', ARGV[5], 'objectId', ARGV[6],
                'persistedAtUtc', ARGV[7], 'correlationId', '', 'snapshot', ARGV[3])
        end
        return 'released'
        """;

    private const string FinalizeScript = """
        if redis.call('HGET', KEYS[1], 'saveClaim') ~= ARGV[1] then return 'lost' end
        redis.call('HSET', KEYS[1], 'payload', ARGV[2])
        redis.call('HDEL', KEYS[1], 'saveClaim')
        redis.call('PERSIST', KEYS[1])
        redis.call('XADD', KEYS[2], '*',
            'eventId', ARGV[3], 'tenantId', ARGV[4], 'objectId', ARGV[5],
            'persistedAtUtc', ARGV[6], 'correlationId', '', 'snapshot', ARGV[2])
        if ARGV[7] == '0' then
            redis.call('SET', KEYS[3], ARGV[2])
            redis.call('DEL', KEYS[1])
        end
        return 'finalized'
        """;

    public async Task<DataObjectSnapshotV3?> LoadAsync(string tenantId, string objectId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(objectId);

        if (!await common.OpenRedisConnectionAsync())
            throw new InvalidOperationException("Unable to open Redis for DataObject load.");
        string key = BuildKey(tenantId, objectId);
        string payload = await common._redisCache.HashGetAsync(key, "payload");
        if (string.IsNullOrWhiteSpace(payload))
            return null;

        if (traceService.IsTracingEnabled())
            traceService.LogMessage($"ObjectId DataObject payload for {objectId}", payload);

        DataObjectSnapshotV3 snapshot = DataObjectSnapshotReader.ReadCurrent(payload);
        if (!string.Equals(snapshot.TenantId, tenantId, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(snapshot.ObjectId, objectId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The DataObject payload identity does not match its tenant-scoped key.");
        }

        return snapshot;
    }

    public async Task<AxPutDataObjectStoreResult> ClaimAsync(
        DataObjectSnapshotV3 savingSnapshot,
        DataObjectSnapshotV3? expectedSnapshot,
        string claimToken,
        bool createOnly)
    {
        ArgumentNullException.ThrowIfNull(savingSnapshot);
        ArgumentException.ThrowIfNullOrWhiteSpace(claimToken);
        IDatabase database = await GetDatabaseAsync();
        RedisResult result = await database.ScriptEvaluateAsync(
            ClaimScript,
            [BuildKey(savingSnapshot.TenantId!, savingSnapshot.ObjectId!), BuildLockKey(savingSnapshot.TenantId!, savingSnapshot.ObjectId!)],
            [createOnly ? 1 : 0, Serialize(savingSnapshot), claimToken, expectedSnapshot is null ? string.Empty : Serialize(expectedSnapshot)]);
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
                originalPayload,
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
                context.ClaimToken,
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

    internal static string BuildLockKey(string tenantId, string objectId) =>
        $"AXD:LOCK:{{{tenantId.Split("~")[0].ToUpperInvariant()}}}:{objectId.ToUpperInvariant()}";

    internal static string BuildStreamKey(string tenantId) =>
        $"{SnapshotStreamPrefix}:{{{tenantId.Split("~")[0].ToUpperInvariant()}}}";

    private async Task<IDatabase> GetDatabaseAsync()
    {
        if (!await common.OpenRedisConnectionAsync())
            throw new InvalidOperationException("Unable to open Redis for DataObject lifecycle update.");
        return common._redisCache.Redis.GetDatabase();
    }

    private static string Serialize(DataObjectSnapshotV3 snapshot) =>
        DataObjectSnapshotWriter.Write(snapshot);

    private static AxPutDataObjectStoreResult ParseClaimResult(string result)
    {
        return result switch
        {
            "claimed" => new(AxPutDataObjectStoreStatus.Claimed),
            "busy" => new(AxPutDataObjectStoreStatus.ActiveConflict),
            _ => throw new InvalidOperationException("Redis returned an invalid DataObject claim result.")
        };
    }

    private static AxPutDataObjectStoreResult ParseResult(string result, AxPutDataObjectStoreStatus successStatus)
    {
        return result switch
        {
            "released" when successStatus == AxPutDataObjectStoreStatus.Released => new(successStatus),
            "finalized" when successStatus == AxPutDataObjectStoreStatus.Finalized => new(successStatus),
            "lost" => new(AxPutDataObjectStoreStatus.ClaimLost),
            _ => throw new InvalidOperationException("Redis returned an invalid DataObject lifecycle result.")
        };
    }
}
