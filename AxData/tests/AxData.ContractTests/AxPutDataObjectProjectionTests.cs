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
    public void Owner_initiator_and_admin_are_allowed_but_unrelated_actor_is_denied()
    {
        DataObjectSnapshotV3 snapshot = Snapshot();

        Assert.True(AxPutDataObjectProjection.IsActorAllowed(snapshot, "owner", []));
        Assert.True(AxPutDataObjectProjection.IsActorAllowed(snapshot, "initiator", []));
        Assert.True(AxPutDataObjectProjection.IsActorAllowed(snapshot, "other", ["Admin"]));
        Assert.False(AxPutDataObjectProjection.IsActorAllowed(snapshot, "other", ["sales"]));
    }

    [Fact]
    public void Loaded_state_checks_unknown_cross_tenant_and_unrelated_actor_without_version_disclosure()
    {
        AxPutDataObjectCheck unknown = AxPutDataObjectProjection.CheckLoadedState(
            null, "tenant-a", "object-a", "owner", []);
        AxPutDataObjectCheck crossTenant = AxPutDataObjectProjection.CheckLoadedState(
            Snapshot(), "tenant-b", "object-a", "owner", []);
        AxPutDataObjectCheck unrelated = AxPutDataObjectProjection.CheckLoadedState(
            Snapshot(), "tenant-a", "object-a", "other", ["sales"]);

        Assert.Equal((AxPutObjectErrors.InvalidRequest, 404, null),
            (unknown.ErrorCode, unknown.HttpStatus, unknown.CurrentVersion));
        Assert.Equal((AxPutObjectErrors.InvalidRequest, 400, null),
            (crossTenant.ErrorCode, crossTenant.HttpStatus, crossTenant.CurrentVersion));
        Assert.Equal((AxPutObjectErrors.AccessDenied, 403, null),
            (unrelated.ErrorCode, unrelated.HttpStatus, unrelated.CurrentVersion));
    }

    [Fact]
    public void Stale_version_and_definition_mismatch_return_current_version()
    {
        DataObjectSnapshotV3 snapshot = Snapshot();

        AxPutDataObjectCheck stale = AxPutDataObjectProjection.CheckExpectedVersion(snapshot, 2);
        AxPutDataObjectCheck definition = AxPutDataObjectProjection.CheckStructDefVersion(snapshot, "hash-b");

        Assert.Equal((AxPutObjectErrors.StaleVersion, 409, 3L),
            (stale.ErrorCode, stale.HttpStatus, stale.CurrentVersion));
        Assert.Equal((AxPutObjectErrors.StructDefChanged, 409, 3L),
            (definition.ErrorCode, definition.HttpStatus, definition.CurrentVersion));
        Assert.True(AxPutDataObjectProjection.CheckExpectedVersion(snapshot, 3).Success);
        Assert.True(AxPutDataObjectProjection.CheckStructDefVersion(snapshot, "HASH-A").Success);
    }

    [Theory]
    [InlineData("Saving")]
    [InlineData("Saved")]
    public void Saving_and_saved_state_reject_competing_save(string status)
    {
        DataObjectSnapshotV3 snapshot = Snapshot(status);

        AxPutDataObjectCheck check = AxPutDataObjectProjection.CheckLoadedState(
            snapshot, "tenant-a", "object-a", "owner", []);

        Assert.False(check.Success);
        Assert.Equal((AxPutObjectErrors.ActiveConflict, 409, 3L),
            (check.ErrorCode, check.HttpStatus, check.CurrentVersion));
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
            Version = 3,
            ObjectIdWasSupplied = true,
            OwnerUserName = "owner",
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
            Version = 1,
            ObjectIdWasSupplied = false,
            OwnerUserName = "owner",
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
    public void Final_snapshot_applies_runtime_values_record_ids_lifecycle_and_one_version_advance()
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
            Version = 3,
            ObjectIdWasSupplied = true,
            OwnerUserName = "owner",
            InitiatorUserName = "initiator",
            TransId = "sales",
            CurrentStructDefVersion = "hash-a",
            Snapshot = source
        };

        DataObjectSnapshotV3 finalized = AxPutDataObjectProjection.CreateFinalSnapshot(
            definition,
            context,
            [new AxPutRecordIdMapping("dc1", "row-a", 42)],
            4,
            "Saved",
            DateTimeOffset.Parse("2026-11-06T12:00:00Z"));

        Assert.Equal(4, finalized.Version);
        Assert.Equal("Saved", finalized.Status);
        DataRowSnapshotV3 row = Assert.Single(Assert.Single(finalized.DataContainers!).Rows);
        Assert.Equal(42, row.RecordId);
        Assert.Equal("Unchanged", row.State);
        Assert.Equal("Saved widget", row.Fields.Single(field => field.Name == "name").Value.Value);
    }

    [Fact]
    public void V1_reader_normalizes_to_current_contract()
    {
        const string payload = """
            {"schemaVersion":1,"objectId":"object-a","tenantId":"tenant-a","transId":"sales",
             "ownerUserName":"owner","initiatorUserName":"initiator","version":3,
             "structDef":{"transId":"sales","version":"hash-a"},"status":"Active",
             "createdAtUtc":"2026-11-05T00:00:00Z","lastAccessedAtUtc":"2026-11-05T00:00:00Z",
             "dataContainers":[{"name":"dc1","isGrid":false,"rows":[{"rowId":"legacy-row1",
             "persistedRecordId":42,"state":"Unchanged","fields":[{"name":"name",
             "value":{"kind":"string","value":"Widget"},"originalValue":{"kind":"string","value":"Widget"},
             "isDirty":false,"isReadOnly":false}]}]}],"files":[],"axOpen":true}
            """;

        DataObjectSnapshotV3 snapshot = DataObjectSnapshotReader.ReadCurrent(payload);

        Assert.Equal(3, snapshot.SchemaVersion);
        Assert.Equal("dc1", Assert.Single(snapshot.DataContainers!).DcId);
        Assert.Equal(42, Assert.Single(Assert.Single(snapshot.DataContainers!).Rows).RecordId);
    }

    private static StructDef Definition()
    {
        StructDef definition = new();
        definition.Frames!.Add(new TFrame { FrameNo = 1 });
        definition.Flds!.Add(new TField { FrameNo = 1, FieldName = "name" });
        definition.Flds.Add(new TField { FrameNo = 1, FieldName = "customer", SourceKey = true });
        return definition;
    }

    private static DataObjectSnapshotV3 Snapshot(string status = "Active") => new()
    {
        SchemaVersion = 3,
        ObjectId = "object-a",
        TenantId = "tenant-a",
        TransId = "sales",
        KeyField = "salesid",
        KeyValue = "42",
        RecordId = 42,
        OwnerUserName = "owner",
        InitiatorUserName = "initiator",
        Version = 3,
        StructDef = new StructDefReferenceSnapshot("sales", "hash-a"),
        CreatedAtUtc = "2026-11-05T00:00:00Z",
        LastAccessedAtUtc = "2026-11-05T00:00:00Z",
        Status = status,
        DataContainers =
        [
            new DataContainerSnapshotV3(
                "dc1",
                false,
                [
                    new DataRowSnapshotV3(
                        "row-a",
                        42,
                        "Edited",
                        [
                            new FieldSnapshotV3(
                                "name",
                                new SnapshotValue("string", "Widget"),
                                IsDirty: true,
                                OriginalValue: new SnapshotValue("string", "Old widget")),
                            new FieldSnapshotV3(
                                "customer",
                                new SnapshotValue("string", "Acme"),
                                SourceId: 7)
                        ])
                ])
        ],
        Files = []
    };
}
