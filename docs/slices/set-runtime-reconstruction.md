# Set runtime reconstruction and flow consolidation

This work follows the production Init simplification: complete legacy processing in one request-owned runtime, then
construct the final AxData state once. It does not make StructDef a persisted domain object and does not weaken
tenant, permission, lifecycle, lock-ownership, malformed-state, or rejected-field safeguards.

## AXD-SET-01 — Reconstruct Set state from one processed StructDef runtime

**Effort:** Deep  
**Needs:** D3, D38, D40, D41, D46-D49  
**After:** AXD-STATE-05  
**Before:** AXD-STATE-06

### Approved assumptions

- The surrounding Set/Save coordination contract prevents either operation from changing the DataObject between
  Set's initial tenant-scoped load and its lock acquisition. Under that contract Set loads the DataObject once; it
  does not perform a second post-lock Redis load. If implementation review cannot identify and test that guarantee,
  stop rather than deleting the second load on assumption alone.
- Administrator is not a DataObject relationship or a PEG assignment. Object access comes from the persisted
  initiator, the current trusted PEG assignment where applicable, and fresh operation permission. There is no generic
  administrator override.
- Public Set request/response shapes and existing AXD error codes do not change.
- D3 remains authoritative: valid independent fields may succeed when another field is rejected. Rejected runtime
  state must not influence a later independent field.

### Read

- `docs/PROGRESS.md` and only this block
- `docs/DECISIONS.md`: D3, D38, D40, D41, D46-D49
- `docs/SALVAGE.md`: **Set, validation, dependencies, and FillGrid** and **StructDef, Validate, and Parser integration**
- `.agents/skills/axdata-slice/references/axpert-parity.md`
- `AxData/src/AxData.Application/Set/SetAxData.cs`
- `AxData/src/AxData.Application/Ports/Axpert/AxpertPorts.cs`
- `AxData/src/AxData.Application/Ports/Axpert/AxpertContracts.cs`
- `AxData/src/AxData.Application/Authorization/AxAuthorizationContext.cs`
- `AxData/src/AxData.Application/Authorization/AxAuthorizationPolicy.cs`
- `AxData/src/AxData.Application/Ports/Identity/UserContext.cs`
- `AxData/src/AxData.Application/Query/QueryAxData.cs`
- `AxData/src/AxData.Domain/Model/AxDataState.cs`, `AxDataContainer.cs`, `AxDataRow.cs`, and `AxFieldValue.cs`
- `AxData/src/AxData.Integration.Axpert/FieldEngine/`
- `AxData/src/AxData.Integration.Axpert/FillGrid/`
- `AxData/src/AxData.Integration.Axpert/Authorization/AxpertAuthorizationProvider.cs`
- `AxData/src/AxData.Integration.Axpert/StructDef/`
- `AxData/src/AxData.Api/Identity/HttpUserContext.cs`
- focused Set, field-engine, FillGrid, authorization, identity, serializer, and AxPut projection tests
- targeted symbol only: `ARM-11.4/AxTstructData/Service/AxPutDataObjectProjection.cs::IsActorAllowed`

Do not reopen the retired `ARM-11.4/AxDataObject` project or perform broad ARM reads.

### Target flow

1. Authenticate and resolve the live tenant/session.
2. Load `AXD:{tenantId}:{objectId}` once and fail closed on key/payload identity mismatch.
3. Require the exact mutable lifecycle state approved for Set (`Active`) and the persisted initiator relationship.
4. Load fresh create/edit, transaction, DC, field, and method permission; possession of ObjectId is never authority.
5. Acquire the short token-owned per-object lock. Do not repeat the DataObject load or lifecycle checks after the lock
   under the approved pre-lock coordination guarantee.
6. Create one Set operation that owns one current mutable legacy StructDef/Validate/Parser runtime and the neutral
   metadata needed by Application. Do not pre-load another projected definition in the handler.
7. Prepare and validate the request once, apply accepted fields in authoritative order, and rebuild the runtime only
   after a rejected field when a later independent field remains eligible.
