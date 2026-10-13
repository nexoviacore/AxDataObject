using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using AxData.Application.Ports;
using AxData.Domain.Model;
using AxData.Infrastructure.Redis;
using AxData.Infrastructure.Serialization;
using StackExchange.Redis;

namespace AxData.IntegrationTests.Redis;

[Collection(RedisTestCollection.Name)]
public sealed class RedisAxDataStoreTests
{
    private static readonly TimeSpan InactivityTtl = TimeSpan.FromSeconds(5);
    private readonly RedisServerFixture _redis;

    public RedisAxDataStoreTests(RedisServerFixture redis)
    {
        _redis = redis;
    }

    [Fact]
    public async Task Stale_write_is_rejected_and_returns_current_version()
    {
        var store = CreateStore();
        var state = CreateState("tenant-a", NewObjectId());
        var initial = await store.WriteAsync(state, null, CancellationToken.None);

        var changed = CreateState(state.TenantId, state.ObjectId, version: 2);
        var stale = await store.WriteAsync(changed, 99, CancellationToken.None);
        var loaded = await store.LoadAsync(state.TenantId, state.ObjectId, CancellationToken.None);

        Assert.Equal(AxDataStoreWriteStatus.Stored, initial.Status);
        Assert.Equal(AxDataStoreWriteStatus.StaleVersion, stale.Status);
        Assert.Equal(1, stale.CurrentVersion);
        Assert.Equal(1, loaded!.Version);
    }

    [Fact]
    public async Task Two_concurrent_writers_with_the_same_expected_version_have_one_winner()
    {
        var store = CreateStore();
        var state = CreateState("tenant-b", NewObjectId());
        await store.WriteAsync(state, null, CancellationToken.None);

        var first = CreateState(state.TenantId, state.ObjectId, version: 2);
        var second = CreateState(state.TenantId, state.ObjectId, version: 2);
        var writes = await Task.WhenAll(
            store.WriteAsync(first, 1, CancellationToken.None),
            store.WriteAsync(second, 1, CancellationToken.None));

        Assert.Single(writes, result => result.Status == AxDataStoreWriteStatus.Stored);
        var loser = Assert.Single(writes, result => result.Status == AxDataStoreWriteStatus.StaleVersion);
        Assert.Equal(2, loser.CurrentVersion);
    }

    [Fact]
    public async Task Successful_compare_and_set_refreshes_the_inactivity_ttl()
    {
        var store = CreateStore();
        var state = CreateState("tenant-c", NewObjectId());
        await store.WriteAsync(state, null, CancellationToken.None);

        var key = BuildKey(state.TenantId, state.ObjectId);
        var initialTtl = await _redis.Connection.GetDatabase().KeyTimeToLiveAsync(key);
        await Task.Delay(TimeSpan.FromMilliseconds(750));
        var depletedTtl = await _redis.Connection.GetDatabase().KeyTimeToLiveAsync(key);

        var changed = CreateState(state.TenantId, state.ObjectId, version: 2);
        await store.WriteAsync(changed, 1, CancellationToken.None);
        var refreshedTtl = await _redis.Connection.GetDatabase().KeyTimeToLiveAsync(key);

        Assert.NotNull(initialTtl);
        Assert.NotNull(depletedTtl);
        Assert.NotNull(refreshedTtl);
        Assert.InRange(initialTtl.Value, TimeSpan.FromSeconds(4), InactivityTtl);
        Assert.True(refreshedTtl > depletedTtl, $"Expected refreshed TTL {refreshedTtl} to exceed {depletedTtl}.");
    }

    [Fact]
    public async Task Same_object_id_in_another_tenant_is_unreachable()
    {
        var store = CreateStore();
        var objectId = NewObjectId();
        var state = CreateState("tenant-d", objectId);
        await store.WriteAsync(state, null, CancellationToken.None);

        var otherTenantState = await store.LoadAsync(
            "tenant-e",
            objectId,
            CancellationToken.None);

        Assert.Null(otherTenantState);
    }

    [Fact]
    public async Task Snapshot_publish_is_atomic_and_ttl_waits_for_durable_acknowledgement()
    {
        if (!SupportsStreams()) return;
        var tenantId = $"tenant-{Guid.NewGuid():N}";
        var objectId = NewObjectId();
        var store = CreateStore();
        await store.WriteAsync(CreateState(tenantId, objectId), null, CancellationToken.None);
        var changed = CreateState(tenantId, objectId, version: 2);
        var transport = CreateTransport();
        var envelope = new SnapshotEnvelope(
            "event-2",
            tenantId,
            objectId,
            2,
            changed,
            DateTimeOffset.UtcNow,
            "corr-snapshot");

        var published = await transport.PublishAsync(envelope, 1, CancellationToken.None);
        var ttlBeforeAck = await _redis.Connection.GetDatabase().KeyTimeToLiveAsync(BuildKey(tenantId, objectId));
        var deliveries = await transport.ReadAsync(
            tenantId, "group-1", "consumer-1", 10, TimeSpan.Zero, CancellationToken.None);
        await transport.AcknowledgeAsync(deliveries.Single(), 2, "group-1", CancellationToken.None);
        var ttlAfterAck = await _redis.Connection.GetDatabase().KeyTimeToLiveAsync(BuildKey(tenantId, objectId));

        Assert.Equal(AxDataStoreWriteStatus.Stored, published.Status);
        Assert.Null(ttlBeforeAck);
        Assert.NotNull(ttlAfterAck);
        Assert.Equal(2, deliveries.Single().Envelope!.ObjectVersion);
        Assert.Equal("corr-snapshot", deliveries.Single().Envelope!.CorrelationId);
    }

