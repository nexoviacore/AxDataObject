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

    /// <summary>Acquires a short token-owned Redis lease for a tenant-scoped object.</summary>
    /// <returns>The lease, or <see langword="null"/> when the key is already owned.</returns>
    public async Task<IAxDataLockLease?> TryAcquireAsync(
        string tenantId,
        string objectId,
        CancellationToken cancellationToken)
    {
        var key = BuildKey(tenantId, objectId);
        var token = RandomNumberGenerator.GetHexString(32);

        cancellationToken.ThrowIfCancellationRequested();
        var acquired = await _database.StringSetAsync(key, token, _leaseDuration, When.NotExists)
            .WaitAsync(cancellationToken);

        return acquired ? new RedisAxDataLockLease(_database, key, token) : null;
    }

    internal static RedisKey BuildKey(string tenantId, string objectId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(objectId);
        return $"AXD:LOCK:{{{tenantId.ToUpperInvariant()}}}:{objectId.ToUpperInvariant()}";
    }

    private sealed class RedisAxDataLockLease(
        IDatabase database,
        RedisKey key,
        RedisValue token) : IAxDataLockLease
    {
        private int _released;

        public string Token => token.ToString();

        /// <summary>Atomically releases the lease only when its ownership token still matches.</summary>
        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _released, 1) != 0)
            {
                return;
            }

            try
            {
                await database.ScriptEvaluateAsync(ReleaseScript, [key], [token]);
            }
            catch
            {
                Volatile.Write(ref _released, 0);
                throw;
            }
        }
    }
}
