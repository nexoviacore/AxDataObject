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
    Task<FieldEvaluationResult> SetValidateAndRefreshAsync(
        AxDataState state,
        FieldAddress field,
        object? value,
        AxUserContext user,
        CancellationToken cancellationToken);
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
