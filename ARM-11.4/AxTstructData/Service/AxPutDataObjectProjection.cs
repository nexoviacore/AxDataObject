using System.Globalization;
using System.Text.RegularExpressions;
using AxData.Contracts.Snapshots;
using AxStructDef;
using AxStructDef.Models;

namespace AxTstructData;

public static partial class AxPutDataObjectProjection
{
    public static AxPutDataObjectCheck CheckLoadedState(
        DataObjectSnapshotV3? snapshot,
        string tenantId,
        string objectId,
        string userName,
        IEnumerable<string> roles)
    {
        if (snapshot is null)
        {
            return new AxPutDataObjectCheck(
                false,
                "The supplied objectid was not found.",
                AxPutObjectErrors.InvalidRequest,
                404);
        }

        string? snapshotError = ValidateSnapshot(snapshot, tenantId, objectId);
        if (snapshotError is not null)
        {
            return new AxPutDataObjectCheck(
                false,
                snapshotError,
                AxPutObjectErrors.InvalidRequest,
                400);
        }

        if (!IsActorAllowed(snapshot, userName, roles))
        {
            return new AxPutDataObjectCheck(
                false,
                "The current actor is not allowed to save this DataObject.",
                AxPutObjectErrors.AccessDenied,
                403);
        }

        if (string.Equals(snapshot.Status, "Closed", StringComparison.OrdinalIgnoreCase))
        {
            return new AxPutDataObjectCheck(
                false,
                "The supplied objectid was not found.",
                AxPutObjectErrors.InvalidRequest,
                404);
        }

        if (string.Equals(snapshot.Status, "Saving", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(snapshot.Status, "Saved", StringComparison.OrdinalIgnoreCase))
        {
            return new AxPutDataObjectCheck(
                false,
                "The DataObject has an active conflicting operation.",
                AxPutObjectErrors.ActiveConflict,
                409,
                snapshot.Version);
        }

        return new AxPutDataObjectCheck(true, CurrentVersion: snapshot.Version);
    }

    public static AxPutDataObjectCheck CheckExpectedVersion(DataObjectSnapshotV3 snapshot, long? expectedVersion) =>
        expectedVersion == snapshot.Version
            ? new AxPutDataObjectCheck(true, CurrentVersion: snapshot.Version)
            : new AxPutDataObjectCheck(
                false,
                "The DataObject was changed by another request.",
                AxPutObjectErrors.StaleVersion,
                409,
                snapshot.Version);

    public static AxPutDataObjectCheck CheckStructDefVersion(
        DataObjectSnapshotV3 snapshot,
        string? currentVersion) =>
        string.IsNullOrWhiteSpace(currentVersion)
            ? new AxPutDataObjectCheck(
                false,
                "The current transaction definition could not be resolved.",
                AxPutObjectErrors.StateUnavailable,
                503,
                snapshot.Version)
            : string.Equals(snapshot.StructDef?.Version, currentVersion, StringComparison.OrdinalIgnoreCase)
                ? new AxPutDataObjectCheck(true, CurrentVersion: snapshot.Version)
                : new AxPutDataObjectCheck(
                    false,
                    "The transaction definition changed. Re-initialize the DataObject.",
                    AxPutObjectErrors.StructDefChanged,
                    409,
                    snapshot.Version);

    public static string? ValidateSnapshot(
        DataObjectSnapshotV3 snapshot,
        string tenantId,
        string objectId)
    {
        if (snapshot.SchemaVersion != 3 ||
            string.IsNullOrWhiteSpace(snapshot.ObjectId) ||
            string.IsNullOrWhiteSpace(snapshot.TenantId) ||
            string.IsNullOrWhiteSpace(snapshot.TransId) ||
            string.IsNullOrWhiteSpace(snapshot.OwnerUserName) ||
            string.IsNullOrWhiteSpace(snapshot.InitiatorUserName) ||
            snapshot.Version < 1 ||
            snapshot.StructDef is null ||
            string.IsNullOrWhiteSpace(snapshot.StructDef.Version) ||
            snapshot.DataContainers is null)
        {
            return "The DataObject payload is incomplete.";
        }

        if (!string.Equals(snapshot.TenantId, tenantId, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(snapshot.ObjectId, objectId, StringComparison.OrdinalIgnoreCase))
        {
            return "The DataObject identity does not match the requested tenant and objectid.";
        }

        if (!string.Equals(snapshot.TransId, snapshot.StructDef.TransId, StringComparison.OrdinalIgnoreCase))
            return "The DataObject transaction does not match its StructDef reference.";

        if (snapshot.RecordId is <= 0)
            return "The DataObject contains an invalid primary record identity.";

        HashSet<string> containers = new(StringComparer.OrdinalIgnoreCase);
        foreach (DataContainerSnapshotV3 container in snapshot.DataContainers)
        {
            if (string.IsNullOrWhiteSpace(container.DcId) ||
                !DcName().IsMatch(container.DcId) ||
                !containers.Add(container.DcId) ||
                container.Rows is null)
            {
                return "The DataObject contains an invalid or duplicate data container.";
            }

            HashSet<string> rowIds = new(StringComparer.Ordinal);
            foreach (DataRowSnapshotV3 row in container.Rows)
            {
                if (string.IsNullOrWhiteSpace(row.RowId) || !rowIds.Add(row.RowId))
                    return "The DataObject contains an invalid or duplicate row identity.";

                bool isNew = string.Equals(row.State, "New", StringComparison.OrdinalIgnoreCase);
                if ((isNew && row.RecordId is not null) || (!isNew && row.RecordId is not > 0))
                    return "The DataObject contains an invalid row record identity.";

                if (!IsRowState(row.State) || row.Fields is null)
                    return "The DataObject contains an invalid row state.";

                HashSet<string> fields = new(StringComparer.OrdinalIgnoreCase);
                foreach (FieldSnapshotV3 field in row.Fields)
                {
                    if (string.IsNullOrWhiteSpace(field.Name) ||
                        !fields.Add(field.Name) ||
                        !IsSnapshotValueValid(field.Value) ||
                        (field.IsDirty is true && !IsSnapshotValueValid(field.OriginalValue)) ||
                        (field.IsDirty is not true && (field.OriginalValue is not null || field.OriginalSourceId is not null)) ||
                        field.SourceId < 0 ||
                        field.OriginalSourceId < 0)
                    {
                        return "The DataObject contains invalid field data.";
                    }
                }
            }
        }

        return null;
    }

    public static bool IsActorAllowed(
        DataObjectSnapshotV3 snapshot,
        string userName,
        IEnumerable<string> roles)
    {
        if (string.Equals(snapshot.OwnerUserName, userName, StringComparison.Ordinal) ||
            string.Equals(snapshot.InitiatorUserName, userName, StringComparison.Ordinal))
        {
            return true;
        }

        return roles.Any(role => string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase));
    }

    public static Dictionary<string, Dictionary<string, Dictionary<string, string>>> ToSubmitData(
        DataObjectSnapshotV3 snapshot)
    {
        Dictionary<string, Dictionary<string, Dictionary<string, string>>> result =
            new(StringComparer.OrdinalIgnoreCase);

        foreach (DataContainerSnapshotV3 container in snapshot.DataContainers ?? [])
        {
            Dictionary<string, Dictionary<string, string>> rows =
                new(StringComparer.OrdinalIgnoreCase);
            for (int index = 0; index < container.Rows.Count; index++)
            {
                DataRowSnapshotV3 row = container.Rows[index];
                Dictionary<string, string> values = new(StringComparer.OrdinalIgnoreCase)
                {
                    ["axp_recid"] = (row.RecordId ?? 0).ToString(CultureInfo.InvariantCulture),
                    ["axrow_action"] = ToLegacyRowAction(row.State)
                };

                foreach (FieldSnapshotV3 field in row.Fields)
                {
                    values[field.Name] = SnapshotValueText(field.Value);
                    if (field.SourceId is long sourceId)
                        values[$"{field.Name}_sourceid"] = sourceId.ToString(CultureInfo.InvariantCulture);
                }

                rows[$"row{index + 1}"] = values;
            }

            result[container.DcId] = rows;
        }

        return result;
    }

    public static Dictionary<string, Dictionary<string, Dictionary<string, string>>> ToPermissionData(
        DataObjectSnapshotV3 snapshot,
        Dictionary<string, Dictionary<string, Dictionary<string, string>>>? overlay)
    {
        Dictionary<string, Dictionary<string, Dictionary<string, string>>> result =
            new(StringComparer.OrdinalIgnoreCase);

        foreach (DataContainerSnapshotV3 container in snapshot.DataContainers ?? [])
        {
            for (int index = 0; index < container.Rows.Count; index++)
            {
                DataRowSnapshotV3 row = container.Rows[index];
                bool includeAll = string.Equals(row.State, "New", StringComparison.OrdinalIgnoreCase);
                List<FieldSnapshotV3> changed = row.Fields
                    .Where(field => includeAll || field.IsDirty is true)
                    .ToList();
                if (changed.Count == 0 && string.Equals(row.State, "Unchanged", StringComparison.OrdinalIgnoreCase))
                    continue;

                Dictionary<string, Dictionary<string, string>> rows = GetOrAdd(result, container.DcId);
                Dictionary<string, string> values = new(StringComparer.OrdinalIgnoreCase)
                {
                    ["axp_recid"] = (row.RecordId ?? 0).ToString(CultureInfo.InvariantCulture),
                    ["axrow_action"] = ToLegacyRowAction(row.State)
                };
                foreach (FieldSnapshotV3 field in changed)
                {
                    values[field.Name] = SnapshotValueText(field.Value);
                    if (field.SourceId is long sourceId)
                        values[$"{field.Name}_sourceid"] = sourceId.ToString(CultureInfo.InvariantCulture);
                }
                rows[$"row{index + 1}"] = values;
            }
        }

        foreach ((string dcId, Dictionary<string, Dictionary<string, string>> rows) in overlay ?? [])
        {
            Dictionary<string, Dictionary<string, string>> targetRows = GetOrAdd(result, dcId);
            foreach ((string rowName, Dictionary<string, string> values) in rows)
            {
                if (!targetRows.TryGetValue(rowName, out Dictionary<string, string>? targetValues))
                {
                    targetValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    targetRows[rowName] = targetValues;
                }

                foreach ((string fieldName, string value) in values)
                    targetValues[fieldName] = value;
            }
        }

        return result;
    }

    public static AxPutRecordIdCapture CaptureRecordIdMappings(
        StructDef definition,
        AxPutDataObjectContext context)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(context);

        List<AxPutRecordIdMapping> mappings = [];
        HashSet<string> addresses = new(StringComparer.OrdinalIgnoreCase);
        foreach (TFrame frame in definition.Frames ?? [])
        {
            string dcId = $"dc{frame.FrameNo}";
            int rowCount = definition.GetRowCount(frame.FrameNo);
            for (int rowNo = 1; rowNo <= rowCount; rowNo++)
            {
                string action = definition.GetRowAction(frame.FrameNo, rowNo);
                if (string.Equals(action, "delete", StringComparison.OrdinalIgnoreCase))
                    continue;

                long recordId = definition.GetRecordId(frame.FrameNo, rowNo);
                if (recordId <= 0)
                {
                    if (frame.FrameNo == 1 && rowNo == 1)
                    {
                        return new AxPutRecordIdCapture(
                            false,
                            [],
                            "The saved primary row did not receive a record identity.");
                    }
                    continue;
                }

                string rowId = context.GetOrCreateRowId(dcId, rowNo);
                string address = $"{dcId}/{rowId}";
                if (!addresses.Add(address))
                {
                    return new AxPutRecordIdCapture(
                        false,
                        [],
                        $"The saved row identity '{address}' is duplicated.");
                }

                mappings.Add(new AxPutRecordIdMapping(dcId, rowId, recordId));
            }
        }

        return new AxPutRecordIdCapture(true, mappings);
    }

