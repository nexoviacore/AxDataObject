# P0-04 salvage assessment

This report assesses `ARM-11.4/AxDataObject` against the new AxData architecture. It also compares the AxGet, AxPut, and permission flows that `AxDataObject` mirrors in `ARM-11.4/AxTstructData`. It is an assessment of behavior to port, not permission to reference the old projects directly from Domain or Application.

For every later Phase 1-3 implementation plan, any work involving a capability below must name the applicable bucket and item before code is written. A plan must not interpret a **Reuse with changes** item as approval to copy the old type wholesale.

## Reuse as-is

No complete production type in `AxDataObject` qualifies for reuse as-is. The project consistently combines at least two concerns that the new design separates (runtime state, Axpert execution, persistence, authentication, transport, or logging), and its public contracts do not carry the required tenant-scoped ObjectId, expected version, or idempotency information.

The following small, implementation-level conventions can be retained unchanged inside their eventual owning type, with focused tests:

- Case-insensitive field-name lookup and one-based DC/row addressing. These match Axpert metadata and can live as internal helpers in `AxData.Domain`.
- Non-grid fields resolving to logical row 1. This invariant can live in the Domain field-address/value model.
- The existing row action codes (`n`, `e`, `d`) and `axp_recid`/`<field>_sourceid` compatibility names. These match the new DC/row model and can be handled at the API/integration boundary without making the old DTOs domain types.

Future plans for the domain state model, request mapping, or compatibility serialization must cite this bucket when retaining these conventions.

## Reuse with changes

### Runtime field and row behavior

- `Models/DataObject.cs` contains useful behavior for field lookup, non-grid/grid value access, old/new value and source-id tracking, row record IDs, row actions, changed-field discovery, and DC/table grouping.
- Port the required behavior into small Domain types with explicit invariants and tests. Do not port `DataObject` itself: it owns a mutable `StructDef`, database helpers, logging, parser state, SQL generation, encryption/date conversion, and Redis reattachment concerns.
- Replace parallel string collections and encoded entries such as `row=value` with typed rows, fields, addresses, and lifecycle enums. Preserve one-based Axpert row translation only at a clear boundary.

### Initialize/AxGet population

- `DataObjectService.GetDataAsync`, `AxTstructData.AxGetService`, and their repositories demonstrate StructDef loading, DC-wise SQL selection, row population, `axp_recid`, source IDs, client date format, and refresh-on-load behavior.
- Rebuild this as an Application initialize use case using `IStructDefProvider`, `IAxpertDataLoader`, and `IAxpertFieldEngine`. The adapter may wrap the proven AxGet behavior, but authentication, tenant resolution, transaction permission, hidden DC/field filtering, ObjectId generation, version 1 creation, and Redis persistence belong to the new orchestration.
- Preserve the important sequence: establish definition/parser/validation context, load values as baseline old/new state, check every load/parse result, then refresh calculated fields. Do not copy the old direct SQL/result-shaping controller flow.

### Set, validation, dependencies, and FillGrid

- `SetFldValueAsync`, `SubmitValueAsync`, and the corresponding Validate calls show value submission followed by refresh/validation and changed-field collection.
- Adapt this behavior behind `IAxpertFieldEngine` and `IAxpertFillGridEngine`. The new use case must authorize first, acquire a short lock, compare the expected version, apply independent fields, evaluate dependents only after their source succeeds, collect structured errors, increment the version, and atomically write one complete Redis state.
- Do not retain the local dependency heuristic that searches `Cexp` text for the changed field. Dependency and FillGrid applicability must come from the Axpert engine/metadata contract.
- Date, number, source-key, and encryption conversions are compatibility concerns for the Axpert adapter or explicit converters; they must not depend on `ARMCommon.Utils` inside Domain.

### Query and response projection

- `BuildResponseData`, `BuildChangedFieldsMap`, and the AxGet response builders demonstrate how DC rows, values, row record IDs, and source IDs are projected.
- Reimplement them as permission-aware Application/API projections. Hidden DCs and fields must be omitted, and read-only metadata must reflect the requesting actor. Never serialize the stored object directly.

### StructDef, Validate, and Parser integration

- The old services show the working integration sequence for `StructDef.LoadStructure`, parser creation/cache restoration, `Validate.InitValidateAsync`, load submission, refresh, and final validation.
- Encapsulate this sequence in `AxData.Integration.Axpert`; expose only result-based ports to Application. A false or failed parser/validation operation must stop the dependent read or calculation. Never read a computed/parsed value merely because the old API left one available.
- Cache behavior must be tenant-aware and must not place concrete StructDef, Validate, Parser, or Common types in Domain/Application.

### Authorization lookup

- The AxGet/AxPut permission services and repository queries contain useful knowledge about Axpert view/create/edit permission precedence, included/excluded DCs, and included/excluded fields.
- Port that knowledge behind an Application authorization port implemented in `AxData.Integration.Axpert`. The new orchestration order remains: authenticate, resolve tenant, tenant-scoped object load, owner/current-approver/admin check, transaction/DC/field permission, execute.
- Client roles, usernames, and ObjectId possession cannot establish authority. Permission-filtered output and mutation checks must be tested independently.

### Redis active-state persistence

