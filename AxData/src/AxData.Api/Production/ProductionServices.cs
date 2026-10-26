using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMCommon.Services;
using AxData.Application.Ports;
using AxData.Application.Ports.Axpert;
using AxData.Infrastructure.Redis;
using AxData.Infrastructure.Serialization;
using AxData.Infrastructure.Sql;
using AxData.Integration.Axpert.Authorization;
using AxData.Integration.Axpert.AxGet;
using AxData.Integration.Axpert.FieldEngine;
using AxData.Integration.Axpert.FillGrid;
using AxData.Integration.Axpert.Runtime;
using AxData.Integration.Axpert.StructDef;
using StackExchange.Redis;

namespace AxData.Api.Production;

public static class ProductionServices
{
    public static IServiceCollection AddProductionServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var redisConnection = Required(
            configuration["AxData:Redis:ConnectionString"],
            "AxData:Redis:ConnectionString");
        var snapshotConnection = Required(
            configuration["AxData:Postgres:ConnectionString"],
            "AxData:Postgres:ConnectionString");
        var redisOptions = new AxDataRedisOptions
        {
            InactivityTtl = TimeSpan.FromMinutes(
                configuration.GetValue("AxData:Redis:InactivityTtlMinutes", 30)),
            LockLeaseDuration = TimeSpan.FromSeconds(
                configuration.GetValue("AxData:Redis:LockLeaseSeconds", 10))
        };
        var snapshotOptions = new AxDataSnapshotOptions
        {
            MaximumSerializedBytes = configuration.GetValue(
                "AxData:Snapshots:MaximumSerializedBytes",
                AxDataSnapshotOptions.DefaultMaximumSerializedBytes),
            MaximumGridRows = configuration.GetValue(
                "AxData:Snapshots:MaximumGridRows",
                AxDataSnapshotOptions.DefaultMaximumGridRows),
            MaximumFieldInstances = configuration.GetValue(
                "AxData:Snapshots:MaximumFieldInstances",
                AxDataSnapshotOptions.DefaultMaximumFieldInstances)
        };
        var fillGridOptions = new AxpertFillGridQueryOptions
        {
            StatementTimeoutMilliseconds = configuration.GetValue(
                "AxData:Axpert:FillGrid:StatementTimeoutMilliseconds",
                5_000),
            MaximumRows = configuration.GetValue(
                "AxData:Axpert:FillGrid:MaximumRows",
                1_000)
        };
        fillGridOptions.Validate();

        services.AddSingleton(redisOptions);
        services.AddSingleton(snapshotOptions);
        services.AddSingleton(fillGridOptions);
        services.AddSingleton<AxDataSnapshotSerializer>();
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnection));
        services.AddSingleton<IAxDataStore, RedisAxDataStore>();
        services.AddSingleton<IAxDataLock, RedisAxDataLock>();
        services.AddSingleton<ISnapshotTransport, RedisSnapshotTransport>();
        services.AddScoped<IDbHelper>(_ =>
        {
            var helper = DBHelper.CreateDbHelper("postgre");
            helper.ConnectionString = snapshotConnection;
            return helper;
        });
        services.AddScoped<IAxDataSnapshotRepository, AxDataSnapshotStore>();

        services.AddSingleton<IAppSettingsService, AppSettingsService>();
        services.AddSingleton<IAppSettingsHelper, AppSettingsHelper>();
        services.AddScoped<IARMLogTraceService, ARMLogTraceService>();
        services.AddScoped<AxpertSessionResolver>();
        services.AddScoped<IAxpertSessionResolver>(provider =>
            provider.GetRequiredService<AxpertSessionResolver>());
        services.AddScoped<IAxpertSessionRuntimeAccessor>(provider =>
            provider.GetRequiredService<AxpertSessionResolver>());
        services.AddScoped<IAxpertRuntimeContextFactory, ProductionAxpertRuntimeContextFactory>();
        services.AddScoped<IAxpertStructDefSource, AxpertStructDefSource>();
        services.AddScoped<IStructDefProvider, AxpertStructDefProvider>();
        services.AddScoped<IAxpertPermissionSource, AxpertPermissionSource>();
        services.AddScoped<IAxpertAuthorizationProvider, AxpertAuthorizationProvider>();
        services.AddScoped<IAxpertDataSource, AxpertDataSource>();
        services.AddScoped<IAxpertDataLoader, AxpertDataLoader>();
        services.AddScoped<IAxpertFieldRuntimeFactory, AxpertFieldRuntimeFactory>();
        services.AddScoped<IAxpertFieldEngine, AxpertFieldEngine>();
        services.AddScoped<IAxpertFillGridRuntimeFactory, AxpertFillGridRuntimeFactory>();
        services.AddScoped<IAxpertFillGridEngine, AxpertFillGridEngine>();
        return services;
    }

    private static string Required(string? value, string key) =>
        !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new InvalidOperationException(
                $"Configuration '{key}' is required for production composition.");
}
