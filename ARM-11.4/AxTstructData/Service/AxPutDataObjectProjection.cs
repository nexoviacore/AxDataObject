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

        if (!IsActorAllowed(snapshot, userName))
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
                409);
        }

        return new AxPutDataObjectCheck(true);
    }

    public static string? ValidateSnapshot(
        DataObjectSnapshotV3 snapshot,
        string tenantId,
        string objectId)
    {
        if (string.IsNullOrWhiteSpace(snapshot.ObjectId) ||
            string.IsNullOrWhiteSpace(snapshot.TenantId) ||
            string.IsNullOrWhiteSpace(snapshot.TransId) ||
            string.IsNullOrWhiteSpace(snapshot.InitiatorUserName) ||
            snapshot.DataContainers is null)
        {
            return "The DataObject payload is incomplete.";
        }

        if (!string.Equals(snapshot.TenantId, tenantId, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(snapshot.ObjectId, objectId, StringComparison.OrdinalIgnoreCase))
        {
            return "The DataObject identity does not match the requested tenant and objectid.";
        }

        if (snapshot.RecordId is <= 0)
            return "The DataObject contains an invalid primary record identity.";

        HashSet<string> containers = new(StringComparer.OrdinalIgnoreCase);
        foreach (DataContainerSnapshotV3 container in snapshot.DataContainers)
        {
            if (string.IsNullOrWhiteSpace(container.DcId) ||
                !DcName().IsMatch(container.DcId) ||
                !containers.Add(container.DcId) ||
                container.Fields is null ||
                container.Rows is null)
            {
                return "The DataObject contains an invalid or duplicate data container.";
            }

            if (container.Fields.Any(field => string.IsNullOrWhiteSpace(field.Name) ||
                (field.Flags & ~DataObjectFieldSnapshot.KnownFlags) != 0) ||
                container.Fields.Select(field => field.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != container.Fields.Count)
            {
                return "The DataObject contains invalid or duplicate field ordinals.";
            }

            HashSet<string> rowIds = new(StringComparer.OrdinalIgnoreCase);
            foreach (DataRowSnapshotV3 row in container.Rows)
            {
                if (string.IsNullOrWhiteSpace(row.RowId) || !rowIds.Add(row.RowId))
                    return "The DataObject contains an invalid or duplicate row identity.";

                bool isNew = string.Equals(row.State, "New", StringComparison.OrdinalIgnoreCase);
                if ((isNew && row.RecordId is not null) || (!isNew && row.RecordId is not > 0))
                    return "The DataObject contains an invalid row record identity.";

                if (!IsRowState(row.State) || row.Values is null || row.Values.Count != container.Fields.Count)
                    return "The DataObject contains an invalid row state.";

                if (row.Values.Any(value => !IsSnapshotValueValid(value)) ||
                    (row.OldValues is not null &&
                     (isNew || row.OldValues.Count != container.Fields.Count ||
                      row.OldValues.Any(value => !IsSnapshotValueValid(value)))))
                {
                    return "The DataObject contains invalid positional field data.";
                }

                for (int ordinal = 0; ordinal < container.Fields.Count; ordinal++)
                {
                    if (!TryDecodeFieldValue(container.Fields[ordinal], row.Values[ordinal], out _, out _))
                        return "The DataObject contains invalid source-key field data.";
                    if (row.OldValues is not null &&
                        !TryDecodeFieldValue(container.Fields[ordinal], row.OldValues[ordinal], out _, out _))
                        return "The DataObject contains invalid source-key old field data.";
                }

                if (row.OldValues is not null && row.OldValues.SequenceEqual(row.Values))
                    return "The DataObject contains an oldValues array with no changed field.";
            }
        }

        return null;
    }

    public static bool IsActorAllowed(
        DataObjectSnapshotV3 snapshot,
        string userName) =>
        string.Equals(snapshot.InitiatorUserName, userName, StringComparison.Ordinal);

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

                for (int ordinal = 0; ordinal < container.Fields.Count; ordinal++)
                {
                    DataObjectFieldSnapshot field = container.Fields[ordinal];
                    _ = TryDecodeFieldValue(field, row.Values[ordinal], out string value, out long sourceId);
                    values[field.Name] = value;
                    if (field.SourceKey)
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
                List<int> changed = Enumerable.Range(0, container.Fields.Count)
                    .Where(ordinal => includeAll ||
                        row.OldValues is not null && row.Values[ordinal] != row.OldValues[ordinal])
                    .ToList();
                if (changed.Count == 0 && string.Equals(row.State, "Unchanged", StringComparison.OrdinalIgnoreCase))
                    continue;

                Dictionary<string, Dictionary<string, string>> rows = GetOrAdd(result, container.DcId);
                Dictionary<string, string> values = new(StringComparer.OrdinalIgnoreCase)
                {
                    ["axp_recid"] = (row.RecordId ?? 0).ToString(CultureInfo.InvariantCulture),
                    ["axrow_action"] = ToLegacyRowAction(row.State)
                };
                foreach (int ordinal in changed)
                {
                    DataObjectFieldSnapshot field = container.Fields[ordinal];
                    _ = TryDecodeFieldValue(field, row.Values[ordinal], out string value, out long sourceId);
                    values[field.Name] = value;
                    if (field.SourceKey)
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
            return CreateSavingSnapshot(context, now);
        }

        return CreateFinalSnapshot(definition, context, [], "Saving", now);
    }

    public static DataObjectSnapshotV3 CreateSavingSnapshot(
        AxPutDataObjectContext context,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(context);
        DataObjectSnapshotV3 existing = context.Snapshot
            ?? throw new InvalidOperationException("An existing DataObject snapshot is required.");
        return CopySnapshot(
            existing,
            "Saving",
            now,
            existing.RecordId,
            existing.DataContainers ?? []);
    }

    public static DataObjectSnapshotV3 CreateFinalSnapshot(
        StructDef definition,
        AxPutDataObjectContext context,
        IReadOnlyList<AxPutRecordIdMapping> mappings,
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
                .OrderBy(field => field.FieldName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(field => field.FieldName, StringComparer.Ordinal)
                .ToList();
            List<DataObjectFieldSnapshot> fieldNames = fields
                .Select(field => new DataObjectFieldSnapshot(
                    field.FieldName,
                    (frame.ReadOnly || field.Readonly ? DataObjectFieldSnapshot.ReadOnlyFlag : 0) |
                    (field.SourceKey ? DataObjectFieldSnapshot.SourceKeyFlag : 0)))
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
                List<SnapshotScalar> projectedValues = [];
                for (int ordinal = 0; ordinal < fields.Count; ordinal++)
                {
                    TField field = fields[ordinal];
                    SnapshotScalar? storedValue = StoredValue(storedContainer, storedRow, field.FieldName);
                    string value = definition.GetFieldValue(field, rowNo);
                    long runtimeSourceId = definition.GetIDValue(field, rowNo);
                    projectedValues.Add(ToSnapshotValue(value, field, runtimeSourceId, storedValue));
                }

                rows.Add(new DataRowSnapshotV3(
                    rowId,
                    recordId,
                    "Unchanged",
                    projectedValues));
            }

            containers.Add(new DataContainerSnapshotV3(dcId, frame.AsGrid, fieldNames, rows));
        }

        long? primaryRecordId = containers
            .SingleOrDefault(container => string.Equals(container.DcId, "dc1", StringComparison.OrdinalIgnoreCase))?
            .Rows.FirstOrDefault()?.RecordId;
        DataObjectSnapshotV3? source = context.Snapshot;
        return new DataObjectSnapshotV3
        {
            ObjectId = context.ObjectId,
            TenantId = context.TenantId,
            TransId = context.TransId,
            RecordId = primaryRecordId ?? source?.RecordId,
            InitiatorUserName = context.InitiatorUserName,
            Status = status,
            DataContainers = containers,
            Workflow = source?.Workflow
        };
    }

    private static DataObjectSnapshotV3 CopySnapshot(
        DataObjectSnapshotV3 source,
        string status,
        DateTimeOffset now,
        long? recordId,
        List<DataContainerSnapshotV3> containers) => new()
    {
        ObjectId = source.ObjectId,
        TenantId = source.TenantId,
        TransId = source.TransId,
        RecordId = recordId,
        InitiatorUserName = source.InitiatorUserName,
        Status = status,
        DataContainers = containers,
        Workflow = source.Workflow
    };

    private static SnapshotScalar? StoredValue(
        DataContainerSnapshotV3? container,
        DataRowSnapshotV3? row,
        string fieldName)
    {
        if (container is null || row is null)
            return null;

        int ordinal = container.Fields.FindIndex(field =>
            string.Equals(field.Name, fieldName, StringComparison.OrdinalIgnoreCase));
        return ordinal >= 0 && ordinal < row.Values.Count ? row.Values[ordinal] : null;
    }

    private static SnapshotScalar ToSnapshotValue(
        string value,
        TField field,
        long sourceId,
        SnapshotScalar? stored)
    {
        if (field.SourceKey)
        {
            if (stored?.Kind == SnapshotScalarKind.Null && string.IsNullOrEmpty(value) && sourceId == 0)
                return SnapshotScalar.Null;
            return SnapshotScalar.String($"{value}~{Math.Max(0, sourceId)}");
        }

        if (stored is not null)
        {
            if (stored.Kind == SnapshotScalarKind.Null && string.IsNullOrEmpty(value))
                return SnapshotScalar.Null;
            if (stored.Kind == SnapshotScalarKind.Number &&
                decimal.TryParse(value, NumberStyles.Number | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out decimal storedNumber))
                return SnapshotScalar.Number(storedNumber.ToString(CultureInfo.InvariantCulture));
            if (stored.Kind == SnapshotScalarKind.Boolean && bool.TryParse(value, out bool storedBoolean))
                return SnapshotScalar.Boolean(storedBoolean);
            return SnapshotScalar.String(value);
        }

        if (string.Equals(field.DataType, "n", StringComparison.OrdinalIgnoreCase) &&
            decimal.TryParse(value, NumberStyles.Number | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out decimal number))
        {
            return SnapshotScalar.Number(number.ToString(CultureInfo.InvariantCulture));
        }

        return string.IsNullOrEmpty(value)
            ? SnapshotScalar.Null
            : SnapshotScalar.String(value);
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

        foreach ((string dcId, DataContainerSnapshotV3 container) in stored)
        {
            if (!fields.TryGetValue(dcId, out List<TField>? dcFields))
                return $"Unknown data container '{dcId}'.";
            foreach (DataObjectFieldSnapshot storedField in container.Fields)
            {
                TField? current = dcFields.SingleOrDefault(field =>
                    string.Equals(field.FieldName, storedField.Name, StringComparison.OrdinalIgnoreCase));
                if (current is null ||
                    current.SourceKey != storedField.SourceKey ||
                    (frames[dcId].ReadOnly || current.Readonly) != storedField.ReadOnly)
                    return $"Stored field metadata does not match current structure for '{dcId}/{storedField.Name}'.";
            }
        }

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

    public static string SnapshotValueText(SnapshotScalar value) =>
        value.Kind == SnapshotScalarKind.Null ? string.Empty : value.Text ?? string.Empty;

    private static bool IsSnapshotValueValid(SnapshotScalar? value)
    {
        if (value is null)
            return false;

        try
        {
            string? text = value.Text;
            switch (value.Kind)
            {
                case SnapshotScalarKind.Null when text is null:
                    break;
                case SnapshotScalarKind.String:
                    _ = text ?? throw new FormatException();
                    break;
                case SnapshotScalarKind.Boolean:
                    _ = bool.Parse(text ?? throw new FormatException());
                    break;
                case SnapshotScalarKind.Number:
                    if (!decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out _) &&
                        (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double number) || !double.IsFinite(number)))
                        throw new FormatException();
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

    private static bool TryDecodeFieldValue(
        DataObjectFieldSnapshot field,
        SnapshotScalar scalar,
        out string value,
        out long sourceId)
    {
        sourceId = 0;
        if (!field.SourceKey)
        {
            value = SnapshotValueText(scalar);
            return true;
        }

        if (scalar.Kind == SnapshotScalarKind.Null)
        {
            value = string.Empty;
            return scalar.Text is null;
        }

        if (scalar.Kind != SnapshotScalarKind.String || scalar.Text is not string packed)
        {
            value = string.Empty;
            return false;
        }

        int separator = packed.LastIndexOf('~');
        if (separator < 0 ||
            !long.TryParse(packed.AsSpan(separator + 1), NumberStyles.None, CultureInfo.InvariantCulture, out sourceId) ||
            sourceId < 0)
        {
            value = string.Empty;
            return false;
        }

        value = packed[..separator];
        return true;
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
