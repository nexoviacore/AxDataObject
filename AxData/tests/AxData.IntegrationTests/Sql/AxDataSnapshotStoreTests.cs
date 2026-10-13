using System.Data;
using AxData.Application.Ports;
using AxData.Domain.Lifecycle;
using AxData.Domain.Model;
using AxData.Infrastructure.Serialization;
using AxData.Infrastructure.Sql;

namespace AxData.IntegrationTests.Sql;

public sealed class AxDataSnapshotStoreTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 29, 8, 30, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("oracle", "WHERE incoming.ObjectVersion > target.ObjectVersion")]
    [InlineData("postgres", "WHERE EXCLUDED.ObjectVersion > AxDataObjectInstance.ObjectVersion")]
    [InlineData("mssql", "WHEN MATCHED AND incoming.ObjectVersion > target.ObjectVersion")]
    public async Task Selects_version_guarded_sql_and_binds_snapshot_as_json(
        string provider,
        string expectedGuard)
    {
        var (store, recorder) = CreateStore(provider);
        var state = CreateState("tenant-a", "object-1", 13, new string('x', 8_000));

        var status = await store.UpsertAsync(
            new AxDataSnapshotWrite("event-13", state, Now),
            CancellationToken.None);

        Assert.Equal(AxDataSnapshotUpsertStatus.Stored, status);
        Assert.Contains(expectedGuard, recorder.LastSql, StringComparison.Ordinal);
        var jsonIndex = Assert.IsAssignableFrom<IReadOnlyList<string>>(recorder.LastParameterNames)
            .ToList()
            .IndexOf("DataObjectJson");
        Assert.Equal(DbType.Object, recorder.LastParameterTypes![jsonIndex]);
        Assert.Contains(new string('x', 8_000), Assert.IsType<string>(recorder.LastParameterValues![jsonIndex]), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("oracle")]
    [InlineData("mssql")]
    public async Task Binds_snapshot_timestamps_as_UTC_DateTimes_for_non_Postgres_providers(string provider)
    {
        var (store, recorder) = CreateStore(provider);
        var state = CreateState("tenant-a", "object-1", 13, "timestamps");

        await store.UpsertAsync(
            new AxDataSnapshotWrite("event-13", state, Now),
            CancellationToken.None);

        var names = Assert.IsAssignableFrom<IReadOnlyList<string>>(recorder.LastParameterNames);
        var values = Assert.IsAssignableFrom<IReadOnlyList<object>>(recorder.LastParameterValues);
        AssertTimestamp("PersistDateTimeUtc", Now);
        AssertTimestamp("LastAccessedDateTimeUtc", Now);
        AssertTimestamp("PendingFromDateTimeUtc", Now.AddMinutes(-10));

        void AssertTimestamp(string parameterName, DateTimeOffset expected)
        {
            var value = Assert.IsType<DateTime>(values[names.ToList().IndexOf(parameterName)]);
            Assert.Equal(DateTimeKind.Utc, value.Kind);
            Assert.Equal(expected.UtcDateTime.Ticks, value.Ticks);
        }
    }

    [Fact]
    public async Task Binds_Postgres_snapshot_timestamps_as_explicit_UTC_timestamptz_values()
    {
        var (store, recorder) = CreateStore("postgres");
        var state = CreateState("tenant-a", "object-1", 13, "timestamps");

        await store.UpsertAsync(
            new AxDataSnapshotWrite("event-13", state, Now),
            CancellationToken.None);

        Assert.Contains("CAST(@PersistDateTimeUtc AS timestamptz)", recorder.LastSql, StringComparison.Ordinal);
        var names = Assert.IsAssignableFrom<IReadOnlyList<string>>(recorder.LastParameterNames);
        var types = Assert.IsAssignableFrom<IReadOnlyList<DbType>>(recorder.LastParameterTypes);
        var values = Assert.IsAssignableFrom<IReadOnlyList<object>>(recorder.LastParameterValues);
        AssertTimestamp("PersistDateTimeUtc", Now);
        AssertTimestamp("LastAccessedDateTimeUtc", Now);
        AssertTimestamp("PendingFromDateTimeUtc", Now.AddMinutes(-10));

        void AssertTimestamp(string parameterName, DateTimeOffset expected)
        {
            var index = names.ToList().IndexOf(parameterName);
            Assert.Equal(DbType.String, types[index]);
            var value = Assert.IsType<string>(values[index]);
            Assert.Equal(expected, DateTimeOffset.Parse(value, System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    [Fact]
    public async Task Older_version_cannot_overwrite_newer_snapshot()
    {
        var (store, recorder) = CreateStore("postgres");
        await store.UpsertAsync(
            new AxDataSnapshotWrite("event-13", CreateState("tenant-a", "object-1", 13, "newer"), Now),
            CancellationToken.None);

        var status = await store.UpsertAsync(
            new AxDataSnapshotWrite("event-12", CreateState("tenant-a", "object-1", 12, "older"), Now.AddSeconds(1)),
            CancellationToken.None);

        Assert.Equal(AxDataSnapshotUpsertStatus.Ignored, status);
        Assert.Equal(13, recorder.GetVersion("tenant-a", "object-1"));
        Assert.Contains("newer", recorder.GetPayload("tenant-a", "object-1"), StringComparison.Ordinal);
        Assert.DoesNotContain("older", recorder.GetPayload("tenant-a", "object-1"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Duplicate_event_is_a_no_op()
    {
        var (store, recorder) = CreateStore("oracle");
        var write = new AxDataSnapshotWrite(
            "duplicate-event",
            CreateState("tenant-a", "object-1", 7, "same"),
            Now);

        var first = await store.UpsertAsync(write, CancellationToken.None);
        var duplicate = await store.UpsertAsync(write, CancellationToken.None);

        Assert.Equal(AxDataSnapshotUpsertStatus.Stored, first);
        Assert.Equal(AxDataSnapshotUpsertStatus.Ignored, duplicate);
        Assert.Equal(7, recorder.GetVersion("tenant-a", "object-1"));
    }

    [Fact]
    public async Task Same_object_id_in_another_tenant_is_untouched()
    {
        var (store, recorder) = CreateStore("mssql");
        await store.UpsertAsync(
            new AxDataSnapshotWrite("event-a", CreateState("tenant-a", "shared-object", 13, "a"), Now),
            CancellationToken.None);
        await store.UpsertAsync(
            new AxDataSnapshotWrite("event-b", CreateState("tenant-b", "shared-object", 4, "b"), Now),
            CancellationToken.None);

        await store.UpsertAsync(
            new AxDataSnapshotWrite("event-a-stale", CreateState("tenant-a", "shared-object", 12, "stale"), Now),
            CancellationToken.None);

        Assert.Equal(13, recorder.GetVersion("tenant-a", "shared-object"));
        Assert.Equal(4, recorder.GetVersion("tenant-b", "shared-object"));
        Assert.Contains("b", recorder.GetPayload("tenant-b", "shared-object"), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("oracle", ":TenantId")]
    [InlineData("postgres", "@TenantId")]
    [InlineData("mssql", "@TenantId")]
    public async Task Loads_latest_snapshot_by_tenant_and_object_and_validates_metadata(
        string provider,
        string parameterMarker)
    {
        var (store, recorder) = CreateStore(provider);
        var state = CreateState("tenant-a", "object-1", 13, "recoverable");
        await store.UpsertAsync(new AxDataSnapshotWrite("event-13", state, Now), CancellationToken.None);

        var found = await store.LoadLatestAsync("tenant-a", "object-1", CancellationToken.None);
        var missingTenant = await store.LoadLatestAsync("tenant-b", "object-1", CancellationToken.None);

        Assert.Equal(SnapshotReadStatus.Found, found.Status);
        Assert.Equal(13, found.State!.Version);
        Assert.Equal(SnapshotReadStatus.NotFound, missingTenant.Status);
        Assert.Contains(parameterMarker, recorder.LastSql, StringComparison.Ordinal);
    }

    private static (AxDataSnapshotStore Store, RecordingDbHelper Recorder) CreateStore(string provider)
    {
        var recorder = RecordingDbHelper.Create(provider, out var helper);
        var serializer = new AxDataSnapshotSerializer(new AxDataSnapshotOptions
        {
            MaximumSerializedBytes = 100_000,
            MaximumGridRows = 100
        });
        return (new AxDataSnapshotStore(helper, serializer), recorder);
    }

    private static AxDataState CreateState(
        string tenantId,
        string objectId,
        long version,
        string marker)
    {
        var row = new AxDataRow(
            "1",
            AxRowState.Unchanged,
            persistedRecordId: 42,
            fields: new Dictionary<string, AxFieldValue>(StringComparer.OrdinalIgnoreCase)
            {
                ["marker"] = new(marker)
            });
        var workflow = new PegActiveState
        {
            WorkflowInstanceId = "workflow-1",
            CurrentTaskId = "task-1",
            InitiatorUserName = "initiator",
            Status = PegWorkflowStatus.Pending,
            ApprovalLevel = 1,
            Approvers = ["approver"],
            WorkflowVersion = 2,
            PendingFromUtc = Now.AddMinutes(-10)
        };

        return AxDataState.Rehydrate(
            objectId,
            tenantId,
            "employee",
            "employeeid",
            "42",
            42,
            "owner",
            "initiator",
            version,
            "struct-rev-1",
            Now.AddHours(-1),
            Now,
            Now.AddHours(1),
            AxDataLifecycleStatus.PendingApproval,
            [new AxDataContainer("dc1", isGrid: false, [row])],
            workflow,
            [],
            axOpen: false);
    }
}
