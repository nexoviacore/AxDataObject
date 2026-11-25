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
    public async Task New_transaction_gets_server_identity_and_persistent_state()
    {
        var fixture = CreateFixture();

        var result = await fixture.Handler.HandleAsync(
            new InitializeAxDataCommand(SampleStructDefFixture.TransId, null),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("01J-OPAQUE", result.Response!.ObjectId);
        Assert.Null(fixture.Loader.LastKey!.KeyField);
        var stored = await fixture.Store.LoadAsync(
            SampleStructDefFixture.TenantId,
            "01J-OPAQUE",
            CancellationToken.None);
        Assert.Equal("owner", stored!.InitiatorUserName);
        Assert.Equal(SampleStructDefFixture.TransId, stored.TransId);
    }

    [Fact]
    public async Task Existing_key_is_passed_to_loader_and_record_id_is_returned()
    {
        var loaded = SampleStructDefFixture.CreateState();
        loaded.RecordId = 100501;
        var fixture = CreateFixture(loaded);
        fixture.FieldEngine.OnInitializeOnLoad = state =>
            state.DataContainers["dc2"].Rows[0].Fields["amount"].SetValue(30m, 600);
        var key = new AxRecordKey("recordid", "100501", null);

        var result = await fixture.Handler.HandleAsync(
            new InitializeAxDataCommand(SampleStructDefFixture.TransId, key),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(key, fixture.Loader.LastKey);
        Assert.Equal(100501, result.Response!.RecordId);
        var stored = await fixture.Store.LoadAsync(
            SampleStructDefFixture.TenantId,
            result.Response.ObjectId,
            CancellationToken.None);
        var amount = stored!.DataContainers["dc2"].Rows[0].Fields["amount"];
        Assert.Equal(100501, stored.RecordId);
        Assert.Equal(30m, amount.Value);
        Assert.Equal(30m, amount.OriginalValue);
        Assert.Equal(600, amount.SourceId);
        Assert.Equal(600, amount.OriginalSourceId);
        Assert.False(amount.IsDirty);
    }

    [Fact]
    public async Task On_load_processing_runs_once_and_its_values_are_stored()
    {
        var fixture = CreateFixture();
        fixture.FieldEngine.OnInitializeOnLoad = state =>
            state.DataContainers["dc2"].Rows[0].Fields["amount"].SetValue(25m, 501);

        var result = await fixture.Handler.HandleAsync(
            new InitializeAxDataCommand(SampleStructDefFixture.TransId, null),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(1, fixture.FieldEngine.InitializeOnLoadCount);
        var stored = await fixture.Store.LoadAsync(
            SampleStructDefFixture.TenantId,
            result.Response!.ObjectId,
            CancellationToken.None);
        var amount = stored!.DataContainers["dc2"].Rows[0].Fields["amount"];
        Assert.Equal(25m, amount.Value);
        Assert.Equal(25m, amount.OriginalValue);
        Assert.Equal(501, amount.SourceId);
        Assert.Equal(501, amount.OriginalSourceId);
        Assert.False(amount.IsDirty);
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
    public async Task Object_id_collision_retry_reuses_clean_initialized_state_without_repeating_on_load()
    {
        var fixture = CreateFixture(objectIds: ["object-1", "object-2"]);
        fixture.FieldEngine.OnInitializeOnLoad = state =>
            state.DataContainers["dc2"].Rows[0].Fields["amount"].SetValue(25m, 501);
        var existing = SampleStructDefFixture.CreateState();
        existing = CloneWithObjectId(existing, "object-1");
        Assert.Equal(
            AxDataStoreWriteStatus.Stored,
            (await fixture.Store.WriteAsync(existing, null, CancellationToken.None)).Status);

        var result = await fixture.Handler.HandleAsync(
            new InitializeAxDataCommand(SampleStructDefFixture.TransId, null),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("object-2", result.Response!.ObjectId);
        Assert.Equal(1, fixture.FieldEngine.InitializeOnLoadCount);
        var stored = await fixture.Store.LoadAsync(
            SampleStructDefFixture.TenantId,
            "object-2",
            CancellationToken.None);
        var amount = stored!.DataContainers["dc2"].Rows[0].Fields["amount"];
        Assert.Equal(25m, amount.Value);
        Assert.Equal(25m, amount.OriginalValue);
        Assert.Equal(501, amount.SourceId);
        Assert.Equal(501, amount.OriginalSourceId);
        Assert.False(amount.IsDirty);
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
        IReadOnlyList<string>? objectIds = null)
    {
        var authorization = new FakeAxpertAuthorizationProvider();
        var definitions = new FakeStructDefProvider(SampleStructDefFixture.Definition);
        var loader = new FakeAxpertDataLoader
        {
            Result = new AxDataLoadResult(true, loaded ?? SampleStructDefFixture.CreateState(), null)
        };
        var fieldEngine = new FakeAxpertFieldEngine(SampleStructDefFixture.Definition);
        var initializer = new PortBasedAxpertInitializer(definitions, loader, fieldEngine);
        var store = new InMemoryAxDataStore();
        var handler = new InitializeAxDataHandler(
            new FakeUserContext(new AxUserContext(
                SampleStructDefFixture.TenantId,
                "owner",
                new HashSet<string>())),
            authorization,
            initializer,
            store,
            new FakeObjectIdGenerator((objectIds ?? ["01J-OPAQUE", "01J-OPAQUE", "01J-OPAQUE"]).ToArray()),
            new FakeClock(Now),
            new InitializeAxDataOptions());
        return new Fixture(handler, authorization, definitions, loader, fieldEngine, store);
    }

    private static AxDataState CloneWithObjectId(AxDataState source, string objectId)
    {
        var clone = new AxDataState(
            objectId,
            source.TenantId,
            source.TransId,
            source.InitiatorUserName,
            source.CreatedAtUtc);
        foreach (var container in source.DataContainers)
        {
            clone.DataContainers.Add(container.Key, container.Value);
        }

        return clone;
    }

    private sealed record Fixture(
        InitializeAxDataHandler Handler,
        FakeAxpertAuthorizationProvider Authorization,
        FakeStructDefProvider StructDefs,
        FakeAxpertDataLoader Loader,
        FakeAxpertFieldEngine FieldEngine,
        InMemoryAxDataStore Store);
}
