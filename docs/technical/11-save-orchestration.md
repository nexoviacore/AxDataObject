## 11. Save orchestration

AxTstructData's `POST /api/v{version}/AxPut` owns business Save. AxData owns Init/Get/Set/Query and the active DataObject; it does not wrap AxPut in an external Save saga. AxPut and AxData share the versioned tenant-scoped DataObject contract stored at `AXD:{tenantId}:{objectId}`.

PEG submission/approval remains distinct from business Save. A future `SaveOnApprove=true` final approval may call AxPut through a narrow trusted server client. No public bypass flag is accepted.

### 11.1 Request mode and state selection

Every AxPut request is ObjectId-aware:

1. Authenticate the ARM session and resolve trusted tenant, user, roles, Axpert session, and dimensions.
2. If `objectid` is absent, generate a cryptographically unpredictable ULID. Omitted `retainDataObject` defaults to true.
3. If `objectid` is present, load the tenant-scoped DataObject, authorize the actor, require `If-Match` or `expectedVersion`, and compare the current StructDef hash.
4. If a new ObjectId is generated, require a `Data` item containing TransId/action, initialize StructDef state, and establish the initial object version.
5. Hydrate one fresh StructDef/Validate/Parser runtime from the DataObject.
6. If `Data.SubmitData` contains values, overlay them. Missing or `{}` SubmitData applies no overlay; AxPut may still save the hydrated or initialized/default values.
7. Run dependency refresh and mandatory final-current-value validation. A failed operation never contributes a computed value or proceeds to SQL.
8. Run AxPut's existing main/grid/child/GenMap/MdMap/attachment transaction sequence and commit once.
9. Apply returned record-ID mappings and finalize the DataObject. Retain it by default; when `retainDataObject=false`, durably close/tombstone it and remove the active Redis key only after commit.

Missing `Data` is valid only for an existing ObjectId, where TransId/action/state are derived from the stored object. Missing `Data` for a newly generated ObjectId is invalid. Validation, SQL-generation, or transaction failure retains the unchanged object.

### 11.2 Concurrency and definition consistency

Object version and StructDef hash solve different problems:

- Existing-object Save requires the client's expected object version. A stale request returns 409 and the current version; it never overwrites silently.
- AxPut atomically claims the object for Saving so Set cannot mutate it during persistence. One successful logical Save produces one client-visible version advance.
- AxPut compares the object's stored SHA-256 StructDef revision with the current authoritative raw definition. A mismatch returns the 409-class definition-conflict error and requires re-Init.
- Snapshot `schemaVersion` is serialization compatibility only; it is not a concurrency version.

Failure responses never serialize `version: null`. Return the current version when the object was loaded or initialized; otherwise omit the property. Always return a supplied/generated ObjectId when it is safe to do so.

### 11.3 Transaction boundary

AxPut already owns the provider transaction. Main data, grids, child transactions, GenMaps, MdMaps, and other same-database required writes remain inside that boundary. Do not introduce an AxData transaction writer or claim atomicity across another database/service.

The first implementation slices establish correct ObjectId state, hydration/overlay, validation, transaction parity, versioning, and retention. Production durability is completed by later slices that add idempotency, transactional outbox, dispatch, retry, and recovery workers.

### 11.4 Idempotency and outbox (later slices)

ObjectId-aware Save will require `Idempotency-Key`. The canonical request hash includes tenant, supplied ObjectId when present, expected version, TransId/action, payload, and retention behavior. A generated ObjectId is stored in the result.

Idempotency completion, business writes, same-database mappings, stored response, and outbox rows join AxPut's transaction. Same key/same hash returns the stored result; same key/different hash conflicts. Cross-database/service work is delivered from the outbox. Dispatch, backoff, poison handling, and cleanup/state-finalization retry are separate final slices.

### 11.5 Logging

AxData logs remain value-free. Inside `ARM-11.4` only, ObjectId-aware AxPut Save preserves the current AxTstructData trace behavior and may add operation-relevant trace messages containing DataObject state, request/result content, session identifiers, and field values. New trace messages must not add tokens, connection strings, or unrelated secrets. The exception does not extend to AxData, workers, or new non-AxTstructData components.
