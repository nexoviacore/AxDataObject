using System.Diagnostics;
using System.Text;
using System.Text.Json;
using AxData.Domain.Lifecycle;
using AxData.Domain.Model;
using AxData.Infrastructure.Serialization;

namespace AxData.IntegrationTests.Serialization;

[Collection(SnapshotCapacityCollection.Name)]
public sealed class AxDataSnapshotSerializerTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 9, 25, 4, 30, 0, TimeSpan.Zero);

    [Fact]
    public void RoundTrip_preserves_typed_values_packed_source_ids_and_aligned_old_values()
    {
        var original = CreateState();
        var restored = CreateSerializer().Deserialize(CreateSerializer().Serialize(original));

        Assert.Equal((original.ObjectId, original.TenantId, original.TransId, original.Status),
            (restored.ObjectId, restored.TenantId, restored.TransId, restored.Status));
        Assert.Equal(original.Workflow!.WorkflowVersion, restored.Workflow!.WorkflowVersion);
        Assert.Empty(restored.Files);
        Assert.False(restored.AxOpen);

        var row = Assert.Single(restored.DataContainers["items"].Rows);
        Assert.Equal("row_z", row.RowId);
        Assert.Equal(42, row.PersistedRecordId);
        var fields = row.Fields;
        Assert.Equal(1234.50m, fields["amount"].Value);
        Assert.Equal(1200m, fields["amount"].OriginalValue);
        Assert.True(fields["date"].IsReadOnly);
        Assert.Null(fields["amount"].SourceId);
        Assert.Null(fields["amount"].OriginalSourceId);
        Assert.True(fields["amount"].IsDirty);
        Assert.Equal("2026-09-25", fields["date"].Value);
        Assert.Equal("AQID", fields["binary"].Value);
        Assert.Equal(true, fields["enabled"].Value);
        Assert.Null(fields["optional"].Value);
        Assert.True(fields["locked"].IsSourceKey);
        Assert.Equal(9, fields["locked"].SourceId);
        Assert.Equal(8, fields["locked"].OriginalSourceId);
    }

    [Fact]
    public void RoundTrip_preserves_field_metadata_for_an_empty_grid()
    {
        var state = new AxDataState("obj-empty", "tenant-a", "empty", "owner", CreatedAt);
        state.DataContainers.Add("dc2", new AxDataContainer(
            "dc2",
            true,
            fields:
            [
                new AxDataFieldMetadata("name"),
                new AxDataFieldMetadata("status", IsReadOnly: true),
                new AxDataFieldMetadata("customer", IsSourceKey: true)
            ]));

        byte[] payload = CreateSerializer().Serialize(state);
        AxDataState restored = CreateSerializer().Deserialize(payload);

        Assert.Equal(
            [("name", false, false), ("status", true, false), ("customer", false, true)],
            restored.DataContainers["dc2"].Fields
                .Select(field => (field.Name, field.IsReadOnly, field.IsSourceKey)));
        Assert.Contains("\"fields\":[[\"name\",0],[\"status\",1],[\"customer\",2]]",
            Encoding.UTF8.GetString(payload), StringComparison.Ordinal);
    }

    [Fact]
    public void Serialize_is_deterministic_compact_and_omits_reconstructable_or_future_state()
    {
        var serializer = CreateSerializer();
        var state = CreateState();

        var first = serializer.Serialize(state);
        var second = serializer.Serialize(state);
        var json = Encoding.UTF8.GetString(first);

        Assert.Equal(first, second);
        Assert.DoesNotContain("schemaVersion", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ownerUserName", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("createdAtUtc", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("lastAccessedAtUtc", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("expiresAtUtc", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("keyField", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("keyValue", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("isReadOnly", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("files", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("axOpen", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[\"date\",1]", json, StringComparison.Ordinal);
        Assert.Contains("[\"locked\",2]", json, StringComparison.Ordinal);
        Assert.Contains("\"values\":[1234.50,\"AQID\",\"2026-09-25\",true,\"fixed~9\",null]", json, StringComparison.Ordinal);
        Assert.Contains("\"oldValues\":[1200,\"AQID\",\"2026-09-25\",true,\"fixed~8\",null]", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"sourceIds\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"originals\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"originalSourceIds\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"kind\"", json, StringComparison.Ordinal);
        Assert.Equal(1, CountOccurrences(json, "\"amount\""));
    }

    [Fact]
    public void Clean_baseline_omits_old_values_and_edit_uses_same_value_for_unchanged_indexes()
    {
        var serializer = CreateSerializer();
        var state = CreateState();
        var fields = state.DataContainers["items"].Rows[0].Fields;
        foreach (var field in fields.Values)
        {
            field.AcceptChanges();
        }

        var cleanJson = Encoding.UTF8.GetString(serializer.Serialize(state));
        Assert.DoesNotContain("\"oldValues\"", cleanJson, StringComparison.Ordinal);
        Assert.Contains("\"fixed~9\"", cleanJson, StringComparison.Ordinal);

        fields["amount"].SetValue(1300m);
        fields["locked"].SetValue("fixed", 10);
        var dirtyJson = Encoding.UTF8.GetString(serializer.Serialize(state));
        Assert.Contains("\"values\":[1300,\"AQID\",\"2026-09-25\",true,\"fixed~10\",null]", dirtyJson, StringComparison.Ordinal);
        Assert.Contains("\"oldValues\":[1234.50,\"AQID\",\"2026-09-25\",true,\"fixed~9\",null]", dirtyJson, StringComparison.Ordinal);

        fields["amount"].SetValue(1234.50m);
        fields["locked"].SetValue("fixed", 9);
        var revertedJson = Encoding.UTF8.GetString(serializer.Serialize(state));
        Assert.DoesNotContain("\"oldValues\"", revertedJson, StringComparison.Ordinal);
    }

    [Fact]
    public void Serialize_preserves_committed_current_bytes()
    {
        var fixturePath = Path.Combine(AppContext.BaseDirectory, "Serialization", "Fixtures", "axdata-snapshot-v3.json");
        var expected = File.ReadAllBytes(fixturePath);
        while (expected.Length > 0 && expected[^1] is (byte)'\n' or (byte)'\r')
        {
            expected = expected[..^1];
        }

        Assert.Equal(expected, CreateSerializer().Serialize(CreateState()));
    }

    [Fact]
    public void Versioned_payloads_are_not_backward_read()
    {
        var current = Encoding.UTF8.GetString(CreateSerializer().Serialize(CreateState()));
        var payload = Encoding.UTF8.GetBytes(current.Insert(1, "\"schemaVersion\":3,"));

        var exception = Assert.Throws<AxDataSnapshotException>(() => CreateSerializer().Deserialize(payload));

        Assert.IsType<JsonException>(exception.InnerException);
    }

    [Fact]
    public void Timestamped_payloads_are_not_backward_read()
    {
        var current = Encoding.UTF8.GetString(CreateSerializer().Serialize(CreateState()));
        var payload = Encoding.UTF8.GetBytes(current.Insert(1, "\"createdAtUtc\":\"2026-09-25T00:00:00Z\","));

        var exception = Assert.Throws<AxDataSnapshotException>(() => CreateSerializer().Deserialize(payload));

        Assert.IsType<JsonException>(exception.InnerException);
    }

    [Fact]
    public void Business_key_locator_payloads_are_not_backward_read()
    {
        var current = Encoding.UTF8.GetString(CreateSerializer().Serialize(CreateState()));
        var payload = Encoding.UTF8.GetBytes(current.Insert(1, "\"keyField\":\"employeeid\",\"keyValue\":\"42\","));

        var exception = Assert.Throws<AxDataSnapshotException>(() => CreateSerializer().Deserialize(payload));

        Assert.IsType<JsonException>(exception.InnerException);
    }

    [Theory]
    [InlineData("[[\"a\",0],[\"A\",0]]", "[\"x\",\"y\"]", null)]
    [InlineData("[[\"a\",0]]", "[]", null)]
    [InlineData("[[\"a\",0]]", "[\"x\",\"y\"]", null)]
    [InlineData("[[\"a\",0]]", "[\"x\"]", "[]")]
    [InlineData("[[\"a\",2]]", "[\"missing-id\"]", null)]
    [InlineData("[[\"a\",2]]", "[\"value~-1\"]", null)]
    [InlineData("[[\"a\",4]]", "[\"x\"]", null)]
    [InlineData("[{\"name\":\"a\",\"sourceKey\":false}]", "[\"x\"]", null)]
    [InlineData("[[\"a\",0]]", "[{}]", null)]
    public void Corrupt_positional_mappings_fail_closed(
        string fields,
        string values,
        string? oldValues)
    {
        var payload = MinimalPayload(fields, values, oldValues);

        Assert.Throws<AxDataSnapshotException>(() => CreateSerializer().Deserialize(Encoding.UTF8.GetBytes(payload)));
    }

    [Fact]
    public void Old_values_without_a_changed_field_fail_closed()
    {
        var payload = MinimalPayload(
            "[[\"a\",0]]",
            "[\"same\"]",
            "[\"same\"]");

        Assert.Throws<AxDataSnapshotException>(() => CreateSerializer().Deserialize(Encoding.UTF8.GetBytes(payload)));
    }

    [Fact]
    public void Legacy_sparse_source_metadata_fails_closed()
    {
        var payload = MinimalPayload(
            "[[\"a\",0]]",
            "[\"current\"]",
            null,
            ",\"sourceIds\":[[0,7]]");

        Assert.Throws<AxDataSnapshotException>(() => CreateSerializer().Deserialize(Encoding.UTF8.GetBytes(payload)));
    }

    [Fact]
    public void Serialize_rejects_rows_with_different_field_sets()
    {
        var state = CreateState();
        Assert.Throws<ArgumentException>(() => state.DataContainers["items"].AddRow(new AxDataRow(
            "row_second",
            AxRowState.New,
            fields: new Dictionary<string, AxFieldValue> { ["different"] = new("x") })));
    }

    [Fact]
    public void RoundTrip_preserves_row_reorder_delete_add_and_fillgrid_replacement_shape()
    {
        var state = CreateState();
        var fields = state.DataContainers["items"].Fields.ToDictionary(
            field => field.Name,
            field => new AxFieldValue(
                field.Name == "amount" ? 2m : null,
                field.IsReadOnly,
                isSourceKey: field.IsSourceKey),
            StringComparer.OrdinalIgnoreCase);
        state.DataContainers["items"].Rows.Clear();
        state.DataContainers["items"].AddRow(new AxDataRow("row_new", AxRowState.New, fields: fields));
        var persistedFields = fields.ToDictionary(
            item => item.Key,
            item => new AxFieldValue(item.Value.Value, item.Value.IsReadOnly, isSourceKey: item.Value.IsSourceKey),
            StringComparer.OrdinalIgnoreCase);
        state.DataContainers["items"].AddRow(new AxDataRow("row_deleted", AxRowState.Deleted, 52, persistedFields));
        state.DataContainers["items"].Rows.Reverse();

        var restored = CreateSerializer().Deserialize(CreateSerializer().Serialize(state));

        Assert.Equal(["row_deleted", "row_new"], restored.DataContainers["items"].Rows.Select(row => row.RowId));
        Assert.Equal(AxRowState.Deleted, restored.DataContainers["items"].Rows[0].State);
        Assert.Equal(52, restored.DataContainers["items"].Rows[0].PersistedRecordId);
        Assert.Equal(AxRowState.New, restored.DataContainers["items"].Rows[1].State);
        Assert.Null(restored.DataContainers["items"].Rows[1].PersistedRecordId);
    }

    [Fact]
    public void Inactive_workflow_is_not_persisted()
    {
        var state = CreateState();
        state.Workflow!.Status = PegWorkflowStatus.Approved;

        var restored = CreateSerializer().Deserialize(CreateSerializer().Serialize(state));

        Assert.Null(restored.Workflow);
    }

    [Fact]
    public void Serialize_and_deserialize_enforce_size_row_field_and_type_limits()
    {
        Assert.Throws<AxDataSnapshotException>(() => CreateSerializer(maximumBytes: 100).Serialize(CreateState()));
        Assert.Throws<AxDataSnapshotException>(() => CreateSerializer(maximumBytes: 10).Deserialize(new byte[11]));

        var tooManyRows = CreateState();
        tooManyRows.DataContainers["items"].AddRow(new AxDataRow(
            "row_second", AxRowState.New,
            fields: tooManyRows.DataContainers["items"].Rows[0].Fields.ToDictionary(
                item => item.Key,
                item => new AxFieldValue(item.Value.Value, item.Value.IsReadOnly, isSourceKey: item.Value.IsSourceKey),
                StringComparer.OrdinalIgnoreCase)));
        Assert.Throws<AxDataSnapshotException>(() => CreateSerializer(maximumRows: 1).Serialize(tooManyRows));
        Assert.Throws<AxDataSnapshotException>(() => CreateSerializer(maximumFields: 4).Serialize(CreateState()));

        var unsupported = CreateState();
        unsupported.DataContainers["items"].Rows[0].Fields.Add("service", new AxFieldValue(new object()));
        Assert.Throws<AxDataSnapshotException>(() => CreateSerializer().Serialize(unsupported));
    }

    [Fact]
    public void Representative_payload_reports_round_trip_metrics()
    {
        var serializer = CreateSerializer();
        var metrics = MeasureRoundTrip(serializer, CreateState());

        Console.WriteLine(
            $"AXD-STATE-03 representative: bytes={metrics.Bytes}; " +
            $"serializeMs={metrics.SerializeElapsed.TotalMilliseconds:F3}; " +
            $"deserializeMs={metrics.DeserializeElapsed.TotalMilliseconds:F3}");
        Assert.True(metrics.Bytes > 0);
    }

    [Fact]
    public void Capacity_envelope_is_deterministic_and_fits_compact_limit()
    {
        var state = CreateCapacityState();
        var serializer = CreateSerializer(
            maximumBytes: AxDataSnapshotOptions.DefaultMaximumSerializedBytes,
            maximumRows: AxDataSnapshotOptions.DefaultMaximumGridRows,
            maximumFields: AxDataSnapshotOptions.DefaultMaximumFieldInstances);
        _ = serializer.Serialize(CreateState());
        var metrics = MeasureCapacityRoundTrip(serializer, state);

        Console.WriteLine(
            $"AXD-STATE-03 capacity: bytes={metrics.Bytes}; serializeMs={metrics.Elapsed.TotalMilliseconds:F2}; " +
            $"deserializeMs={metrics.DeserializeElapsed.TotalMilliseconds:F2}; allocatedBytes={metrics.AllocatedBytes}; " +
            $"releasedPayloadBytes={metrics.Bytes}");

        Assert.True(metrics.Bytes <= AxDataSnapshotOptions.DefaultMaximumSerializedBytes);
        Assert.True(metrics.Elapsed < TimeSpan.FromMilliseconds(500), $"Serialization took {metrics.Elapsed}.");
        Assert.True(metrics.AllocatedBytes < 256L * 1024 * 1024);
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
            ["amount"] = AxFieldValue.Rehydrate(1234.50m, 1200m, true, false),
            ["date"] = new AxFieldValue(new DateOnly(2026, 9, 25), isReadOnly: true),
            ["binary"] = new AxFieldValue(new byte[] { 1, 2, 3 }),
            ["enabled"] = new AxFieldValue(true),
            ["optional"] = new AxFieldValue(null),
            ["locked"] = AxFieldValue.Rehydrate(
                "fixed", "fixed", true, false, sourceId: 9, originalSourceId: 8, isSourceKey: true)
        };
        var container = new AxDataContainer(
            "items",
            isGrid: true,
            [new AxDataRow("row_z", AxRowState.Edited, 42, fields)]);

        var state = AxDataState.Rehydrate(
            "obj-1", "tenant-a", "employee", 42,
            "initiator", CreatedAt, CreatedAt.AddMinutes(5),
            AxDataLifecycleStatus.PendingApproval, [container],
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
        return state;
    }

    private static AxDataState CreateCapacityState()
    {
        var state = new AxDataState("obj-capacity", "tenant-a", "capacity", "owner", CreatedAt);
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

    private static string MinimalPayload(
        string fields,
        string values,
        string? oldValues,
        string extraRowProperties = "") =>
        "{" +
        "\"objectId\":\"obj\",\"tenantId\":\"tenant\",\"transId\":\"trans\"," +
        "\"initiatorUserName\":\"owner\"," +
        "\"status\":\"Active\"," +
        "\"dataContainers\":[{\"dcId\":\"dc1\",\"isGrid\":true,\"fields\":" + fields +
        ",\"rows\":[{\"rowId\":\"row1\",\"state\":\"New\",\"values\":" + values +
        (oldValues is null ? string.Empty : ",\"oldValues\":" + oldValues) +
        extraRowProperties + "}]}]}";

    private static int CountOccurrences(string value, string fragment)
    {
        var count = 0;
        var index = 0;
        while ((index = value.IndexOf(fragment, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += fragment.Length;
        }

        return count;
    }

    private static RoundTripMetrics MeasureRoundTrip(AxDataSnapshotSerializer serializer, AxDataState state)
    {
        var timer = Stopwatch.StartNew();
        var payload = serializer.Serialize(state);
        timer.Stop();
        var serializeElapsed = timer.Elapsed;
        timer.Restart();
        _ = serializer.Deserialize(payload);
        timer.Stop();
        return new RoundTripMetrics(payload.Length, serializeElapsed, timer.Elapsed);
    }

    private static CapacityMetrics MeasureCapacitySerialization(AxDataSnapshotSerializer serializer, AxDataState state)
    {
        byte[]? expected = null;
        var bestElapsed = TimeSpan.MaxValue;
        var bestAllocatedBytes = long.MaxValue;
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            var timer = Stopwatch.StartNew();
            var current = serializer.Serialize(state);
            timer.Stop();
            var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
            expected ??= current;
            Assert.True(expected.AsSpan().SequenceEqual(current), "Capacity snapshots were not deterministic.");
            if (timer.Elapsed < bestElapsed)
            {
                bestElapsed = timer.Elapsed;
                bestAllocatedBytes = allocatedBytes;
            }
        }

        return new CapacityMetrics(expected!.Length, bestElapsed, bestAllocatedBytes, expected);
    }

    private static CapacityRoundTripMetrics MeasureCapacityRoundTrip(
        AxDataSnapshotSerializer serializer,
        AxDataState state)
    {
        CapacityMetrics serialization = MeasureCapacitySerialization(serializer, state);
        TimeSpan deserializeElapsed = MeasureDeserialization(serializer, serialization.Payload);
        return new(
            serialization.Bytes,
            serialization.Elapsed,
            serialization.AllocatedBytes,
            deserializeElapsed);
    }

    private static TimeSpan MeasureDeserialization(AxDataSnapshotSerializer serializer, byte[] payload)
    {
        var best = TimeSpan.MaxValue;
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var timer = Stopwatch.StartNew();
            _ = serializer.Deserialize(payload);
            timer.Stop();
            if (timer.Elapsed < best)
            {
                best = timer.Elapsed;
            }
        }

        return best;
    }

    private sealed record CapacityMetrics(int Bytes, TimeSpan Elapsed, long AllocatedBytes, byte[] Payload);
    private sealed record CapacityRoundTripMetrics(
        int Bytes,
        TimeSpan Elapsed,
        long AllocatedBytes,
        TimeSpan DeserializeElapsed);
    private sealed record RoundTripMetrics(int Bytes, TimeSpan SerializeElapsed, TimeSpan DeserializeElapsed);
}

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class SnapshotCapacityCollection
{
    public const string Name = "Snapshot capacity";
}
