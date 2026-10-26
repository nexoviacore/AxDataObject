using AxData.Api.Development;
using AxData.Application.Ports.Axpert;
using AxData.Integration.Axpert.AxGet;
using AxData.Integration.Axpert.Development;
using AxData.Integration.Axpert.FieldEngine;
using AxData.Integration.Axpert.FillGrid;
using AxData.Integration.Axpert.Runtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace AxData.ContractTests;

public sealed class RealDataReviewCompositionTests
{
    [Fact]
    public void Real_review_is_rejected_outside_development()
    {
        var services = new ServiceCollection();
        var configuration = Configuration(("AxData:RealDataReview:Enabled", "true"));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddRealDataReviewMode(configuration, Environment("Production")));

        Assert.Contains("Development", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Real_review_is_rejected_for_non_allowlisted_host()
    {
        var services = new ServiceCollection();
        var configuration = ValidRealReviewConfiguration(
            ("AxData:RealDataReview:ConnectionString", "Host=untrusted;Database=axpert;Username=review"));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddRealDataReviewMode(configuration, Environment(Environments.Development)));

        Assert.Contains("allowlisted", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Real_review_startup_is_rejected_for_writable_connection()
    {
        var service = new RealDataReviewStartupService(new StubStartupGuard(isReadOnly: false));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.StartAsync(CancellationToken.None));

        Assert.Contains("writable", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Real_review_registers_real_adapters()
    {
        var services = new ServiceCollection();
        services.AddHttpContextAccessor();

        var enabled = services.AddRealDataReviewMode(
            ValidRealReviewConfiguration(),
            Environment(Environments.Development));

        Assert.True(enabled);
        Assert.Contains(services, IsRegistration<IAxpertDataLoader, AxpertDataLoader>);
        Assert.Contains(services, IsRegistration<IAxpertFieldEngine, AxpertFieldEngine>);
        Assert.Contains(services, IsRegistration<IAxpertFillGridRuntimeFactory, AxpertFillGridRuntimeFactory>);
        var options = Assert.IsType<AxpertFillGridQueryOptions>(services.Single(
            descriptor => descriptor.ServiceType == typeof(AxpertFillGridQueryOptions)).ImplementationInstance);
        Assert.Equal(5_000, options.StatementTimeoutMilliseconds);
        Assert.Equal(1_000, options.MaximumRows);
        Assert.DoesNotContain(services, descriptor =>
            descriptor.ImplementationType == typeof(M1DevelopmentDataLoader));
    }

    [Theory]
    [InlineData("AxData:RealDataReview:FillGrid:StatementTimeoutMilliseconds", "0")]
    [InlineData("AxData:RealDataReview:FillGrid:MaximumRows", "0")]
    public void Real_review_rejects_invalid_FillGrid_safety_limits(string key, string value)
    {
        var services = new ServiceCollection();

        Assert.Throws<InvalidOperationException>(() => services.AddRealDataReviewMode(
            ValidRealReviewConfiguration((key, value)),
            Environment(Environments.Development)));
    }

    [Fact]
    public void M1_mode_keeps_fake_adapters()
    {
        var services = new ServiceCollection();
        var configuration = Configuration(
            ("AxData:M1:Enabled", "true"),
            ("AxData:M1:TenantId", "sample-tenant"),
            ("AxData:M1:UserName", "owner"),
            ("AxData:M1:TransId", "sample-order"),
            ("AxData:Redis:ConnectionString", "localhost:6385"),
            ("AxData:Postgres:ConnectionString", "Host=localhost;Database=axdata;Username=axdata"));

        var enabled = services.AddM1DevelopmentMode(configuration, Environment(Environments.Development));

        Assert.True(enabled);
        Assert.Contains(services, IsRegistration<IAxpertDataLoader, M1DevelopmentDataLoader>);
        Assert.Contains(services, IsRegistration<IAxpertFieldEngine, M1DevelopmentFieldEngine>);
    }

    [Fact]
    public void M1_identity_is_rejected_outside_development()
    {
        var services = new ServiceCollection();
        var configuration = Configuration(("AxData:M1:Enabled", "true"));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddM1DevelopmentMode(configuration, Environment(Environments.Production)));

        Assert.Contains("Development", exception.Message, StringComparison.Ordinal);
    }

    private static bool IsRegistration<TService, TImplementation>(ServiceDescriptor descriptor) =>
        descriptor.ServiceType == typeof(TService) &&
        descriptor.ImplementationType == typeof(TImplementation);

    private static IConfiguration ValidRealReviewConfiguration(
        params (string Key, string Value)[] overrides)
    {
        var values = new Dictionary<string, string?>
        {
            ["AxData:RealDataReview:Enabled"] = "true",
            ["AxData:RealDataReview:TenantId"] = "uat2",
            ["AxData:RealDataReview:UserName"] = "reviewer",
            ["AxData:RealDataReview:SessionId"] = "ARM-uat2-review",
            ["AxData:RealDataReview:DatabaseType"] = "postgre",
            ["AxData:RealDataReview:ConnectionString"] = "Host=axpert-staging;Database=axpert;Username=review",
            ["AxData:RealDataReview:AllowedHosts:0"] = "axpert-staging",
            ["AxData:RealDataReview:Roles:0"] = "reviewer",
            ["AxData:Redis:ConnectionString"] = "localhost:6385",
            ["AxData:Postgres:ConnectionString"] = "Host=localhost;Database=axdata;Username=axdata"
        };
        foreach (var (key, value) in overrides)
        {
            values[key] = value;
        }

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    private static IConfiguration Configuration(params (string Key, string Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.ToDictionary(value => value.Key, value => (string?)value.Value))
            .Build();

    private static IHostEnvironment Environment(string name) => new TestHostEnvironment
    {
        EnvironmentName = name,
        ApplicationName = "AxData.ContractTests",
        ContentRootPath = AppContext.BaseDirectory,
        ContentRootFileProvider = new NullFileProvider()
    };

    private sealed class StubStartupGuard(bool isReadOnly) : IAxpertReviewStartupGuard
    {
        public Task<bool> IsReadOnlyAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(isReadOnly);
        }
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public required string EnvironmentName { get; set; }
        public required string ApplicationName { get; set; }
        public required string ContentRootPath { get; set; }
        public required IFileProvider ContentRootFileProvider { get; set; }
    }
}
