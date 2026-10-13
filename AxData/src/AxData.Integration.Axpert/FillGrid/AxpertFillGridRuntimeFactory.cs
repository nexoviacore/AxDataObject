using AxData.Integration.Axpert.FieldEngine;
using AxData.Integration.Axpert.Runtime;
using AxData.Integration.Axpert.StructDef;
using LegacyStructDef = AxStructDef.StructDef;

namespace AxData.Integration.Axpert.FillGrid;

public sealed class AxpertFillGridRuntimeFactory(
    IAxpertRuntimeContextFactory contextFactory,
    IAxpertStructDefSource structDefSource,
    AxpertFillGridQueryOptions queryOptions) : IAxpertFillGridRuntimeFactory
{
    public async Task<AxpertOperationResult<AxpertFillGridRuntime>> CreateAsync(
        AxpertFillGridRuntimeRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var current = await structDefSource.LoadCurrentAsync(
            request.TenantId,
            request.TransId,
            cancellationToken).ConfigureAwait(false);
        if (current is null ||
            !string.Equals(
                current.ContentHash,
                request.PublishedDefinitionVersion,
                StringComparison.OrdinalIgnoreCase))
        {
            return AxpertOperationResult<AxpertFillGridRuntime>.Failure(
                "The current StructDef content hash does not match the AxData snapshot.");
        }

        var runtimeResult = await contextFactory.CreateAsync(request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (!runtimeResult.Succeeded || runtimeResult.Value is null)
        {
            return AxpertOperationResult<AxpertFillGridRuntime>.Failure(
                runtimeResult.Error ?? "The Axpert FillGrid runtime could not be created.");
        }

        try
        {
            var context = runtimeResult.Value;
            var definition = await LegacyStructDef.LoadStructure(
                context.Common,
                request.TransId,
                request.User.Roles.ToList(),
                context.TraceService,
                refreshCache: true).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (definition?.Fgs is null)
            {
                return AxpertOperationResult<AxpertFillGridRuntime>.Failure(
                    "The Axpert StructDef does not expose FillGrid metadata.");
            }

            var sqlResult = await AxpertFillGridSqlSource.LoadAsync(
                context,
                request.TenantId,
                request.TransId,
                cancellationToken).ConfigureAwait(false);
            if (!sqlResult.Succeeded || sqlResult.Value is null)
            {
                return AxpertOperationResult<AxpertFillGridRuntime>.Failure(
                    sqlResult.Error ?? "The Axpert FillGrid SQL could not be loaded.");
            }

            var queries = new List<AxpertFillGridQuery>(definition.Fgs.Count);
            foreach (var fillGrid in definition.Fgs)
            {
                var name = string.IsNullOrWhiteSpace(fillGrid.Fname) ? fillGrid.Name : fillGrid.Fname;
                if (!sqlResult.Value.TryGetValue(name, out var sqlText))
                {
                    return AxpertOperationResult<AxpertFillGridRuntime>.Failure(
                        "The Axpert FillGrid SQL metadata is incomplete.");
                }

                queries.Add(AxpertFillGridRuntime.FromLegacy(fillGrid) with { SqlText = sqlText });
            }

            return AxpertOperationResult<AxpertFillGridRuntime>.Success(
                new AxpertFillGridRuntime(
                    queries,
                    new AxpertFillGridQueryExecutor(context.Common._dbHelper, queryOptions)));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return AxpertOperationResult<AxpertFillGridRuntime>.Failure(
                "The Axpert FillGrid runtime could not be initialized.");
        }
    }
}
