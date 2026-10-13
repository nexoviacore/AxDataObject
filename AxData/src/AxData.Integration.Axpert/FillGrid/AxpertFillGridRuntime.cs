using AxData.Application.Ports;
using AxData.Integration.Axpert.FieldEngine;
using AxStructDef.Models;

namespace AxData.Integration.Axpert.FillGrid;

public sealed record AxpertFillGridRuntimeRequest(
    string TenantId,
    string TransId,
    string PublishedDefinitionVersion,
    AxUserContext User);

public interface IAxpertFillGridRuntimeFactory
{
    Task<AxpertOperationResult<AxpertFillGridRuntime>> CreateAsync(
        AxpertFillGridRuntimeRequest request,
        CancellationToken cancellationToken);
}

public interface IAxpertFillGridQueryExecutor
{
    Task<AxpertOperationResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>> ExecuteAsync(
        AxpertFillGridQuery query,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken);
}

public sealed record AxpertFillGridQuery(
    string Name,
    string SqlText,
    string SourceContainer,
    string TargetContainer,
    IReadOnlyList<string> ParameterNames,
    IReadOnlyDictionary<string, string> TargetToSourceFields,
    int AddRows,
    bool FirmBind,
    bool ExecuteOnSave,
    bool AutoShow = true,
    bool MultiSelect = false,
    bool FromIView = false);

public sealed class AxpertFillGridRuntime
{
    public AxpertFillGridRuntime(
        IReadOnlyList<AxpertFillGridQuery> queries,
        IAxpertFillGridQueryExecutor executor)
    {
        ArgumentNullException.ThrowIfNull(queries);
        ArgumentNullException.ThrowIfNull(executor);
        Queries = queries;
        Executor = executor;
    }

    public IReadOnlyList<AxpertFillGridQuery> Queries { get; }

    public IAxpertFillGridQueryExecutor Executor { get; }

    public static AxpertFillGridQuery FromLegacy(TFg definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (definition.AddRows is not (1 or 2 or 3))
        {
            throw new InvalidOperationException("The Axpert FillGrid AddRows value is unsupported.");
        }

        var mappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in definition.Map)
        {
            var parts = entry.Split('=', 2, StringSplitOptions.TrimEntries);
            if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
            {
                throw new InvalidOperationException("The Axpert FillGrid field map is invalid.");
            }

            if (!mappings.TryAdd(parts[0], parts[1]))
            {
                throw new InvalidOperationException("The Axpert FillGrid contains a duplicate target field map.");
            }
        }

        return new AxpertFillGridQuery(
            definition.Fname,
            definition.SQLText,
            definition.SourceDcName,
            definition.TargetDcName,
            SplitNames(definition.ParamNames),
            mappings,
            definition.AddRows,
            definition.Firmbind,
            definition.ExecuteOnSave,
            definition.AutoShow,
            definition.MultiSelect,
            definition.FromIView);
    }

    private static IReadOnlyList<string> SplitNames(string names) =>
        names.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(name => name.TrimStart('@', ':'))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
}
