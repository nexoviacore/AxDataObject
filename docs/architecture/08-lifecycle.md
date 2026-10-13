## 8. Primary transaction lifecycle

### 8.1 Initialize

For a new transaction, the client supplies TransId with an empty key. For an existing transaction, it supplies a key field/value or RecordId. AxData:

1. Authenticates and authorizes the caller.
2. Loads the applicable StructDef.
3. Creates a new data array or loads existing data through an adapter.
4. Generates ObjectId and version `1`.
5. Stores the active serialized state in Redis.
6. Returns ObjectId, version, and relevant initial metadata.

### 8.2 Set

For every submitted DC/row/field, AxData:

1. Confirms ObjectId ownership and write permission.
2. Acquires short-lived object concurrency protection.
3. Confirms the request version is current.
4. Applies the row action and field value.
5. Validates the field.
6. Evaluates dependent fields only when the source field succeeds.
7. Runs applicable FillGrid rules.
8. Continues across independent fields and collects all errors.
9. Increments the object version and updates Redis atomically.
10. Enqueues one complete snapshot after the Set request completes; it does not persist after every individual field evaluation.
11. Returns success/errors and, when `GetDeps=true`, the fields changed by dependency processing.

### 8.3 Get/query

Get returns requested fields/DCs or the complete transaction. It applies permissions before serialization:

- No transaction access: authorization error.
- Hidden DC/field: omitted.
- Read-only transaction/DC/field: returned with read-only metadata.
- Current approver: `PendingApproval=true` and only authorized operations.
- Non-initiator with view permission but no approval role: read-only transaction.

### 8.4 Save

Save performs final validation and executes the main tstruct, grids, child transactions, GenMaps, and MdMaps inside the available transactional boundary. It is idempotent; retrying the same logical request returns the stored result rather than writing again.

Post-save printing, notification, and integration work is recorded through a transactional outbox and performed asynchronously.

### 8.5 Print

Print accepts explicit print-form names or a tstruct field containing them. The request is authorized against ObjectId, then queued or executed through the print integration. Generated output is stored in the configured location and returned through an authorized, time-limited link.

### 8.6 Destroy or evict

An active object can be removed from Redis after its lifecycle completes or its inactivity TTL expires. Durable snapshot and workflow records follow the configured retention policy. Redis eviction is not permanent deletion; the object can be reconstructed when its durable state is still valid.

`AxOpen=true` keeps the logical object open beyond normal completion, subject to a configured maximum retention policy. Changing it to false permits cleanup.
