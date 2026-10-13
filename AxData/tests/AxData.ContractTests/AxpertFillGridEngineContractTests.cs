using AxData.Application.Ports;
using AxData.Application.Ports.Axpert;
using AxData.Domain.Lifecycle;
using AxData.Domain.Model;
using AxData.Integration.Axpert.FieldEngine;
using AxData.Integration.Axpert.FillGrid;

namespace AxData.ContractTests;

public sealed class AxpertFillGridEngineContractTests
{
    private static readonly AxUserContext User = new(
        "sample-tenant",
        "owner",
        new HashSet<string>(StringComparer.OrdinalIgnoreCase));

    [Fact]
    public async Task Append_maps_query_columns_and_binds_current_parent_value()
    {
        var executor = new StubExecutor(
            new Dictionary<string, object?> { ["PAYCODE"] = "BASIC", ["execution_order"] = 10 });
        var adapter = CreateAdapter(executor, addRows: 3);
        var state = CreateState(withTargetRow: true);

        var result = await adapter.ExecuteApplicableAsync(
            state,
            [new FieldAddress("dc1", "row_header", "npaycatid")],
            User,
            CancellationToken.None);

        Assert.True(result.Succeeded, result.Error);
        var operation = Assert.Single(result.Operations);
        Assert.Equal(FillGridMode.Append, operation.Mode);
        var row = Assert.Single(operation.Rows);
        Assert.Equal("BASIC", row.Values["paycode"]);
        Assert.Equal(10, row.Values["rvar"]);
        Assert.Equal(42, executor.LastParameters!["npaycatid"]);
        Assert.Single(state.DataContainers["dc2"].Rows);
    }

    [Fact]
    public async Task Replace_returns_declarative_rows_without_mutating_state()
    {
        var executor = new StubExecutor(
            new Dictionary<string, object?> { ["paycode"] = "HRA", ["execution_order"] = 20 });
        var adapter = CreateAdapter(executor, addRows: 2);
        var state = CreateState(withTargetRow: true);
        var existing = state.DataContainers["dc2"].Rows[0];

        var result = await adapter.ExecuteApplicableAsync(
            state,
            [new FieldAddress("dc1", "row_header", "npaycatid")],
            User,
            CancellationToken.None);

        Assert.True(result.Succeeded, result.Error);
        var operation = Assert.Single(result.Operations);
        Assert.Equal(FillGridMode.Replace, operation.Mode);
        Assert.Equal("HRA", Assert.Single(operation.Rows).Values["paycode"]);
        Assert.Same(existing, Assert.Single(state.DataContainers["dc2"].Rows));
    }

    [Fact]
    public async Task Add_only_when_empty_is_not_applicable_to_populated_grid()
    {
        var executor = new StubExecutor();
        var adapter = CreateAdapter(executor, addRows: 1);

        var result = await adapter.ExecuteApplicableAsync(
            CreateState(withTargetRow: true),
            [new FieldAddress("dc1", "row_header", "npaycatid")],
            User,
            CancellationToken.None);

        Assert.True(result.Succeeded, result.Error);
        Assert.Empty(result.Operations);
        Assert.Null(executor.LastParameters);
    }

    [Fact]
    public async Task Add_only_when_empty_returns_append_for_an_empty_target()
    {
        var executor = new StubExecutor(
            new Dictionary<string, object?> { ["paycode"] = "BASIC", ["execution_order"] = 10 });
        var adapter = CreateAdapter(executor, addRows: 1);

        var result = await adapter.ExecuteApplicableAsync(
            CreateState(withTargetRow: false),
            [new FieldAddress("dc1", "row_header", "npaycatid")],
            User,
            CancellationToken.None);

        Assert.True(result.Succeeded, result.Error);
        Assert.Equal(FillGridMode.Append, Assert.Single(result.Operations).Mode);
        Assert.Equal(1, executor.CallCount);
    }

