## 12. PEG processing and state consistency

### 12.1 Active state and persistence boundary

The active PEG workflow state is maintained in Redis and associated with the AxData object. The exact durable role of `axactivetasks` and `axactivetaskstatus`—including which transitions are written, when they are written, and how Redis recovery uses them—must be finalized after inspection of the existing PEG implementation.

The implementation must expose storage through contracts so the selected durability rule can be applied without changing PEG domain logic:

```csharp
public interface IPegWorkflowService
{
    Task<PegInitiationResult> InitiateAsync(...);
    Task<PegActionResult> ApproveAsync(...);
    Task<PegActionResult> RejectAsync(...);
    Task<PegActionResult> ReturnAsync(...);
    Task<PegActionResult> RecallAsync(...);
}

public interface IPegActiveStateStore
{
    Task<PegState?> GetAsync(...);
    Task CompareAndSetAsync(PegState state, long expectedVersion, ...);
}

public interface IPegHistoryRepository
{
    Task RecordTransitionAsync(PegTransition transition, ...);
    Task<PegRecoveryState?> GetRecoveryStateAsync(...);
}
```

Every transition checks:

- Authenticated user is authorized for the task/action.
- Task is still open and at the expected level.
- Expected workflow version matches.
- Idempotency key has not been completed with different input.

It then atomically updates the Redis PEG state and completes any configured durable history/recovery persistence before returning success. Notification intent is written to the Transactional Outbox according to the persistence boundary selected for the operation.

### 12.2 Final approval with deferred save

In the initial same-deployment model, the PEG application service will orchestrate a narrow trusted AxPut call. The permission/PEG-initiation checks already established by PEG may be skipped only through trusted server code, while final validation and AxPut transaction rules cannot be skipped. Save idempotency remains a prerequisite for production retry/recovery rather than a current capability.

Required outcome:

- Save succeeds: record the final Approved transition according to the configured PEG persistence rule and synchronize Redis.
- Save fails validation/systemically: do not report Approved; retain a recoverable final-level state and return errors.

Whether AxData business Save and durable PEG history can share one SQL transaction depends on the selected PEG persistence rule and existing database connections/routines. If they cannot, use a recoverable `FinalApprovalPendingSave` saga state and idempotent recovery so a crash cannot produce an untracked saved record or a false Approved response.

### 12.3 Approval separated from explicit Save

For `SaveOnApprove=false`, PEG is initiated and completed while the current AxData transaction remains in Redis. Final approval:

1. Validates and records the final PEG action.
2. Marks the workflow `Approved`.
3. Changes the AxData lifecycle state to `ApprovedAwaitingSave`.
4. Does not call the business-data writer.
5. Returns success with an explicit indication that database Save is still required.

The authorized client subsequently calls `POST /api/v{version}/AxPut`. AxPut loads the approved tenant-scoped Redis state, verifies the current workflow state and authorization, establishes its server-owned `Saving` claim, then performs the business Save. Retained state returns to `Active`; no DataObject version participates.

PEG completion and later Save are separate logical operations. Their future idempotency identities must also remain separate. Recovery must preserve `ApprovedAwaitingSave` according to the eventual PEG persistence design.

### 12.4 Redis and outbox consistency

Redis state and a SQL Transactional Outbox cannot normally participate in one atomic transaction. Before implementation, PEG must select one recoverable write pattern:

1. Persist the PEG transition and outbox record together in the PEG/database transaction, then update Redis; or
2. Atomically update Redis and append the transition to a durable Redis Stream, then let a consumer populate PEG history/outbox; or
3. Use an explicit saga state and reconciliation worker that can detect and repair an incomplete Redis/database step.

The API must not report a critical transition as successful until the chosen durability point succeeds. This choice depends on the existing `axactivetasks`, `axactivetaskstatus`, Redis configuration, and AxPersist infrastructure and is tracked as a blocking clarification.
