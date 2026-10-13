using StackExchange.Redis;
using System.Collections.Concurrent;

public class RedisJobStore
{
    private static readonly ConcurrentDictionary<string, IConnectionMultiplexer> _connections = new ConcurrentDictionary<string, IConnectionMultiplexer>();

    private IConnectionMultiplexer GetConnection(string accessCode, string connectionString)
    {
        return _connections.GetOrAdd(accessCode, _ =>
        {
            Console.WriteLine($"Opening Redis connection for {accessCode}");
            return ConnectionMultiplexer.Connect(connectionString);
        });
    }

    private IDatabase GetDatabase(string accessCode, string connectionString)
    {
        var mux = GetConnection(accessCode, connectionString);
        return mux.GetDatabase();
    }

    private string GetJobKey(string accessCode, string jobId)
        => $"{accessCode}-Periodic-job:{jobId}";

    private string GetScheduleKey(string accessCode)
        => $"{accessCode}-Periodic_jobs";
    public async Task SaveJob(string accessCode, string connectionString, string jobId, string payload)
    {
        var db = GetDatabase(accessCode, connectionString);
        await db.StringSetAsync(GetJobKey(accessCode, jobId), payload);
    }

    public async Task<string?> GetJob(string accessCode, string connectionString, string jobId)
    {
        var db = GetDatabase(accessCode, connectionString);
        return await db.StringGetAsync(GetJobKey(accessCode, jobId));
    }
    public async Task<bool> JobExists(string accessCode, string connectionString, string jobId)
    {
        var db = GetDatabase(accessCode, connectionString);
        return await db.KeyExistsAsync(GetJobKey(accessCode, jobId));
    }
    public async Task Schedule(string accessCode, string connectionString, string jobId, DateTime runAtUtc)
    {
        var db = GetDatabase(accessCode, connectionString);

        long ts = new DateTimeOffset(runAtUtc).ToUnixTimeSeconds();

        await db.SortedSetAddAsync(
            GetScheduleKey(accessCode),
            jobId,
            ts);
    }
    public async Task<string?> PopDueJob(string accessCode, string connectionString)
    {
        var db = GetDatabase(accessCode, connectionString);
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        string script = @"
local key = KEYS[1]
local now = tonumber(ARGV[1])

local result = redis.call('ZRANGE', key, 0, 0, 'WITHSCORES')

if (#result == 0) then
    return nil
end

local jobId = result[1]
local score = tonumber(result[2])

if (score > now) then
    return nil
end

redis.call('ZREM', key, jobId)
return jobId
";

        var result = await db.ScriptEvaluateAsync(
            script,
            new RedisKey[] { GetScheduleKey(accessCode) },
            new RedisValue[] { now });

        return result.IsNull ? null : result.ToString();
    }
    public async Task Remove(string accessCode, string connectionString, string jobId)
    {
        var db = GetDatabase(accessCode, connectionString);

        await db.SortedSetRemoveAsync(GetScheduleKey(accessCode), jobId);
        await db.KeyDeleteAsync(GetJobKey(accessCode, jobId));
    }
    public async Task RemoveJobData(string accessCode, string connectionString, string jobId)
    {
        var db = GetDatabase(accessCode, connectionString);
        await db.KeyDeleteAsync(GetJobKey(accessCode, jobId));
    }
    public async Task<bool> IsScheduled(string accessCode, string connectionString, string jobId)
    {
        var db = GetDatabase(accessCode, connectionString);

        var score = await db.SortedSetScoreAsync(
            GetScheduleKey(accessCode),
            jobId);

        return score.HasValue;
    }
    public void CloseAllConnections()
    {
        foreach (var mux in _connections.Values)
        {
            mux.Close();
            mux.Dispose();
        }

        _connections.Clear();
    }
}