using AxData.Application.Ports;
using AxData.Application.Ports.Axpert;
using AxData.Domain.Lifecycle;
using AxData.Domain.Model;
using AxStructDef.Models;
using System.Globalization;
using LegacyStructDef = AxStructDef.StructDef;

namespace AxData.Integration.Axpert.AxGet;

public interface IAxpertDataSource
{
    Task<AxpertDataSourceResult> LoadAsync(
        AxpertDataSourceRequest request,
        CancellationToken cancellationToken);
}

public sealed record AxpertDataSourceRequest(
    string TenantId,
    string TransId,
    AxRecordKey Key,
    string UserName,
    IReadOnlySet<string> Roles);

public sealed record AxpertDataSourceResult(
    bool Succeeded,
    long? RecordId,
    IReadOnlyList<AxpertDataContainerResult>? DataContainers,
    string? Error = null);

public sealed record AxpertDataContainerResult(
    string Name,
    IReadOnlyList<AxpertDataRowResult> Rows);

public sealed record AxpertDataRowResult(
    long? RecordId,
    IReadOnlyDictionary<string, object?> Fields,
    IReadOnlyDictionary<string, long?>? SourceIds = null);

internal sealed record AxpertLoadedContainerShape(
    string Name,
    IReadOnlyList<AxpertLoadedRowShape> Rows);

internal sealed record AxpertLoadedRowShape(
    long? RecordId,
    IReadOnlySet<string> Fields,
    IReadOnlySet<string> NonNullFields);

