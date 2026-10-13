## 11. Save orchestration

Save orchestration first evaluates PEG applicability, `SaveOnApprove`, and current workflow state using the matrix in section 6.6. Database transaction processing begins only when business-data persistence is currently allowed. Submission and approval completion are workflow operations and must not be mistaken for a committed business Save.

### 11.1 Idempotency

Logical key selection:

1. Use `Idempotency-Key` when supplied.
2. For bulk save, derive/validate a stable key from tenant, ObjectId, CallerId, and Sequence.
3. Reject reuse of the same key with a different request hash.

Processing:

```text
Begin database transaction
Try insert idempotency row as InProgress
If existing Completed row with same request hash:
    return stored result
If existing row has different request hash:
    return idempotency conflict
Perform final validation
Save main record and DC/grid rows
Save required child transactions
Execute GenMaps and MdMaps once for operation key
Insert outbox records
Store response fields and mark idempotency Completed
Commit
Update AxData RecordId/status/version in Redis
Return stored result
```

After a successful explicit Save in the `SaveOnApprove=false` path, update the Redis lifecycle from `ApprovedAwaitingSave` to `Saved` while retaining the completed PEG state for the configured retention period.

An `InProgress` record left by a crash requires lease/recovery rules; it cannot block forever.

### 11.2 Transaction boundary

All writes using the same transactional database connection participate in one transaction. If current AxPut commits internally, an adapter or new orchestration entry point will be required; this must be assessed before implementation.

For mappings to a different database/service, use an outbox/saga and define compensation or eventual-consistency behavior. Do not claim a single atomic transaction where the infrastructure cannot provide one.

### 11.3 Post-save behavior

Print generation, notifications, and integration queue pushes are represented as outbox events in the database transaction. Save returns the accepted artifact/event information. Workers later deliver and update status.
