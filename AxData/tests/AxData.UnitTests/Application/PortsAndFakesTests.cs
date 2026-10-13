using AxData.Application.Ports;
using AxData.Application.Ports.Axpert;
using AxData.TestSupport.Fakes;
using AxData.TestSupport.Fixtures;

namespace AxData.UnitTests.Application;

public sealed class PortsAndFakesTests
{
    private static readonly AxUserContext User = new(
        SampleStructDefFixture.TenantId,
        "owner",
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "users" });

    [Fact]
    public async Task Setting_quantity_returns_the_scripted_dependency_change()
    {
        var state = SampleStructDefFixture.CreateState();
        var engine = new FakeAxpertFieldEngine(SampleStructDefFixture.Definition);
        var quantity = new FieldAddress("dc2", "row_new", "qty");

        var result = await engine.SetValidateAndRefreshAsync(
            state,
            quantity,
            3m,
            User,
            CancellationToken.None);

        Assert.True(result.Succeeded);
        var dependency = Assert.Single(result.Changes, change => change.Source == FieldChangeSource.Dependency);
        Assert.Equal(new FieldAddress("dc2", "row_new", "amount"), dependency.Address);
        Assert.Equal(37.50m, dependency.Value);
        Assert.Equal(37.50m, state.DataContainers["dc2"].Rows[0].Fields["amount"].Value);
    }

    [Fact]
    public async Task Failed_submission_returns_no_computed_or_stale_changes()
    {
        var state = SampleStructDefFixture.CreateState();
        var engine = new FakeAxpertFieldEngine(SampleStructDefFixture.Definition);

        var result = await engine.SetValidateAndRefreshAsync(
            state,
            new FieldAddress("dc2", "row_new", "qty"),
            "not-a-number",
            User,
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Empty(result.Changes);
        Assert.Single(result.Errors);
        Assert.Equal(12.50m, state.DataContainers["dc2"].Rows[0].Fields["amount"].Value);
    }

    [Fact]
    public async Task In_memory_store_scopes_object_ids_by_tenant_and_checks_versions()
    {
        var store = new InMemoryAxDataStore();
        var state = SampleStructDefFixture.CreateState();

        var stored = await store.WriteAsync(state, null, CancellationToken.None);
        var anotherTenant = await store.LoadAsync("another-tenant", state.ObjectId, CancellationToken.None);
        var stale = await store.WriteAsync(state, 99, CancellationToken.None);

        Assert.Equal(AxDataStoreWriteStatus.Stored, stored.Status);
        Assert.Null(anotherTenant);
        Assert.Equal(AxDataStoreWriteStatus.StaleVersion, stale.Status);
        Assert.Equal(1, stale.CurrentVersion);
    }

    [Fact]
    public void Sample_definition_contains_non_grid_grid_dependency_and_fill_grid()
    {
        var definition = SampleStructDefFixture.Definition;

        Assert.Contains(definition.DataContainers, container => !container.IsGrid);
        Assert.Contains(definition.DataContainers, container => container.IsGrid);
        Assert.Single(definition.Dependencies);
        Assert.Single(definition.FillGrids);
    }
}