8. Read one complete processed payload from the accepted runtime and construct a fresh candidate `AxDataState`.
9. Validate all FillGrid results into a plan, then apply that complete plan to the candidate once.
10. Perform one Redis write that atomically verifies the lock token. Ownership loss remains
    `409 / AXD-CONC-002`; no partial or intermediate state is written.

### Authorization and administrator removal

- Preserve authentication, live tenant/session resolution, tenant-scoped ObjectId loading, initiator/current-approver
  relationships, and fresh Axpert transaction/DC/field/method permission.
- Set is initiator-only and editable. Current approver is not allowed to Set; PEG actions require the current trusted
  assignment. Query may use an explicitly permitted viewer relationship. Save is initiator-only.
- Remove `IsAdministrator`, `AxActorRelation.Administrator`, the central policy branch that grants all operations,
  Development's literal `admin` promotion, and Query's administrator editability exception.
- Do not replace this with a client bypass flag or a literal-role shortcut. A future administrative operation requires
  its own trusted server permission and separately approved operation-specific policy.
- Update security tests for same tenant/different user, guessed ObjectId, cross tenant, initiator, current approver,
  viewer, and former `admin` role behavior.

### Request preparation and metadata

- Keep the behavior currently spread across `IsValid`, `FieldMetadataMatches`, `ValidateMetadata`, `ResolveRow`, and
  `OrderFields`, but replace repeated scans with one indexed preparation result.
- The preparation must validate request shape and duplicate addresses; current DC/field existence; opaque row identity;
  `n/e/d` action legality; delete-without-fields; static and permission read-only; included/excluded DCs and fields;
  source-key flags; field order; and the exact stored/current metadata required to decode existing positional state.
- `FieldMetadataMatches` is not a StructDef-version check. Remove the standalone method only after its source-key,
  static-read-only, field-set, and malformed-state protections exist in the new preparation/reconstruction boundary.
- Runtime address resolution remains defensive at the Integration boundary even when Application already prepared the
  address. It must return a result and never read a computed value after a failed legacy operation.

### One runtime and final-state reconstruction

- The production Set path must not separately load StructDef in authorization mapping, the handler, the field engine,
  and FillGrid. Design one Set-owned operation boundary that loads the current legacy definition once and exposes only
  neutral metadata/results to Application. Retain non-production ports only where their tests still need them.
- Hydrate the runtime once from the accepted stored baseline/current pairs, row order, persisted record IDs, and row
  actions. Do not serialize or share StructDef/Validate/Parser.
- The final candidate is new state, not the loaded state mutated in place. StructDef supplies processed current values,
  source IDs, dependencies, and current static metadata. AxData supplies ObjectId, tenant, TransId, initiator, lifecycle,
  workflow, files, stable row IDs, persisted record IDs, row states/order, and dirty baseline pairs.
- Match legacy frame/row positions to the prepared AxData row plan; never derive or replace stable `rowId` from legacy
  `rowN`. Preserve new rows as `New`, persisted edits/deletes as their approved states, and revert-to-baseline cleanup.
- Build the complete candidate successfully before mutating any state passed to the store.

### Rejected-field continuation

- Do not remove rebuild merely because AxTstructData does not use it. AxPut fails the whole persistence validation and
  never publishes a partially accepted runtime, whereas D3 requires AxData Set to retain valid independent fields.
- On rejection, skip transitive dependents of the rejected source. If a later independent field remains, recreate the
  operation runtime from the last accepted baseline and replay only accepted submissions before continuing.
- Do not rebuild after successful fields or when no eligible continuation remains. Record runtime creation/rebuild
  counts in focused tests.
- Rebuild may be removed only by a separately approved D3 contract change or evidence that the targeted legacy API can
  prove rejected roots cannot contaminate later independent results.

### FillGrid

- Reuse the Set operation's loaded definition/query metadata where the adapter can do so without sharing mutable
  runtime state across requests. Keep the Set/FillGrid legacy safety gates unless separate evidence approves narrowing.
