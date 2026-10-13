using AxData.Application.Ports;
using AxData.Domain.Model;
using AxData.Infrastructure.Redis;
using AxData.Infrastructure.Serialization;

namespace AxData.IntegrationTests.Redis;

[Collection(RedisTestCollection.Name)]
public sealed class RedisAxDataLockTests
{
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMilliseconds(250);
    private readonly RedisServerFixture _redis;

    public RedisAxDataLockTests(RedisServerFixture redis)
    {
        _redis = redis;
    }

    [Fact]
    public async Task Only_one_holder_can_acquire_an_object_lock()
    {
        var dataLock = CreateLock();
        var objectId = NewObjectId();
        await using var first = await dataLock.TryAcquireAsync("tenant-a", objectId, CancellationToken.None);

        var second = await dataLock.TryAcquireAsync("tenant-a", objectId, CancellationToken.None);

        Assert.NotNull(first);
        Assert.Null(second);
    }

    [Fact]
    public async Task Releasing_the_owned_lease_permits_reacquisition()
    {
        var dataLock = CreateLock();
        var objectId = NewObjectId();
        var first = await dataLock.TryAcquireAsync("tenant-b", objectId, CancellationToken.None);
        Assert.NotNull(first);

        await first.DisposeAsync();
        await using var second = await dataLock.TryAcquireAsync("tenant-b", objectId, CancellationToken.None);

        Assert.NotNull(second);
    }

    [Fact]
    public async Task Expired_lease_cannot_release_another_holders_lock()
    {
        var dataLock = CreateLock();
        var objectId = NewObjectId();
        var expired = await dataLock.TryAcquireAsync("tenant-c", objectId, CancellationToken.None);
        Assert.NotNull(expired);

        var current = await AcquireAfterExpiryAsync(dataLock, "tenant-c", objectId);
        await using (current)
        {
            await expired.DisposeAsync();
            var contender = await dataLock.TryAcquireAsync("tenant-c", objectId, CancellationToken.None);

            Assert.Null(contender);
        }
    }

    [Fact]
    public async Task Version_check_rejects_expired_holders_stale_write()
    {
        var dataLock = CreateLock();
        var store = CreateStore();
        var objectId = NewObjectId();
        var initial = CreateState("tenant-d", objectId);
        await store.WriteAsync(initial, null, CancellationToken.None);

        var expired = await dataLock.TryAcquireAsync(initial.TenantId, objectId, CancellationToken.None);
        Assert.NotNull(expired);

        await using (var current = await AcquireAfterExpiryAsync(dataLock, initial.TenantId, objectId))
        {
            var currentUpdate = CreateState(initial.TenantId, objectId, version: 2);
            var stored = await store.WriteAsync(currentUpdate, 1, CancellationToken.None);
            Assert.Equal(AxDataStoreWriteStatus.Stored, stored.Status);
        }

        var staleUpdate = CreateState(initial.TenantId, objectId, version: 2);
        var stale = await store.WriteAsync(staleUpdate, 1, CancellationToken.None);
        await expired.DisposeAsync();

        Assert.Equal(AxDataStoreWriteStatus.StaleVersion, stale.Status);
        Assert.Equal(2, stale.CurrentVersion);
    }

    private RedisAxDataLock CreateLock() => new(
        _redis.Connection,
        CreateOptions());

    private RedisAxDataStore CreateStore() => new(
        _redis.Connection,
        new AxDataSnapshotSerializer(new AxDataSnapshotOptions
        {
            MaximumSerializedBytes = 1024 * 1024,
            MaximumGridRows = 100
        }),
        CreateOptions());

    private static AxDataRedisOptions CreateOptions() => new()
    {
        InactivityTtl = TimeSpan.FromSeconds(5),
        LockLeaseDuration = LeaseDuration
    };

    private static async Task<IAxDataLockLease> AcquireAfterExpiryAsync(
        IAxDataLock dataLock,
        string tenantId,
        string objectId)
    {
        var timeout = DateTimeOffset.UtcNow.AddSeconds(5);
        while (DateTimeOffset.UtcNow < timeout)
        {
            var lease = await dataLock.TryAcquireAsync(tenantId, objectId, CancellationToken.None);
            if (lease is not null)
            {
                return lease;
            }

            await Task.Delay(25);
        }

        throw new TimeoutException("The Redis lock lease did not expire within the test timeout.");
    }

    private static AxDataState CreateState(string tenantId, string objectId, int version = 1)
    {
        var state = new AxDataState(
            objectId,
            tenantId,
            "sample-order",
            "owner",
            "owner",
            "1",
            DateTimeOffset.UtcNow);

        while (state.Version < version)
        {
            state.RegisterMutation();
        }

        return state;
    }

    private static string NewObjectId() => $"object-{Guid.NewGuid():N}";
}
