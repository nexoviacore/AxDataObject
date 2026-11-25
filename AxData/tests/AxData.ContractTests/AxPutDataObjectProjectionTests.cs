using AxData.Contracts.Snapshots;
using AxStructDef;
using AxStructDef.Models;
using AxTstructData;

namespace AxData.ContractTests;

public sealed class AxPutDataObjectProjectionTests
{
    [Fact]
    public void Current_snapshot_hydrates_rows_values_record_ids_and_source_ids()
    {
        DataObjectSnapshotV3 snapshot = Snapshot();

        Assert.Null(AxPutDataObjectProjection.ValidateSnapshot(snapshot, "tenant-a", "object-a"));
        var data = AxPutDataObjectProjection.ToSubmitData(snapshot);

        Assert.Equal("42", data["dc1"]["row1"]["axp_recid"]);
        Assert.Equal("edit", data["dc1"]["row1"]["axrow_action"]);
        Assert.Equal("Widget", data["dc1"]["row1"]["name"]);
        Assert.Equal("7", data["dc1"]["row1"]["customer_sourceid"]);
    }

    [Fact]
    public void Source_key_uses_last_tilde_while_normal_field_tilde_remains_literal()
    {
        DataObjectSnapshotV3 snapshot = Snapshot();
        DataRowSnapshotV3 row = snapshot.DataContainers![0].Rows[0];
        row.Values[0] = SnapshotScalar.String("Widget~123");
        row.Values[1] = SnapshotScalar.String("North~Admin~7");

        Assert.Null(AxPutDataObjectProjection.ValidateSnapshot(snapshot, "tenant-a", "object-a"));
        var data = AxPutDataObjectProjection.ToSubmitData(snapshot);

        Assert.Equal("Widget~123", data["dc1"]["row1"]["name"]);
        Assert.False(data["dc1"]["row1"].ContainsKey("name_sourceid"));
        Assert.Equal("North~Admin", data["dc1"]["row1"]["customer"]);
        Assert.Equal("7", data["dc1"]["row1"]["customer_sourceid"]);
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("admin~bad")]
    [InlineData("admin~-1")]
    public void Malformed_packed_source_key_fails_closed(string packed)
    {
        DataObjectSnapshotV3 snapshot = Snapshot();
        snapshot.DataContainers![0].Rows[0].Values[1] = SnapshotScalar.String(packed);

        Assert.Equal(
            "The DataObject contains invalid source-key field data.",
            AxPutDataObjectProjection.ValidateSnapshot(snapshot, "tenant-a", "object-a"));
    }

    [Fact]
    public void Unknown_field_flag_fails_closed()
    {
        DataObjectSnapshotV3 snapshot = Snapshot();
        snapshot.DataContainers![0].Fields[0] = new DataObjectFieldSnapshot("name", 4);

        Assert.Equal(
            "The DataObject contains invalid or duplicate field ordinals.",
            AxPutDataObjectProjection.ValidateSnapshot(snapshot, "tenant-a", "object-a"));
    }

    [Fact]
    public void Snapshot_rejects_dirty_baseline_identical_to_current_value_and_source()
    {
        DataObjectSnapshotV3 snapshot = Snapshot();
        List<DataContainerSnapshotV3> containers = Assert.IsType<List<DataContainerSnapshotV3>>(snapshot.DataContainers);
        DataRowSnapshotV3 row = containers[0].Rows[0];
        containers[0].Rows[0] = row with
        {
            OldValues = row.Values.ToList()
        };

        Assert.Equal(
            "The DataObject contains an oldValues array with no changed field.",
            AxPutDataObjectProjection.ValidateSnapshot(snapshot, "tenant-a", "object-a"));
    }

    [Fact]
    public void Only_initiator_is_allowed_even_when_an_unrelated_actor_has_former_admin_role()
    {
        DataObjectSnapshotV3 snapshot = Snapshot();

        Assert.True(AxPutDataObjectProjection.IsActorAllowed(snapshot, "initiator"));
        Assert.False(AxPutDataObjectProjection.IsActorAllowed(snapshot, "other"));
        Assert.False(AxPutDataObjectProjection.IsActorAllowed(snapshot, "owner"));
    }

