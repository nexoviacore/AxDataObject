using System.Diagnostics;
using System.Text;
using System.Text.Json;
using AxData.Domain.Lifecycle;
using AxData.Domain.Model;
using AxData.Infrastructure.Serialization;

namespace AxData.IntegrationTests.Serialization;

public sealed class AxDataSnapshotSerializerTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 9, 25, 4, 30, 0, TimeSpan.Zero);

    [Fact]
    public void RoundTrip_preserves_snapshot_state_and_typed_values()
    {
        var original = CreateState();
        var serializer = CreateSerializer();

        var restored = serializer.Deserialize(serializer.Serialize(original));

        Assert.Equal(original.ObjectId, restored.ObjectId);
        Assert.Equal(original.TenantId, restored.TenantId);
        Assert.Equal(original.TransId, restored.TransId);
        Assert.Equal(original.StructDefVersion, restored.StructDefVersion);
        Assert.Equal(original.Version, restored.Version);
        Assert.Equal(original.Status, restored.Status);
        Assert.Equal(original.CreatedAtUtc, restored.CreatedAtUtc);
        Assert.Equal(original.LastAccessedAtUtc, restored.LastAccessedAtUtc);
        Assert.Equal(original.ExpiresAtUtc, restored.ExpiresAtUtc);
        Assert.Equal(original.Workflow!.WorkflowVersion, restored.Workflow!.WorkflowVersion);
        Assert.Equal(original.Workflow.Approvers, restored.Workflow.Approvers);
        Assert.Equal(original.Files, restored.Files);

        var fields = restored.DataContainers["items"].Rows.Single().Fields;
        Assert.Equal(1234.50m, fields["amount"].Value);
        Assert.Equal(1200m, fields["amount"].OriginalValue);
        Assert.Equal(77, fields["amount"].SourceId);
        Assert.Equal(76, fields["amount"].OriginalSourceId);
        Assert.True(fields["amount"].IsDirty);
        Assert.Equal(new DateOnly(2026, 9, 25), fields["date"].Value);
        Assert.Equal(new byte[] { 1, 2, 3 }, Assert.IsType<byte[]>(fields["binary"].Value));
        Assert.Null(fields["optional"].Value);
        Assert.True(fields["locked"].IsReadOnly);
    }

    [Fact]
    public void Serialize_is_deterministic_and_contains_only_structdef_reference()
    {
        var serializer = CreateSerializer();
        var state = CreateState();

        var first = serializer.Serialize(state);
        var second = serializer.Serialize(state);
        var json = Encoding.UTF8.GetString(first);

        Assert.Equal(first, second);
        Assert.Contains("\"schemaVersion\":3", json, StringComparison.Ordinal);
        Assert.Contains("\"dcId\":\"items\"", json, StringComparison.Ordinal);
        Assert.Contains("\"structDef\":{\"transId\":\"employee\",\"version\":\"rev-7\"}", json, StringComparison.Ordinal);
        Assert.Contains("\"sourceId\":77", json, StringComparison.Ordinal);
        Assert.Contains("\"originalSourceId\":76", json, StringComparison.Ordinal);
        Assert.DoesNotContain("connection", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("credential", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Deserialize_reads_committed_v1_fixture()
    {
        var fixturePath = Path.Combine(AppContext.BaseDirectory, "Serialization", "Fixtures", "axdata-snapshot-v1.json");

        var serializer = CreateSerializer();
        var restored = serializer.Deserialize(File.ReadAllBytes(fixturePath));
        var restoredAgain = serializer.Deserialize(File.ReadAllBytes(fixturePath));

        Assert.Equal("obj-fixture", restored.ObjectId);
        Assert.Equal("rev-7", restored.StructDefVersion);
        Assert.Equal(3, restored.Version);
        Assert.Equal(1234.50m, restored.DataContainers["dc1"].Rows[0].Fields["salary"].Value);
        Assert.Equal(42, restored.DataContainers["dc1"].Rows[0].PersistedRecordId);
        Assert.NotEqual("42", restored.DataContainers["dc1"].Rows[0].RowId);
        Assert.Equal(restored.DataContainers["dc1"].Rows[0].RowId, restoredAgain.DataContainers["dc1"].Rows[0].RowId);
    }

    [Fact]
    public void Deserialize_reads_committed_v2_fixture()
    {
        var fixturePath = Path.Combine(AppContext.BaseDirectory, "Serialization", "Fixtures", "axdata-snapshot-v2.json");

        var restored = CreateSerializer().Deserialize(File.ReadAllBytes(fixturePath));

        Assert.Equal("obj-v2", restored.ObjectId);
        Assert.Equal(4, restored.Version);
        Assert.Equal("row_v2", restored.DataContainers["dc1"].Rows.Single().RowId);
        Assert.Equal(1234.50m, restored.DataContainers["dc1"].Rows.Single().Fields["salary"].Value);
        Assert.Null(restored.DataContainers["dc1"].Rows.Single().Fields["salary"].SourceId);
    }

    [Fact]
    public void V3_omits_original_pair_for_clean_fields()
    {
        using var document = JsonDocument.Parse(CreateSerializer().Serialize(CreateState()));
        var locked = document.RootElement.GetProperty("dataContainers")[0]
            .GetProperty("rows")[0]
            .GetProperty("fields")
            .EnumerateArray()
            .Single(field => field.GetProperty("name").GetString() == "locked");

        Assert.Equal(9, locked.GetProperty("sourceId").GetInt64());
        Assert.False(locked.TryGetProperty("originalValue", out _));
        Assert.False(locked.TryGetProperty("originalSourceId", out _));
        Assert.False(locked.TryGetProperty("isDirty", out _));
    }

    [Fact]
    public void Serialize_rejects_payload_over_byte_limit()
    {
        var serializer = CreateSerializer(maximumBytes: 100);

        var exception = Assert.Throws<AxDataSnapshotException>(() => serializer.Serialize(CreateState()));

        Assert.Contains("maximum", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Deserialize_rejects_payload_over_byte_limit_before_parsing()
    {
        var serializer = CreateSerializer(maximumBytes: 10);

        var exception = Assert.Throws<AxDataSnapshotException>(() => serializer.Deserialize(new byte[11]));

        Assert.Contains("maximum", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Serialize_rejects_grid_over_row_limit()
    {
        var state = CreateState();
        state.DataContainers["items"].AddRow(new AxDataRow("row_second", AxRowState.New));

        var exception = Assert.Throws<AxDataSnapshotException>(() => CreateSerializer(maximumRows: 1).Serialize(state));

        Assert.Contains("items", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Serialize_rejects_field_instances_over_limit()
    {
        var state = CreateState();

        var exception = Assert.Throws<AxDataSnapshotException>(() =>
            CreateSerializer(maximumFields: 4).Serialize(state));

        Assert.Contains("field count", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Serialize_rejects_unsupported_runtime_field_type()
    {
        var state = CreateState();
        state.DataContainers["items"].Rows[0].Fields.Add("service", new AxFieldValue(new object()));

        var exception = Assert.Throws<AxDataSnapshotException>(() => CreateSerializer().Serialize(state));

        Assert.Contains("not supported", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Deserialize_rejects_unknown_schema_version()
    {
        var payload = Encoding.UTF8.GetBytes("{\"schemaVersion\":4}");

        var exception = Assert.Throws<AxDataSnapshotException>(() => CreateSerializer().Deserialize(payload));

        Assert.Contains("not supported", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RoundTrip_preserves_row_array_order_independently_of_row_id()
    {
        var state = CreateState();
        state.DataContainers["items"].AddRow(new AxDataRow("row_a", AxRowState.New));
        state.DataContainers["items"].Rows.Reverse();

        var restored = CreateSerializer().Deserialize(CreateSerializer().Serialize(state));

        Assert.Equal(["row_a", "row_z"], restored.DataContainers["items"].Rows.Select(row => row.RowId));
    }

    [Fact]
    public void Capacity_envelope_is_deterministic_and_fits_v3_limit()
    {
        var state = CreateCapacityState();
        var serializer = CreateSerializer(
            maximumBytes: AxDataSnapshotOptions.DefaultMaximumSerializedBytes,
            maximumRows: AxDataSnapshotOptions.DefaultMaximumGridRows,
            maximumFields: AxDataSnapshotOptions.DefaultMaximumFieldInstances);
        _ = serializer.Serialize(CreateState());
        var retainedBaseline = GC.GetTotalMemory(forceFullCollection: true);
        var metrics = MeasureCapacitySerialization(serializer, state);
        var retainedBytes = GC.GetTotalMemory(forceFullCollection: true) - retainedBaseline;

        Console.WriteLine(
            $"P1D-05B capacity: bytes={metrics.Bytes}; elapsedMs={metrics.Elapsed.TotalMilliseconds:F2}; " +
            $"allocatedBytes={metrics.AllocatedBytes}; retainedBytes={retainedBytes}");

        Assert.True(metrics.Bytes <= AxDataSnapshotOptions.DefaultMaximumSerializedBytes);
        Assert.True(metrics.Elapsed < TimeSpan.FromMilliseconds(500), $"Serialization took {metrics.Elapsed}.");
        Assert.True(metrics.AllocatedBytes < 256L * 1024 * 1024);
        Assert.True(retainedBytes < 10L * 1024 * 1024, $"Serialization retained {retainedBytes} bytes.");
    }

    private static AxDataSnapshotSerializer CreateSerializer(
        int maximumBytes = 100_000,
        int maximumRows = 100,
        int maximumFields = AxDataSnapshotOptions.DefaultMaximumFieldInstances) =>
        new(new AxDataSnapshotOptions
        {
            MaximumSerializedBytes = maximumBytes,
            MaximumGridRows = maximumRows,
            MaximumFieldInstances = maximumFields
        });

    private static AxDataState CreateState()
    {
        var fields = new Dictionary<string, AxFieldValue>(StringComparer.OrdinalIgnoreCase)
        {
            ["amount"] = AxFieldValue.Rehydrate(
                1234.50m,
                1200m,
                isDirty: true,
                isReadOnly: false,
                sourceId: 77,
                originalSourceId: 76),
            ["date"] = new AxFieldValue(new DateOnly(2026, 9, 25)),
            ["binary"] = new AxFieldValue(new byte[] { 1, 2, 3 }),
            ["optional"] = new AxFieldValue(null),
            ["locked"] = new AxFieldValue("fixed", isReadOnly: true, sourceId: 9)
        };
        var container = new AxDataContainer(
            "items",
            isGrid: true,
            [new AxDataRow("row_z", AxRowState.New, fields: fields)]);

        return AxDataState.Rehydrate(
            "obj-1",
            "tenant-a",
            "employee",
            "employeeid",
            "42",
            42,
            "owner",
            "initiator",
            8,
            "rev-7",
            CreatedAt,
            CreatedAt.AddMinutes(5),
            CreatedAt.AddHours(1),
            AxDataLifecycleStatus.PendingApproval,
            [container],
            new PegActiveState
            {
                WorkflowInstanceId = "workflow-1",
                CurrentTaskId = "task-2",
                InitiatorUserName = "initiator",
                Status = PegWorkflowStatus.Pending,
                ApprovalLevel = 2,
                Approvers = ["approver-b", "approver-a"],
                WorkflowVersion = 4,
                PendingFromUtc = CreatedAt.AddMinutes(2)
            },
            [new AxFileReference("file-1", "resume.pdf", "application/pdf")],
            axOpen: true);
    }

    private static AxDataState CreateCapacityState()
    {
        var state = new AxDataState(
            "obj-capacity",
            "tenant-a",
            "capacity",
            "owner",
            "owner",
            "hash",
            CreatedAt);
        for (var dc = 1; dc <= 50; dc++)
        {
            var rows = new List<AxDataRow>(70);
            for (var row = 1; row <= 70; row++)
            {
                var fields = new Dictionary<string, AxFieldValue>(100, StringComparer.OrdinalIgnoreCase);
                for (var field = 1; field <= 100; field++)
                {
                    fields.Add($"field{field:D3}", new AxFieldValue("0123456789abcdef"));
                }

                rows.Add(new AxDataRow($"row_{dc:D2}_{row:D2}", AxRowState.New, fields: fields));
            }

            state.DataContainers.Add($"dc{dc}", new AxDataContainer($"dc{dc}", true, rows));
        }

        return state;
    }

    private static CapacityMetrics MeasureCapacitySerialization(
        AxDataSnapshotSerializer serializer,
        AxDataState state)
    {
        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var timer = Stopwatch.StartNew();
        var first = serializer.Serialize(state);
        timer.Stop();
        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        var second = serializer.Serialize(state);
        Assert.True(first.AsSpan().SequenceEqual(second), "Capacity snapshots were not deterministic.");
        return new CapacityMetrics(first.Length, timer.Elapsed, allocatedBytes);
    }

    private sealed record CapacityMetrics(int Bytes, TimeSpan Elapsed, long AllocatedBytes);
}
