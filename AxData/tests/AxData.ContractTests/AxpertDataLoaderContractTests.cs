using AxData.Application.Ports;
using AxData.Application.Ports.Axpert;
using AxData.Domain.Lifecycle;
using AxData.Integration.Axpert;
using AxData.Integration.Axpert.AxGet;
using AxData.TestSupport.Fakes;
using AxData.TestSupport.Fixtures;

namespace AxData.ContractTests;

public sealed class AxpertDataLoaderContractTests
{
    private static readonly AxUserContext User = new(
        SampleStructDefFixture.TenantId,
        "owner",
        new HashSet<string>(["employee"], StringComparer.OrdinalIgnoreCase));

    [Fact]
    public void RecordId_is_forwarded_as_the_legacy_permission_key()
    {
        Assert.Equal("recordid", AxpertRecordKeyMapping.Field(new AxRecordKey(null, null, 101)));
        Assert.Equal("101", AxpertRecordKeyMapping.Value(new AxRecordKey(null, null, 101)));
        Assert.Equal("orderid", AxpertRecordKeyMapping.Field(new AxRecordKey("orderid", "ORDER-1", null)));
        Assert.Equal("ORDER-1", AxpertRecordKeyMapping.Value(new AxRecordKey("orderid", "ORDER-1", null)));
    }

    [Fact]
    public async Task Loads_sample_structdef_header_grid_rows_and_baseline_values()
    {
        var source = new StubDataSource(SuccessfulResult());
        var loader = new AxpertDataLoader(source, new FakeRowIdGenerator("row_header", "row_line"));
        var key = new AxRecordKey(null, null, 101);

        var result = await loader.LoadAsync(SampleStructDefFixture.Definition, key, User, default);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.State);
        Assert.Equal(101, result.State.RecordId);
        Assert.Equal(SampleStructDefFixture.Version, source.LastRequest?.PublishedDefinitionVersion);
        Assert.Equal("owner", source.LastRequest?.UserName);
        Assert.Equal(10, result.State.DataContainers["dc1"].Rows.Single().PersistedRecordId);
        Assert.Equal("row_header", result.State.DataContainers["dc1"].Rows.Single().RowId);
        Assert.Equal("ACME", result.State.DataContainers["dc1"].Rows.Single().Fields["customer"].Value);
        var line = result.State.DataContainers["dc2"].Rows.Single();
        Assert.Equal(AxRowState.Unchanged, line.State);
        Assert.Equal(20, line.PersistedRecordId);
        Assert.Equal(2m, line.Fields["qty"].Value);
        Assert.False(line.Fields["qty"].IsDirty);
    }

    [Fact]
    public async Task Translates_key_field_lookup_and_new_rows()
    {
        var source = new StubDataSource(new AxpertDataSourceResult(
            true,
            null,
            [
                new("dc1", [new(null, new Dictionary<string, object?> { ["customer"] = "NEW" })]),
                new("dc2", [])
            ]));
        var loader = new AxpertDataLoader(source, new FakeRowIdGenerator("row_new"));
        var key = new AxRecordKey("order-number", "N-1", null);

        var result = await loader.LoadAsync(SampleStructDefFixture.Definition, key, User, default);

        Assert.True(result.Succeeded);
        Assert.Equal(key, source.LastRequest?.Key);
        var row = result.State!.DataContainers["dc1"].Rows.Single();
        Assert.Equal("row_new", row.RowId);
        Assert.Equal(AxRowState.New, row.State);
    }

    [Fact]
    public async Task Does_not_read_payload_after_axget_failure()
    {
        var loader = new AxpertDataLoader(new StubDataSource(new AxpertDataSourceResult(
            false,
            101,
            null,
            "database unavailable")), new FakeRowIdGenerator());

        var result = await loader.LoadAsync(
            SampleStructDefFixture.Definition,
            new AxRecordKey(null, null, 101),
            User,
            default);

        Assert.False(result.Succeeded);
        Assert.Null(result.State);
        Assert.Equal("database unavailable", result.Error);
    }

    [Theory]
    [InlineData("unknown-container", "customer")]
    [InlineData("dc1", "unknown-field")]
    public async Task Rejects_payload_that_does_not_match_structdef(string container, string field)
    {
        var loader = new AxpertDataLoader(new StubDataSource(new AxpertDataSourceResult(
            true,
            101,
            [new(container, [new(10, new Dictionary<string, object?> { [field] = "value" })])])),
            new FakeRowIdGenerator("row_invalid"));

        var result = await loader.LoadAsync(
            SampleStructDefFixture.Definition,
            new AxRecordKey(null, null, 101),
            User,
            default);

        Assert.False(result.Succeeded);
        Assert.Null(result.State);
    }

    [Fact]
    public async Task Rejects_cross_tenant_definition_before_calling_source()
    {
        var source = new StubDataSource(SuccessfulResult());
        var loader = new AxpertDataLoader(source, new FakeRowIdGenerator());
        var otherTenant = User with { TenantId = "other-tenant" };

        var result = await loader.LoadAsync(
            SampleStructDefFixture.Definition,
            new AxRecordKey(null, null, 101),
            otherTenant,
            default);

        Assert.False(result.Succeeded);
        Assert.Equal(0, source.CallCount);
    }

    private static AxpertDataSourceResult SuccessfulResult() => new(
        true,
        101,
        [
            new("dc1", [new(10, new Dictionary<string, object?> { ["customer"] = "ACME" })]),
            new("dc2", [new(20, new Dictionary<string, object?>
            {
                ["item"] = "widget",
                ["qty"] = 2m,
                ["rate"] = 12.5m,
                ["amount"] = 25m
            })])
        ]);

    private sealed class StubDataSource(AxpertDataSourceResult result) : IAxpertDataSource
    {
        public int CallCount { get; private set; }
        public AxpertDataSourceRequest? LastRequest { get; private set; }

        public Task<AxpertDataSourceResult> LoadAsync(
            AxpertDataSourceRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CallCount++;
            LastRequest = request;
            return Task.FromResult(result);
        }
    }
}