    [Fact]
    public void Loaded_state_checks_unknown_cross_tenant_and_unrelated_actor_without_version_disclosure()
    {
        AxPutDataObjectCheck unknown = AxPutDataObjectProjection.CheckLoadedState(
            null, "tenant-a", "object-a", "initiator", []);
        AxPutDataObjectCheck crossTenant = AxPutDataObjectProjection.CheckLoadedState(
            Snapshot(), "tenant-b", "object-a", "initiator", []);
        AxPutDataObjectCheck unrelated = AxPutDataObjectProjection.CheckLoadedState(
            Snapshot(), "tenant-a", "object-a", "other", ["sales"]);

        Assert.Equal((AxPutObjectErrors.InvalidRequest, 404), (unknown.ErrorCode, unknown.HttpStatus));
        Assert.Equal((AxPutObjectErrors.InvalidRequest, 400), (crossTenant.ErrorCode, crossTenant.HttpStatus));
        Assert.Equal((AxPutObjectErrors.AccessDenied, 403), (unrelated.ErrorCode, unrelated.HttpStatus));
    }

    [Theory]
    [InlineData("Saving")]
    [InlineData("Saved")]
    public void Saving_and_saved_state_reject_competing_save(string status)
    {
        DataObjectSnapshotV3 snapshot = Snapshot(status);

        AxPutDataObjectCheck check = AxPutDataObjectProjection.CheckLoadedState(
            snapshot, "tenant-a", "object-a", "initiator", []);

        Assert.False(check.Success);
        Assert.Equal((AxPutObjectErrors.ActiveConflict, 409), (check.ErrorCode, check.HttpStatus));
    }

    [Fact]
    public void Overlay_rejects_unknown_dc_row_field_and_malformed_identities()
    {
        StructDef definition = Definition();
        DataObjectSnapshotV3 snapshot = Snapshot();

        Assert.Contains("Unknown data container", Validate("dc9", "row1", "name", "x"));
        Assert.Contains("Unknown row", Validate("dc1", "row2", "name", "x"));
        Assert.Contains("Unknown field", Validate("dc1", "row1", "missing", "x"));
        Assert.Contains("Invalid record identity", Validate("dc1", "row1", "axp_recid", "43"));
        Assert.Contains("Invalid source identity", Validate("dc1", "row1", "customer_sourceid", "bad"));

        string? Validate(string dc, string row, string field, string value) =>
            AxPutDataObjectProjection.ValidateAgainstStructure(
                new Dictionary<string, Dictionary<string, Dictionary<string, string>>>
                {
                    [dc] = new() { [row] = new() { [field] = value } }
                },
                definition,
                snapshot);
    }

    [Fact]
    public void Empty_overlay_is_valid_and_dirty_state_drives_permission_projection()
    {
        DataObjectSnapshotV3 snapshot = Snapshot();

        Assert.Null(AxPutDataObjectProjection.ValidateAgainstStructure([], Definition(), snapshot));
        var permissionData = AxPutDataObjectProjection.ToPermissionData(snapshot, []);

        Assert.Equal("Widget", permissionData["dc1"]["row1"]["name"]);
        Assert.False(permissionData["dc1"]["row1"].ContainsKey("customer"));
    }

