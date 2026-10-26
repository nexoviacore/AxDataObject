using System.Data.Common;
using ARMCommon.Helpers;
using ARMCommon.Interface;
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

namespace AxData.Api.Development;

public static class RealDataReviewServices
{
    public static bool AddRealDataReviewMode(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        if (!configuration.GetValue<bool>("AxData:RealDataReview:Enabled"))
        {
            return false;
        }

        if (!environment.IsDevelopment())
        {
            throw new InvalidOperationException(
                "AxData RealDataReview mode can run only in the Development environment.");
        }

        var section = configuration.GetSection("AxData:RealDataReview");
        var tenantId = Required(section["TenantId"], "AxData:RealDataReview:TenantId");
        var userName = Required(section["UserName"], "AxData:RealDataReview:UserName");
        var sessionId = Required(section["SessionId"], "AxData:RealDataReview:SessionId");
        var databaseType = Required(section["DatabaseType"], "AxData:RealDataReview:DatabaseType");
        var axpertConnection = Required(
            section["ConnectionString"],
            "AxData:RealDataReview:ConnectionString");
        var roles = section.GetSection("Roles").Get<string[]>() ?? [];
        var allowedHosts = section.GetSection("AllowedHosts").Get<string[]>() ?? [];
        ValidateAllowedHosts(axpertConnection, allowedHosts);

        var redisConnection = Required(
            configuration["AxData:Redis:ConnectionString"],
            "AxData:Redis:ConnectionString");
        var snapshotConnection = Required(
            configuration["AxData:Postgres:ConnectionString"],
            "AxData:Postgres:ConnectionString");
        if (string.Equals(axpertConnection, snapshotConnection, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "The Axpert review and AxData snapshot databases must use separate connections.");
        }

        var runtimeOptions = new AxpertReviewRuntimeOptions
        {
            TenantId = tenantId,
            SessionId = sessionId,
            UserName = userName,
            Roles = roles,
            DatabaseType = databaseType,
            ConnectionString = axpertConnection,
            AxpertSessionId = section["AxpertSessionId"]
        };
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
            StatementTimeoutMilliseconds = section.GetValue(
                "FillGrid:StatementTimeoutMilliseconds",
                5_000),
            MaximumRows = section.GetValue(
                "FillGrid:MaximumRows",
                1_000)
        };
        fillGridOptions.Validate();

        services.AddSingleton(new DevelopmentIdentityOptions(tenantId, userName, roles));
        services.AddSingleton(runtimeOptions);
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

        services.AddScoped<IARMLogTraceService, ARMLogTraceService>();
        services.AddScoped<IAxpertRuntimeContextFactory, AxpertRuntimeContextFactory>();
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

        services.AddSingleton<IAxpertReviewStartupGuard, AxpertReviewStartupGuard>();
        services.AddHostedService<RealDataReviewStartupService>();
        return true;
    }

    internal static void ValidateAllowedHosts(string connectionString, IReadOnlyCollection<string> allowedHosts)
    {
        if (allowedHosts.Count == 0)
        {
            throw new InvalidOperationException(
                "At least one AxData RealDataReview database host must be allowlisted.");
        }

        var builder = new DbConnectionStringBuilder { ConnectionString = connectionString };
        var hostValue = FindValue(builder, "Host", "Server", "Data Source");
        var hosts = hostValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (hosts.Length == 0 || hosts.Any(host => !allowedHosts.Contains(host, StringComparer.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                "The AxData RealDataReview database host is not allowlisted.");
        }
    }

    private static string FindValue(DbConnectionStringBuilder builder, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (builder.TryGetValue(key, out var value) &&
                !string.IsNullOrWhiteSpace(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)))
            {
                return Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)!;
            }
        }

        throw new InvalidOperationException(
            "The AxData RealDataReview connection string does not contain a database host.");
    }

    private static string Required(string? value, string key) =>
        !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new InvalidOperationException(
                $"Configuration '{key}' is required when AxData RealDataReview mode is enabled.");
}

public sealed class RealDataReviewStartupService(IAxpertReviewStartupGuard guard) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!await guard.IsReadOnlyAsync(cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException(
                "The Axpert RealDataReview database connection is writable or could not be verified as read-only.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