    public static DataObjectSnapshotV3 CreateSavingSnapshot(
        StructDef definition,
        AxPutDataObjectContext context,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(context);

        if (context.Snapshot is DataObjectSnapshotV3 existing)
        {
            return CopySnapshot(
                existing,
                existing.Version,
                "Saving",
                now,
                existing.RecordId,
                existing.DataContainers ?? []);
        }

        return CreateFinalSnapshot(definition, context, [], context.Version, "Saving", now);
    }

    public static DataObjectSnapshotV3 CreateFinalSnapshot(
        StructDef definition,
        AxPutDataObjectContext context,
        IReadOnlyList<AxPutRecordIdMapping> mappings,
        long version,
        string status,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(mappings);
        ArgumentException.ThrowIfNullOrWhiteSpace(status);

        Dictionary<string, AxPutRecordIdMapping> recordIds = mappings.ToDictionary(
            mapping => $"{mapping.DcId.ToUpperInvariant()}:{mapping.RowId}",
            StringComparer.Ordinal);
        List<DataContainerSnapshotV3> containers = [];
        foreach (TFrame frame in definition.Frames ?? [])
        {
            string dcId = $"dc{frame.FrameNo}";
            DataContainerSnapshotV3? storedContainer = context.Snapshot?.DataContainers?
                .SingleOrDefault(container => string.Equals(container.DcId, dcId, StringComparison.OrdinalIgnoreCase));
            List<TField> fields = (definition.Flds ?? [])
                .Where(field => field.FrameNo == frame.FrameNo)
                .ToList();
            List<DataRowSnapshotV3> rows = [];
            int rowCount = definition.GetRowCount(frame.FrameNo);
            for (int rowNo = 1; rowNo <= rowCount; rowNo++)
            {
                if (string.Equals(definition.GetRowAction(frame.FrameNo, rowNo), "delete", StringComparison.OrdinalIgnoreCase))
                    continue;

                DataRowSnapshotV3? storedRow = storedContainer?.Rows.ElementAtOrDefault(rowNo - 1);
                string rowId = context.GetOrCreateRowId(dcId, rowNo);
                long runtimeRecordId = definition.GetRecordId(frame.FrameNo, rowNo);
                long? recordId = recordIds.TryGetValue($"{dcId.ToUpperInvariant()}:{rowId}", out AxPutRecordIdMapping? mapping)
                    ? mapping.RecordId
                    : runtimeRecordId > 0 ? runtimeRecordId : storedRow?.RecordId;
                List<FieldSnapshotV3> projectedFields = [];
                foreach (TField field in fields)
                {
                    FieldSnapshotV3? storedField = storedRow?.Fields.SingleOrDefault(candidate =>
                        string.Equals(candidate.Name, field.FieldName, StringComparison.OrdinalIgnoreCase));
                    string value = definition.GetFieldValue(field, rowNo);
                    SnapshotValue projectedValue = ToSnapshotValue(value, field, storedField?.Value);
                    long runtimeSourceId = definition.GetIDValue(field, rowNo);
                    projectedFields.Add(new FieldSnapshotV3(
                        field.FieldName,
                        projectedValue,
                        storedField?.IsReadOnly,
                        runtimeSourceId > 0 ? runtimeSourceId : null));
                }

                rows.Add(new DataRowSnapshotV3(rowId, recordId, "Unchanged", projectedFields));
            }

            containers.Add(new DataContainerSnapshotV3(dcId, frame.AsGrid, rows));
        }

        long? primaryRecordId = containers
            .SingleOrDefault(container => string.Equals(container.DcId, "dc1", StringComparison.OrdinalIgnoreCase))?
            .Rows.FirstOrDefault()?.RecordId;
        DataObjectSnapshotV3? source = context.Snapshot;
        return new DataObjectSnapshotV3
        {
            SchemaVersion = 3,
            ObjectId = context.ObjectId,
            TenantId = context.TenantId,
            TransId = context.TransId,
            KeyField = context.KeyField ?? source?.KeyField,
            KeyValue = context.KeyValue ?? source?.KeyValue,
            RecordId = primaryRecordId ?? source?.RecordId,
            OwnerUserName = context.OwnerUserName,
            InitiatorUserName = context.InitiatorUserName,
            Version = version,
            StructDef = new StructDefReferenceSnapshot(
                context.TransId,
                context.CurrentStructDefVersion ?? source?.StructDef?.Version
                    ?? throw new InvalidOperationException("The current StructDef version is unavailable.")),
            CreatedAtUtc = source?.CreatedAtUtc ?? now.ToString("O", CultureInfo.InvariantCulture),
            LastAccessedAtUtc = now.ToString("O", CultureInfo.InvariantCulture),
            ExpiresAtUtc = source?.ExpiresAtUtc,
            Status = status,
            DataContainers = containers,
            Workflow = source?.Workflow,
            Files = source?.Files ?? [],
            AxOpen = source?.AxOpen ?? false
        };
    }