    [Fact]
    public async Task Missing_mapped_column_fails_without_mutating_state()
    {
        var executor = new StubExecutor(new Dictionary<string, object?> { ["paycode"] = "BASIC" });
        var adapter = CreateAdapter(executor, addRows: 3);
        var state = CreateState(withTargetRow: true);

        var result = await adapter.ExecuteApplicableAsync(
            state,
            [new FieldAddress("dc1", "row_header", "npaycatid")],
            User,
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Single(state.DataContainers["dc2"].Rows);
    }

    [Fact]
    public async Task Multiple_applicable_definitions_for_distinct_targets_are_returned_in_metadata_order()
    {
        var executor = new StubExecutor(
            new Dictionary<string, object?> { ["paycode"] = "BASIC", ["execution_order"] = 10 });
        var state = CreateState(withTargetRow: true);
        state.DataContainers.Add("bonuses", new AxDataContainer("bonuses", true, []));
        var first = Query("fillgrid1", "dc2");
        var second = Query("fillgrid2", "bonuses");
        var adapter = new AxpertFillGridEngine(
            new StubRuntimeFactory(new AxpertFillGridRuntime([first, second], executor)));

        var result = await adapter.ExecuteApplicableAsync(
            state,
            [new FieldAddress("dc1", "row_header", "npaycatid")],
            User,
            CancellationToken.None);

        Assert.True(result.Succeeded, result.Error);
        Assert.Collection(
            result.Operations,
            operation => Assert.Equal("dc2", operation.TargetContainer),
            operation => Assert.Equal("bonuses", operation.TargetContainer));
        Assert.Equal(2, executor.CallCount);
    }

    [Fact]
    public async Task Multiple_applicable_definitions_for_the_same_target_fail_before_querying()
    {
        var executor = new StubExecutor();
        var adapter = new AxpertFillGridEngine(
            new StubRuntimeFactory(new AxpertFillGridRuntime(
                [Query("fillgrid1", "dc2"), Query("fillgrid2", "dc2")],
                executor)));

        var result = await adapter.ExecuteApplicableAsync(
            CreateState(withTargetRow: true),
            [new FieldAddress("dc1", "row_header", "npaycatid")],
            User,
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(0, executor.CallCount);
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, true, false)]
    [InlineData(true, false, true)]
    public async Task Manual_or_interactive_definitions_are_not_automatically_executed(
        bool autoShow,
        bool multiSelect,
        bool fromIView)
    {
        var executor = new StubExecutor();
        var query = Query("fillgrid1", "dc2") with
        {
            AutoShow = autoShow,
            FirmBind = false,
            MultiSelect = multiSelect,
            FromIView = fromIView
        };
        var adapter = new AxpertFillGridEngine(
            new StubRuntimeFactory(new AxpertFillGridRuntime([query], executor)));

        var result = await adapter.ExecuteApplicableAsync(
            CreateState(withTargetRow: false),
            [new FieldAddress("dc1", "row_header", "npaycatid")],
            User,
            CancellationToken.None);

        Assert.True(result.Succeeded, result.Error);
        Assert.Empty(result.Operations);
        Assert.Equal(0, executor.CallCount);
    }

    private static AxpertFillGridEngine CreateAdapter(StubExecutor executor, int addRows)
    {
        var query = Query("fillgrid1", "dc2") with { AddRows = addRows };
        return new AxpertFillGridEngine(new StubRuntimeFactory(new AxpertFillGridRuntime([query], executor)));
    }

    private static AxpertFillGridQuery Query(string name, string target) => new(
        name,
        "select paycode, execution_order from paycodes where category = :npaycatid",
        "dc1",
        target,
        ["npaycatid"],
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["paycode"] = "paycode",
            ["rvar"] = "execution_order"
        },
        3,
        true,
        false);

    private static AxDataState CreateState(bool withTargetRow)
    {
        var state = new AxDataState(
            "object-1",
            "sample-tenant",
            "sample-order",
            "owner",
            "owner",
            "1",
            DateTimeOffset.UnixEpoch);
        state.DataContainers.Add(
            "dc1",
            new AxDataContainer(
                "dc1",
                false,
                [new AxDataRow("row_header", AxRowState.Unchanged, 1, new Dictionary<string, AxFieldValue>
                {
                    ["npaycatid"] = new(42)
                })]));
        state.DataContainers.Add(
            "dc2",
            new AxDataContainer(
                "dc2",
                true,
                withTargetRow
                    ? [new AxDataRow("row_existing", AxRowState.Unchanged, 2, new Dictionary<string, AxFieldValue>
                    {
                        ["paycode"] = new("OLD"),
                        ["rvar"] = new(1)
                    })]
                    : []));
        return state;
    }

    private sealed class StubRuntimeFactory(AxpertFillGridRuntime runtime) : IAxpertFillGridRuntimeFactory
    {
        public Task<AxpertOperationResult<AxpertFillGridRuntime>> CreateAsync(
            AxpertFillGridRuntimeRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(AxpertOperationResult<AxpertFillGridRuntime>.Success(runtime));
        }
    }

    private sealed class StubExecutor(params IReadOnlyDictionary<string, object?>[] rows)
        : IAxpertFillGridQueryExecutor
    {
        public IReadOnlyDictionary<string, object?>? LastParameters { get; private set; }

        public int CallCount { get; private set; }

        public Task<AxpertOperationResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>> ExecuteAsync(
            AxpertFillGridQuery query,
            IReadOnlyDictionary<string, object?> parameters,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CallCount++;
            LastParameters = parameters;
            return Task.FromResult(AxpertOperationResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>.Success(rows));
        }
    }
}