public sealed class AxpertDataLoader(
    IAxpertDataSource source,
    IRowIdGenerator rowIdGenerator) : IAxpertDataLoader
{
    /// <summary>Loads Axpert transaction data and converts it to validated AxData state.</summary>
    /// <returns>State only when the Axpert load reports success and its result can be safely projected.</returns>
    public async Task<AxDataLoadResult> LoadAsync(
        StructDefHandle definition,
        AxRecordKey key,
        AxUserContext user,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(user);

        if (!string.Equals(definition.TenantId, user.TenantId, StringComparison.OrdinalIgnoreCase))
        {
            return Failure("The StructDef tenant does not match the trusted user tenant.");
        }

        var result = await source.LoadAsync(
            new AxpertDataSourceRequest(
                definition.TenantId,
                definition.TransId,
                key,
                user.UserName,
                user.Roles),
            cancellationToken);

        if (!result.Succeeded)
        {
            return Failure(result.Error ?? "AxGet failed.");
        }

        if (result.DataContainers is null)
        {
            return Failure("AxGet returned no data payload.");
        }

        try
        {
            return new AxDataLoadResult(true, BuildState(definition, key, user, result), null);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            return Failure("AxGet returned data that does not match the published StructDef.");
        }
    }

    /// <summary>Builds a tenant-scoped domain state from a successful AxGet result.</summary>
    internal AxDataState BuildState(
        StructDefHandle definition,
        AxRecordKey key,
        AxUserContext user,
        AxpertDataSourceResult result)
    {
        var returnedContainers = result.DataContainers!
            .ToDictionary(container => container.Name, StringComparer.OrdinalIgnoreCase);
        var state = new AxDataState(
            "axpert-load",
            definition.TenantId,
            definition.TransId,
            user.UserName,
            DateTimeOffset.UnixEpoch)
        {
            RecordId = result.RecordId
        };

        foreach (var containerDefinition in definition.DataContainers)
        {
            returnedContainers.TryGetValue(containerDefinition.Name, out var returned);
            var rows = returned?.Rows.Select(row => BuildRow(containerDefinition, row)).ToArray()
                ?? [];
            if (result.RecordId is null && !containerDefinition.IsGrid && rows.Length == 0)
            {
                rows = [BuildNewRow(containerDefinition)];
            }

            state.DataContainers.Add(
                containerDefinition.Name,
                new AxDataContainer(
                    containerDefinition.Name,
                    containerDefinition.IsGrid,
                    rows,
                    containerDefinition.Fields
                        .OrderBy(field => field.Name, StringComparer.OrdinalIgnoreCase)
                        .ThenBy(field => field.Name, StringComparer.Ordinal)
                        .Select(field => new AxDataFieldMetadata(
                            field.Name,
                            field.IsReadOnly,
                            field.SourceKey))));
        }

        var expectedContainers = definition.DataContainers
            .Select(container => container.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (returnedContainers.Keys.Any(name => !expectedContainers.Contains(name)))
        {
            throw new ArgumentException("AxGet returned an unknown data container.");
        }

        return state;
    }

    /// <summary>Builds pre-persistence initialization data from a successful AxGet result.</summary>
    internal AxpertInitializedData BuildInitializedData(
        LegacyStructDef definition,
        string transId,
        long? recordId,
        IReadOnlyList<AxpertLoadedContainerShape> dataContainers)
    {
        if (definition.Frames is null || definition.Flds is null)
        {
            throw new InvalidOperationException("The loaded StructDef or AxGet payload is incomplete.");
        }

        var returnedContainers = dataContainers
            .ToDictionary(container => container.Name, StringComparer.OrdinalIgnoreCase);
        var expectedContainers = definition.Frames
            .Select(frame => $"dc{frame.FrameNo}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (returnedContainers.Keys.Any(name => !expectedContainers.Contains(name)))
        {
            throw new ArgumentException("AxGet returned an unknown data container.");
        }

        var containers = new List<AxDataContainer>(definition.Frames.Count);

        foreach (var frame in definition.Frames.OrderBy(frame => frame.FrameNo))
        {
            var containerName = $"dc{frame.FrameNo}";
            var fields = definition.Flds
                .Where(field => field.FrameNo == frame.FrameNo)
                .OrderBy(field => field.OrderNo)
                .ToArray();
            returnedContainers.TryGetValue(containerName, out var returned);
            var rows = returned?.Rows
                .Select((row, index) => BuildRuntimeRow(
                    definition,
                    frame,
                    fields,
                    row,
                    frame.AsGrid ? index + 1 : 1))
                .ToArray() ?? [];
            if (recordId is null && !frame.AsGrid && rows.Length == 0)
            {
                rows = [BuildRuntimeRow(
                    definition,
                    frame,
                    fields,
                    new AxpertLoadedRowShape(
                        null,
                        new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                        new HashSet<string>(StringComparer.OrdinalIgnoreCase)),
                    rowNumber: 1)];
            }

            containers.Add(new AxDataContainer(
                containerName,
                frame.AsGrid,
                rows,
                fields
                    .OrderBy(field => field.FieldName, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(field => field.FieldName, StringComparer.Ordinal)
                    .Select(field => new AxDataFieldMetadata(
                        field.FieldName,
                        frame.ReadOnly || field.Readonly,
                        field.SourceKey))));
        }

        return new AxpertInitializedData(transId, recordId, containers, [], AxOpen: false);
    }

    private AxDataRow BuildRow(
        DataContainerDefinition definition,
        AxpertDataRowResult row)
    {
        var knownFields = definition.Fields.ToDictionary(field => field.Name, StringComparer.OrdinalIgnoreCase);
        if (row.Fields.Keys.Any(name => !knownFields.ContainsKey(name)))
        {
            throw new ArgumentException("AxGet returned an unknown field.");
        }

        var fields = definition.Fields.ToDictionary(
            field => field.Name,
            field => new AxFieldValue(
                row.Fields.TryGetValue(field.Name, out var value) ? value : null,
                field.IsReadOnly,
                row.SourceIds is not null && row.SourceIds.TryGetValue(field.Name, out var sourceId)
                    ? sourceId
                    : null,
                field.SourceKey),
            StringComparer.OrdinalIgnoreCase);

        return row.RecordId is > 0
            ? new AxDataRow(rowIdGenerator.NewRowId(), AxRowState.Unchanged, row.RecordId, fields)
            : new AxDataRow(rowIdGenerator.NewRowId(), AxRowState.New, fields: fields);
    }

    private AxDataRow BuildNewRow(DataContainerDefinition definition)
    {
        var fields = definition.Fields.ToDictionary(
            field => field.Name,
            field => new AxFieldValue(null, field.IsReadOnly, isSourceKey: field.SourceKey),
            StringComparer.OrdinalIgnoreCase);
        return new AxDataRow(
            rowIdGenerator.NewRowId(),
            AxRowState.New,
            fields: fields);
    }

    private AxDataRow BuildRuntimeRow(
        LegacyStructDef definition,
        TFrame frame,
        IReadOnlyList<TField> fields,
        AxpertLoadedRowShape row,
        int rowNumber)
    {
        var knownFields = fields.ToDictionary(field => field.FieldName, StringComparer.OrdinalIgnoreCase);
        if (row.Fields.Any(name => !knownFields.ContainsKey(name)))
        {
            throw new ArgumentException("AxGet returned an unknown field.");
        }

        var values = fields.ToDictionary(
            field => field.FieldName,
            field => new AxFieldValue(
                FromAxpertString(
                    definition.GetFieldValue(field, rowNumber),
                    field,
                    row.NonNullFields.Contains(field.FieldName)),
                frame.ReadOnly || field.Readonly,
                NormalizeSourceId(definition.GetIDValue(field, rowNumber)),
                field.SourceKey),
            StringComparer.OrdinalIgnoreCase);

        return row.RecordId is > 0
            ? new AxDataRow(rowIdGenerator.NewRowId(), AxRowState.Unchanged, row.RecordId, values)
            : new AxDataRow(rowIdGenerator.NewRowId(), AxRowState.New, fields: values);
    }

    private static long? NormalizeSourceId(long sourceId) => sourceId > 0 ? sourceId : null;

    private static object? FromAxpertString(string value, TField field, bool hadNonNullValue)
    {
        if (string.Equals(field.DataType, "n", StringComparison.OrdinalIgnoreCase))
        {
            return decimal.TryParse(
                value,
                NumberStyles.Number | NumberStyles.AllowExponent,
                CultureInfo.InvariantCulture,
                out var number)
                ? number
                : value;
        }

        return !hadNonNullValue && string.IsNullOrEmpty(value) ? null : value;
    }

    private static AxDataLoadResult Failure(string error) => new(false, null, error);
}
