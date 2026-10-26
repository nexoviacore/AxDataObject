using AxData.Application.Ports;
using AxData.Application.Ports.Axpert;
using AxData.Domain.Lifecycle;
using AxData.Domain.Model;

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
    string PublishedDefinitionVersion,
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

public sealed class AxpertDataLoader(
    IAxpertDataSource source,
    IRowIdGenerator rowIdGenerator) : IAxpertDataLoader
{
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
                definition.Version,
                key,
                user.UserName,
                user.Roles),
            cancellationToken).ConfigureAwait(false);

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

    private AxDataState BuildState(
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
            user.UserName,
            definition.Version,
            DateTimeOffset.UnixEpoch)
        {
            KeyField = key.KeyField,
            KeyValue = key.KeyValue,
            RecordId = result.RecordId
        };

        foreach (var containerDefinition in definition.DataContainers)
        {
            returnedContainers.TryGetValue(containerDefinition.Name, out var returned);
            var rows = returned?.Rows.Select(row => BuildRow(containerDefinition, row)).ToArray()
                ?? [];
            state.DataContainers.Add(
                containerDefinition.Name,
                new AxDataContainer(containerDefinition.Name, containerDefinition.IsGrid, rows));
        }

        if (returnedContainers.Keys.Any(name => definition.DataContainers.All(
                candidate => !string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase))))
        {
            throw new ArgumentException("AxGet returned an unknown data container.");
        }

        return state;
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
                    : null),
            StringComparer.OrdinalIgnoreCase);

        return row.RecordId is > 0
            ? new AxDataRow(rowIdGenerator.NewRowId(), AxRowState.Unchanged, row.RecordId, fields)
            : new AxDataRow(rowIdGenerator.NewRowId(), AxRowState.New, fields: fields);
    }

    private static AxDataLoadResult Failure(string error) => new(false, null, error);
}
