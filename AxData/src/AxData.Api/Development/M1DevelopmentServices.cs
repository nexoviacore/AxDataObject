using ARMCommon.Helpers;
using ARMCommon.Interface;
using AxData.Application.Ports;
using AxData.Application.Ports.Axpert;
using AxData.Infrastructure.Redis;
using AxData.Infrastructure.Serialization;
using AxData.Infrastructure.Sql;
using AxData.Integration.Axpert.Development;
using AxData.Integration.Axpert.FillGrid;
using StackExchange.Redis;

namespace AxData.Api.Development;

public static class M1DevelopmentServices
{
    public static bool AddM1DevelopmentMode(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var enabled = configuration.GetValue<bool>("AxData:M1:Enabled");
        if (!enabled)
        {
            return false;
        }

        if (!environment.IsDevelopment())
        {
            throw new InvalidOperationException("AxData M1 mode can run only in the Development environment.");
        }

        var redisConnection = Required(configuration["AxData:Redis:ConnectionString"], "AxData:Redis:ConnectionString");
        var postgresConnection = Required(configuration["AxData:Postgres:ConnectionString"], "AxData:Postgres:ConnectionString");
        var tenantId = Required(configuration["AxData:M1:TenantId"], "AxData:M1:TenantId");
        var userName = Required(configuration["AxData:M1:UserName"], "AxData:M1:UserName");
        var transId = Required(configuration["AxData:M1:TransId"], "AxData:M1:TransId");

        var redisOptions = new AxDataRedisOptions
        {
            InactivityTtl = TimeSpan.FromMinutes(configuration.GetValue("AxData:Redis:InactivityTtlMinutes", 30)),
            LockLeaseDuration = TimeSpan.FromSeconds(configuration.GetValue("AxData:Redis:LockLeaseSeconds", 10))
        };
        var snapshotOptions = new AxDataSnapshotOptions
        {
            MaximumSerializedBytes = configuration.GetValue("AxData:Snapshots:MaximumSerializedBytes", 1_048_576),
            MaximumGridRows = configuration.GetValue("AxData:Snapshots:MaximumGridRows", 10_000)
        };
        var axpertOptions = new M1DevelopmentAxpertOptions
        {
            TenantId = tenantId,
            TransId = transId,
            StructDefVersion = "1"
        };

        services.AddSingleton(new DevelopmentIdentityOptions(tenantId, userName, []));
        services.AddSingleton(axpertOptions);
        services.AddSingleton(redisOptions);
        services.AddSingleton(snapshotOptions);
        services.AddSingleton<AxDataSnapshotSerializer>();
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnection));
        services.AddSingleton<IAxDataStore, RedisAxDataStore>();
        services.AddSingleton<IAxDataLock, RedisAxDataLock>();
        services.AddSingleton<ISnapshotTransport, RedisSnapshotTransport>();
        services.AddScoped<IDbHelper>(_ =>
        {
            var helper = DBHelper.CreateDbHelper("postgre");
            helper.ConnectionString = postgresConnection;
            return helper;
        });
        services.AddScoped<IAxDataSnapshotRepository, AxDataSnapshotStore>();
        services.AddSingleton<IStructDefProvider, M1DevelopmentStructDefProvider>();
        services.AddSingleton<IAxpertAuthorizationProvider, M1DevelopmentAuthorizationProvider>();
        services.AddSingleton<IAxpertDataLoader, M1DevelopmentDataLoader>();
        services.AddSingleton<IAxpertFieldEngine, M1DevelopmentFieldEngine>();
        services.AddSingleton<IAxpertFillGridRuntimeFactory, M1DevelopmentFillGridRuntimeFactory>();
        services.AddSingleton<IAxpertFillGridEngine, AxpertFillGridEngine>();
        return true;
    }

    private static string Required(string? value, string key) =>
        !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new InvalidOperationException($"Configuration '{key}' is required when AxData M1 mode is enabled.");
}
