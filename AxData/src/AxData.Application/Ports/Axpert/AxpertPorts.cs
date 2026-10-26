using AxData.Domain.Model;
using AxData.Application.Ports;

namespace AxData.Application.Ports.Axpert;

public interface IStructDefProvider
{
    Task<StructDefHandle?> GetAsync(string tenantId, string transId, CancellationToken cancellationToken);

    Task<StructDefHandle?> GetVersionAsync(
        string tenantId,
        string transId,
        string version,
        CancellationToken cancellationToken);
}

public interface IAxpertAuthorizationProvider
{
    Task<AxCreatePermission> GetCreatePermissionAsync(
        string tenantId,
        string transId,
        AxUserContext user,
        CancellationToken cancellationToken);

    Task<AxQueryPermission> GetQueryPermissionAsync(
        string tenantId,
        string transId,
        AxRecordKey key,
        AxUserContext user,
        CancellationToken cancellationToken);

    Task<AxQueryPermission> GetSetPermissionAsync(
        string tenantId,
        string transId,
        AxRecordKey key,
        AxUserContext user,
        CancellationToken cancellationToken);
}

public interface IAxpertDataLoader
{
    Task<AxDataLoadResult> LoadAsync(
        StructDefHandle definition,
        AxRecordKey key,
        AxUserContext user,
        CancellationToken cancellationToken);
}

public interface IAxpertFieldEngine
{
    Task<FieldOperationStartResult> CreateOperationAsync(
        AxDataState state,
        AxUserContext user,
        CancellationToken cancellationToken);
}

public interface IAxpertFieldOperation : IAsyncDisposable
{
    Task<FieldEvaluationResult> EvaluateAsync(
        FieldAddress field,
        object? value,
        CancellationToken cancellationToken);

    Task<FieldOperationStatus> RebuildAsync(CancellationToken cancellationToken);

    Task<FieldEvaluationResult> CompleteAsync(CancellationToken cancellationToken);
}

public static class AxpertFieldEngineExtensions
{
    public static async Task<FieldEvaluationResult> SetValidateAndRefreshAsync(
        this IAxpertFieldEngine engine,
        AxDataState state,
        FieldAddress field,
        object? value,
        AxUserContext user,
        CancellationToken cancellationToken)
    {
        var started = await engine.CreateOperationAsync(state, user, cancellationToken).ConfigureAwait(false);
        if (!started.Succeeded || started.Operation is null)
        {
            return FieldEvaluationResult.Failure(new FieldEvaluationError(
                field,
                "AXD-VAL-001",
                started.Error ?? "The Axpert field operation could not be created."));
        }

        await using var operation = started.Operation;
        var evaluated = await operation.EvaluateAsync(field, value, cancellationToken).ConfigureAwait(false);
        return evaluated.Succeeded
            ? await operation.CompleteAsync(cancellationToken).ConfigureAwait(false)
            : evaluated;
    }
}

public interface IAxpertFillGridEngine
{
    Task<FillGridExecutionResult> ExecuteApplicableAsync(
        AxDataState state,
        IReadOnlyCollection<FieldAddress> changedParents,
        AxUserContext user,
        CancellationToken cancellationToken);
}

public interface IAxpertTransactionWriter
{
    Task<AxSaveResult> SaveAsync(
        AxDataState state,
        AxSaveContext context,
        CancellationToken cancellationToken);
}