    private static DataObjectSnapshotV3 CopySnapshot(
        DataObjectSnapshotV3 source,
        long version,
        string status,
        DateTimeOffset now,
        long? recordId,
        List<DataContainerSnapshotV3> containers) => new()
    {
        SchemaVersion = 3,
        ObjectId = source.ObjectId,
        TenantId = source.TenantId,
        TransId = source.TransId,
        KeyField = source.KeyField,
        KeyValue = source.KeyValue,
        RecordId = recordId,
        OwnerUserName = source.OwnerUserName,
        InitiatorUserName = source.InitiatorUserName,
        Version = version,
        StructDef = source.StructDef,
        CreatedAtUtc = source.CreatedAtUtc,
        LastAccessedAtUtc = now.ToString("O", CultureInfo.InvariantCulture),
        ExpiresAtUtc = source.ExpiresAtUtc,
        Status = status,
        DataContainers = containers,
        Workflow = source.Workflow,
        Files = source.Files,
        AxOpen = source.AxOpen
    };

    private static SnapshotValue ToSnapshotValue(string value, TField field, SnapshotValue? stored)
    {
        if (stored is not null)
        {
            if (string.Equals(stored.Kind, "null", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(value))
                return new SnapshotValue("null", null);
            return new SnapshotValue(stored.Kind, value);
        }

        if (string.Equals(field.DataType, "n", StringComparison.OrdinalIgnoreCase) &&
            decimal.TryParse(value, NumberStyles.Number | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out decimal number))
        {
            return new SnapshotValue("decimal", number.ToString(CultureInfo.InvariantCulture));
        }

        return string.IsNullOrEmpty(value)
            ? new SnapshotValue("null", null)
            : new SnapshotValue("string", value);
    }

    private static Dictionary<string, Dictionary<string, string>> GetOrAdd(
        Dictionary<string, Dictionary<string, Dictionary<string, string>>> data,
        string dcId)
    {
        if (!data.TryGetValue(dcId, out Dictionary<string, Dictionary<string, string>>? rows))
        {
            rows = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            data[dcId] = rows;
        }
        return rows;
    }

    public static string? ValidateAgainstStructure(
        Dictionary<string, Dictionary<string, Dictionary<string, string>>> data,
        StructDef definition,
        DataObjectSnapshotV3? snapshot)
    {
        Dictionary<string, TFrame> frames = (definition.Frames ?? [])
            .ToDictionary(frame => $"dc{frame.FrameNo}", StringComparer.OrdinalIgnoreCase);
        Dictionary<string, List<TField>> fields = (definition.Flds ?? [])
            .GroupBy(field => $"dc{field.FrameNo}", StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);
        Dictionary<string, DataContainerSnapshotV3> stored = (snapshot?.DataContainers ?? [])
            .ToDictionary(container => container.DcId, StringComparer.OrdinalIgnoreCase);

        foreach ((string dcId, Dictionary<string, Dictionary<string, string>> rows) in data)
        {
            if (!frames.ContainsKey(dcId) || !fields.TryGetValue(dcId, out List<TField>? dcFields))
                return $"Unknown data container '{dcId}'.";

            stored.TryGetValue(dcId, out DataContainerSnapshotV3? storedContainer);
            foreach ((string rowName, Dictionary<string, string> values) in rows)
            {
                Match match = RowName().Match(rowName);
                if (!match.Success ||
                    !int.TryParse(match.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out int rowNo) ||
                    rowNo < 1)
                {
                    return $"Invalid row address '{rowName}' in '{dcId}'.";
                }

                DataRowSnapshotV3? storedRow = null;
                if (storedContainer is not null)
                {
                    if (rowNo > storedContainer.Rows.Count)
                        return $"Unknown row '{rowName}' in '{dcId}'.";
                    storedRow = storedContainer.Rows[rowNo - 1];
                }

                foreach ((string fieldName, string value) in values)
                {
                    if (string.Equals(fieldName, "axp_recid", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out long recordId) ||
                            recordId < 0 ||
                            (storedRow is not null && recordId != (storedRow.RecordId ?? 0)))
                        {
                            return $"Invalid record identity for '{dcId}/{rowName}'.";
                        }
                        continue;
                    }

                    if (string.Equals(fieldName, "axrow_action", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!IsLegacyRowAction(value) ||
                            (storedRow is not null &&
                             !string.Equals(value, ToLegacyRowAction(storedRow.State), StringComparison.OrdinalIgnoreCase)))
                        {
                            return $"Invalid row action for '{dcId}/{rowName}'.";
                        }
                        continue;
                    }

                    bool isSourceId = fieldName.EndsWith("_sourceid", StringComparison.OrdinalIgnoreCase);
                    string baseName = isSourceId ? fieldName[..^9] : fieldName;
                    TField? field = dcFields.SingleOrDefault(candidate =>
                        string.Equals(candidate.FieldName, baseName, StringComparison.OrdinalIgnoreCase));
                    if (field is null || (isSourceId && !field.SourceKey))
                        return $"Unknown field '{fieldName}' in '{dcId}'.";

                    if (isSourceId &&
                        (!long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out long sourceId) || sourceId < 0))
                    {
                        return $"Invalid source identity for '{dcId}/{rowName}/{fieldName}'.";
                    }
                }
            }
        }

