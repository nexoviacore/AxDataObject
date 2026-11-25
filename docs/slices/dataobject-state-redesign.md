# DataObject state redesign

This workstream splits the approved DataObject-state direction into reviewable changes. Run one slice per chat and in the order below. Do not combine slices unless the user explicitly approves a revised plan.

## Fixed constraints

- Keep the active Redis key as `AXD:{SCHEMANAME}:{OBJECTID}`. In AxData, the trusted tenant ID resolves to the schema name. Do not add TransId, a locator key, dual reads, or lazy migration; the feature is still in development.
- Redis Cluster treats text inside `{...}` as the hash tag. `{SCHEMANAME}` therefore keeps all keys for one schema in one slot. This workstream does not change that distribution choice.
- Minimize only persisted DataObject state. StructDef remains the source for reconstructable metadata such as data type and read-only status.
- Preserve `recordId`, stable `rowId`, row order/state, and any source-ID/original data proven necessary for Set and Save parity.
- Public clients can never supply a bypass flag. Any temporary version-validation bypass is trusted server configuration/code only and must produce safe identifier-only diagnostics.
- No compatibility reader or migration is required for the development-only state formats. Tests and local development data may be recreated at the format cutover.
- Any implementation that changes `ARM-11.4/` requires a separate **Cross-project change** plan and explicit approval. Prefer an AxData adapter. Likely high-blast-radius candidates include AxTstructData's DataObject contract/store/projection/Save composition; do not edit them from this planning slice.

## AXD-STATE-01 — Defer the snapshot worker

**Effort:** Standard  
**Needs:** D37  
**After:** current in-flight slice, if any

**Read:**

- `docs/architecture/10-state-persistence.md`
- `docs/architecture/11-reliability.md`
- `docs/technical/08-redis.md`
- `docs/slices/phase-1-foundation.md` P1-13 through P1-15
- `docs/slices/phase-2-durable-save.md` P2-09

**Scope:**

- Remove the snapshot publisher/consumer/recovery worker from active composition, deployment, health checks, walkthroughs, and active requirements.
- Keep Redis as the only active DataObject-state store for the current development scope.
- Mark durable snapshot recovery as future use. Decide in this slice whether dormant source stays compiled but unwired or is removed; do not leave an apparently active half-path.
- Remove worker-specific configuration only after proving nothing else consumes it.
- Do not remove AxPut idempotency/outbox/reconciliation merely because those also use workers; they are separate Save-durability concerns.

**Tests / done when:**

- Focused composition tests prove the snapshot worker is not registered or required.
- Redis Store/Query/Set/Save flows still pass, and Redis miss has one documented result with no hidden database recovery attempt.
- Deployment/configuration documentation contains no active snapshot-worker requirement.
- Postman: not applicable unless an HTTP status or response changes.

## AXD-STATE-02 — Disable schema-version validation and StructDef version binding

**Effort:** Deep  
**Needs:** D38  
**After:** AXD-STATE-01

**Read:**

- `docs/DECISIONS.md` D8, D28, D33-D35
- `docs/technical/18-serialization.md`
- `docs/technical/19-structdef-cache.md`
- `docs/technical/21-testing.md`
- current Init/Get/Set/Query/Save state-contract and StructDef-provider tests

**Scope:**

- Disable schema-version validation/control and stop writing StructDef version/hash/header data into the active DataObject state. AXD-STATE-03 decides whether a purely internal serialization-format discriminator is still needed; it must not restore schema-version validation.
- Bypass schema/StructDef version validation through trusted server code or configuration. A request payload must never control the bypass.
- Retain the removed validation/header structure as a concise future-use comment or design note, not as dead duplicated implementation. Future reference shape:

  ```json
  "structDef": {
    "transId": "...",
    "version": "..."
  }
  ```

- Add safe diagnostics that identify the operation, tenant/schema, TransId, ObjectId, and trace ID without logging field values, bodies, session IDs, secrets, or tokens.
- Update or supersede old exact-revision/hash-mismatch requirements consistently across Init/Get/Set/Query/Save. Do not leave one flow enforcing a version that no longer exists in state.
- StructDef retrieval is still required to validate operations and reconstruct metadata; this slice removes version binding, not StructDef itself.

**Tests / done when:**

- State serialization contains no StructDef header/version/hash and no schema-version enforcement. Any internal format discriminator retained for safe parsing is documented as such and is not validated as a business/StructDef schema version.
- All flows use the current StructDef without a stored-version rejection, while normal validation and authorization remain intact.
- Tests prove clients cannot activate a bypass and diagnostics contain only safe identifiers.
- Existing non-version behavior has focused regression coverage.
- Postman: update only if the public contract/error behavior changes.

