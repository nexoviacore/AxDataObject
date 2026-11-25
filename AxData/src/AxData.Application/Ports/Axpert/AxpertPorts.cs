using AxData.Domain.Model;
using AxData.Application.Ports;

namespace AxData.Application.Ports.Axpert;

public interface IStructDefProvider
{
    /// <summary>Resolves the transaction definition for a tenant and transaction.</summary>
    /// <returns>The definition, or <see langword="null"/> when the transaction is unknown.</returns>
    Task<StructDefHandle?> GetAsync(string tenantId, string transId, CancellationToken cancellationToken);
}

public interface IAxpertAuthorizationProvider
{
    /// <summary>Gets the Axpert create permission for a transaction and authenticated user.</summary>
    Task<AxCreatePermission> GetCreatePermissionAsync(
        string tenantId,
        string transId,
        AxUserContext user,
        CancellationToken cancellationToken);

    /// <summary>Gets query permissions, including visible data containers and fields.</summary>
    Task<AxQueryPermission> GetQueryPermissionAsync(
        string tenantId,
        string transId,
        AxRecordKey key,
        AxUserContext user,
        IReadOnlyList<AxPermissionDataContainer> dataContainers,
        CancellationToken cancellationToken);

    /// <summary>Gets mutation permissions for an existing record.</summary>
    Task<AxQueryPermission> GetSetPermissionAsync(
        string tenantId,
        string transId,
        AxRecordKey key,
        AxUserContext user,
        CancellationToken cancellationToken);
}

public interface IAxpertDataLoader
{
    /// <summary>Loads an existing record or creates the initial in-memory transaction shape.</summary>
    /// <returns>A result whose state may be read only when the operation succeeded.</returns>
    Task<AxDataLoadResult> LoadAsync(
        StructDefHandle definition,
        AxRecordKey key,
        AxUserContext user,
        CancellationToken cancellationToken);
}

public interface IAxpertInitializer
{
    /// <summary>Loads transaction metadata and runs Axpert initialization for a new AxData object.</summary>
    /// <returns>Initialized data on success; otherwise a categorized initialization failure.</returns>
    Task<AxpertInitializationResult> InitializeAsync(
        string tenantId,
        string transId,
        AxRecordKey key,
        AxUserContext user,
        CancellationToken cancellationToken);
}

public interface IAxpertFieldEngine
{
    /// <summary>Creates a stateful field-evaluation operation bound to one AxData state and user.</summary>
    Task<FieldOperationStartResult> CreateOperationAsync(
        AxDataState state,
        AxUserContext user,
        CancellationToken cancellationToken);
}

public interface IAxpertFieldOperation : IAsyncDisposable
{
    StructDefHandle? Definition { get; }

    /// <summary>Runs Axpert load-time initialization and validation.</summary>
    Task<FieldEvaluationResult> InitializeOnLoadAsync(CancellationToken cancellationToken);

    /// <summary>Applies and validates one submitted field value in the current operation.</summary>
    Task<FieldEvaluationResult> EvaluateAsync(
        FieldAddress field,
        object? value,
        CancellationToken cancellationToken);

    /// <summary>Rebuilds the Axpert runtime after a rejected field evaluation.</summary>
    Task<FieldOperationStatus> RebuildAsync(CancellationToken cancellationToken);

    /// <summary>Completes evaluation and returns the final dependent-field changes.</summary>
    Task<FieldEvaluationResult> CompleteAsync(CancellationToken cancellationToken);

    /// <summary>Executes FillGrid rules affected by the supplied changed parent fields.</summary>
    Task<FillGridExecutionResult> ExecuteApplicableFillGridAsync(
        AxDataState state,
        IReadOnlyCollection<FieldAddress> changedParents,
        AxUserContext user,
        CancellationToken cancellationToken);
}

public static class AxpertFieldEngineExtensions
{
    /// <summary>Creates an operation, evaluates one value, and returns the completed dependent changes.</summary>
    /// <returns>The first failed stage, or the completion result when evaluation succeeds.</returns>
    public static async Task<FieldEvaluationResult> SetValidateAndRefreshAsync(
        this IAxpertFieldEngine engine,
        AxDataState state,
        FieldAddress field,
        object? value,
        AxUserContext user,
        CancellationToken cancellationToken)
    {
        var started = await engine.CreateOperationAsync(state, user, cancellationToken);
        if (!started.Succeeded || started.Operation is null)
        {
            return FieldEvaluationResult.Failure(new FieldEvaluationError(
                field,
                "AXD-VAL-001",
                started.Error ?? "The Axpert field operation could not be created."));
        }

        await using var operation = started.Operation;
        var evaluated = await operation.EvaluateAsync(field, value, cancellationToken);
        return evaluated.Succeeded
            ? await operation.CompleteAsync(cancellationToken)
            : evaluated;
    }
}

public interface IAxpertFillGridEngine
{
    /// <summary>Executes every FillGrid definition whose parent dependencies intersect the changed fields.</summary>
    Task<FillGridExecutionResult> ExecuteApplicableAsync(
        AxDataState state,
        IReadOnlyCollection<FieldAddress> changedParents,
        AxUserContext user,
        CancellationToken cancellationToken);
}

public interface IAxpertTransactionWriter
{
    /// <summary>Persists an AxData transaction through Axpert using the supplied trusted save context.</summary>
    Task<AxSaveResult> SaveAsync(
        AxDataState state,
        AxSaveContext context,
        CancellationToken cancellationToken);
}
