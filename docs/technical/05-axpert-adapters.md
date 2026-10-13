## 5. Existing Axpert adapter contracts

Exact signatures will be finalized after source inspection.

```csharp
public interface IStructDefProvider
{
    Task<StructDefHandle> GetAsync(
        string tenantId, string transId, CancellationToken cancellationToken);
}

public interface IAxpertDataLoader
{
    Task<AxDataLoadResult> LoadAsync(
        StructDefHandle definition, AxRecordKey key,
        AxUserContext user, CancellationToken cancellationToken);
}

public interface IAxpertFieldEngine
{
    Task<FieldEvaluationResult> SetValidateAndRefreshAsync(
        AxDataState state, FieldAddress field, object? value,
        AxUserContext user, CancellationToken cancellationToken);
}

public interface IAxpertFillGridEngine
{
    Task<FillGridResult> ExecuteApplicableAsync(
        AxDataState state, IReadOnlyCollection<FieldAddress> changedParents,
        AxUserContext user, CancellationToken cancellationToken);
}

public interface IAxpertTransactionWriter
{
    Task<AxSaveResult> SaveAsync(
        AxDataState state, AxSaveContext context,
        CancellationToken cancellationToken);
}
```

`IAxpertFieldEngine` will adapt existing Validate and Parser behavior. `IAxpertTransactionWriter` may wrap AxPut or its underlying routines only after verifying that transaction ownership and idempotency can be controlled externally.