- `DataObjectRepository` demonstrates serialization round-trips and service reattachment after a Redis read.
- Replace it with an Application state-store port and an infrastructure implementation using `AXD:{tenantId}:{objectId}`, schema-versioned snapshots, TTL, a short owned lock, and atomic version compare-and-set. Store data/state only; do not serialize services, connections, parser instances, credentials, or an unnecessary full StructDef.
- Snapshot publication occurs after a completed Set, not after each field evaluation. Redis eviction is recoverable eviction, not durable deletion.

### Save/AxPut, GenMap, and MdMap behavior

- `AxTstructData.AxPutService` and the incomplete/commented `AxDataObject` save path contain business knowledge for create/edit row selection, record-ID allocation, child transactions, GenMaps, MdMaps, final validation, attachments, and table-wise commands.
- Treat these as reference behavior for `IAxpertTransactionWriter`, not as reusable orchestration. The writer must fit the new idempotent Save transaction: main/DC/child writes, mappings, idempotency record, and outbox records execute once within the available transaction boundary.
- Existing SQL-building and mapping routines require database-provider review, parameterization review, explicit success checks, and tests before any port. Cross-database mappings require an outbox/saga rather than a claimed single transaction.
- PEG submit/approval and business Save remain distinct. `SaveOnApprove=true` uses trusted internal Save only at final approval; `SaveOnApprove=false` reaches `ApprovedAwaitingSave` and requires explicit Save.

Future plans for initialize, set, query, authorization, Redis, snapshots, save, GenMap/MdMap, or PEG integration must cite the corresponding subsection in this bucket.

## Discard

- The `AxDataObject` project/application composition (`Program.cs`, project references, and direct ARMCommon/AxStructDef/AxValidate/AxParser wiring): it violates the required Api -> Application -> Domain dependency direction and Axpert port boundary.
- `DataObjectController` and the old DTOs as public API contracts: they use ARMSessionId and client roles, identify state by TransId/user/record rather than tenant-scoped ObjectId, and lack expected version, idempotency key, correlation/error contract, and permission-filtered query semantics.
- The Redis key `{schema}:ARM:{transId}:{userName}:{recordId}:ARM-AX-DATAOBJECT`: it is not the required `AXD:{tenantId}:{objectId}` identity and does not implement ObjectId authorization, version CAS, owned locking, or recovery semantics.
- Blind Redis replacement and load-or-create during Set: these permit silent overwrite and bypass the required stale-version `409` behavior.
- Disabled/commented permission and SetData paths: inactive code is not an implementation and must not be revived as the new security or Save boundary.
- The monolithic serialized `DataObject`/mutable `StructDef` state shape and `ReAttachServices`: runtime services, parser/validation machinery, database access, SQL generation, and business state must not share a serialized aggregate.
- Direct database and Redis calls from the state object, including synchronous `.Result`/`GetAwaiter().GetResult()`: Domain state must be infrastructure-free and async operations must remain async.
- Hand-built authorization inside controllers/services and trust in request roles/user fields: identity and tenant come only from trusted authenticated context.
- Catch-and-continue/default behavior around parser, conversion, database, and sequence operations: failures cannot be swallowed before stale or computed values are read.
- Logging serialized exceptions, submitted values, permission result data, SQL-oriented details, or record parameter values: logs are limited to safe identifiers such as tenant, TransId, ObjectId, version, and trace ID.
- Ad-hoc SQL concatenation and database-specific branching in the runtime object: persistence belongs behind the Axpert writer/database adapter with parameterized, tested provider behavior.
- Deleting the active object immediately after Save as the universal lifecycle rule: retention, PEG state, AxOpen, durable snapshots, and recovery determine cleanup.
- Health/status and user-permission controllers duplicated from the old service: the new solution already owns its API surface and should expose authorization through its use cases rather than recreate the legacy microservice.
- Old appsettings files and launch/deployment settings: they are service-specific, may encode obsolete infrastructure assumptions, and are not configuration sources for the new solution.

Future plans must cite this bucket when rejecting a legacy shortcut that overlaps one of these items.

## Open questions

These require an explicit decision or verified adapter spike; this report does not answer them.

- Which minimum StructDef reference/subset must be embedded in a snapshot to guarantee exact recovery when the shared StructDef cache changes?
- What is the authoritative Axpert dependency/FillGrid traversal contract, including independent-field continuation after one submitted field fails?
- Which Parser and Validate methods can expose reliable success-plus-value results, and which require wrappers to prevent stale-value reads?
- The two `SubmitValue` overloads apply opposite encryption operations. What is the authoritative plaintext/ciphertext contract for load, client submission, validation, snapshot storage, and database Save?
- Which AxGet permission/DC-SQL behaviors are authoritative when user, role, record, included/excluded DC, and included/excluded field rules overlap?
- Which `DataObject` GenMap, child-transaction, auto-number, and MdMap routines represent complete production behavior, versus an incomplete port from AxPut/StructDef?
- Can the existing self-committing AxPut path be wrapped while preserving the required idempotency/outbox transaction, or is a lower-level writer entry point required?
- How are record-ID allocation and sequence increments made idempotent across retries, validation-only requests, child transactions, and failed commits?
- Which mapping operations can share the business database transaction, and which target another database/service and therefore require saga/outbox handling?
- What durable PEG persistence/recovery rule will coordinate Redis, `axactivetasks`, `axactivetaskstatus`, the business Save, and the transactional outbox?
- What retention rule applies after Saved, ApprovedAwaitingSave, Rejected, Recalled, and AxOpen states, and when is snapshot-backed Redis eviction permitted?

Any later plan touching one of these questions must stop for confirmation unless the question has been resolved in the designated decision record.
