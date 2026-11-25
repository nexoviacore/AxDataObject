## 21. Testing strategy

### 21.1 Unit tests

- Object state and Saving lifecycle transitions.
- Row action validation.
- Permission filtering and approver restrictions.
- Set error collection and dependency skipping.
- FillGrid append/replace behavior.
- SaveOnApprove state-machine paths.
- Idempotency request hashing and duplicate results.
- Error mapping and serialization compatibility.

### 21.2 Adapter/contract tests

- Current StructDef loading without stored version/hash binding.
- Validate/Parser input/output equivalence with existing behavior.
- AxGet loading and AxPut saving.
- GenMap/MdMap result and transaction behavior.
- Existing PEG routine and table behavior.
- Delphi print-service request/response behavior.

### 21.3 Integration tests

- Redis token-owned atomic writes, lease expiry, and shared Set/AxPut exclusion.
- Dormant snapshot last-delivery-wins upsert remains isolated contract coverage, not an active runtime requirement.
- Database rollback when a child/GenMap/MdMap fails.
- Duplicate Save and duplicate PEG actions.
- Outbox commit and worker retry/deduplication.
- Redis loss/TTL eviction returns `404 / AXD-NOTFOUND-001` without DataObject database recovery.

### 21.4 Security tests

- Guessing another user's ObjectId.
- Cross-tenant ObjectId access.
- Approver accessing an unassigned task.
- Client attempting to activate internal permission bypass.
- Hidden/read-only field behavior.
- AFL secret leakage, path traversal, oversized files, and expired download links.

### 21.5 End-to-end scenarios

1. New transaction -> Set -> dependencies -> Save without PEG.
2. Existing transaction -> permission-filtered Get -> edit -> Save.
3. `SaveOnApprove=true` -> multi-level approval -> final Save -> print/notification.
4. Deferred Save validation failure at final approval -> recover/correct/retry.
5. `SaveOnApprove=false` -> PEG -> final approval -> `ApprovedAwaitingSave` -> explicit Save API.
6. Return for correction and resubmission.
7. Reject in both SaveOnApprove modes.
8. Recall by initiator; a literal `admin` role grants no relationship or bypass.
9. Timed-out AxPut is not automatically retried; future idempotency coverage owns duplicate replay.
10. Concurrent same-user requests produce one active operation and `409 / AXD-CONC-002` for overlap; sequential stale tabs are accepted last-completed-wins.
11. Redis restart preserves state only according to Redis durability; missing active state returns not found while PEG persistence/recovery remains separately designed.
12. Bulk Save using CallerId and Sequence.

### 21.6 P1D-05 field-runtime and capacity evidence

- AxTstructData parity: runtime initialization order, existing-record baseline load, normalized source IDs, submitted values, validation, and result readback.
- One normal Set performs one DataObject load, creates and hydrates one Axpert runtime regardless of submitted field count, reads the final runtime once, and performs one token-owned write; rebuilding after a rejected field is explicit and cannot leak rejected state into independent fields.
- Existing normalized fields round-trip current/original display values and source IDs; arbitrary client IDs are not trusted.
- Versioned v1/v2/v3 payloads are rejected; the discriminator-free current positional payload is deterministic, sparse for clean fields, and enforces its measured byte/row limits.
- Definition changes do not reject an existing object: the next operation uses the current StructDef, no `AXD-CONC-003` path remains, and clients cannot bind or bypass a definition version.
- Binding-bypass diagnostics contain only operation, tenant/schema, TransId, ObjectId, and trace ID; they never contain values, bodies, secrets, or tokens.
- Baseline and after-change measurements include runtime creations, StructDef loads, hydration/readback counts, semaphore wait, validation duration, Set latency/throughput, serialized bytes, and approximate memory for small, medium, and 50 DC x 70 rows x 100 fields cases.
- Concurrency covers two objects, tenants, and simultaneous Set requests. The global semaphore is kept or narrowed only from documented thread-safety and throughput evidence.