    [Fact]
    public void Record_ids_map_to_stable_existing_and_generated_row_ids()
    {
        DataObjectSnapshotV3 snapshot = Snapshot();
        StructDef definition = new();
        TFrame header = new() { FrameNo = 1, RowCount = 1 };
        TFrame grid = new() { FrameNo = 2, RowCount = 2, AsGrid = true };
        definition.Frames!.Add(header);
        definition.Frames.Add(grid);
        definition.SubmitRecordId(1, 1, 42);
        definition.SubmitRecordId(2, 1, 101);
        definition.SubmitRecordId(2, 2, 102);
        AxPutDataObjectContext context = new()
        {
            ObjectId = "object-a",
            TenantId = "tenant-a",
            ObjectIdWasSupplied = true,
            InitiatorUserName = "initiator",
            TransId = "sales",
            Snapshot = snapshot
        };

        AxPutRecordIdCapture first = AxPutDataObjectProjection.CaptureRecordIdMappings(definition, context);
        AxPutRecordIdCapture second = AxPutDataObjectProjection.CaptureRecordIdMappings(definition, context);

        Assert.True(first.Success);
        Assert.Equal(3, first.Mappings.Count);
        Assert.Equal(new AxPutRecordIdMapping("dc1", "row-a", 42), first.Mappings[0]);
        Assert.Matches("^[0-7][0-9A-HJKMNP-TV-Z]{25}$", first.Mappings[1].RowId);
        Assert.Equal(first.Mappings, second.Mappings);
    }

