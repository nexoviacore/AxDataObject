using AxData.Application.Ports;
using AxData.Application.Ports.Axpert;
using AxData.Application.Set;
using AxData.Domain.Lifecycle;
using AxData.Domain.Model;
using AxData.TestSupport.Fakes;
using AxData.TestSupport.Fixtures;

namespace AxData.UnitTests.Application.Set;

public sealed class SetAxDataTests
{
    [Fact]
    public async Task Existing_record_forwards_full_business_key_to_authorization()
    {
        var fixture = await CreateFixtureAsync();
        fixture.State.KeyField = "orderdate";
        fixture.State.KeyValue = "05/10/2026";
        fixture.State.RecordId = 42;

        var result = await fixture.Handler.HandleAsync(Edit("qty", 3m), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(new AxRecordKey("orderdate", "05/10/2026", 42), fixture.Authorization.LastSetKey);
    }

    [Fact]
    public async Task Edit_applies_values_and_increments_version_once_with_one_cas_write()
    {
        var fixture = await CreateFixtureAsync();
        var initialWrites = fixture.Store.WriteCount;

        var result = await fixture.Handler.HandleAsync(Edit("qty", 3m), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Response!.Version);
        Assert.Equal(3m, fixture.State.DataContainers["dc2"].Rows.Single(row => row.RowId == "row_existing").Fields["qty"].Value);
        Assert.Equal(initialWrites + 1, fixture.Store.WriteCount);
        Assert.Equal(1, fixture.Store.LastExpectedVersion);
        Assert.Equal(1, fixture.Lock.AcquireCount);
        var envelope = Assert.Single(fixture.Transport.Published);
        Assert.Equal(2, envelope.ObjectVersion);
        Assert.Equal(fixture.State.ObjectId, envelope.ObjectId);
    }

    [Fact]
    public async Task Successful_set_records_safe_stage_counts_and_propagates_correlation()
    {
        var log = new RecordingOperationalEventSink();
        var fixture = await CreateFixtureAsync(operationalLog: log);
        var command = Edit("qty", 3m) with { CorrelationId = "corr-set" };

        var result = await fixture.Handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Contains(log.Events, item => item.Name == "authorization.completed" && item.Outcome == "allowed");
        Assert.Contains(log.Events, item => item.Name == "lock.acquire.completed" && item.Outcome == "acquired");
        Assert.Contains(log.Events, item => item.Name == "validation.completed" && item.Count == 0);
        Assert.Contains(log.Events, item => item.Name == "dependency.evaluation.completed" && item.Count > 0);
        Assert.Contains(log.Events, item => item.Name == "snapshot.publish.completed" && item.Outcome == "stored");
        Assert.Equal("corr-set", Assert.Single(fixture.Transport.Published).CorrelationId);
    }

    [Fact]
    public async Task New_transient_row_is_created_from_definition()
    {
        var fixture = await CreateFixtureAsync();
        var command = new SetAxDataCommand("object-1", 1, false,
        [
            new SetDataContainerValue("dc2",
            [
                new SetRowValue(null, "n",
                [
                    new SetFieldValue("rate", 5m),
                    new SetFieldValue("qty", 2m)
                ])
            ])
        ]);

        var result = await fixture.Handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.Succeeded);
        var created = Assert.Single(result.Response!.CreatedRows);
        Assert.Equal(new SetCreatedRow("dc2", 0, "row_fake_1"), created);
        var row = Assert.Single(fixture.State.DataContainers["dc2"].Rows, row => row.RowId == created.RowId);
        Assert.Equal(AxRowState.New, row.State);
        Assert.Null(row.PersistedRecordId);
        Assert.Equal(10m, row.Fields["amount"].Value);
    }

