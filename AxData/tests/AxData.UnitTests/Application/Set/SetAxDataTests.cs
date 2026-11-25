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
    public async Task Existing_record_forwards_record_id_to_authorization()
    {
        var fixture = await CreateFixtureAsync();
        fixture.State.RecordId = 42;

        var result = await fixture.Handler.HandleAsync(Edit("qty", 3m), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(new AxRecordKey(null, null, 42), fixture.Authorization.LastSetKey);
    }

    [Fact]
    public async Task Edit_applies_values_with_one_owned_write()
    {
        var fixture = await CreateFixtureAsync();
        var initialWrites = fixture.Store.WriteCount;

        var result = await fixture.Handler.HandleAsync(Edit("qty", 3m), CancellationToken.None);

        Assert.True(result.Succeeded);
        var stored = await StoredAsync(fixture);
        Assert.Equal(3m, stored.DataContainers["dc2"].Rows.Single(row => row.RowId == "row_existing").Fields["qty"].Value);
        Assert.Equal(initialWrites + 1, fixture.Store.WriteCount);
        Assert.Equal("fake-lock-token", fixture.Store.LastLockToken);
        Assert.Equal(1, fixture.Lock.AcquireCount);
    }

    [Fact]
    public async Task Stored_source_key_metadata_mismatch_fails_closed_without_write()
    {
        var fixture = await CreateFixtureAsync();
        AxDataContainer lines = fixture.State.DataContainers["dc2"];
        foreach (AxDataRow row in lines.Rows)
        {
            AxFieldValue qty = row.Fields["qty"];
            row.Fields["qty"] = new AxFieldValue(qty.Value, qty.IsReadOnly);
        }
        fixture.State.DataContainers["dc2"] = new AxDataContainer(
            lines.Name,
            lines.IsGrid,
            lines.Rows,
            lines.Fields.Select(field => field.Name == "qty" ? field with { IsSourceKey = false } : field));
        var initialWrites = fixture.Store.WriteCount;

        var result = await fixture.Handler.HandleAsync(Edit("qty", 3m), CancellationToken.None);

        Assert.Equal("AXD-INFRA-002", result.Error!.Code);
        Assert.Equal(initialWrites, fixture.Store.WriteCount);
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
        Assert.Contains(log.Events, item =>
            item.Name == "redis.write.completed" &&
            item.Outcome == "stored" &&
            item.CorrelationId == "corr-set");
        var binding = Assert.Single(log.Events, item => item.Name == "structdef.binding.bypassed");
        Assert.Equal(("set", "current-definition", "corr-set"),
            (binding.Stage, binding.Outcome, binding.CorrelationId));
        Assert.Equal((SampleStructDefFixture.TenantId, SampleStructDefFixture.TransId, "object-1"),
            (binding.TenantId, binding.TransId, binding.ObjectId));
    }

    [Fact]
    public async Task New_transient_row_is_created_from_definition()
    {
        var fixture = await CreateFixtureAsync();
        var command = new SetAxDataCommand("object-1", false,
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
        var row = Assert.Single((await StoredAsync(fixture)).DataContainers["dc2"].Rows, row => row.RowId == created.RowId);
        Assert.Equal(AxRowState.New, row.State);
        Assert.Null(row.PersistedRecordId);
        Assert.Equal(10m, row.Fields["amount"].Value);
    }

    [Fact]
    public async Task Edit_updates_server_issued_unpersisted_row_and_keeps_it_new()
    {
        var fixture = await CreateFixtureAsync();
        var command = new SetAxDataCommand("object-1", false,
        [new SetDataContainerValue("dc2", [new SetRowValue("row_new", "e", [new SetFieldValue("qty", 3m)])])]);

        var result = await fixture.Handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.Succeeded);
        var row = (await StoredAsync(fixture)).DataContainers["dc2"].Rows.Single(candidate => candidate.RowId == "row_new");
        Assert.Equal(AxRowState.New, row.State);
        Assert.Null(row.PersistedRecordId);
        Assert.Equal(3m, row.Fields["qty"].Value);
    }

    [Fact]
    public async Task Edit_updates_empty_init_non_grid_row_and_keeps_it_new()
    {
        var fixture = await CreateFixtureAsync();
        var container = fixture.State.DataContainers["dc1"];
        container.Rows.Clear();
        container.AddRow(new AxDataRow(
            "row_header_new",
            AxRowState.New,
            fields: new Dictionary<string, AxFieldValue>
            {
                ["customer"] = new(null)
            }));
        var command = new SetAxDataCommand("object-1", false,
        [new SetDataContainerValue("dc1", [new SetRowValue("row_header_new", "e", [new SetFieldValue("customer", 2m)])])]);

        var result = await fixture.Handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.Succeeded);
        var row = Assert.Single((await StoredAsync(fixture)).DataContainers["dc1"].Rows);
        Assert.Equal(AxRowState.New, row.State);
        Assert.Null(row.PersistedRecordId);
        Assert.Equal(2m, row.Fields["customer"].Value);
    }

    [Fact]
    public async Task Delete_removes_server_issued_unpersisted_row()
    {
        var fixture = await CreateFixtureAsync();
        var command = new SetAxDataCommand("object-1", false,
        [new SetDataContainerValue("dc2", [new SetRowValue("row_new", "d", [])])]);

        var result = await fixture.Handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.DoesNotContain((await StoredAsync(fixture)).DataContainers["dc2"].Rows, row => row.RowId == "row_new");
    }

    [Fact]
    public async Task Delete_marks_persisted_row_deleted()
    {
        var fixture = await CreateFixtureAsync();
        var command = new SetAxDataCommand("object-1", false,
        [new SetDataContainerValue("dc2", [new SetRowValue("row_existing", "d", [])])]);

        var result = await fixture.Handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(AxRowState.Deleted, (await StoredAsync(fixture)).DataContainers["dc2"].Rows.Single(row => row.RowId == "row_existing").State);
    }

    [Fact]
    public async Task Delete_uses_row_id_after_rows_are_reordered()
    {
        var fixture = await CreateFixtureAsync();
        fixture.State.DataContainers["dc2"].Rows.Reverse();
        var command = new SetAxDataCommand("object-1", false,
        [new SetDataContainerValue("dc2", [new SetRowValue("row_existing", "d", [])])]);

        var result = await fixture.Handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.Succeeded);
        var stored = await StoredAsync(fixture);
        Assert.Equal(AxRowState.Deleted, stored.DataContainers["dc2"].Rows.Single(row => row.RowId == "row_existing").State);
        Assert.Equal(AxRowState.New, stored.DataContainers["dc2"].Rows.Single(row => row.RowId == "row_new").State);
    }

    [Fact]
    public async Task Invalid_source_skips_its_dependents_and_continues_independent_fields_in_definition_order()
    {
        var fixture = await CreateFixtureAsync();
        var initialWrites = fixture.Store.WriteCount;
        var command = new SetAxDataCommand("object-1", true,
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
        var stored = await StoredAsync(fixture);
        var error = Assert.Single(result.Response!.Errors);
        Assert.Equal(new FieldAddress("dc2", "row_existing", "qty"), error.Address);
        Assert.Empty(result.Response.Changed);
        Assert.Equal(1m, stored.DataContainers["dc2"].Rows.Single(row => row.RowId == "row_existing").Fields["qty"].Value);
        Assert.Equal(6m, stored.DataContainers["dc2"].Rows.Single(row => row.RowId == "row_existing").Fields["rate"].Value);
        Assert.Equal(4m, stored.DataContainers["dc2"].Rows.Single(row => row.RowId == "row_existing").Fields["amount"].Value);
        Assert.Equal(
            [new FieldAddress("dc2", "row_existing", "rate"), new FieldAddress("dc2", "row_existing", "qty")],
            fixture.FieldEngine.EvaluatedFields);
        Assert.Equal(initialWrites + 1, fixture.Store.WriteCount);
    }

    [Fact]
    public async Task Invalid_source_skips_transitive_submitted_dependents()
    {
        var baseDefinition = SampleStructDefFixture.Definition;
        var definition = baseDefinition with
        {
            DataContainers = baseDefinition.DataContainers
                .Select(container => string.Equals(container.Name, "dc2", StringComparison.OrdinalIgnoreCase)
                    ? container with { EvaluationOrder = ["item", "qty", "amount", "rate"] }
                    : container)
                .ToArray(),
            Dependencies =
            [
                new FieldDependency(
                    "dc2", "qty", "dc2", "amount", FieldDependencyKind.Expression,
                    DeclarationOrder: 1, SourceFrameNo: 2, TargetFrameNo: 2,
                    SourceIsGrid: true, TargetIsGrid: true),
                new FieldDependency(
                    "dc2", "amount", "dc2", "rate", FieldDependencyKind.Expression,
                    DeclarationOrder: 2, SourceFrameNo: 2, TargetFrameNo: 2,
                    SourceIsGrid: true, TargetIsGrid: true)
            ]
        };
        var fixture = await CreateFixtureAsync(currentDefinition: definition);
        var initialWrites = fixture.Store.WriteCount;
        var command = new SetAxDataCommand("object-1", false,
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
        Assert.Single(result.Response!.Errors);
        Assert.Equal(
            [new FieldAddress("dc2", "row_existing", "qty")],
            fixture.FieldEngine.EvaluatedFields);
        Assert.Equal(4m, fixture.State.DataContainers["dc2"].Rows
            .Single(row => row.RowId == "row_existing").Fields["rate"].Value);
        Assert.Equal(initialWrites, fixture.Store.WriteCount);
    }

    [Fact]
    public async Task Multiple_fields_use_one_operation_and_one_completion()
    {
        var fixture = await CreateFixtureAsync();
        var command = new SetAxDataCommand("object-1", true,
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
        Assert.Equal(1, fixture.Store.LoadCount);
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
        var command = new SetAxDataCommand("object-1", false,
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
        Assert.Equal(3m, (await StoredAsync(fixture)).DataContainers["dc2"].Rows
            .Single(row => row.RowId == "row_existing").Fields["qty"].Value);
    }

    [Fact]
    public async Task All_validation_errors_are_collected_without_writing()
    {
        var fixture = await CreateFixtureAsync();
        var initialWrites = fixture.Store.WriteCount;
        var command = new SetAxDataCommand("object-1", false,
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
        Assert.Equal(2, result.Response!.Errors.Count);
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
        var row = Assert.Single((await StoredAsync(fixture)).DataContainers["dc2"].Rows, row => row.RowId == "row_fake_1");
        Assert.Equal(AxRowState.New, row.State);
        Assert.Equal("filled", row.Fields["item"].Value);
        Assert.Contains(new FieldAddress("dc1", "row_header", "customer"), fixture.FillGridEngine.LastChangedParents);
        Assert.Equal(4, result.Response!.Changed.Count);
        Assert.All(result.Response.Changed, change => Assert.Equal(FieldChangeSource.FillGrid, change.Source));
    }

    [Fact]
    public async Task Fill_grid_batch_applies_all_operations_with_one_write()
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
        Assert.Equal(initialWrites + 1, fixture.Store.WriteCount);
        var stored = await StoredAsync(fixture);
        Assert.Contains(stored.DataContainers["dc2"].Rows, row => Equals(row.Fields["item"].Value, "first"));
        Assert.Contains(stored.DataContainers["dc2"].Rows, row => Equals(row.Fields["item"].Value, "second"));
        Assert.Equal(2, result.Response!.Changed.Count(change => change.Source == FieldChangeSource.FillGrid));
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
        var row = Assert.Single((await StoredAsync(fixture)).DataContainers["dc2"].Rows);
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
        Assert.Single((await StoredAsync(fixture)).DataContainers["dc2"].Rows);
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

    [Fact]
    public async Task Saving_blocks_Set_but_retained_finalization_returns_to_mutable_Active()
    {
        var fixture = await CreateFixtureAsync();
        fixture.State.BeginSaving();

        var blocked = await fixture.Handler.HandleAsync(HeaderEdit("customer", "blocked"), CancellationToken.None);

        Assert.Equal("AXD-CONC-002", blocked.Error!.Code);
        fixture.State.FinalizeSaving(retainDataObject: true);
        await fixture.Store.WriteAsync(fixture.State, "save-claim-token", CancellationToken.None);
        var updated = await fixture.Handler.HandleAsync(HeaderEdit("customer", 2m), CancellationToken.None);

        Assert.True(updated.Succeeded);
        Assert.Equal(AxDataLifecycleStatus.Active, fixture.State.Status);
    }

    [Fact]
    public async Task Set_tracks_the_clean_baseline_and_revert_removes_dirty_metadata()
    {
        var fixture = await CreateFixtureAsync();
        var changed = await fixture.Handler.HandleAsync(Edit("qty", 2m), CancellationToken.None);

        Assert.True(changed.Succeeded);
        var row = (await StoredAsync(fixture)).DataContainers["dc2"].Rows.Single(candidate => candidate.RowId == "row_existing");
        Assert.True(row.Fields["qty"].IsDirty);
        Assert.Equal(1m, row.Fields["qty"].OriginalValue);

        var reverted = await fixture.Handler.HandleAsync(Edit("qty", 1m), CancellationToken.None);

        Assert.True(reverted.Succeeded);
        row = (await StoredAsync(fixture)).DataContainers["dc2"].Rows.Single(candidate => candidate.RowId == "row_existing");
        Assert.False(row.Fields["qty"].IsDirty);
        Assert.False(row.Fields["amount"].IsDirty);
    }

    [Theory]
    [InlineData("unknown", "qty")]
    [InlineData("dc2", "unknown")]
    public async Task Unknown_metadata_returns_request_error(string container, string field)
    {
        var fixture = await CreateFixtureAsync();
        var command = new SetAxDataCommand("object-1", false,
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
        var command = new SetAxDataCommand("object-1", false,
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
    public async Task Lock_contention_returns_active_conflict()
    {
        var fixture = await CreateFixtureAsync();
        fixture.Lock.CanAcquire = false;

        var result = await fixture.Handler.HandleAsync(Edit("qty", 2m), CancellationToken.None);

        Assert.Equal("AXD-CONC-002", result.Error!.Code);
    }

    [Fact]
    public async Task Lost_lock_ownership_returns_active_conflict()
    {
        var fixture = await CreateFixtureAsync();
        var store = new OwnershipLostStore(fixture.State);
        var fillGridEngine = new FakeAxpertFillGridEngine
        {
            Result = new(true, [new FillGridResult(true, "dc2", FillGridMode.Append, [])])
        };
        var handler = new SetAxDataHandler(
            new FakeUserContext(new AxUserContext(SampleStructDefFixture.TenantId, "owner", new HashSet<string>())),
            store,
            new FakeAxDataLock(),
            fixture.Authorization,
            new FakeAxpertFieldEngine(SampleStructDefFixture.Definition, fillGridEngine),
            new FakeRowIdGenerator());

        var result = await handler.HandleAsync(Edit("qty", 2m), CancellationToken.None);

        Assert.Equal("AXD-CONC-002", result.Error!.Code);
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

    [Theory]
    [InlineData("viewer")]
    [InlineData("admin")]
    public async Task Viewer_and_former_admin_role_cannot_set_another_initiators_object(string role)
    {
        var fixture = await CreateFixtureAsync(
            userName: "other",
            roles: new HashSet<string>([role], StringComparer.OrdinalIgnoreCase));

        var result = await fixture.Handler.HandleAsync(Edit("qty", 2m), CancellationToken.None);

        Assert.Equal("AXD-AUTH-002", result.Error!.Code);
        Assert.Equal(0, fixture.Lock.AcquireCount);
    }

    [Fact]
    public async Task Current_approver_cannot_set_an_active_object()
    {
        var fixture = await CreateFixtureAsync(userName: "approver");
        fixture.State.Workflow = new PegActiveState
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

        var result = await fixture.Handler.HandleAsync(Edit("qty", 2m), CancellationToken.None);

        Assert.Equal("AXD-AUTH-002", result.Error!.Code);
        Assert.Equal(0, fixture.Lock.AcquireCount);
    }

    private static SetAxDataCommand Edit(string field, object? value) => new(
        "object-1",
        false,
        [new SetDataContainerValue("dc2", [new SetRowValue("row_existing", "e", [new SetFieldValue(field, value)])])]);

    private static SetAxDataCommand HeaderEdit(string field, object? value) => new(
        "object-1",
        false,
        [new SetDataContainerValue("dc1", [new SetRowValue("row_header", "e", [new SetFieldValue(field, value)])])]);

    private static async Task<AxDataState> StoredAsync(Fixture fixture) =>
        (await fixture.Store.LoadAsync(
            fixture.State.TenantId,
            fixture.State.ObjectId,
            CancellationToken.None))!;

    private static async Task<Fixture> CreateFixtureAsync(
        string userName = "owner",
        string actorTenant = SampleStructDefFixture.TenantId,
        StructDefHandle? currentDefinition = null,
        IOperationalEventSink? operationalLog = null,
        IReadOnlySet<string>? roles = null)
    {
        var state = SampleStructDefFixture.CreateState();
        state.DataContainers["dc2"].AddRow(new AxDataRow(
            "row_existing",
            AxRowState.Unchanged,
            10,
            new Dictionary<string, AxFieldValue>
            {
                ["item"] = new("persisted"),
                ["qty"] = new(1m, isSourceKey: true),
                ["rate"] = new(4m),
                ["amount"] = new(4m)
            }));
        var store = new InMemoryAxDataStore();
        await store.WriteAsync(state, null, CancellationToken.None);
        var authorization = new FakeAxpertAuthorizationProvider();
        var dataLock = new FakeAxDataLock();
        var fillGridEngine = new FakeAxpertFillGridEngine
        {
            Result = new(true, [new FillGridResult(true, "dc2", FillGridMode.Append, [])])
        };
        var fieldEngine = new FakeAxpertFieldEngine(
            currentDefinition ?? SampleStructDefFixture.Definition,
            fillGridEngine);
        var handler = new SetAxDataHandler(
            new FakeUserContext(new AxUserContext(actorTenant, userName, roles ?? new HashSet<string>())),
            store,
            dataLock,
            authorization,
            fieldEngine,
            new FakeRowIdGenerator(),
            operationalLog);
        return new Fixture(handler, state, store, authorization, dataLock, fieldEngine, fillGridEngine);
    }

    private sealed record Fixture(
        SetAxDataHandler Handler,
        AxDataState State,
        InMemoryAxDataStore Store,
        FakeAxpertAuthorizationProvider Authorization,
        FakeAxDataLock Lock,
        FakeAxpertFieldEngine FieldEngine,
        FakeAxpertFillGridEngine FillGridEngine);

    private sealed class OwnershipLostStore(AxDataState state) : IAxDataStore
    {
        public Task<AxDataState?> LoadAsync(string tenantId, string objectId, CancellationToken cancellationToken) =>
            Task.FromResult<AxDataState?>(state);

        public Task<AxDataStoreWriteResult> WriteAsync(
            AxDataState updated,
            string? lockToken,
            CancellationToken cancellationToken) =>
            Task.FromResult(new AxDataStoreWriteResult(AxDataStoreWriteStatus.Conflict));
    }
}