    [Fact]
    public void Missing_primary_record_id_rejects_mapping_capture()
    {
        StructDef definition = new();
        definition.Frames!.Add(new TFrame { FrameNo = 1, RowCount = 1 });
        AxPutDataObjectContext context = new()
        {
            ObjectId = "object-new",
            TenantId = "tenant-a",
            ObjectIdWasSupplied = false,
            InitiatorUserName = "owner",
            TransId = "sales",
            RuntimeInitialized = true
        };

        AxPutRecordIdCapture capture = AxPutDataObjectProjection.CaptureRecordIdMappings(definition, context);

        Assert.False(capture.Success);
        Assert.Empty(capture.Mappings);
        Assert.Contains("primary row", capture.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Final_snapshot_applies_runtime_values_record_ids_and_lifecycle()
    {
        DataObjectSnapshotV3 source = Snapshot();
        StructDef definition = Definition();
        definition.Frames![0].RowCount = 1;
        definition.SubmitValue("name", 1, "Saved widget", "", 0, 0, 0);
        definition.SubmitValue("customer", 1, "Acme", "", 7, 0, 42);
        definition.SubmitRecordId(1, 1, 42);
        AxPutDataObjectContext context = new()
        {
            ObjectId = "object-a",
            TenantId = "tenant-a",
            ObjectIdWasSupplied = true,
            InitiatorUserName = "initiator",
            TransId = "sales",
            Snapshot = source
        };

        DataObjectSnapshotV3 finalized = AxPutDataObjectProjection.CreateFinalSnapshot(
            definition,
            context,
            [new AxPutRecordIdMapping("dc1", "row-a", 42)],
            "Active",
            DateTimeOffset.Parse("2026-11-06T12:00:00Z"));

        Assert.Equal("Active", finalized.Status);
        Assert.Null(typeof(DataObjectSnapshotV3).GetProperty("StructDef"));
        DataContainerSnapshotV3 container = Assert.Single(finalized.DataContainers!);
        DataRowSnapshotV3 row = Assert.Single(container.Rows);
        Assert.Equal(42, row.RecordId);
        Assert.Equal("Unchanged", row.State);
        Assert.Equal("Saved widget", row.Values[FieldIndex(container, "name")].Text);
        Assert.Null(row.OldValues);
        Assert.Equal("Acme~7", row.Values[FieldIndex(container, "customer")].Text);
        Assert.True(container.Fields[FieldIndex(container, "name")].ReadOnly);
        Assert.True(container.Fields[FieldIndex(container, "customer")].SourceKey);
    }

    [Fact]
    public void Final_snapshot_uses_structdef_numeric_type_without_inferring_string_business_types()
    {
        StructDef definition = new();
        definition.Frames!.Add(new TFrame { FrameNo = 1, RowCount = 1 });
        definition.Flds!.Add(new TField { FrameNo = 1, FieldName = "amount", DataType = "n" });
        definition.Flds.Add(new TField { FrameNo = 1, FieldName = "date_text", DataType = "c" });
        definition.SubmitValue("amount", 1, "12.50", "", 0, 0, 0);
        definition.SubmitValue("date_text", 1, "2026-11-16", "", 0, 0, 0);
        definition.SubmitRecordId(1, 1, 42);
        AxPutDataObjectContext context = new()
        {
            ObjectId = "object-new",
            TenantId = "tenant-a",
            ObjectIdWasSupplied = false,
            InitiatorUserName = "initiator",
            TransId = "sales"
        };

        DataObjectSnapshotV3 snapshot = AxPutDataObjectProjection.CreateFinalSnapshot(
            definition,
            context,
            [new AxPutRecordIdMapping("dc1", context.GetOrCreateRowId("dc1", 1), 42)],
            "Active",
            DateTimeOffset.Parse("2026-11-16T00:00:00Z"));

        DataContainerSnapshotV3 container = Assert.Single(snapshot.DataContainers!);
        DataRowSnapshotV3 row = Assert.Single(container.Rows);
        Assert.Equal(SnapshotScalarKind.Number, row.Values[FieldIndex(container, "amount")].Kind);
        Assert.Equal("12.50", row.Values[FieldIndex(container, "amount")].Text);
        Assert.Equal(SnapshotScalarKind.String, row.Values[FieldIndex(container, "date_text")].Kind);
        Assert.Equal("2026-11-16", row.Values[FieldIndex(container, "date_text")].Text);
    }

    [Fact]
    public void Versioned_reader_payload_is_rejected_instead_of_migrated()
    {
        const string payload = """
            {"schemaVersion":1,"objectId":"object-a","tenantId":"tenant-a","transId":"sales",
             "initiatorUserName":"initiator","status":"Active",
             "dataContainers":[]}
            """;

        Assert.Throws<System.Text.Json.JsonException>(() => DataObjectSnapshotReader.ReadCurrent(payload));
    }

    [Fact]
    public void Business_key_locator_payload_is_rejected_instead_of_retained()
    {
        const string payload = """
            {"objectId":"object-a","tenantId":"tenant-a","transId":"sales",
             "keyField":"salesid","keyValue":"42","recordId":42,
             "initiatorUserName":"initiator","status":"Active","dataContainers":[]}
            """;

        Assert.Throws<System.Text.Json.JsonException>(() => DataObjectSnapshotReader.ReadCurrent(payload));
    }

    private static StructDef Definition()
    {
        StructDef definition = new();
        definition.Frames!.Add(new TFrame { FrameNo = 1 });
        definition.Flds!.Add(new TField { FrameNo = 1, FieldName = "name", Readonly = true });
        definition.Flds.Add(new TField { FrameNo = 1, FieldName = "customer", SourceKey = true });
        return definition;
    }

    private static DataObjectSnapshotV3 Snapshot(string status = "Active") => new()
    {
        ObjectId = "object-a",
        TenantId = "tenant-a",
        TransId = "sales",
        RecordId = 42,
        InitiatorUserName = "initiator",
        Status = status,
        DataContainers =
        [
            new DataContainerSnapshotV3(
                "dc1",
                false,
                [new DataObjectFieldSnapshot("name", DataObjectFieldSnapshot.ReadOnlyFlag), new DataObjectFieldSnapshot("customer", DataObjectFieldSnapshot.SourceKeyFlag)],
                [
                    new DataRowSnapshotV3(
                        "row-a",
                        42,
                        "Edited",
                        [
                            SnapshotScalar.String("Widget"),
                            SnapshotScalar.String("Acme~7")
                        ],
                        [SnapshotScalar.String("Old widget"), SnapshotScalar.String("Acme~7")])
                ])
        ]
    };

    private static int FieldIndex(DataContainerSnapshotV3 container, string name) =>
        container.Fields.FindIndex(field => string.Equals(field.Name, name, StringComparison.OrdinalIgnoreCase));
}