## AXD-STATE-03 — Compact positional DataObject state

**Effort:** Deep  
**Needs:** AXD-STATE-02, D39  
**After:** AXD-STATE-02

**Read:**

- `docs/DECISIONS.md` D32-D35
- `docs/technical/04-domain-model.md`
- `docs/technical/08-redis.md`
- `docs/technical/18-serialization.md`
- `docs/reports/P1D-05-field-engine-handoff.md`
- current Store/Restore, Query, Set, FillGrid, and Save contracts

**Scope:**

- Measure the current payload and implement the approved positional direction: store field names once per data container and align each row's `values` array by ordinal.
- Keep required object identity/routing data, TransId, lifecycle needed by active flows, each `dcId`/`isGrid`, stable `rowId`, nullable `recordId`, row state/order, typed field values, and error/state information that cannot be reconstructed safely.
- Do not persist read-only flags, data types, captions, or other metadata that can be obtained from the current StructDef.
- Keep source IDs and dirty-field originals sparse. The ordinal mapping must be deterministic:
  - `fields[n]` identifies `values[n]` in every row.
  - source ID entry `n` belongs to the same field ordinal.
  - an original entry exists only for a dirty field and carries the baseline value and baseline source ID needed to distinguish the user's edit from the loaded value.
- Prove whether source IDs/originals can be reloaded authoritatively before removing either. Do not replace them with an extra database/StructDef lookup unless Set and Save parity, latency, and failure behavior are demonstrated.
- Include active workflow data only when PEG is active. Treat file and `AxOpen` data as future candidates until their owning slices prove a current requirement.
- Define malformed-array handling: duplicate fields, missing/extra values, ordinal drift, and invalid sparse indices must fail deterministically rather than silently remap data.
- Define retained-Save lifecycle behavior so a successful Save does not permanently prevent later Set. `Saving` may block mutation; a retained object must return to an explicitly mutable state after completion.
- Record the lifecycle matrix explicitly: Active permits Set; Saving rejects Set but Query returns the complete authorized visible snapshot as non-editable (not only StructDef read-only fields); a retained successful Save permits a later Set according to the chosen post-Save status.

**Tests / done when:**

- Round-trip tests cover single-row and grid DCs, nulls, typed values, row reorder/delete/add, FillGrid replacement, nullable/persisted record IDs, source IDs, sparse originals, and retained Save followed by Set.
- Corrupt ordinal mappings fail closed.
- Store/Restore reconstructs the current domain/API shape without exposing positional storage details to clients.
- Report before/after serialized bytes and serialization/deserialization time for representative and capacity payloads.
- No backward reader or lazy migration is added.
- Postman: not applicable unless the public API shape changes.

## AXD-STATE-03A — Correct native values and sparse array pairs

**Effort:** Deep  
**Needs:** AXD-STATE-03, D42  
**After:** AXD-STATE-05; before AXD-STATE-06

**Read:**

- `docs/DECISIONS.md` D34, D39, D42
- `docs/technical/04-domain-model.md`
- `docs/technical/18-serialization.md`
- `docs/reports/AXD-STATE-03-positional-state.md`
- current shared snapshot reader/writer, AxData serializer, AxPut projection/store, and their focused tests

**Scope:**

- Correct the Redis wire format so ordinary row values are JSON-native scalars aligned with `fields`: `null`, string, boolean, or number. Do not write `{ "kind": ..., "value": ... }` wrappers.
- Obtain field type/read-only metadata from the current StructDef during reconstruction. Do not infer a business type from a string's appearance, and do not reintroduce per-value type tags without a new approved decision.
- Encode sparse metadata as arrays of exact two-item pairs, keyed by zero-based field ordinal:

  ```json
  "sourceIds": [[1, 42]],
  "originals": [[1, 100]],
  "originalSourceIds": [[1, 41]]
  ```

  In this example ordinal `1` consistently identifies `fields[1]`: its current value is `values[1]`, current source ID is `42`, original value is `100`, and original source ID is `41`.

- Omit each sparse property when it has no entries. Require unique, in-range integer ordinals; positive Int64 source IDs; valid scalar original values; and an `originalSourceIds` entry only for a dirty ordinal represented by `originals`.
- Preserve the shared AxData/AxPut single Redis shape through Init, Set, AxPut claim, release, retained finalize, and non-retained close. Default JSON serialization must not silently restore object maps or typed wrappers.
- Keep the current-only/no-migration rule. Clear incompatible development Redis objects at cutover.

**Cross-project change:**

