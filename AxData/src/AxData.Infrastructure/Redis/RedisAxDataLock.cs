using System.Security.Cryptography;
using AxData.Application.Ports;
using StackExchange.Redis;

namespace AxData.Infrastructure.Redis;

public sealed class RedisAxDataLock : IAxDataLock
{
    private const string ReleaseScript = """
        if redis.call('GET', KEYS[1]) == ARGV[1] then
            return redis.call('DEL', KEYS[1])
        end

        return 0
        """;

    private readonly IDatabase _database;
    private readonly TimeSpan _leaseDuration;

    public RedisAxDataLock(
        IConnectionMultiplexer connectionMultiplexer,
        AxDataRedisOptions options)
    {
        ArgumentNullException.ThrowIfNull(connectionMultiplexer);
        ArgumentNullException.ThrowIfNull(options);

        if (options.LockLeaseDuration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "The lock lease duration must be positive.");
        }

        _database = connectionMultiplexer.GetDatabase();
        _leaseDuration = options.LockLeaseDuration;
    }

    public async Task<IAxDataLockLease?> TryAcquireAsync(
        string tenantId,
        string objectId,
        CancellationToken cancellationToken)
    {
        var key = BuildKey(tenantId, objectId);
        var token = RandomNumberGenerator.GetHexString(32);

        cancellationToken.ThrowIfCancellationRequested();
        var acquired = await _database.StringSetAsync(key, token, _leaseDuration, When.NotExists)
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);

        return acquired ? new RedisAxDataLockLease(_database, key, token) : null;
    }

    private static RedisKey BuildKey(string tenantId, string objectId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(objectId);
        return $"AXD:LOCK:{tenantId.ToUpperInvariant()}:{objectId.ToUpperInvariant()}";
    }

    private sealed class RedisAxDataLockLease(
        IDatabase database,
        RedisKey key,
        RedisValue token) : IAxDataLockLease
    {
        private int _released;

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _released, 1) != 0)
            {
                return;
            }

            try
            {
                await database.ScriptEvaluateAsync(ReleaseScript, [key], [token])
                    .ConfigureAwait(false);
            }
            catch
            {
                Volatile.Write(ref _released, 0);
                throw;
            }
        }
    }
}
