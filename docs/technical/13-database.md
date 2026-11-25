## 13. Database persistence

### 13.1 AxDataObjectInstance (dormant future use)

The compiled snapshot table is not an active DataObject dependency. Its current version-free shape is retained only for possible future redesign; Redis misses do not read it.

| Field | Purpose |
|---|---|
| ObjectId | Primary logical identifier |
| TenantId/AxpertInstanceId | Tenant isolation |
| TransId | Tstruct identifier |
| RecordId | Persisted Axpert record ID |
| DataObjectJson | Latest serialized snapshot |
| InitiatorUserName | Workflow initiator |
| PersistDateTimeUtc | Snapshot time |
| LastAccessedDateTimeUtc | Expiry/recovery support |
| LifecycleStatus | Active/Saved/Rejected/etc. |
| ApprovalStatus | Workflow projection |
| PendingWith | Current approvers projection |
| PendingFromDateTimeUtc | Current pending time |

The dormant schema has at most one row per tenant/ObjectId and uses last-delivery-wins replacement. Reactivating it requires a separately approved ordering, durability, retention, and authorization design.

### 13.2 Idempotency store

Proposed `AxDataIdempotency` fields:

- TenantId, OperationType, IdempotencyKey: unique key.
- ObjectId and workflow task/instance where applicable.
- RequestHash.
- Status: `InProgress`, `Completed`, `FailedRetryable`, `FailedFinal`.
- ResultJson and relevant RecordId.
- LeaseExpiresAtUtc, CreatedAtUtc, CompletedAtUtc.

### 13.3 Transactional outbox

Proposed `AxOutbox` fields:

- EventId and deduplication key.
- TenantId, ObjectId, event type, payload JSON.
- CreatedAtUtc, AvailableAtUtc.
- Processing status, attempt count, last error.
- LockedUntilUtc, processed timestamp.

Workers claim bounded batches using database-safe locking, deliver idempotently, and move permanently failing messages to a review/dead-letter state.

### 13.4 Existing PEG tables

The implementation must inspect `axactivetasks`, `axactivetaskstatus`, and current database routines before defining migrations. Existing behavior is reused; new columns/tables are proposed only after a schema gap analysis.