- Expected ARM files: `ARM-11.4/AxTstructData/Service/AxPutDataObjectStore.cs` and, only if the shared internal representation requires it, `ARM-11.4/AxTstructData/Service/AxPutDataObjectProjection.cs` plus focused AxPut tests.
- An AxData-only adapter cannot guarantee the format because AxTstructData writes the same Redis payload during claim, rollback, and finalize.
- Blast radius is the ObjectId-aware AxPut DataObject persistence path; do not alter AxValidate, AxStructDef, AxParser, Common, or unrelated AxPut behavior.
- The implementation chat must list exact ARM files, obtain separate approval for this section, and update `docs/ARM-11.4-CHANGES.md`.

**Tests / done when:**

- A golden fixture exactly matches the approved native-value/array-pair JSON, including omission of empty sparse properties.
- Round trips cover null/string/boolean/number values, StructDef-driven reconstruction, decimals, dates, source IDs, dirty originals, original source IDs, and malformed/duplicate/out-of-range pairs.
- AxPut claim/release/finalize preserves the exact wire shape.
- Before/after payload bytes and Store/Restore timings are remeasured against the same representative and capacity fixtures.
- Postman: not applicable unless a public contract changes.

## AXD-STATE-03B — Establish a clean Init baseline

**Effort:** Standard  
**Needs:** AXD-STATE-03A, D43  
**After:** AXD-STATE-03A; before AXD-STATE-06

**Read:**

- `docs/DECISIONS.md` D34, D43
- current Init/on-load orchestration, `AxFieldValue` dirty-baseline behavior, serializer, Set hydration, and Save finalization tests

**Scope:**

- After successful Init on-load validation/default/calculation processing, accept the resulting current value/source-ID pair as the stored baseline before the first Redis write.
- Do not store null originals merely because a new/default field changed from its temporary pre-Init placeholder.
- A failed on-load operation still stores nothing. Object-ID collision retry must reuse the already accepted initialized state without repeating on-load processing.
- Later accepted user edits create sparse `originals` and, when applicable, `originalSourceIds`; returning to the baseline removes those entries. A retained successful Save establishes a clean new baseline.
- Do not remove source IDs or originals that are genuinely required for a dirty field between Set and Save.

**Tests / done when:**

- New and existing-record Init with defaults/calculations serialize no `originals` or `originalSourceIds` immediately after success.
- A later Set produces only the changed ordinals, revert-to-baseline removes them, and retained Save leaves a clean state that permits another Set.
- Failed Init and collision-retry behavior remain unchanged.
- No ARM-11.4 change is expected; if evidence requires one, stop and provide a separate Cross-project change plan.
- Postman: not applicable unless public behavior changes.

## AXD-STATE-03C — Remove active-state timestamps

**Effort:** Standard  
**Needs:** AXD-STATE-03B, D44  
**After:** AXD-STATE-03B; before AXD-STATE-06

**Read:**

- `docs/DECISIONS.md` D44
- `docs/technical/08-redis.md`
- current Init response, Redis persistence/write/claim/release/finalize code, shared snapshot contracts, and focused tests

**Scope:**

- Remove `createdAtUtc`, `lastAccessedAtUtc`, and `expiresAtUtc` from the active Redis DataObject payload and its required read contract.
- Active Redis keys persist without an inactivity TTL while no durable recovery store is active. Init, Set, AxPut claim/release, and retained finalization must preserve that persistence; lock leases remain bounded.
- Remove the Init API `expiresAt` response and inactivity-TTL configuration so the HTTP contract matches actual persistence.
- Do not add access timestamps elsewhere in active state. Future audit/history requirements belong to their owning log or durable-audit design.
- Keep `initiatorUserName`, lifecycle `status`, identity/routing fields, required record IDs, data containers, and active workflow state.

**Cross-project change:**

- Expected ARM files: `ARM-11.4/AxTstructData/Service/AxPutDataObjectProjection.cs` and `ARM-11.4/AxTstructData/Service/AxPutDataObjectStore.cs` only if shared contract/TTL handling requires it, plus focused tests.
- An AxData-only adapter cannot remove timestamps end to end because AxTstructData constructs and rewrites claim/finalized snapshots and owns TTL preservation during Save.
- Blast radius is the ObjectId-aware AxPut DataObject lifecycle path; no shared Axpert libraries should change.
- The implementation chat must list exact ARM files, obtain separate approval for this section, and update `docs/ARM-11.4-CHANGES.md`.

**Tests / done when:**

- Golden and live Redis payloads contain none of the three timestamp properties after Init, Set, AxPut claim/release, or retained finalize.
- Init omits expiry, active Redis keys have no TTL through Set and AxPut lifecycle operations, and no payload timestamp is used as expiry authority.
- Postman: not applicable if the Init response remains unchanged.

