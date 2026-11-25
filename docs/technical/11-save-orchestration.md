## 11. Save orchestration

AxTstructData's `POST /api/v{version}/AxPut` owns business Save. AxData owns Init/Get/Set/Query and the active DataObject; it does not wrap AxPut in an external Save saga. AxPut and AxData share the tenant-scoped DataObject contract stored at `AXD:{tenantId}:{objectId}`.

PEG submission/approval remains distinct from business Save. A future `SaveOnApprove=true` final approval may call AxPut through a narrow trusted server client. No public bypass flag is accepted.

### 11.1 Request mode and state selection

Every AxPut request is ObjectId-aware:

1. Authenticate the ARM session and resolve trusted tenant, user, roles, Axpert session, and dimensions.
2. If `objectid` is absent, generate a cryptographically unpredictable ULID. Omitted `retainDataObject` defaults to true.
3. If `objectid` is present, atomically establish the persistent `Saving` claim, load the tenant-scoped DataObject, and authorize the actor.
4. If a new ObjectId is generated, require a `Data` item containing TransId/action and initialize StructDef state.
5. Hydrate one fresh StructDef/Validate/Parser runtime from the DataObject.
6. If `Data.SubmitData` contains values, overlay them. Missing or `{}` SubmitData applies no overlay; AxPut may still save the hydrated or initialized/default values.
7. Run dependency refresh and mandatory final-current-value validation. A failed operation never contributes a computed value or proceeds to SQL.
8. Run AxPut's existing main/grid/child/GenMap/MdMap/attachment transaction sequence and commit once.
9. Apply returned record-ID mappings and finalize the DataObject. Retain it by default; when `retainDataObject=false`, durably close/tombstone it and remove the active Redis key only after commit.

Missing `Data` is valid only for an existing ObjectId, where TransId/action/state are derived from the stored object. Missing `Data` for a newly generated ObjectId is invalid. Validation, SQL-generation, or transaction failure retains the unchanged object.

### 11.2 Concurrency and definition consistency

AxPut atomically establishes a persistent `Saving` claim while checking the shared Set lock. Set checks the claimed lifecycle and uses a token-owned short lease. Overlap returns `409 / AXD-CONC-002`; an expired Set holder cannot commit because its final Redis write verifies the current lease token.

There is no client object version, `If-Match`, `expectedVersion`, or stale-tab rejection. Sequential requests are independent and last-completed wins. Do not automatically retry a timed-out AxPut request. Every operation loads the current StructDef without a stored definition binding. Responses omit object version and return a supplied/generated ObjectId when safe.

### 11.3 Transaction boundary

AxPut already owns the provider transaction. Main data, grids, child transactions, GenMaps, MdMaps, and other same-database required writes remain inside that boundary. Do not introduce an AxData transaction writer or claim atomicity across another database/service.

The current implementation establishes ObjectId state, hydration/overlay, validation, transaction parity, server-side exclusion, and retention. Broader idempotency/outbox durability remains future work and is not part of the current HTTP contract.

### 11.4 Idempotency and outbox (future work)

`Idempotency-Key` is not accepted or interpreted now. If duplicate-request replay is added later, its request identity, stored-result semantics, business-transaction boundary, and reconciliation rules require a separate approved design. Until then, callers must not automatically retry AxPut after a timeout.

### 11.5 Logging

AxData logs remain value-free. Inside `ARM-11.4` only, ObjectId-aware AxPut Save preserves the current AxTstructData trace behavior and may add operation-relevant trace messages containing DataObject state, request/result content, session identifiers, and field values. New trace messages must not add tokens, connection strings, or unrelated secrets. The exception does not extend to AxData, workers, or new non-AxTstructData components.
