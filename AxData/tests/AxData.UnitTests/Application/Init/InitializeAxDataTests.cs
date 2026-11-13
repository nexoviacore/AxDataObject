using AxData.Application.Init;
using AxData.Application.Ports;
using AxData.Application.Ports.Axpert;
using AxData.Domain.Model;
using AxData.TestSupport.Fakes;
using AxData.TestSupport.Fixtures;

namespace AxData.UnitTests.Application.Init;

public sealed class InitializeAxDataTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-09-25T08:00:00Z");

    [Fact]
    public async Task New_transaction_gets_server_identity_version_and_expiry()
    {
        var fixture = CreateFixture();

        var result = await fixture.Handler.HandleAsync(
            new InitializeAxDataCommand(SampleStructDefFixture.TransId, null),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("01J-OPAQUE", result.Response!.ObjectId);
        Assert.Equal(1, result.Response.Version);
        Assert.Equal(Now.AddMinutes(30), result.Response.ExpiresAt);
        Assert.Null(fixture.Loader.LastKey!.KeyField);
        var stored = await fixture.Store.LoadAsync(
            SampleStructDefFixture.TenantId,
            "01J-OPAQUE",
            CancellationToken.None);
        Assert.Equal("owner", stored!.OwnerUserName);
        Assert.Equal(SampleStructDefFixture.Version, stored.StructDefVersion);
    }

    [Fact]
    public async Task Existing_key_is_passed_to_loader_and_record_id_is_returned()
    {
        var loaded = SampleStructDefFixture.CreateState();
        loaded.RecordId = 100501;
        loaded.KeyField = "recordid";
        loaded.KeyValue = "100501";
        var fixture = CreateFixture(loaded);
        var key = new AxRecordKey("recordid", "100501", null);

        var result = await fixture.Handler.HandleAsync(
            new InitializeAxDataCommand(SampleStructDefFixture.TransId, key),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(key, fixture.Loader.LastKey);
        Assert.Equal(100501, result.Response!.RecordId);
    }

    [Fact]
    public async Task On_load_processing_runs_once_and_its_values_are_stored()
    {
        var fixture = CreateFixture();
        fixture.FieldEngine.OnInitializeOnLoad = state =>
            state.DataContainers["dc2"].Rows[0].Fields["amount"].SetValue(25m);

        var result = await fixture.Handler.HandleAsync(
            new InitializeAxDataCommand(SampleStructDefFixture.TransId, null),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(1, fixture.FieldEngine.InitializeOnLoadCount);
        var stored = await fixture.Store.LoadAsync(
            SampleStructDefFixture.TenantId,
            result.Response!.ObjectId,
            CancellationToken.None);
        Assert.Equal(25m, stored!.DataContainers["dc2"].Rows[0].Fields["amount"].Value);
    }

    [Fact]
    public async Task Failed_on_load_processing_does_not_store_an_object()
    {
        var fixture = CreateFixture();
        fixture.FieldEngine.InitializeOnLoadResult = FieldEvaluationResult.Failure(
            new FieldEvaluationError(
                new FieldAddress("dc1", "row_new", "amount"),
                "AXD-VAL-001",
                "The scripted on-load calculation failed."));

        var result = await fixture.Handler.HandleAsync(
            new InitializeAxDataCommand(SampleStructDefFixture.TransId, null),
            CancellationToken.None);

        Assert.Equal("AXD-VAL-001", result.Error!.Code);
        Assert.Equal(1, fixture.FieldEngine.InitializeOnLoadCount);
        Assert.Null(await fixture.Store.LoadAsync(
            SampleStructDefFixture.TenantId,
            "01J-OPAQUE",
            CancellationToken.None));
    }

    [Fact]
    public async Task Object_id_collision_retries_do_not_repeat_on_load_processing()
    {
        var fixture = CreateFixture(objectId: "object-1");
        var existing = SampleStructDefFixture.CreateState();
        Assert.Equal(
            AxDataStoreWriteStatus.Stored,
            (await fixture.Store.WriteAsync(existing, null, CancellationToken.None)).Status);

        var result = await fixture.Handler.HandleAsync(
            new InitializeAxDataCommand(SampleStructDefFixture.TransId, null),
            CancellationToken.None);

        Assert.Equal("AXD-INFRA-001", result.Error!.Code);
        Assert.Equal(1, fixture.FieldEngine.InitializeOnLoadCount);
    }

    [Fact]
    public async Task Unauthorized_request_stops_before_definition_or_data_load()
    {
        var fixture = CreateFixture();
        fixture.Authorization.Permission = new AxCreatePermission(false);

        var result = await fixture.Handler.HandleAsync(
            new InitializeAxDataCommand(SampleStructDefFixture.TransId, null),
            CancellationToken.None);

        Assert.Equal("AXD-AUTH-002", result.Error!.Code);
        Assert.Equal(0, fixture.StructDefs.CallCount);
        Assert.Equal(0, fixture.Loader.CallCount);
    }

    [Fact]
    public async Task Unknown_transaction_returns_invalid_request_without_loading_data()
    {
        var fixture = CreateFixture();

        var result = await fixture.Handler.HandleAsync(
            new InitializeAxDataCommand("unknown", null),
            CancellationToken.None);

        Assert.Equal("AXD-REQ-001", result.Error!.Code);
        Assert.Equal(0, fixture.Loader.CallCount);
    }

    private static Fixture CreateFixture(
        AxDataState? loaded = null,
        string objectId = "01J-OPAQUE")
    {
        var authorization = new FakeAxpertAuthorizationProvider();
        var definitions = new FakeStructDefProvider(SampleStructDefFixture.Definition);
        var loader = new FakeAxpertDataLoader
        {
            Result = new AxDataLoadResult(true, loaded ?? SampleStructDefFixture.CreateState(), null)
        };
        var fieldEngine = new FakeAxpertFieldEngine(SampleStructDefFixture.Definition);
        var store = new InMemoryAxDataStore();
        var handler = new InitializeAxDataHandler(
            new FakeUserContext(new AxUserContext(
                SampleStructDefFixture.TenantId,
                "owner",
                new HashSet<string>())),
            authorization,
            definitions,
            loader,
            fieldEngine,
            store,
            new FakeObjectIdGenerator(objectId, objectId, objectId),
            new FakeClock(Now),
            new InitializeAxDataOptions());
        return new Fixture(handler, authorization, definitions, loader, fieldEngine, store);
    }

    private sealed record Fixture(
        InitializeAxDataHandler Handler,
        FakeAxpertAuthorizationProvider Authorization,
        FakeStructDefProvider StructDefs,
        FakeAxpertDataLoader Loader,
        FakeAxpertFieldEngine FieldEngine,
        InMemoryAxDataStore Store);
}
