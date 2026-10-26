using ARMCommon.Interface;
using ARMCommon.Services;
using AxData.Api.Production;
using AxData.Application.Ports;
using AxData.Application.Ports.Axpert;
using AxData.Integration.Axpert.AxGet;
using AxData.Integration.Axpert.FieldEngine;
using AxData.Integration.Axpert.FillGrid;
using AxData.Integration.Axpert.Runtime;
using AxData.Integration.Axpert.StructDef;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AxData.ContractTests;

public sealed class ProductionCompositionTests
{
    [Fact]
    public void Production_registers_live_session_and_all_real_Axpert_factories()
    {
        var services = new ServiceCollection();
        services.AddHttpContextAccessor();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["AxData:Redis:ConnectionString"] = "localhost:6385",
                ["AxData:Postgres:ConnectionString"] = "Host=localhost;Database=axdata;Username=axdata"
            }).Build();

        services.AddProductionServices(configuration);

        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IAxpertSessionResolver) &&
            descriptor.Lifetime == ServiceLifetime.Scoped);
        Assert.Contains(services, Registration<IAxpertRuntimeContextFactory, ProductionAxpertRuntimeContextFactory>);
        Assert.Contains(services, Registration<IStructDefProvider, AxpertStructDefProvider>);
        Assert.Contains(services, Registration<IAxpertDataSource, AxpertDataSource>);
        Assert.Contains(services, Registration<IAxpertFieldRuntimeFactory, AxpertFieldRuntimeFactory>);
        Assert.Contains(services, Registration<IAxpertFillGridRuntimeFactory, AxpertFillGridRuntimeFactory>);
        Assert.Contains(services, Registration<IAppSettingsService, AppSettingsService>);
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IARMLogTraceService));
    }

    private static bool Registration<TService, TImplementation>(ServiceDescriptor descriptor) =>
        descriptor.ServiceType == typeof(TService) &&
        descriptor.ImplementationType == typeof(TImplementation);
}
