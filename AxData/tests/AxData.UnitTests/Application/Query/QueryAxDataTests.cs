using AxData.Application.Ports;
using AxData.Application.Ports.Axpert;
using AxData.Application.Query;
using AxData.Domain.Lifecycle;
using AxData.Domain.Model;
using AxData.TestSupport.Fakes;
using AxData.TestSupport.Fixtures;

namespace AxData.UnitTests.Application.Query;

public sealed class QueryAxDataTests
{
    [Fact]
    public async Task Existing_record_forwards_full_business_key_to_authorization()
    {
        var state = CreateState();
        state.KeyField = "orderdate";
        state.KeyValue = "05/10/2026";
        state.RecordId = 42;
        var fixture = await CreateFixtureAsync("owner", state);

        var result = await fixture.Handler.HandleAsync(All(), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(new AxRecordKey("orderdate", "05/10/2026", 42), fixture.Authorization.LastQueryKey);
    }

    [Fact]
    public async Task Redis_miss_recovers_authorized_snapshot_and_continues_query()
    {
        var state = CreateState();
        var store = new InMemoryAxDataStore();
        var durable = new FakeSnapshotRepository();
        durable.Seed(state);
        var authorization = new FakeAxpertAuthorizationProvider();
        var definitions = new FakeStructDefProvider(SampleStructDefFixture.Definition);
        var handler = new QueryAxDataHandler(
            new FakeUserContext(new AxUserContext(state.TenantId, "owner", new HashSet<string>())),
            store,
            definitions,
            authorization,
            FakeRecovery.Create(store, authorization, definitions, durable));

        var result = await handler.HandleAsync(All(), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.NotNull(await store.LoadAsync(state.TenantId, state.ObjectId, CancellationToken.None));
        Assert.Equal(
            state.DataContainers["dc2"].Rows.Select(row => row.RowId),
            result.Response!.DataContainers.Single(container => container.DcId == "dc2").Rows.Select(row => row.RowId));
    }

    [Fact]
    public async Task Complete_query_omits_hidden_nodes_and_applies_read_only_metadata()
    {
        var fixture = await CreateFixtureAsync("viewer");
        fixture.Authorization.QueryPermission = FakeAxpertAuthorizationProvider.CreateQueryPermission(
            canEditTransaction: false,
            excludedFields: FakeAxpertAuthorizationProvider.FieldSets(("dc1", ["secret"])),
            readOnlyDataContainers: FakeAxpertAuthorizationProvider.Set("dc2"),
            allowedOperations: FakeAxpertAuthorizationProvider.Set("query"));

        var result = await fixture.Handler.HandleAsync(All(), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.True(result.Response!.ReadOnly);
        Assert.False(result.Response.PendingApproval);
        Assert.Equal(["query"], result.Response.AllowedOperations);
        var header = Assert.Single(result.Response.DataContainers, container => container.DcId == "dc1");
        var fields = Assert.Single(header.Rows).Fields;
        Assert.Contains(fields, field => field.Name == "customer" && field.ReadOnly);
        Assert.DoesNotContain(fields, field => field.Name == "secret");
        Assert.All(result.Response.DataContainers.SelectMany(container => container.Rows).SelectMany(row => row.Fields),
            field => Assert.True(field.ReadOnly));
    }

    [Fact]
    public async Task Explicit_selection_returns_only_requested_permitted_fields()
    {
        var fixture = await CreateFixtureAsync("owner");
        fixture.Authorization.QueryPermission = FakeAxpertAuthorizationProvider.CreateQueryPermission(
            excludedFields: FakeAxpertAuthorizationProvider.FieldSets(("dc2", ["rate"])),
            readOnlyFields: FakeAxpertAuthorizationProvider.FieldSets(("dc2", ["amount"])));
        var command = new QueryAxDataCommand(
            "object-1",
            false,
            [new QuerySelection("dc2", ["qty", "rate", "amount"])]);

        var result = await fixture.Handler.HandleAsync(command, CancellationToken.None);

        var container = Assert.Single(result.Response!.DataContainers);
        var fields = Assert.Single(container.Rows).Fields;
        Assert.Equal(["qty", "amount"], fields.Select(field => field.Name));
        Assert.False(fields[0].ReadOnly);
        Assert.True(fields[1].ReadOnly);
    }

    [Fact]
    public async Task Explicit_exclude_wins_over_container_and_field_includes()
    {
        var fixture = await CreateFixtureAsync("owner");
        fixture.Authorization.QueryPermission = FakeAxpertAuthorizationProvider.CreateQueryPermission(
            includedDataContainers: FakeAxpertAuthorizationProvider.Set("dc1"),
            includedFields: FakeAxpertAuthorizationProvider.FieldSets(("dc1", ["customer", "secret"])),
            excludedFields: FakeAxpertAuthorizationProvider.FieldSets(("dc1", ["secret"])));

        var result = await fixture.Handler.HandleAsync(All(), CancellationToken.None);

        var container = Assert.Single(result.Response!.DataContainers);
        Assert.Equal("dc1", container.DcId);
        Assert.Equal(["customer"], Assert.Single(container.Rows).Fields.Select(field => field.Name));
    }

    [Fact]
    public async Task Only_assigned_current_approver_gets_pending_approval_and_actions()
    {
        var state = CreateState();
        state.Workflow = new PegActiveState
        {
            WorkflowInstanceId = "workflow-1",
            CurrentTaskId = "task-1",
            InitiatorUserName = "owner",
            Status = PegWorkflowStatus.Pending,
            ApprovalLevel = 1,
            Approvers = ["approver"],
            WorkflowVersion = 1,
            PendingFromUtc = DateTimeOffset.Parse("2026-09-25T08:00:00Z")
        };
        var fixture = await CreateFixtureAsync("approver", state);
        fixture.Authorization.QueryPermission = FakeAxpertAuthorizationProvider.CreateQueryPermission(
            canEditTransaction: false,
            allowedOperations: FakeAxpertAuthorizationProvider.Set("query", "approve", "reject", "return", "save"));

        var result = await fixture.Handler.HandleAsync(All(), CancellationToken.None);

        Assert.True(result.Response!.PendingApproval);
        Assert.True(result.Response.ReadOnly);
        Assert.Equal(["query", "approve", "reject", "return"], result.Response.AllowedOperations);
    }

    [Fact]
    public async Task Unauthorized_actor_gets_standard_access_denied_result()
    {
        var fixture = await CreateFixtureAsync("unrelated");
        fixture.Authorization.QueryPermission = AxQueryPermission.Denied;

        var result = await fixture.Handler.HandleAsync(All(), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("AXD-AUTH-002", result.Error!.Code);
    }

    [Fact]
    public async Task StructDef_hash_mismatch_fails_closed_after_authorization()
    {
        var state = CreateState();
        var store = new InMemoryAxDataStore();
        await store.WriteAsync(state, null, CancellationToken.None);
        var authorization = new FakeAxpertAuthorizationProvider();
        var definitions = new FakeStructDefProvider(SampleStructDefFixture.Definition with { Version = "changed" });
        var handler = new QueryAxDataHandler(
            new FakeUserContext(new AxUserContext(state.TenantId, "owner", new HashSet<string>())),
            store,
            definitions,
            authorization,
            FakeRecovery.Create(store, authorization, definitions));

        var result = await handler.HandleAsync(All(), CancellationToken.None);

        Assert.Equal("AXD-CONC-003", result.Error!.Code);
        Assert.Equal(1, authorization.QueryCallCount);
        Assert.Equal(1, (await store.LoadAsync(state.TenantId, state.ObjectId, CancellationToken.None))!.Version);
    }

    [Fact]
    public async Task Tenant_scoped_lookup_does_not_reveal_another_tenants_object()
    {
        var fixture = await CreateFixtureAsync("owner", actorTenant: "other-tenant");

        var result = await fixture.Handler.HandleAsync(All(), CancellationToken.None);

        Assert.Equal("AXD-NOTFOUND-001", result.Error!.Code);
        Assert.Equal(0, fixture.Authorization.QueryCallCount);
    }

    [Theory]
    [MemberData(nameof(InvalidSelections))]
    public async Task Invalid_selection_returns_invalid_request(QueryAxDataCommand command)
    {
        var fixture = await CreateFixtureAsync("owner");

        var result = await fixture.Handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal("AXD-REQ-001", result.Error!.Code);
    }

    public static TheoryData<QueryAxDataCommand> InvalidSelections => new()
    {
        new QueryAxDataCommand("object-1", false, []),
        new QueryAxDataCommand("object-1", true, [new QuerySelection("dc1", ["*"])]),
        new QueryAxDataCommand("object-1", false, [new QuerySelection("unknown", ["*"])]),
        new QueryAxDataCommand("object-1", false, [new QuerySelection("dc1", ["unknown"])]),
        new QueryAxDataCommand("object-1", false, [
            new QuerySelection("dc1", ["customer"]),
            new QuerySelection("DC1", ["secret"])])
    };

    private static QueryAxDataCommand All() => new("object-1", true, []);

    private static async Task<Fixture> CreateFixtureAsync(
        string userName,
        AxDataState? state = null,
        string actorTenant = SampleStructDefFixture.TenantId)
    {
        var store = new InMemoryAxDataStore();
        await store.WriteAsync(state ?? CreateState(), null, CancellationToken.None);
        var authorization = new FakeAxpertAuthorizationProvider();
        var definitionProvider = new FakeStructDefProvider(SampleStructDefFixture.Definition);
        var handler = new QueryAxDataHandler(
            new FakeUserContext(new AxUserContext(actorTenant, userName, new HashSet<string>())),
            store,
            definitionProvider,
            authorization,
            FakeRecovery.Create(store, authorization, definitionProvider));
        return new Fixture(handler, authorization);
    }

    private static AxDataState CreateState()
    {
        var state = SampleStructDefFixture.CreateState();
        state.DataContainers["dc1"].Rows[0].Fields.Add("secret", new AxFieldValue("classified"));
        return state;
    }

    private sealed record Fixture(
        QueryAxDataHandler Handler,
        FakeAxpertAuthorizationProvider Authorization);
}