        return null;
    }

    public static string SnapshotValueText(SnapshotValue value) =>
        string.Equals(value.Kind, "null", StringComparison.Ordinal) ? string.Empty : value.Value ?? string.Empty;

    private static bool IsSnapshotValueValid(SnapshotValue? value)
    {
        if (value is null)
            return false;

        try
        {
            string? text = value.Value;
            switch (value.Kind)
            {
                case "null" when text is null:
                    break;
                case "string":
                    _ = text ?? throw new FormatException();
                    break;
                case "boolean":
                    _ = bool.Parse(text ?? throw new FormatException());
                    break;
                case "byte":
                    _ = byte.Parse(text ?? throw new FormatException(), CultureInfo.InvariantCulture);
                    break;
                case "int16":
                    _ = short.Parse(text ?? throw new FormatException(), CultureInfo.InvariantCulture);
                    break;
                case "int32":
                    _ = int.Parse(text ?? throw new FormatException(), CultureInfo.InvariantCulture);
                    break;
                case "int64":
                    _ = long.Parse(text ?? throw new FormatException(), CultureInfo.InvariantCulture);
                    break;
                case "single":
                    _ = float.Parse(text ?? throw new FormatException(), CultureInfo.InvariantCulture);
                    break;
                case "double":
                    _ = double.Parse(text ?? throw new FormatException(), CultureInfo.InvariantCulture);
                    break;
                case "decimal":
                    _ = decimal.Parse(text ?? throw new FormatException(), CultureInfo.InvariantCulture);
                    break;
                case "dateTimeUtc":
                    _ = DateTime.Parse(text ?? throw new FormatException(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
                    break;
                case "dateTimeOffsetUtc":
                    _ = DateTimeOffset.Parse(text ?? throw new FormatException(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
                    break;
                case "date":
                    _ = DateOnly.ParseExact(text ?? throw new FormatException(), "yyyy-MM-dd", CultureInfo.InvariantCulture);
                    break;
                case "time":
                    _ = TimeOnly.Parse(text ?? throw new FormatException(), CultureInfo.InvariantCulture);
                    break;
                case "guid":
                    _ = Guid.ParseExact(text ?? throw new FormatException(), "D");
                    break;
                case "binary":
                    _ = Convert.FromBase64String(text ?? throw new FormatException());
                    break;
                default:
                    throw new FormatException();
            }
            return true;
        }
        catch (Exception exception) when (exception is FormatException or OverflowException)
        {
            return false;
        }
    }

    private static bool IsRowState(string state) =>
        string.Equals(state, "New", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(state, "Unchanged", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(state, "Edited", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(state, "Deleted", StringComparison.OrdinalIgnoreCase);

    private static string ToLegacyRowAction(string state) =>
        string.Equals(state, "New", StringComparison.OrdinalIgnoreCase) ? "new" :
        string.Equals(state, "Deleted", StringComparison.OrdinalIgnoreCase) ? "delete" : "edit";

    private static bool IsLegacyRowAction(string action) =>
        string.Equals(action, "new", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(action, "edit", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(action, "delete", StringComparison.OrdinalIgnoreCase);

    [GeneratedRegex("^dc[1-9][0-9]*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DcName();

    [GeneratedRegex("^row([1-9][0-9]*)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex RowName();
}
