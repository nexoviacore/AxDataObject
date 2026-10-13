## 13. Database persistence

### 13.1 AxDataObjectInstance

The stated base fields are retained and supplemented with version/lifecycle data needed for safe recovery. Exact data types follow the supported database.

| Field | Purpose |
|---|---|
| ObjectId | Primary logical identifier |
| TenantId/AxpertInstanceId | Tenant isolation |
| TransId | Tstruct identifier |
| KeyField, KeyValue | Business lookup key |
| RecordId | Persisted Axpert record ID |
| DataObjectJson | Latest serialized snapshot |
| ObjectVersion | Optimistic concurrency/recovery version |
| StructDefVersion | Definition used by the object |
| Username/OwnerUserName | Object owner |
| InitiatorUserName | Workflow initiator |
| PersistDateTimeUtc | Snapshot time |
| LastAccessedDateTimeUtc | Expiry/recovery support |
| LifecycleStatus | Active/Saved/Rejected/etc. |
| ApprovalStatus | Workflow projection |
| PendingWith | Current approvers projection |
| PendingFromDateTimeUtc | Current pending time |
| AxOpen | Lifecycle override |

One current snapshot row exists per tenant/ObjectId. If full history is required, use a separate append-only history/audit table rather than repeatedly expanding this row.

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