    [Fact]
    public async Task Stale_snapshot_write_does_not_append_stream_entry()
    {
        if (!SupportsStreams()) return;
        var tenantId = $"tenant-{Guid.NewGuid():N}";
        var objectId = NewObjectId();
        var store = CreateStore();
        await store.WriteAsync(CreateState(tenantId, objectId), null, CancellationToken.None);
        var transport = CreateTransport();
        var changed = CreateState(tenantId, objectId, version: 2);
        var envelope = new SnapshotEnvelope("event-stale", tenantId, objectId, 2, changed, DateTimeOffset.UtcNow);

        var result = await transport.PublishAsync(envelope, 99, CancellationToken.None);
        var length = await _redis.Connection.GetDatabase().StreamLengthAsync($"AXD:SNAPSHOTS:{{{tenantId.ToUpperInvariant()}}}");

        Assert.Equal(AxDataStoreWriteStatus.StaleVersion, result.Status);
        Assert.Equal(0, length);
    }

    [Fact]
    public async Task Restarted_consumer_claims_unacknowledged_delivery()
    {
        if (!SupportsStreams()) return;
        var tenantId = $"tenant-{Guid.NewGuid():N}";
        var objectId = NewObjectId();
        var store = CreateStore();
        await store.WriteAsync(CreateState(tenantId, objectId), null, CancellationToken.None);
        var transport = CreateTransport();
        var changed = CreateState(tenantId, objectId, version: 2);
        await transport.PublishAsync(
            new SnapshotEnvelope("event-restart", tenantId, objectId, 2, changed, DateTimeOffset.UtcNow),
            1,
            CancellationToken.None);

        var first = await transport.ReadAsync(
            tenantId, "restart-group", "consumer-before", 1, TimeSpan.Zero, CancellationToken.None);
        var reclaimed = await transport.ReadAsync(
            tenantId, "restart-group", "consumer-after", 1, TimeSpan.Zero, CancellationToken.None);

        Assert.Single(first);
        Assert.Single(reclaimed);
        Assert.Equal(first[0].StreamEntryId, reclaimed[0].StreamEntryId);
        Assert.True(reclaimed[0].DeliveryCount >= 2);
    }

    private RedisAxDataStore CreateStore() => new(
        _redis.Connection,
        new AxDataSnapshotSerializer(new AxDataSnapshotOptions
        {
            MaximumSerializedBytes = 1024 * 1024,
            MaximumGridRows = 100
        }),
        new AxDataRedisOptions
        {
            InactivityTtl = InactivityTtl,
            LockLeaseDuration = TimeSpan.FromSeconds(1)
        });

    private RedisSnapshotTransport CreateTransport() => new(
        _redis.Connection,
        new AxDataSnapshotSerializer(new AxDataSnapshotOptions
        {
            MaximumSerializedBytes = 1024 * 1024,
            MaximumGridRows = 100
        }),
        new AxDataRedisOptions
        {
            InactivityTtl = InactivityTtl,
            LockLeaseDuration = TimeSpan.FromSeconds(1)
        });

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

    private static RedisKey BuildKey(string tenantId, string objectId) =>
        $"AXD:{{{tenantId.ToUpperInvariant()}}}:{objectId.ToUpperInvariant()}";

    private bool SupportsStreams() =>
        _redis.Connection.GetServer(_redis.Connection.GetEndPoints().Single()).Version.Major >= 5;
}

[CollectionDefinition(Name)]
public sealed class RedisTestCollection : ICollectionFixture<RedisServerFixture>
{
    public const string Name = "Redis integration tests";
}

public sealed class RedisServerFixture : IAsyncLifetime
{
    private Process? _process;
    private ConnectionMultiplexer? _connection;

    public ConnectionMultiplexer Connection =>
        _connection ?? throw new InvalidOperationException("The Redis fixture is not initialized.");

    public async Task InitializeAsync()
    {
        var port = GetAvailablePort();
        var executable = Environment.GetEnvironmentVariable("AXDATA_REDIS_SERVER") ?? "redis-server";
        var startInfo = new ProcessStartInfo(executable)
        {
            CreateNoWindow = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add("--port");
        startInfo.ArgumentList.Add(port.ToString());
        startInfo.ArgumentList.Add("--bind");
        startInfo.ArgumentList.Add(IPAddress.Loopback.ToString());
        startInfo.ArgumentList.Add("--save");
        startInfo.ArgumentList.Add(string.Empty);
        startInfo.ArgumentList.Add("--appendonly");
        startInfo.ArgumentList.Add("no");
        startInfo.ArgumentList.Add("--loglevel");
        startInfo.ArgumentList.Add("warning");

        _process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("The Redis integration-test process could not be started.");

        var configuration = new ConfigurationOptions
        {
            AbortOnConnectFail = false,
            ConnectTimeout = 1000,
            SyncTimeout = 1000
        };
        configuration.EndPoints.Add(IPAddress.Loopback, port);
        _connection = await ConnectionMultiplexer.ConnectAsync(configuration);

        for (var attempt = 0; attempt < 30; attempt++)
        {
            try
            {
                await _connection.GetDatabase().PingAsync();
                return;
            }
            catch (RedisConnectionException) when (attempt < 29)
            {
                await Task.Delay(100);
            }
        }
    }

    public async Task DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.CloseAsync();
            _connection.Dispose();
        }

        if (_process is { HasExited: false })
        {
            _process.Kill(entireProcessTree: true);
            await _process.WaitForExitAsync();
        }

        _process?.Dispose();
    }

    private static int GetAvailablePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}