## AXD-STATE-03D — Remove persisted business-key locators

**Effort:** Medium  
**Needs:** AXD-STATE-03, D45  
**After:** AXD-STATE-03C

**Read:**

- `docs/DECISIONS.md` D39, D45
- current Init/AxGet, Query/Set authorization, shared snapshot, dormant SQL snapshot, and ObjectId-aware AxPut contracts
- cached `docs/contracts/axput-save-parity.md` trace when its fingerprints still match

**Scope:**

- Keep `keyField`/`keyValue` only as Init/AxGet request-scoped locators; never copy them into DataObject state.
- Existing-record Init stores AxGet's resolved positive `recordId`; new state keeps `recordId` null until Save.
- Query, Set, and recovery authorize existing state by `recordId`; null uses the create permission path.
- Remove business-key locators from the domain model, current-only Redis/shared snapshot shape, dormant SQL snapshot contract, and provider bootstrap migrations.
- ObjectId-aware AxPut derives `recordid` plus invariant `recordId` text for legacy edit permission/loading. Do not change legacy non-ObjectId request behavior or Save ordering.
- Reject locator-bearing current payloads; do not add a compatibility reader, lazy migration, or client bypass.

**Tests / done when:**

- Init proves a business locator reaches AxGet while stored state retains only the resolved `recordId`.
- Query/Set authorization receives record-ID-only addressing; new state still follows create permission.
- Shared serialization omits locators and rejects old locator-bearing payloads; dormant SQL parameters and migrations omit locator columns.
- AxPut characterization proves server-derived record-ID edit addressing and unchanged Save order.
- Postman: not applicable; the Init request shape is unchanged.

## AXD-STATE-03E — Pack source IDs and align old values

**Effort:** Deep  
**Needs:** AXD-STATE-03A, D46  
**After:** AXD-STATE-03D

**Scope:**

- Replace per-row `sourceIds`, `originals`, and `originalSourceIds` arrays with source-key-only `value~sourceId` strings and optional exact-length `oldValues`.
- Store `{name,sourceKey}` once per DC, validate it against current StructDef, and split only source-key fields at the last tilde. Preserve ordinary tilde-bearing strings literally.
- Treat source ID `0` as unresolved to match AxTstructData; reject negative, non-numeric, or overflowing suffixes.
- When an existing row is dirty, copy current encoded values into unchanged `oldValues` positions and baselines into dirty positions. Omit `oldValues` for clean/new rows and after revert.
- Reject legacy sparse properties and add no compatibility reader or migration.

**Tests / done when:**

- Round trips cover typed ordinary values, source-key null/zero/positive IDs, display values containing tildes, value-only/source-only changes, real null baselines, unchanged-index copies, new rows, and revert.
- AxPut hydrates separate legacy field/`_sourceid` entries, selects changed fields from aligned arrays, validates current StructDef metadata, and writes retained clean snapshots in the same shape.
- Focused serializer, StructDef/AxGet, Redis lifecycle, and AxPut projection tests pass; full suites and AxSave characterization run because the shared Save contract changes.
- Postman: not applicable; no HTTP request/response or AXD error contract changes.

## AXD-STATE-03F — Persist compact field flags and remove Query StructDef load

**Effort:** Standard  
**Needs:** AXD-STATE-03E, D47  
**After:** AXD-STATE-03E

**Scope:**

- Replace `{name,sourceKey}` descriptors with exact two-item `[name,flags]` arrays: `0` normal, `1` static read-only, `2` source key, `3` both.
- Populate ordered metadata during Init/AxGet, including empty DCs, and preserve it through serialization.
- Make Query/Get validate selections, preserve field order, normalize fresh permissions, and calculate final read-only from DataObject metadata without loading StructDef.
- Keep Set and ObjectId-aware AxPut on the current StructDef and fail closed when stored source-key or static-read-only flags drift.
- Reject legacy object descriptors, malformed arrays, duplicate names, and unknown flag bits. Add no compatibility reader or migration.

**Tests / done when:**

- Round trips preserve flags for populated and empty DCs; malformed and legacy descriptors fail closed.
- Query/Get succeeds without StructDef, passes the DataObject field map to authorization, preserves stored order, omits hidden fields, and combines transaction/DC/static-field/field-permission read-only rules.
- Set and AxPut detect current StructDef metadata mismatch, and AxPut emits retained snapshots in the compact shape.
- Full no-restore suites, E2E, and AxSave characterization run because the shared Query/Save contract changes.
- Postman: not applicable; no HTTP request/response or AXD error contract changes.

