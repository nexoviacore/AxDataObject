## 8. Primary transaction lifecycle

### 8.1 Initialize

For a new transaction, the client supplies TransId with an empty key. For an existing transaction, it supplies a key field/value or RecordId. AxData:

1. Authenticates and authorizes the caller.
2. Loads the applicable StructDef.
3. Creates a new data array or loads existing data through an adapter.
4. Generates a cryptographically unpredictable ObjectId.
5. Stores the active serialized state in Redis.
6. Returns ObjectId and relevant initial metadata without an object version.

### 8.2 Set

For every submitted DC/row/field, AxData:

1. Confirms the trusted initiator/current-approver/admin relation and write permission after tenant-scoped loading.
2. Acquires short-lived object concurrency protection.
3. Loads the current StructDef without a stored definition binding.
4. Applies the row action and field value.
5. Validates the field.
6. Evaluates dependent fields only when the source field succeeds.
7. Runs applicable FillGrid rules.
8. Continues across independent fields and collects all errors.
9. Atomically verifies the lease token, writes the complete Redis state once, and refreshes TTL.
10. Returns success/errors and, when `GetDeps=true`, the fields changed by dependency processing.

### 8.3 Get/query

Get returns requested fields/DCs or the complete transaction. It applies permissions before serialization:

- No transaction access: authorization error.
- Hidden DC/field: omitted.
- Read-only transaction/DC/field: returned with read-only metadata.
- Current approver: `PendingApproval=true` and only authorized operations.
- Non-initiator with view permission but no approval role: read-only transaction.

### 8.4 Save

AxTstructData AxPut performs final validation and executes the main tstruct, grids, child transactions, GenMaps, and MdMaps inside its provider transaction. Existing-object Save first establishes the shared persistent `Saving` claim. Timed-out AxPut requests are not automatically retried; idempotency is future work.

Post-save printing, notification, and integration work is recorded through a transactional outbox and performed asynchronously.

### 8.5 Print

Print accepts explicit print-form names or a tstruct field containing them. The request is authorized against ObjectId, then queued or executed through the print integration. Generated output is stored in the configured location and returned through an authorized, time-limited link.

### 8.6 Destroy or evict

An active object remains in Redis until an explicit lifecycle action removes or closes it. Redis is the only active DataObject-state store in the current scope, so active keys have no inactivity TTL. Eviction or manual deletion makes the object unavailable and later access returns not found without database recovery.

`AxOpen` remains a possible domain concept but is not persisted in the current compact active-state payload.
