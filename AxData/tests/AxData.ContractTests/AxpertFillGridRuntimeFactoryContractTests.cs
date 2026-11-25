using AxData.Application.Ports;
using AxData.Integration.Axpert.FieldEngine;
using AxData.Integration.Axpert.FillGrid;
using AxData.Integration.Axpert.Runtime;
using AxData.Integration.Axpert.StructDef;
using AxStructDef.Models;

namespace AxData.ContractTests;

public sealed class AxpertFillGridRuntimeFactoryContractTests
{
    private static readonly AxUserContext User = new(
        "uat2",
        "reviewer",
        new HashSet<string>(StringComparer.OrdinalIgnoreCase));

    [Fact]
    public async Task Runtime_creation_is_attempted_without_preloading_a_second_definition()
    {
        var runtime = new StubRuntimeFactory();
        var factory = new AxpertFillGridRuntimeFactory(
            runtime,
            new AxpertFillGridQueryOptions());

        var result = await factory.CreateAsync(
            new AxpertFillGridRuntimeRequest("uat2", "dtchk", User),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.True(runtime.WasCalled);
    }

    [Fact]
    public async Task Failed_runtime_is_not_consumed()
    {
        var runtime = new StubRuntimeFactory();
        var factory = new AxpertFillGridRuntimeFactory(
            runtime,
            new AxpertFillGridQueryOptions());

        var result = await factory.CreateAsync(
            new AxpertFillGridRuntimeRequest("uat2", "dtchk", User),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.True(runtime.WasCalled);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Real_TFg_AddRows_values_are_preserved_for_the_engine(int addRows)
    {
        var query = AxpertFillGridRuntime.FromLegacy(new TFg
        {
            Fname = "fillgrid1",
            SQLText = "select :category as paycode",
            TargetDcName = "dc2",
            ParamNames = "category,CATEGORY,category",
            Map = ["paycode=paycode"],
            AddRows = addRows,
            AutoShow = true
        });

        Assert.Equal(addRows, query.AddRows);
        Assert.Equal(["category"], query.ParameterNames);
        Assert.True(query.AutoShow);
    }

    [Fact]
    public void Unknown_TFg_AddRows_value_fails_closed()
    {
        var definition = new TFg
        {
            Fname = "fillgrid1",
            SQLText = "select 1 as paycode",
            TargetDcName = "dc2",
            Map = ["paycode=paycode"],
            AddRows = 0
        };

        Assert.Throws<InvalidOperationException>(() => AxpertFillGridRuntime.FromLegacy(definition));
    }

    private sealed class StubRuntimeFactory : IAxpertRuntimeContextFactory
    {
        public bool WasCalled { get; private set; }

        public Task<AxpertOperationResult<AxpertRuntimeContext>> CreateAsync(
            string tenantId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            WasCalled = true;
            return Task.FromResult(
                AxpertOperationResult<AxpertRuntimeContext>.Failure("runtime unavailable"));
        }
    }

}