    [Fact]
    public async Task Delete_marks_persisted_row_deleted_and_increments_once()
    {
        var fixture = await CreateFixtureAsync();
        var command = new SetAxDataCommand("object-1", 1, false,
        [new SetDataContainerValue("dc2", [new SetRowValue("row_existing", "d", [])])]);

        var result = await fixture.Handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Response!.Version);
        Assert.Equal(AxRowState.Deleted, fixture.State.DataContainers["dc2"].Rows.Single(row => row.RowId == "row_existing").State);
    }

    [Fact]
    public async Task Delete_uses_row_id_after_rows_are_reordered()
    {
        var fixture = await CreateFixtureAsync();
        fixture.State.DataContainers["dc2"].Rows.Reverse();
        var command = new SetAxDataCommand("object-1", 1, false,
        [new SetDataContainerValue("dc2", [new SetRowValue("row_existing", "d", [])])]);

        var result = await fixture.Handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(AxRowState.Deleted, fixture.State.DataContainers["dc2"].Rows.Single(row => row.RowId == "row_existing").State);
        Assert.Equal(AxRowState.New, fixture.State.DataContainers["dc2"].Rows.Single(row => row.RowId == "row_new").State);
    }

    [Fact]
    public async Task Invalid_source_skips_its_dependents_and_continues_independent_fields_in_definition_order()
    {
        var fixture = await CreateFixtureAsync();
        var initialWrites = fixture.Store.WriteCount;
        var command = new SetAxDataCommand("object-1", 1, true,
        [
            new SetDataContainerValue("dc2",
            [
                new SetRowValue("row_existing", "e",
                [
                    new SetFieldValue("rate", 6m),
                    new SetFieldValue("qty", "not-a-number")
                ])
            ])
        ]);

        var result = await fixture.Handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Response!.Version);
        var error = Assert.Single(result.Response.Errors);
        Assert.Equal(new FieldAddress("dc2", "row_existing", "qty"), error.Address);
        Assert.Empty(result.Response.Changed);
        Assert.Equal(1m, fixture.State.DataContainers["dc2"].Rows.Single(row => row.RowId == "row_existing").Fields["qty"].Value);
        Assert.Equal(6m, fixture.State.DataContainers["dc2"].Rows.Single(row => row.RowId == "row_existing").Fields["rate"].Value);
        Assert.Equal(4m, fixture.State.DataContainers["dc2"].Rows.Single(row => row.RowId == "row_existing").Fields["amount"].Value);
        Assert.Equal(
            [new FieldAddress("dc2", "row_existing", "rate"), new FieldAddress("dc2", "row_existing", "qty")],
            fixture.FieldEngine.EvaluatedFields);
        Assert.Equal(initialWrites + 1, fixture.Store.WriteCount);
    }

    [Fact]
    public async Task Multiple_fields_use_one_operation_and_one_completion()
    {
        var fixture = await CreateFixtureAsync();
        var command = new SetAxDataCommand("object-1", 1, true,
        [
            new SetDataContainerValue("dc2",
            [
                new SetRowValue("row_existing", "e",
                [
                    new SetFieldValue("qty", 3m),
                    new SetFieldValue("rate", 6m)
                ])
            ])
        ]);

        var result = await fixture.Handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(1, fixture.FieldEngine.OperationCount);
        Assert.Equal(1, fixture.FieldEngine.CompleteCount);
        Assert.Equal(0, fixture.FieldEngine.RebuildCount);
        Assert.Equal(
            [new FieldAddress("dc2", "row_existing", "rate"), new FieldAddress("dc2", "row_existing", "qty")],
            fixture.FieldEngine.EvaluatedFields);
    }

    [Fact]
    public async Task Rejected_field_rebuilds_before_later_independent_field()
    {
        var fixture = await CreateFixtureAsync();
        var command = new SetAxDataCommand("object-1", 1, false,
        [
            new SetDataContainerValue("dc2",
            [
                new SetRowValue("row_existing", "e",
                [
                    new SetFieldValue("rate", "invalid"),
                    new SetFieldValue("qty", 3m)
                ])
            ])
        ]);

        var result = await fixture.Handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Single(result.Response!.Errors);
        Assert.Equal(1, fixture.FieldEngine.OperationCount);
        Assert.Equal(1, fixture.FieldEngine.RebuildCount);
        Assert.Equal(1, fixture.FieldEngine.CompleteCount);
        Assert.Equal(3m, fixture.State.DataContainers["dc2"].Rows
            .Single(row => row.RowId == "row_existing").Fields["qty"].Value);
    }

    [Fact]
    public async Task All_validation_errors_are_collected_without_writing_or_advancing_version()
    {
        var fixture = await CreateFixtureAsync();
        var initialWrites = fixture.Store.WriteCount;
        var command = new SetAxDataCommand("object-1", 1, false,
        [
            new SetDataContainerValue("dc2",
            [
                new SetRowValue("row_existing", "e",
                [
                    new SetFieldValue("rate", "not-a-number"),
                    new SetFieldValue("qty", "also-not-a-number")
                ])
            ])
        ]);

        var result = await fixture.Handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(1, result.Response!.Version);
        Assert.Equal(2, result.Response.Errors.Count);
        Assert.Equal(
            [new FieldAddress("dc2", "row_existing", "rate"), new FieldAddress("dc2", "row_existing", "qty")],
            result.Response.Errors.Select(error => error.Address));
        Assert.Equal(AxRowState.Unchanged, fixture.State.DataContainers["dc2"].Rows.Single(row => row.RowId == "row_existing").State);
        Assert.Equal(initialWrites, fixture.Store.WriteCount);
    }

    [Fact]
    public async Task Get_deps_returns_only_dependency_changes()
    {
        var fixture = await CreateFixtureAsync();

        var result = await fixture.Handler.HandleAsync(Edit("qty", 3m) with { GetDeps = true }, CancellationToken.None);

        var dependency = Assert.Single(result.Response!.Changed);
        Assert.Equal(FieldChangeSource.Dependency, dependency.Source);
        Assert.Equal(new FieldAddress("dc2", "row_existing", "amount"), dependency.Address);
        Assert.Equal(12m, dependency.Value);
    }

    [Fact]
    public async Task Fill_grid_append_adds_transient_rows_and_returns_them_as_dependencies()
    {
        var fixture = await CreateFixtureAsync();
        fixture.FillGridEngine.Result = new(
            true,
            [new FillGridResult(
                true,
                "dc2",
                FillGridMode.Append,
                [new FillGridRow(new Dictionary<string, object?>
                {
                    ["item"] = "filled",
                    ["qty"] = 2m,
                    ["rate"] = 8m,
                    ["amount"] = 16m
                })])]);

        var result = await fixture.Handler.HandleAsync(HeaderEdit("customer", 2m) with { GetDeps = true }, CancellationToken.None);

        Assert.True(result.Succeeded);
        var row = Assert.Single(fixture.State.DataContainers["dc2"].Rows, row => row.RowId == "row_fake_1");
        Assert.Equal(AxRowState.New, row.State);
        Assert.Equal("filled", row.Fields["item"].Value);
        Assert.Contains(new FieldAddress("dc1", "row_header", "customer"), fixture.FillGridEngine.LastChangedParents);
        Assert.Equal(4, result.Response!.Changed.Count);
        Assert.All(result.Response.Changed, change => Assert.Equal(FieldChangeSource.FillGrid, change.Source));
    }

    [Fact]
    public async Task Fill_grid_batch_applies_all_operations_with_one_version_increment_and_one_write()
    {
        var fixture = await CreateFixtureAsync();
        var initialWrites = fixture.Store.WriteCount;
        fixture.FillGridEngine.Result = new(
            true,
            [
                new FillGridResult(
                    true,
                    "dc2",
                    FillGridMode.Append,
                    [new FillGridRow(new Dictionary<string, object?> { ["item"] = "first" })]),
                new FillGridResult(
                    true,
                    "dc2",
                    FillGridMode.Append,
                    [new FillGridRow(new Dictionary<string, object?> { ["item"] = "second" })])
            ]);

        var result = await fixture.Handler.HandleAsync(
            HeaderEdit("customer", 2m) with { GetDeps = true },
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Response!.Version);
        Assert.Equal(initialWrites + 1, fixture.Store.WriteCount);
        Assert.Equal(2, fixture.Transport.Published.Single().ObjectVersion);
        Assert.Contains(fixture.State.DataContainers["dc2"].Rows, row => Equals(row.Fields["item"].Value, "first"));
        Assert.Contains(fixture.State.DataContainers["dc2"].Rows, row => Equals(row.Fields["item"].Value, "second"));
        Assert.Equal(2, result.Response.Changed.Count(change => change.Source == FieldChangeSource.FillGrid));
    }

    [Fact]
    public async Task Fill_grid_replace_clears_existing_rows_before_adding_returned_rows()
    {
        var fixture = await CreateFixtureAsync();
        fixture.FillGridEngine.Result = new(
            true,
            [new FillGridResult(
                true,
                "dc2",
                FillGridMode.Replace,
                [new FillGridRow(new Dictionary<string, object?> { ["item"] = "replacement" })])]);

        var result = await fixture.Handler.HandleAsync(HeaderEdit("customer", 2m), CancellationToken.None);

        Assert.True(result.Succeeded);
        var row = Assert.Single(fixture.State.DataContainers["dc2"].Rows);
        Assert.Equal("row_fake_1", row.RowId);
        Assert.Equal("replacement", row.Fields["item"].Value);
    }

    [Fact]
    public async Task Fill_grid_executes_when_target_grid_is_empty()
    {
        var fixture = await CreateFixtureAsync();
        fixture.State.DataContainers["dc2"].Rows.Clear();
        fixture.FillGridEngine.Result = new(
            true,
            [new FillGridResult(
                true,
                "dc2",
                FillGridMode.Append,
                [new FillGridRow(new Dictionary<string, object?> { ["item"] = "filled" })])]);

        var result = await fixture.Handler.HandleAsync(HeaderEdit("customer", 2m), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(1, fixture.FillGridEngine.CallCount);
        Assert.Single(fixture.State.DataContainers["dc2"].Rows);
    }

    [Fact]
    public async Task Fill_grid_failure_surfaces_dependency_error_without_a_second_write()
    {
        var fixture = await CreateFixtureAsync();
        var initialWrites = fixture.Store.WriteCount;
        fixture.FillGridEngine.Result = new(false, [], "Scripted failure.");

        var result = await fixture.Handler.HandleAsync(HeaderEdit("customer", 2m), CancellationToken.None);

        Assert.Equal("AXD-INFRA-001", result.Error!.Code);
        Assert.Equal(initialWrites, fixture.Store.WriteCount);
    }

    [Theory]
    [InlineData("unknown", "qty")]
    [InlineData("dc2", "unknown")]
    public async Task Unknown_metadata_returns_request_error(string container, string field)
    {
        var fixture = await CreateFixtureAsync();
        var command = new SetAxDataCommand("object-1", 1, false,
        [
            new SetDataContainerValue(container,
            [new SetRowValue("row_existing", "e", [new SetFieldValue(field, 2m)])])
        ]);

        var result = await fixture.Handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal("AXD-REQ-001", result.Error!.Code);
        Assert.Equal(1, fixture.Lock.AcquireCount);
    }

    [Fact]
    public async Task Deleting_unknown_row_returns_request_error()
    {
        var fixture = await CreateFixtureAsync();
        var command = new SetAxDataCommand("object-1", 1, false,
        [new SetDataContainerValue("dc2", [new SetRowValue("row_unknown", "d", [])])]);

        var result = await fixture.Handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal("AXD-REQ-001", result.Error!.Code);
    }

    [Fact]
    public async Task Read_only_field_is_rejected()
    {
        var fixture = await CreateFixtureAsync();
        fixture.Authorization.SetPermission = FakeAxpertAuthorizationProvider.CreateQueryPermission(
            readOnlyFields: FakeAxpertAuthorizationProvider.FieldSets(("dc2", ["qty"])));

        var result = await fixture.Handler.HandleAsync(Edit("qty", 2m), CancellationToken.None);

        Assert.Equal("AXD-AUTH-002", result.Error!.Code);
        Assert.Equal(1, fixture.Lock.AcquireCount);
    }

    [Fact]
    public async Task Hidden_field_is_rejected()
    {
        var fixture = await CreateFixtureAsync();
        fixture.Authorization.SetPermission = FakeAxpertAuthorizationProvider.CreateQueryPermission(
            excludedFields: FakeAxpertAuthorizationProvider.FieldSets(("dc2", ["qty"])));

        var result = await fixture.Handler.HandleAsync(Edit("qty", 2m), CancellationToken.None);

        Assert.Equal("AXD-AUTH-002", result.Error!.Code);
    }

    [Fact]
    public async Task Stale_version_returns_current_version_without_mutation()
    {
        var fixture = await CreateFixtureAsync();
        var command = Edit("qty", 2m) with { ExpectedVersion = 99 };

        var result = await fixture.Handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal("AXD-CONC-001", result.Error!.Code);
        Assert.Equal(1, result.CurrentVersion);
        Assert.Equal(1m, fixture.State.DataContainers["dc2"].Rows.Single(row => row.RowId == "row_existing").Fields["qty"].Value);
    }

    [Fact]
    public async Task Lock_contention_returns_active_conflict()
    {
        var fixture = await CreateFixtureAsync();
        fixture.Lock.CanAcquire = false;

        var result = await fixture.Handler.HandleAsync(Edit("qty", 2m), CancellationToken.None);

        Assert.Equal("AXD-CONC-002", result.Error!.Code);
    }

    [Fact]
    public async Task StructDef_hash_mismatch_fails_closed_without_lock_or_write()
    {
        var fixture = await CreateFixtureAsync(
            currentDefinition: SampleStructDefFixture.Definition with { Version = "changed" });
        var initialWrites = fixture.Store.WriteCount;

        var result = await fixture.Handler.HandleAsync(Edit("qty", 2m), CancellationToken.None);

        Assert.Equal("AXD-CONC-003", result.Error!.Code);
        Assert.Equal(0, fixture.Lock.AcquireCount);
        Assert.Equal(initialWrites, fixture.Store.WriteCount);
        Assert.Empty(fixture.Transport.Published);
        Assert.Equal(1m, fixture.State.DataContainers["dc2"].Rows.Single(row => row.RowId == "row_existing").Fields["qty"].Value);
    }

    [Fact]
    public async Task Cas_race_returns_store_current_version()
    {
        var fixture = await CreateFixtureAsync();
        var store = new StaleOnWriteStore(fixture.State, 7);
        var definitionProvider = new FakeStructDefProvider(SampleStructDefFixture.Definition);
        var transport = new InMemorySnapshotTransport(store);
        var handler = new SetAxDataHandler(
            new FakeUserContext(new AxUserContext(SampleStructDefFixture.TenantId, "owner", new HashSet<string>())),
            store,
            new FakeAxDataLock(),
            definitionProvider,
            fixture.Authorization,
            new FakeAxpertFieldEngine(SampleStructDefFixture.Definition),
            new FakeAxpertFillGridEngine
            {
                Result = new(true, [new FillGridResult(true, "dc2", FillGridMode.Append, [])])
            },
            transport,
            new FakeRowIdGenerator(),
            new FakeClock(DateTimeOffset.UtcNow),
            FakeRecovery.Create(store, fixture.Authorization, definitionProvider));

        var result = await handler.HandleAsync(Edit("qty", 2m), CancellationToken.None);

        Assert.Equal("AXD-CONC-001", result.Error!.Code);
        Assert.Equal(7, result.CurrentVersion);
    }

    [Fact]
    public async Task Unrelated_actor_and_cross_tenant_lookup_do_not_mutate()
    {
        var unrelated = await CreateFixtureAsync(userName: "other");
        unrelated.Authorization.SetPermission = AxQueryPermission.Denied;
        var denied = await unrelated.Handler.HandleAsync(Edit("qty", 2m), CancellationToken.None);

        var crossTenant = await CreateFixtureAsync(actorTenant: "other-tenant");
        var hidden = await crossTenant.Handler.HandleAsync(Edit("qty", 2m), CancellationToken.None);

        Assert.Equal("AXD-AUTH-002", denied.Error!.Code);
        Assert.Equal("AXD-NOTFOUND-001", hidden.Error!.Code);
        Assert.Equal(0, crossTenant.Authorization.SetCallCount);
    }

    [Fact]
    public async Task Unassigned_approver_cannot_set_pending_object()
    {
        var fixture = await CreateFixtureAsync(userName: "approver");
        fixture.State.Workflow = new PegActiveState
        {
            WorkflowInstanceId = "workflow-1",
            CurrentTaskId = "task-1",
            InitiatorUserName = "owner",
            Status = PegWorkflowStatus.Pending,
            ApprovalLevel = 1,
            Approvers = ["someone-else"],
            WorkflowVersion = 1,
            PendingFromUtc = DateTimeOffset.Parse("2026-09-25T08:00:00Z")
        };

        var result = await fixture.Handler.HandleAsync(Edit("qty", 2m), CancellationToken.None);

        Assert.Equal("AXD-AUTH-002", result.Error!.Code);
        Assert.Equal(0, fixture.Lock.AcquireCount);
    }

    private static SetAxDataCommand Edit(string field, object? value) => new(
        "object-1",
        1,
        false,
        [new SetDataContainerValue("dc2", [new SetRowValue("row_existing", "e", [new SetFieldValue(field, value)])])]);

    private static SetAxDataCommand HeaderEdit(string field, object? value) => new(
        "object-1",
        1,
        false,
        [new SetDataContainerValue("dc1", [new SetRowValue("row_header", "e", [new SetFieldValue(field, value)])])]);

    private static async Task<Fixture> CreateFixtureAsync(
        string userName = "owner",
        string actorTenant = SampleStructDefFixture.TenantId,
        StructDefHandle? currentDefinition = null,
        IOperationalEventSink? operationalLog = null)
    {
        var state = SampleStructDefFixture.CreateState();
        state.DataContainers["dc2"].AddRow(new AxDataRow(
            "row_existing",
            AxRowState.Unchanged,
            10,
            new Dictionary<string, AxFieldValue>
            {
                ["item"] = new("persisted"),
                ["qty"] = new(1m),
                ["rate"] = new(4m),
                ["amount"] = new(4m)
            }));
        var store = new InMemoryAxDataStore();
        await store.WriteAsync(state, null, CancellationToken.None);
        var authorization = new FakeAxpertAuthorizationProvider();
        var dataLock = new FakeAxDataLock();
        var fieldEngine = new FakeAxpertFieldEngine(SampleStructDefFixture.Definition);
        var fillGridEngine = new FakeAxpertFillGridEngine
        {
            Result = new(true, [new FillGridResult(true, "dc2", FillGridMode.Append, [])])
        };
        var definitionProvider = new FakeStructDefProvider(currentDefinition ?? SampleStructDefFixture.Definition);
        var transport = new InMemorySnapshotTransport(store);
        var handler = new SetAxDataHandler(
            new FakeUserContext(new AxUserContext(actorTenant, userName, new HashSet<string>())),
            store,
            dataLock,
            definitionProvider,
            authorization,
            fieldEngine,
            fillGridEngine,
            transport,
            new FakeRowIdGenerator(),
            new FakeClock(DateTimeOffset.UtcNow),
            FakeRecovery.Create(store, authorization, definitionProvider),
            operationalLog);
        return new Fixture(handler, state, store, authorization, dataLock, fieldEngine, fillGridEngine, transport);
    }

    private sealed record Fixture(
        SetAxDataHandler Handler,
        AxDataState State,
        InMemoryAxDataStore Store,
        FakeAxpertAuthorizationProvider Authorization,
        FakeAxDataLock Lock,
        FakeAxpertFieldEngine FieldEngine,
        FakeAxpertFillGridEngine FillGridEngine,
        InMemorySnapshotTransport Transport);

    private sealed class StaleOnWriteStore(AxDataState state, long currentVersion) : IAxDataStore
    {
        public Task<AxDataState?> LoadAsync(string tenantId, string objectId, CancellationToken cancellationToken) =>
            Task.FromResult<AxDataState?>(state);

        public Task<AxDataStoreWriteResult> WriteAsync(
            AxDataState updated,
            long? expectedVersion,
            CancellationToken cancellationToken) =>
            Task.FromResult(new AxDataStoreWriteResult(AxDataStoreWriteStatus.StaleVersion, currentVersion));
    }
}