- Convert all returned operations into a validated FillGrid plan before applying any operation. Validate target DC,
  mode, field mappings, values, and row-ID uniqueness once; remove the current validate-then-revalidate loop.
- Append/Replace behavior, empty-target applicability, changed-parent applicability, dependency response projection,
  and one final Redis write remain unchanged.

### Cross-project change — explicitly approved for implementation

- **Project/file:** `ARM-11.4/AxTstructData/Service/AxPutDataObjectProjection.cs`, limited to
  `IsActorAllowed`, plus its focused tests under `AxData/tests/AxData.ContractTests/`.
- **Why AxData cannot adapt around it:** ObjectId-aware AxPut owns Save authorization inside AxTstructData. Removing the
  AxData administrator relation alone would leave Save's literal `admin` role bypass active and make cross-flow access
  inconsistent.
- **Blast radius:** ObjectId-aware AxPut actor-to-DataObject relationship only. Do not change AxValidate, AxStructDef,
  AxParser, Common, general AxPut permission lookup, SQL persistence, or unrelated Save behavior.
- Record the ARM edit, reason, behavioral impact, and verification in `docs/ARM-11.4-CHANGES.md` in the implementation
  task.

### Expected implementation surface

Keep the implementation under about ten changed files where practical. If administrator removal plus Set runtime
reconstruction cannot meet that bound, implement them as two consecutive reviewed commits/tasks while preserving this
single target contract; do not leave an AxData/AxPut authorization mismatch between deployable commits.

Likely AxData changes are the Set handler and Axpert ports/contracts, the production field/FillGrid operation boundary,
authorization/identity policy, and focused unit/contract test files. Update `docs/CODEMAP.md` only for new or moved
types. Do not change public HTTP contracts unless review finds an unavoidable discrepancy and obtains separate approval.

### Verification

- Set authorization: initiator allowed only with live editable permission; current approver/viewer/unrelated/former
  `admin` denied; cross-tenant and guessed ObjectId fail closed; hidden/read-only DCs and fields remain protected.
- Lifecycle: only `Active` permits Set; all other statuses fail before runtime evaluation and persistence.
- One-load/one-runtime instrumentation: one DataObject load, one current StructDef load, one hydration, one final read,
  zero rebuilds normally, and one rebuild only for rejected-field continuation.
- Reconstruction: existing/new/edit/delete/reorder rows, stable row IDs, nullable persisted IDs, typed/null/source-key
  values, dirty originals, dependency changes, and revert-to-baseline round-trip through the positional serializer.
- Failure atomicity: malformed metadata, validation failure, FillGrid failure, cancellation, and lost lock ownership write
  no candidate state; successful Set performs exactly one token-owned write.
- FillGrid: Append/Replace, empty target, changed parent, invalid result, and unique server row IDs.
- AxPut: initiator remains allowed; former `admin`, former owner, unrelated user, and cross-tenant actor are denied while
  live transaction/DC/field Save permission remains mandatory.
- Run focused builds/tests without restore through `scripts/Invoke-SliceVerification.ps1`, then the full no-restore
  suites required by the cross-flow/security risk. Report focused and full results separately.

### Done when

- The production Set path follows the target flow with one DataObject load, one current StructDef runtime, one fresh
  candidate construction, and one owned Redis write.
- No generic administrator relationship or literal `admin` ObjectId bypass remains in AxData or ObjectId-aware AxPut.
- D3 continuation, permission filtering, lifecycle, metadata/source-key safety, FillGrid, and lock-ownership behavior
  pass focused and full verification.
- Relevant authorization, Set algorithm, StructDef/runtime, testing, `docs/CODEMAP.md`, `docs/PROGRESS.md`, follow-up,
  and ARM-change-ledger documentation matches the implemented result.
- **Postman:** not applicable if HTTP shapes, statuses, and AXD codes remain unchanged; otherwise update the current
  review pack in the same task.
