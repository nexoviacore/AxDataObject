## 9. PEG Lifecycle

### 9.1 Initiate

PEG uses the applicable Axpert workflow configuration to determine whether the transaction requires approval and identifies the first-level approver(s).

On initiation, PEG establishes the active workflow state:

- `ApprovalStatus = Pending`
- `ApprovalLevel = 1`
- Current `Approvers`
- Workflow/Task information
- Initiator/Creator and tenant context

The active PEG state is maintained in Redis along with the associated `AxDataObjectInstance`. Notifications are delivered asynchronously through the Transactional Outbox.

Durable persistence of PEG workflow state to `axactivetasks` and `axactivetaskstatus`, where required, will be finalized as part of the persistence and recovery design.

### 9.2 Intermediate Approval

The current approver and expected task/workflow version are validated.

On a valid approval action, PEG:

- Records the approval action and comments.
- Updates the current approval level.
- Determines the next approver(s).
- Updates the active PEG state in Redis.
- Queues the required notifications/events.

The workflow state remains associated with the same `ObjectId` and AxData runtime object throughout the approval lifecycle.

### 9.3 Final Approval and `SaveOnApprove`

#### `SaveOnApprove = true`

```text
Initiate PEG
      ↓
AxData + PEG active state in Redis
      ↓
Intermediate approvals
      ↓
Final approver action
      ↓
Idempotent AxData Save
      ↓
Save succeeds
      ↓
Mark PEG as Approved
      ↓
Complete workflow
```

The business transaction is persisted to the business database as part of the final approval process.

If the AxData Save fails:

- The workflow must not be marked as successfully Approved.
- Validation/save errors are returned.
- The final approval state remains recoverable for retry or correction.

#### `SaveOnApprove = false`

```text
AxData Object created/updated
        ↓
Data remains in redis.
        ↓
AxSaveData() -> When saving data to DB
        ↓
Initiate PEG
        ↓
Intermediate approvals
        ↓
Final approval
        ↓
Complete PEG

```

In this mode, PEG approval does **not automatically persist the AxData transaction to the business database**.

The current AxData runtime state remains available in Redis after approval, subject to the configured object lifecycle and retention policy.

A separate **AxData Save API** is exposed to explicitly persist the current AxData runtime state:

```text
AxData Runtime State
        ↓
     Save API
        ↓
Business Database
        +
Updated Redis Runtime State
```

When the Save API is successfully executed:

- The current transaction data is persisted to the business database.
- The corresponding AxData runtime state remains in Redis.
- Subsequent operations continue to use the Redis runtime object until its lifecycle is completed or the configured cleanup/retention policy removes it.

Therefore, with `SaveOnApprove = false`, **PEG approval completion and business-data persistence are separate operations**. The business data is persisted only when the explicit AxData Save API is invoked.

### 9.4 Reject

The current approver is validated before processing the rejection.

PEG:

- Records the rejection action and comments.
- Sets `ApprovalStatus = Rejected`.
- Updates the active workflow state in Redis.
- Closes/updates the applicable approval task.
- Records the workflow audit information.
- Queues notifications through the Transactional Outbox.

For deferred-save workflows (`SaveOnApprove = true`), rejection does not perform the final business-data Save.

The associated AxData runtime object may be retained in Redis for the configured retention/cleanup period to support audit, recovery, or configured correction behavior.

### 9.5 Return

Return sends the transaction back to an authorized initiator/creator or previous workflow participant for correction.

PEG:

- Validates the current approver and target user.
- Updates the approval level and task assignment.
- Preserves the associated AxData runtime state.
- Records the return action and comments.
- Updates the workflow state in Redis.
- Queues notifications to the relevant participants.

Return does not perform a final business-data Save.

The returned transaction can subsequently be corrected and resubmitted according to the configured workflow rules.

### 9.6 Recall

The initiator/creator or an authorized administrator may recall a pending transaction, subject to the configured workflow rules.

PEG:

- Validates recall authorization.
- Sets `ApprovalStatus = Recalled`.
- Closes or updates outstanding approval tasks.
- Preserves the workflow audit history.
- Updates the active PEG state in Redis.
- Queues the required notifications.

Recall does not perform a final business-data Save unless explicitly required by the configured business rules.
