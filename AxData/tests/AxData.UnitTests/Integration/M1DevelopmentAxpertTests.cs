using AxData.Application.Ports;
using AxData.Application.Ports.Axpert;
using AxData.Integration.Axpert.Development;
using AxData.Integration.Axpert.FillGrid;
using AxData.TestSupport.Fakes;

namespace AxData.UnitTests.Integration;

public sealed class M1DevelopmentAxpertTests
{
    private static readonly M1DevelopmentAxpertOptions Options = new();
    private static readonly AxUserContext User = new(
        Options.TenantId,
        "owner",
        new HashSet<string>(StringComparer.OrdinalIgnoreCase),
        IsAdministrator: false);

    [Fact]
    public async Task Fixture_load_and_dependency_support_the_M1_walkthrough()
    {
        var provider = new M1DevelopmentStructDefProvider(Options);
        var definition = await provider.GetAsync(Options.TenantId, Options.TransId, CancellationToken.None);
        Assert.NotNull(definition);

        var loader = new M1DevelopmentDataLoader(new FakeRowIdGenerator("row_header"));
        var loaded = await loader.LoadAsync(
            definition!,
            new AxRecordKey(null, null, null),
            User,
            CancellationToken.None);
        Assert.True(loaded.Succeeded);

        var state = loaded.State!;
        var lines = state.DataContainers["dc2"];
        var row = new AxData.Domain.Model.AxDataRow(
            "row_new",
            AxData.Domain.Lifecycle.AxRowState.New,
            fields: definition.DataContainers.Single(container => container.Name == "dc2").Fields.ToDictionary(
                field => field.Name,
                field => new AxData.Domain.Model.AxFieldValue(null, field.IsReadOnly),
                StringComparer.OrdinalIgnoreCase));
        lines.AddRow(row);

        var engine = new M1DevelopmentFieldEngine();
        await engine.SetValidateAndRefreshAsync(
            state,
            new FieldAddress("dc2", "row_new", "rate"),
            12.5m,
            User,
            CancellationToken.None);
        var result = await engine.SetValidateAndRefreshAsync(
            state,
            new FieldAddress("dc2", "row_new", "qty"),
            2m,
            User,
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Contains(result.Changes, change =>
            change.Source == FieldChangeSource.Dependency && Equals(change.Value, 25.0m));
    }

    [Fact]
    public async Task Unknown_tenant_is_denied()
    {
        var authorization = new M1DevelopmentAuthorizationProvider(Options);
        var permission = await authorization.GetQueryPermissionAsync(
            "other-tenant",
            Options.TransId,
            new AxRecordKey(null, null, null),
            User,
            CancellationToken.None);

        Assert.False(permission.IsAllowed);
    }

    [Fact]
    public async Task Calculated_amount_is_exposed_as_read_only()
    {
        var authorization = new M1DevelopmentAuthorizationProvider(Options);
        var permission = await authorization.GetQueryPermissionAsync(
            Options.TenantId,
            Options.TransId,
            new AxRecordKey(null, null, null),
            User,
            CancellationToken.None);

        Assert.Contains("amount", permission.ReadOnlyFields["dc2"]);
    }

    [Fact]
    public async Task Real_fill_grid_adapter_runs_against_the_M1_runtime_boundary()
    {
        var adapter = new AxpertFillGridEngine(new M1DevelopmentFillGridRuntimeFactory());
        var state = (await new M1DevelopmentDataLoader(new FakeRowIdGenerator("row_header")).LoadAsync(
            (await new M1DevelopmentStructDefProvider(Options).GetAsync(
                Options.TenantId,
                Options.TransId,
                CancellationToken.None))!,
            new AxRecordKey(null, null, null),
            User,
            CancellationToken.None)).State!;

        var result = await adapter.ExecuteApplicableAsync(
            state,
            [new FieldAddress("dc1", "row_header", "customer")],
            User,
            CancellationToken.None);

        Assert.True(result.Succeeded, result.Error);
        Assert.Empty(result.Operations);
    }
}