## AXD-STATE-04 — Remove ownerUserName without making ObjectId a credential

**Effort:** Deep  
**Needs:** D40 replacement access rule  
**After:** AXD-STATE-03

**Stop condition:** The approved direction removes `ownerUserName` and owner-based access, but an ObjectId is still never a credential. Before editing code, this slice must propose and obtain approval for the trusted server-side rule that binds an authenticated session/user to an object. If no enforceable replacement is approved, stop; do not remove owner checks.

**Read:**

- `AGENTS.md` ObjectId authorization order
- `docs/DECISIONS.md` D19, D29, D40
- `docs/technical/21-testing.md` section 21.4
- current authorization policy, endpoint, and Redis-state tests

**Scope after the stop condition is resolved:**

- Remove `ownerUserName` from persisted state and remove owner-as-authority logic consistently.
- Implement only the approved authenticated binding/access rule; never authorize mere ObjectId possession.
- Keep tenant/schema isolation and transaction/DC/field permissions.
- Defer PEG-added viewers/editors to a future explicit ACL design. Do not pre-create ambiguous user lists in this slice.

**Tests / done when:**

- Security tests cover same tenant/different user, cross-tenant, guessed/stolen ObjectId, expired/mismatched session, admin/current approver only if the replacement design retains them, and hidden DC/field behavior.
- Serialized state contains no `ownerUserName`.
- Postman: update authorization positive/negative cases if endpoint behavior changes.

## AXD-STATE-05 — Remove object version with a server-side concurrency rule

**Effort:** Deep  
**Needs:** D41 replacement concurrency rule  
**After:** AXD-STATE-04

**Stop condition:** Browser-session multi-tab handling does not serialize duplicate or concurrent HTTP requests at the server. Before editing code, this slice must propose and obtain approval for a server-enforced replacement for version compare-and-set, stale-write detection, and retry behavior. It must also explicitly reconcile the replacement with the current AGENTS.md mutation rule. If no replacement is approved, stop; do not remove versions.

**Read:**

- `AGENTS.md` mutation/version rule
- `docs/DECISIONS.md` D11, D36, D41
- `docs/technical/08-redis.md`
- `docs/technical/11-save-orchestration.md`
- `docs/technical/21-testing.md`
- current Set, Save, Redis-store, lock, ETag/If-Match, and retry tests

**Scope after the stop condition is resolved:**

- Remove object version from persisted state, public contracts, logs, and control flow.
- Remove `If-Match`/`expectedVersion`, stale-version errors, increments, and compare-and-set only to the extent covered by the approved replacement.
- Preserve the short lock or replace it only with an equally explicit server-side rule. Define duplicate Set/Save, retry-after-timeout, lock expiry, and overlapping request outcomes.
- The current assumption is one editing user per DataObject; future multi-user view-only access does not authorize concurrent editing.

**Tests / done when:**

- Concurrency tests prove no silent lost update for overlapping Set/Save requests and deterministic behavior for duplicates/retries and lock expiry.
- State/API/logs contain no object-version field or version requirement.
- Postman: remove version headers/variables/assertions and add the approved concurrency response assertions.

## AXD-STATE-06 — Cross-flow regression and documentation closeout

**Effort:** Standard  
**Needs:** AXD-STATE-01 through AXD-STATE-05 and AXD-STATE-03A through AXD-STATE-03C  
**After:** all preceding slices

**Read:** all diffs and completion notes from this workstream plus the active Init/Get/Set/Query/FillGrid/Save and PEG contracts.

**Scope / done when:**

- Verify every generation, read, update, delete, TTL, lock, and monitoring path still uses `AXD:{SCHEMANAME}:{OBJECTID}`.
- Verify no TransId locator, compatibility reader, lazy migration, schema/StructDef version enforcement, object version, `ownerUserName`, active-state timestamp, typed `{kind,value}` wrapper, object-keyed sparse map, or active snapshot-worker dependency remains accidentally.
- Verify empty sparse arrays are omitted, Init begins clean, and all later sparse entries use the approved array-pair ordinal mapping.
- Run the focused state, authorization, concurrency, Save, and Redis suites, then the no-restore solution build/tests required by the repository.
- Reconcile architecture/technical docs, Postman, configuration, deployment, `docs/CODEMAP.md`, and `docs/PROGRESS.md` with implemented behavior. Preserve historical completion records; mark superseded decisions rather than rewriting history.
- Review any approved `ARM-11.4/` changes and ensure `docs/ARM-11.4-CHANGES.md` records files, reason, behavioral impact, and verification.

