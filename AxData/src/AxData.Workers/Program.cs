using ARMCommon.Helpers;
using AxData.Infrastructure.Redis;
using AxData.Infrastructure.Serialization;
using AxData.Infrastructure.Sql;
using AxData.Workers.Snapshots;
using AxData.Workers.Observability;
using AxData.Application.Ports;
using StackExchange.Redis;

var redisConnection = RequiredEnvironment("AXDATA_REDIS_CONNECTION");
var postgresConnection = RequiredEnvironment("AXDATA_POSTGRES_CONNECTION");
var tenantId = RequiredEnvironment("AXDATA_TENANT_ID");
var detailLevel = Enum.TryParse<OperationalLogDetail>(
    Environment.GetEnvironmentVariable("AxData__Logging__DetailLevel"),
    ignoreCase: true,
    out var configuredDetail)
    ? configuredDetail
    : OperationalLogDetail.Baseline;
var operationalLog = new ConsoleOperationalEventSink(detailLevel);

using var shutdown = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    shutdown.Cancel();
};

var redis = await ConnectionMultiplexer.ConnectAsync(redisConnection).ConfigureAwait(false);
await using var redisLifetime = redis.ConfigureAwait(false);
var serializer = new AxDataSnapshotSerializer(new AxDataSnapshotOptions
{
    MaximumSerializedBytes = 1_048_576,
    MaximumGridRows = 10_000
});
var redisOptions = new AxDataRedisOptions
{
    InactivityTtl = TimeSpan.FromMinutes(30),
    LockLeaseDuration = TimeSpan.FromSeconds(10)
};
var transport = new RedisSnapshotTransport(redis, serializer, redisOptions);
var dbHelper = DBHelper.CreateDbHelper("postgre");
dbHelper.ConnectionString = postgresConnection;
var repository = new AxDataSnapshotStore(dbHelper, serializer);
var consumer = new SnapshotConsumer(
    transport,
    repository,
    new SnapshotConsumerOptions
    {
        TenantId = tenantId,
        ConsumerName = $"{Environment.MachineName}-{Environment.ProcessId}"
    },
    operationalLog);

operationalLog.Write(new OperationalEvent(
    "snapshot.worker.started", "worker", "started", TenantId: tenantId));
try
{
    while (!shutdown.IsCancellationRequested)
    {
        var result = await consumer.RunOnceAsync(shutdown.Token).ConfigureAwait(false);
        if (result.Read > 0)
        {
            operationalLog.Write(new OperationalEvent(
                "snapshot.batch.completed", "worker", "completed", TenantId: tenantId,
                Count: result.Read, SecondaryCount: result.Acknowledged,
                ReasonCode: result.DeadLettered > 0 ? "contains-deadletters" : null));
        }

        await Task.Delay(TimeSpan.FromMilliseconds(500), shutdown.Token).ConfigureAwait(false);
    }
}
catch (OperationCanceledException) when (shutdown.IsCancellationRequested)
{
}
catch (Exception exception)
{
    operationalLog.Write(new OperationalEvent(
        "snapshot.worker.exception", "worker", "failed", OperationalLogLevel.Error,
        TenantId: tenantId, ReasonCode: exception.GetType().Name));
    throw;
}

static string RequiredEnvironment(string name) =>
    !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(name))
        ? Environment.GetEnvironmentVariable(name)!
        : throw new InvalidOperationException($"Environment variable '{name}' is required.");
