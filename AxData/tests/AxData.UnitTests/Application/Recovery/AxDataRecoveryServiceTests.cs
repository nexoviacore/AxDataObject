using AxData.Application.Authorization;
using AxData.Application.Ports;
using AxData.Application.Ports.Axpert;
using AxData.Application.Recovery;
using AxData.Domain.Lifecycle;
using AxData.Domain.Model;
using AxData.TestSupport.Fakes;
using AxData.TestSupport.Fixtures;

namespace AxData.UnitTests.Application.Recovery;

public sealed class AxDataRecoveryServiceTests
{
    [Fact]
    public async Task Authorized_snapshot_is_restored_to_tenant_scoped_active_state()
    {
        var (service, active, durable, _, actor) = CreateFixture();
        durable.Seed(SampleStructDefFixture.CreateState());

        var result = await service.RecoverAsync(actor, "object-1", AxAuthorizationOperation.Query, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.NotNull(await active.LoadAsync(actor.TenantId, "object-1", CancellationToken.None));
    }

    [Theory]
    [InlineData(SnapshotReadStatus.NotFound, "AXD-NOTFOUND-001")]
    [InlineData(SnapshotReadStatus.Invalid, "AXD-INFRA-002")]
    [InlineData(SnapshotReadStatus.Unavailable, "AXD-INFRA-001")]
    public async Task Durable_read_status_maps_to_defined_error(SnapshotReadStatus status, string code)
    {
        var (service, _, durable, _, actor) = CreateFixture();
        durable.ReadStatus = status;

        var result = await service.RecoverAsync(actor, "object-1", AxAuthorizationOperation.Query, CancellationToken.None);

        Assert.Equal(code, result.Error!.Code);
    }

    [Fact]
    public async Task Expired_snapshot_returns_gone_and_closed_snapshot_returns_not_found()
    {
        var expiredFixture = CreateFixture();
        var expired = SampleStructDefFixture.CreateState();
        Assert.True(expired.TransitionTo(AxDataLifecycleStatus.Expired));
        expiredFixture.Durable.Seed(expired);

        var expiredResult = await expiredFixture.Service.RecoverAsync(
            expiredFixture.Actor, expired.ObjectId, AxAuthorizationOperation.Query, CancellationToken.None);

        var closedFixture = CreateFixture();
        var closed = SampleStructDefFixture.CreateState();
        Assert.True(closed.TransitionTo(AxDataLifecycleStatus.Saved));
        Assert.True(closed.TransitionTo(AxDataLifecycleStatus.Closed));
        closedFixture.Durable.Seed(closed);
        var closedResult = await closedFixture.Service.RecoverAsync(
            closedFixture.Actor, closed.ObjectId, AxAuthorizationOperation.Query, CancellationToken.None);

        Assert.Equal("AXD-NOTFOUND-002", expiredResult.Error!.Code);
        Assert.Equal("AXD-NOTFOUND-001", closedResult.Error!.Code);
    }

    [Fact]
    public async Task Missing_current_structdef_fails_permanently_without_restore()
    {
        var (service, active, durable, _, actor) = CreateFixture(includeDefinition: false);
        durable.Seed(SampleStructDefFixture.CreateState());

        var result = await service.RecoverAsync(actor, "object-1", AxAuthorizationOperation.Query, CancellationToken.None);

        Assert.Equal("AXD-INFRA-002", result.Error!.Code);
        Assert.Equal(500, result.Error.HttpStatusCode);
        Assert.Null(await active.LoadAsync(actor.TenantId, "object-1", CancellationToken.None));
    }

    [Fact]
    public async Task StructDef_hash_mismatch_returns_D28_conflict_without_restore()
    {
        var mismatch = SampleStructDefFixture.Definition with { Version = "changed" };
        var (service, active, durable, _, actor) = CreateFixture(definition: mismatch);
        durable.Seed(SampleStructDefFixture.CreateState());

        var result = await service.RecoverAsync(actor, "object-1", AxAuthorizationOperation.Query, CancellationToken.None);

        Assert.Equal("AXD-CONC-003", result.Error!.Code);
        Assert.Equal(409, result.Error.HttpStatusCode);
        Assert.Null(await active.LoadAsync(actor.TenantId, "object-1", CancellationToken.None));
        Assert.Equal(SnapshotReadStatus.Found, (await durable.LoadLatestAsync(actor.TenantId, "object-1", CancellationToken.None)).Status);
    }

    [Fact]
    public async Task Unauthorized_actor_cannot_restore_by_object_id()
    {
        var (service, active, durable, authorization, _) = CreateFixture();
        durable.Seed(SampleStructDefFixture.CreateState());
        authorization.QueryPermission = FakeAxpertAuthorizationProvider.CreateQueryPermission(isAllowed: false);
        var attacker = new AxUserContext(SampleStructDefFixture.TenantId, "attacker", new HashSet<string>());

        var result = await service.RecoverAsync(attacker, "object-1", AxAuthorizationOperation.Query, CancellationToken.None);

        Assert.Equal("AXD-AUTH-002", result.Error!.Code);
        Assert.Null(await active.LoadAsync(attacker.TenantId, "object-1", CancellationToken.None));
    }

    [Fact]
    public async Task Other_tenant_cannot_load_snapshot_with_same_object_id()
    {
        var (service, active, durable, _, _) = CreateFixture();
        durable.Seed(SampleStructDefFixture.CreateState());
        var other = new AxUserContext("tenant-b", "owner", new HashSet<string>());

        var result = await service.RecoverAsync(other, "object-1", AxAuthorizationOperation.Query, CancellationToken.None);

        Assert.Equal("AXD-NOTFOUND-001", result.Error!.Code);
        Assert.Null(await active.LoadAsync(other.TenantId, "object-1", CancellationToken.None));
    }

    private static Fixture CreateFixture(bool includeDefinition = true, StructDefHandle? definition = null)
    {
        var active = new InMemoryAxDataStore();
        var durable = new FakeSnapshotRepository();
        var authorization = new FakeAxpertAuthorizationProvider();
        var definitions = includeDefinition
            ? new FakeStructDefProvider(definition ?? SampleStructDefFixture.Definition)
            : new FakeStructDefProvider();
        var service = new AxDataRecoveryService(active, durable, definitions, authorization);
        var actor = new AxUserContext(SampleStructDefFixture.TenantId, "owner", new HashSet<string>());
        return new Fixture(service, active, durable, authorization, actor);
    }

    private sealed record Fixture(
        AxDataRecoveryService Service,
        InMemoryAxDataStore Active,
        FakeSnapshotRepository Durable,
        FakeAxpertAuthorizationProvider Authorization,
        AxUserContext Actor);
}
