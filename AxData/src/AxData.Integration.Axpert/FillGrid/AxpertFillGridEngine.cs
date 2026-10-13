using AxData.Application.Ports;
using AxData.Application.Ports.Axpert;
using AxData.Domain.Model;

namespace AxData.Integration.Axpert.FillGrid;

public sealed class AxpertFillGridEngine(IAxpertFillGridRuntimeFactory runtimeFactory) : IAxpertFillGridEngine
{
    private static readonly SemaphoreSlim LegacyRuntimeGate = new(1, 1);

    public async Task<FillGridExecutionResult> ExecuteApplicableAsync(
        AxDataState state,
        IReadOnlyCollection<FieldAddress> changedParents,
        AxUserContext user,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(changedParents);
        ArgumentNullException.ThrowIfNull(user);

        if (!string.Equals(state.TenantId, user.TenantId, StringComparison.OrdinalIgnoreCase))
        {
            return Failure("The FillGrid runtime tenant does not match the trusted user tenant.");
        }

        await LegacyRuntimeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var runtimeResult = await runtimeFactory.CreateAsync(
                new AxpertFillGridRuntimeRequest(
                    state.TenantId,
                    state.TransId,
                    state.StructDefVersion,
                    user),
                cancellationToken).ConfigureAwait(false);
            if (!runtimeResult.Succeeded || runtimeResult.Value is null)
            {
                return Failure(runtimeResult.Error ?? "The Axpert FillGrid runtime could not be created.");
            }

            var applicable = runtimeResult.Value.Queries
                .Where(query => IsApplicable(query, state, changedParents))
                .ToArray();
            if (applicable.Length == 0)
            {
                return new FillGridExecutionResult(true, []);
            }

            if (applicable
                .GroupBy(query => query.TargetContainer, StringComparer.OrdinalIgnoreCase)
                .Any(group => group.Count() > 1))
            {
                return Failure("More than one applicable Axpert FillGrid definition targets the same data container.");
            }

            var operations = new List<FillGridResult>(applicable.Length);
            foreach (var query in applicable)
            {
                var operation = await ExecuteAsync(runtimeResult.Value, query, state, cancellationToken)
                    .ConfigureAwait(false);
                if (!operation.Succeeded)
                {
                    return Failure(operation.Error ?? "The Axpert FillGrid query failed.");
                }

                operations.Add(operation);
            }

            return new FillGridExecutionResult(true, operations);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return Failure("Axpert FillGrid execution failed.");
        }
        finally
        {
            LegacyRuntimeGate.Release();
        }
    }

    private static bool IsApplicable(
        AxpertFillGridQuery query,
        AxDataState state,
        IReadOnlyCollection<FieldAddress> changedParents)
    {
        if (query.ExecuteOnSave || query.MultiSelect || query.FromIView ||
            !state.DataContainers.TryGetValue(query.TargetContainer, out var target))
        {
            return false;
        }

        if (query.AddRows == 1)
        {
            return query.AutoShow && target.Rows.Count == 0;
        }

        var parentChanged = changedParents.Any(parent =>
            (string.IsNullOrWhiteSpace(query.SourceContainer) ||
             string.Equals(parent.DataContainer, query.SourceContainer, StringComparison.OrdinalIgnoreCase)) &&
            query.ParameterNames.Contains(parent.FieldName, StringComparer.OrdinalIgnoreCase));

        return (query.AutoShow && target.Rows.Count == 0) || (query.FirmBind && parentChanged);
    }

    private static async Task<FillGridResult> ExecuteAsync(
        AxpertFillGridRuntime runtime,
        AxpertFillGridQuery query,
        AxDataState state,
        CancellationToken cancellationToken)
    {
        var mode = query.AddRows == 2 ? FillGridMode.Replace : FillGridMode.Append;
        if (string.IsNullOrWhiteSpace(query.TargetContainer) ||
            string.IsNullOrWhiteSpace(query.SqlText) ||
            query.TargetToSourceFields.Count == 0 ||
            !TryBindParameters(query, state, out var parameters))
        {
            return Failure(query.TargetContainer, mode, "The Axpert FillGrid definition is incomplete.");
        }

        var execution = await runtime.Executor.ExecuteAsync(query, parameters, cancellationToken)
            .ConfigureAwait(false);
        if (!execution.Succeeded || execution.Value is null)
        {
            return Failure(query.TargetContainer, mode, execution.Error ?? "The Axpert FillGrid query failed.");
        }

        var rows = new List<FillGridRow>(execution.Value.Count);
        foreach (var sourceRow in execution.Value)
        {
            var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var mapping in query.TargetToSourceFields)
            {
                if (!TryGetValue(sourceRow, mapping.Value, out var value))
                {
                    return Failure(query.TargetContainer, mode, "The Axpert FillGrid result is missing a mapped column.");
                }

                values.Add(mapping.Key, value);
            }

            rows.Add(new FillGridRow(values));
        }

        return new FillGridResult(true, query.TargetContainer, mode, rows);
    }

    private static bool TryBindParameters(
        AxpertFillGridQuery query,
        AxDataState state,
        out IReadOnlyDictionary<string, object?> parameters)
    {
        var bound = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in query.ParameterNames)
        {
            var matches = state.DataContainers.Values
                .SelectMany(container => container.Rows)
                .Where(row => row.State != AxData.Domain.Lifecycle.AxRowState.Deleted)
                .SelectMany(row => row.Fields)
                .Where(field => string.Equals(field.Key, name, StringComparison.OrdinalIgnoreCase))
                .Select(field => field.Value.Value)
                .Distinct()
                .Take(2)
                .ToArray();
            if (matches.Length != 1)
            {
                parameters = bound;
                return false;
            }

            bound.Add(name, matches[0]);
        }

        parameters = bound;
        return true;
    }

    private static bool TryGetValue(
        IReadOnlyDictionary<string, object?> row,
        string name,
        out object? value)
    {
        foreach (var pair in row)
        {
            if (string.Equals(pair.Key, name, StringComparison.OrdinalIgnoreCase))
            {
                value = pair.Value;
                return true;
            }
        }

        value = null;
        return false;
    }

    private static FillGridResult Failure(string target, FillGridMode mode, string error) =>
        new(false, target, mode, [], error);

    private static FillGridExecutionResult Failure(string error) =>
        new(false, [], error);
}
