using AxData.Application.Ports;
using AxData.Contracts.Snapshots;
using AxData.Domain.Model;
using AxData.Infrastructure.Redis;
using AxData.Infrastructure.Serialization;
using AxData.TestSupport.Fixtures;
using AxTstructData;
using System.Reflection;
using System.Text;

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
    public async Task Token_check_rejects_expired_holders_write()
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
            var currentUpdate = CreateState(initial.TenantId, objectId);
            var stored = await store.WriteAsync(currentUpdate, current.Token, CancellationToken.None);
            Assert.Equal(AxDataStoreWriteStatus.Stored, stored.Status);

            var expiredUpdate = CreateState(initial.TenantId, objectId);
            var rejected = await store.WriteAsync(expiredUpdate, expired.Token, CancellationToken.None);
            Assert.Equal(AxDataStoreWriteStatus.Conflict, rejected.Status);
        }

        await expired.DisposeAsync();
    }

    [Fact]
    public async Task AxPut_claim_rejects_state_changed_after_preparation()
    {
        var state = SampleStructDefFixture.CreateState();
        var tenantId = state.TenantId;
        var objectId = state.ObjectId;
        var stateKey = $"AXD:{{{tenantId.ToUpperInvariant()}}}:{objectId.ToUpperInvariant()}";
        var lockKey = $"AXD:LOCK:{{{tenantId.ToUpperInvariant()}}}:{objectId.ToUpperInvariant()}";
        var database = _redis.Connection.GetDatabase();
        await database.HashSetAsync(stateKey, "payload", "newer-state");
        var script = (string)typeof(AxPutDataObjectStore)
            .GetField("ClaimScript", BindingFlags.NonPublic | BindingFlags.Static)!
            .GetRawConstantValue()!;

        var result = await database.ScriptEvaluateAsync(
            script,
            [stateKey, lockKey],
            [0, "stale-saving-state", "claim-token", "older-state"]);

        Assert.Equal("busy", (string)result!);
        Assert.Equal("newer-state", (string?)await database.HashGetAsync(stateKey, "payload"));
        Assert.False(await database.HashExistsAsync(stateKey, "saveClaim"));
    }

    [Fact]
    public void AxPut_expected_payload_is_byte_compatible_with_AxData_serialization()
    {
        var serializer = new AxDataSnapshotSerializer(new AxDataSnapshotOptions
        {
            MaximumSerializedBytes = 1024 * 1024,
            MaximumGridRows = 100
        });
        var payload = Encoding.UTF8.GetString(serializer.Serialize(SampleStructDefFixture.CreateState()));
        var snapshot = DataObjectSnapshotReader.ReadCurrent(payload);
        var serialize = typeof(AxPutDataObjectStore)
            .GetMethod("Serialize", BindingFlags.NonPublic | BindingFlags.Static)!;

        var axPutPayload = Assert.IsType<string>(serialize.Invoke(null, [snapshot]));

        Assert.Equal(payload, axPutPayload);
    }

    [Fact]
    public async Task AxPut_claim_release_and_finalize_preserve_the_shared_wire_shape()
    {
        if (!_redis.SupportsRedis62OrThrowWhenRequired()) return;
        var state = SampleStructDefFixture.CreateState();
        var tenantId = state.TenantId;
        var objectId = state.ObjectId;
        var serializer = new AxDataSnapshotSerializer(new AxDataSnapshotOptions
        {
            MaximumSerializedBytes = 1024 * 1024,
            MaximumGridRows = 100
        });
        var payload = Encoding.UTF8.GetString(serializer.Serialize(state));
        Assert.DoesNotContain("createdAtUtc", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("lastAccessedAtUtc", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("expiresAtUtc", payload, StringComparison.OrdinalIgnoreCase);
        var database = _redis.Connection.GetDatabase();
        var stateKey = $"AXD:{{{tenantId.ToUpperInvariant()}}}:{objectId.ToUpperInvariant()}";
        var lockKey = $"AXD:LOCK:{{{tenantId.ToUpperInvariant()}}}:{objectId.ToUpperInvariant()}";
        var streamKey = $"AXD:SNAPSHOTS:{{{tenantId.ToUpperInvariant()}}}";
        var closedKey = $"AXD:CLOSED:{{{tenantId.ToUpperInvariant()}}}:{objectId.ToUpperInvariant()}";
        await database.KeyDeleteAsync([stateKey, lockKey, streamKey, closedKey]);
        await database.HashSetAsync(stateKey, "payload", payload);
        await database.KeyExpireAsync(stateKey, TimeSpan.FromSeconds(30));

        var claimScript = Script("ClaimScript");
        var releaseScript = Script("ReleaseScript");
        var finalizeScript = Script("FinalizeScript");

        Assert.Equal("claimed", (string)(await database.ScriptEvaluateAsync(
            claimScript, [stateKey, lockKey], [0, payload, "release-token", payload]))!);
        Assert.Equal(payload, (string?)await database.HashGetAsync(stateKey, "payload"));
        Assert.Equal("released", (string)(await database.ScriptEvaluateAsync(
            releaseScript,
            [stateKey, streamKey],
            ["release-token", 0, payload, "event-release", tenantId, objectId, "2026-11-16T00:00:00Z"]))!);
        Assert.Equal(payload, (string?)await database.HashGetAsync(stateKey, "payload"));
        Assert.Null(await database.KeyTimeToLiveAsync(stateKey));

        Assert.Equal("claimed", (string)(await database.ScriptEvaluateAsync(
            claimScript, [stateKey, lockKey], [0, payload, "retain-token", payload]))!);
        Assert.Equal("finalized", (string)(await database.ScriptEvaluateAsync(
            finalizeScript,
            [stateKey, streamKey, closedKey],
            ["retain-token", payload, "event-retain", tenantId, objectId, "2026-11-16T00:00:00Z", 1]))!);
        Assert.Equal(payload, (string?)await database.HashGetAsync(stateKey, "payload"));
        Assert.Null(await database.KeyTimeToLiveAsync(stateKey));

        Assert.Equal("claimed", (string)(await database.ScriptEvaluateAsync(
            claimScript, [stateKey, lockKey], [0, payload, "close-token", payload]))!);
        Assert.Equal("finalized", (string)(await database.ScriptEvaluateAsync(
            finalizeScript,
            [stateKey, streamKey, closedKey],
            ["close-token", payload, "event-close", tenantId, objectId, "2026-11-16T00:00:00Z", 0]))!);
        Assert.False(await database.KeyExistsAsync(stateKey));
        Assert.Equal(payload, (string?)await database.StringGetAsync(closedKey));

        static string Script(string name) => (string)typeof(AxPutDataObjectStore)
            .GetField(name, BindingFlags.NonPublic | BindingFlags.Static)!
            .GetRawConstantValue()!;
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
        }));

    private static AxDataRedisOptions CreateOptions() => new()
    {
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

    private static AxDataState CreateState(string tenantId, string objectId) => new(
            objectId,
            tenantId,
            "sample-order",
            "owner",
            DateTimeOffset.UtcNow);

    private static string NewObjectId() => $"object-{Guid.NewGuid():N}";
}
