# Progress

One block per finished slice, newest last, at most 5 lines each. Codex appends; you skim.
Format:

## P0-02 done (YYYY-MM-DD)

- Done: ...
- Decisions: ... (also copy to docs/DECISIONS.md)
- Tests: ...
- Next: ...

---

## P0-02 done (2026-09-24)

- Done: Created the .NET 8 solution skeleton, project dependency graph, build guard rails, health endpoint, and architecture test.
- Decisions: D1 is .NET 8.0 (recorded in docs/DECISIONS.md).
- Next: P0-03 Axpert discovery spike and P0-04 salvage assessment.

## P0-03 done (2026-09-25)

- Done: Recorded Axpert callable contracts, PEG table/routine shapes, tenant/user context, permission contracts, and a sanitized StructDef example.
- Decisions: D10 (self-committing AxPut), D11 (provider/queue/Redis), and D12 (permission source) are answered in docs/DECISIONS.md.
- Next: P0-04 salvage assessment; retain D15 as a P3-09 blocker.

## P0-04 done (2026-09-25)

- Done: Classified old AxDataObject and mirrored AxGet/AxPut/permission behavior in docs/SALVAGE.md.
- Decisions: No complete old component is reusable as-is; useful behavior requires ports and separation.
- Next: Future Phase 1-3 plans must name the applicable SALVAGE.md bucket before implementation.

## P1-01 done (2026-09-25)

- Done: Added the infrastructure-free domain aggregate, containers, rows, fields, lifecycle enums, PEG projection, and AXD error catalog.
- Decisions: Version starts at 1 and advances once per successful aggregate mutation; D7 transient and persisted row identity rules are enforced.
- Tests: Lifecycle transitions, version increments, n/e/d row rules, field dirty tracking, and error mappings pass (32 unit tests total).
- Next: P1-02 serialization contracts and schema versioning.

## P1-02 done (2026-09-25)

- Done: Added deterministic schema-v1 snapshot serialization, typed values, exact rehydration, and byte/grid limits.
- Decisions: D8 stores an immutable StructDef version reference only and fails closed when that revision cannot be resolved.
- Tests: V1 fixture compatibility, round trip, deterministic output, limits, unsupported values, and schema rejection pass (41 tests total).
- Next: P1-03 Redis state store; P1-14 must resolve exact historical StructDef revisions.

## P1-03 done (2026-09-25)

- Done: Added the five Axpert ports, state/system/identity ports, application-owned contracts, in-memory fakes, and a scripted StructDef fixture.
- Decisions: Fake dependency evaluation is explicit (`qty × rate = amount`); production dependency and FillGrid traversal remain adapter work.
- Tests: Dependency changes, failed-evaluation safety, fixture shape, tenant isolation, and fake-store version checks pass (45 tests total).
- Next: P1-04 Redis state store with atomic compare-and-set and TTL.

## P1-04 done (2026-09-25)

- Done: Added the tenant-scoped Redis state store with atomic create/CAS snapshot writes and configurable inactivity TTL.
- Decisions: Successful writes refresh TTL; loads do not, because authorization occurs after tenant-scoped loading.
- Tests: Stale and concurrent writes, TTL refresh, tenant isolation, and the full suite pass (49 executed tests).
- Next: P1-05 authentication and tenant context.

## P1-05 done (2026-09-25)

- Done: Added the application lock port and token-owned Redis lease with bounded expiry and atomic compare-delete release.
- Decisions: Lock keys use `AXD:LOCK:{tenantId}:{objectId}`; version CAS remains the correctness boundary after lease expiry.
- Tests: Ownership, reacquisition, expired-holder safety, stale-write protection, and the full suite pass (53 executed tests).
- Next: P1-06 authentication and tenant context.

## P1-06 done (2026-09-25)

- Done: Added `/api/v1` conventions, correlation and safe-exception middleware, trusted claims context, AXD HTTP responses, request metadata parsing, and allow-listed logging scopes.
- Decisions: D4 uses quoted `If-Match` first with a matching JSON fallback; claim identity and tenant never come from request bodies.
- Tests: Correlation, safe failures, 409 current version, claims, headers, idempotency, logging allow-list, and the full suite pass (74 executed tests).
- Next: P1-07 authorization policy and its security-first matrix tests.

## P1-07 done (2026-09-25)

- Done: Added the pure tenant, actor-relation, operation-permission, and editability authorization policy with no infrastructure dependencies.
- Decisions: Identity matching is ordinal; tenant mismatch always denies; assigned pending PEG tasks establish approver access; admin is not a permission bypass.
- Tests: Full relation-operation matrix, guessed/cross-tenant ObjectIds, unrelated/unassigned actors, combined owner-approver roles, and no bypass input pass (128 executed tests).
- Next: P1-08 Init orchestration using this policy before transaction/DC/field execution.

## P1-08 done (2026-09-25)

- Done: Added authorized new/existing Init orchestration, `POST /api/v1/axdata`, create-only state storage, and cryptographically random ULID ObjectIds.
- Decisions: Unknown TransId is AXD-REQ-001; Init checks explicit create permission before StructDef/data loading and rebuilds all server-owned identity metadata.
- Tests: New, existing, unauthorized, unknown TransId, API contract, and ObjectId tests pass (137 executed tests).
- Next: P1-09 permission-filtered Get/query.

## P1-09 done (2026-09-25)

- Done: Added tenant-scoped GET/query orchestration, complete or selective projection, normalized query permissions, and GET/POST API routes.
- Decisions: Hidden nodes are omitted before serialization; viewers/approvers are read-only; `PendingApproval` is emitted only for the assigned current approver.
- Tests: Selection, omission, read-only metadata, approver operations, denial, tenant isolation, and API contracts pass (151 executed tests).
- Next: P1-10 Set request/response contracts and row addressing.

## P1-10 done (2026-09-25)

- Done: Added Set request handling, StructDef row/field resolution, n/e/d actions, write authorization, owned locking, version checks, and one CAS write/version increment.
- Decisions: Unknown metadata/row identities are AXD-REQ; known non-writable nodes are AXD-AUTH; encrypted conversion and validation/dependency continuation remain P1B-02/P1-11 work.
- Tests: Row actions, permissions/security matrix, lock/stale/CAS conflicts, versioning, and API contracts pass (168 executed tests).
- Next: P1-11 Set validation, dependency changes, and FillGrid orchestration.

## P1-11 done (2026-09-28)

- Done: Set now evaluates submitted fields in explicit StructDef evaluation order, collects validation errors, continues independent fields, and returns dependency-only changes for GetDeps.
- Decisions: D3 applies valid independent fields; all-invalid requests leave state/version unchanged; an optional StructDef evaluation order is authoritative over display order.
- Tests: Unit Set continuation/order/error/GetDeps tests (125) and Set API contract tests (31) pass; solution build passes with restore disabled.
- Next: P1-12 FillGrid orchestration.

## P1-12 done (2026-09-28)

- Done: Set delegates FillGrid applicability to its Axpert port, applies validated Append/Replace grid rows, returns FillGrid changes for GetDeps, and retains one version increment/CAS write.
- Decisions: D16 keeps Axpert as traversal authority; failed or malformed FillGrid results return AXD-INFRA-001 without a Redis write.
- Tests: Added append, replace, empty-target, changed-parent, and failure tests; Set unit tests (129) and API contract tests (31) pass with restore disabled.
- Next: P1-13 snapshot table and version-guarded upsert.

## P1-13 done (2026-09-29)

- Done: Added Oracle/Postgres/SQL Server snapshot-table migrations and an ARMCommon-backed, parameterized version-guarded snapshot upsert.
- Decisions: D13 is implemented in Infrastructure only; incoming versions must be strictly greater, so duplicate/equal and stale deliveries are no-ops.
- Tests: Six SQL integration-contract tests and the full suite pass (182 executed); solution build passes with restore disabled.
- Next: P1-14 snapshot publish/consume and recovery on Redis miss.

## P1-14 done (2026-09-29)

- Done: Added atomic Redis state/stream publication, version-guarded consumer retry/DLQ/ack trimming, durability-gated TTL, SQL reads, and authorized recovery-on-miss.
- Decisions: D5 and D22 applied; permanent corrupt/exact-StructDef recovery failures are AXD-INFRA-002/500, while transient SQL unavailability remains AXD-INFRA-001/503.
- Tests: Build and 200 tests pass without restore; Redis Stream cases require Redis 6.2 and are gated on this host because its installed Redis is 3.0.504.
- Next: Run the stream integration cases on Redis 6.2+ during M1 real-infrastructure verification, then complete the M1 HTTP recovery walkthrough.

## P1-15 done (2026-09-29)

- Done: Wired fake Axpert with real Memurai/Postgres and proved Init, Set, durable snapshot, Redis eviction, and Get recovery over HTTP.
- Decisions: D23 uses PostgreSQL `timestamptz` with explicit UTC adapter casts; existing timezone-less values migrate as UTC.
- Tests: Release build and 207 tests pass; real M1 recovery returned version 2 and dependency amount 25.0 after eviction.
- Next: Begin Phase 1B real read adapters before Phase 2.

## P1B-01 done (2026-09-30)

- Done: Added result-safe StructDef and AxGet adapters with immutable projection, strict payload validation, and tenant/TransId/published-version cache isolation.
- Decisions: Published definition revision is supplied explicitly by the Axpert source; `Struct.VersionNo` is never used, and unavailable exact revisions fail closed.
- Tests: Build passes; 141 unit, 34 integration, and 42 contract tests pass without restore; end-to-end test project exits successfully.
- Next: Wire the authoritative Axpert published-revision source in deployment composition, then P1B-02 field engine adapter.

## P1B-02 done (2026-09-30)

- Done: Added the real Validate/Parser field adapter, checked runtime results, state diffing, read-only dependency updates, and serialized legacy access.
- Decisions: D17 fails closed before Parser values are consumed; D18 confirms long `SubmitValue` encrypts and short `SubmitValue` decrypts. Approved ARM changes are now tracked in `docs/ARM-11.4-CHANGES.md`.
- Tests: Serial build passes; 141 unit, 34 integration, and 49 contract tests pass without restore; end-to-end exits successfully. Default parallel solution build exits without diagnostics on the legacy graph.
- Next: Wire the tenant/version-aware field runtime factory in deployment composition, then P1B-03 FillGrid adapter.

## P1B-03 done (2026-09-30)

- Done: Added the result-safe FillGrid adapter, TFg normalization, applicability/parameter binding, declarative row mapping, and real-adapter M1 test-host boundary.
- Decisions: D24 provisionally maps AddRows 1 to empty-only Append, 2 to Replace, and 3 to Append pending future production verification.
- Tests: Serial build passes; 142 unit, 34 integration, and 53 contract tests pass without restore; end-to-end exits successfully.
- Next: Supply the deployment FillGrid runtime/query executor from the tenant/version-aware Axpert composition.

## P1C-01 done (2026-10-01)

- Done: Added current-row `tstructs` hashing, tenant-scoped read-only runtime, Redis-free cache sink, and real StructDef/AxGet/Validate/Parser adapters.
- Real smoke: `uat2/dtchk` loads the expected record and an in-memory `qty` Set produces the `amount` dependency change; no Save/AxPut runs.
- Decisions: D8 revised (props SHA-256 revision); D26 applied (concrete Axpert types stay in Integration.Axpert).
- Tests: 143 unit, 34 integration, and 56 contract tests pass without restore; end-to-end exits successfully.
- Next: P1C-02 real authorization, then Development-only review composition and the HTTP walkthrough.

## P1C-02 done (2026-10-05)

- Done: Added real create/view/edit authorization with user precedence, DC/field visibility, read-only metadata, fail-closed behavior, and full business-key forwarding.
- Real smoke: `uat2/dtchk` record `100000000008013` allows create/view/edit; cross-tenant and unrelated identities are denied, and the real-object dependency smoke passes.
- Decisions: D12/D19 applied (user-over-role precedence, fail-closed).
- Tests: serial build passes; 145 unit, 34 integration, and 63 contract tests pass without restore; end-to-end exits successfully. The read-only live characterization also passes.
- Next: P1C-03 Development-only review composition, then the P1C-04 HTTP walkthrough.

## Pre-D36 AxPut D30 spike planning done (2026-10-05)

- Done: Traced create/edit selection, supplied RecordId handling, StructDef main-table resolution, and the SQL-only existence probe.
- Finding: Builder transaction-table DDL is absent from this repository, so main-key uniqueness requires supplied Builder source or disposable-schema catalog proof.
- Tests: Added a plan only; no tests or ARM-11.4 files changed. All live AxPut/provider steps are user-run.
- Next: Retain this evidence for P2-03/P2-09; D36 supersedes the former external-saga reconciliation plan.

## Pre-D36 AxPut site-number trace done (2026-10-05)

- Done: Traced allocator callers and `psiteno` from tenant AxProps through the session hash into `GetRecordIdsFromDB`.
- Finding: Site number is tenant-configured/session-copied; fallback is zero, negative becomes one; RecordId is Int64 and supports 15-digit IDs.
- Tests: Documentation-only trace; no tests run and no ARM-11.4 files changed.

## P1C-03 done (2026-10-06)

- Done: Split fake M1 from Development-only RealDataReview composition with trusted configured identity and TransId-neutral real adapter wiring, including FillGrid.
- Decisions: D25/D27 applied; startup rejects non-Development, non-allowlisted hosts, shared Axpert/snapshot connections, and Axpert principals with tenant-schema write privileges.
- Tests: Serial build passes; 145 unit, 34 integration, and 70 contract tests pass without restore; end-to-end exits successfully.
- Next: P1C-04 HTTP walkthrough across representative authorized TransIds.

## P1C-04 done (2026-10-06)

- Done: The one-command Development walkthrough passes existing-record AxGet and new-state paths through Set/dependencies, Redis, local SQL, exact-key eviction, recovery Get, and D28 mismatch recovery.
- Real review: `uat2/dtchk` hash `ae4022d3a0240b199dc185ca7cd263ebf3c672ae4e430b06b951512328a090b4`; both cases reached version 2 with zero errors; safe report is `docs/reports/P1C-04-real-data-review.md`.
- Decisions: D28 accepted; record-ID keys normalize consistently for legacy permission/AxGet calls; launch profiles are disabled by the runner so local review configuration remains authoritative.
- Tests: Serial no-restore build passes; 149 unit, 34 integration, and 73 contract tests pass; end-to-end exits successfully; live HTTP walkthrough passes on Memurai 8.2.0 / Redis 8.2.10.

## M1.5 Postman review pack done (2026-10-06)

- Done: Added ordered Init/Get-recovery/Set/Query requests, variable chaining, assertions, and separated manual-identity review guidance.
- Tests: Collection and environment JSON validated locally; no product code changed.
- Next: Use the pack for milestone review and update it with every endpoint-affecting slice.
- Postman: updated.

## P1B-02 existing-record validation context fix done (2026-10-07)

- Done: Field evaluation now restores AxPut-equivalent transaction, row, and field record identity before full Axpert validation, preventing unchanged unique fields from matching their own record.
- Tests: 9 focused field-engine tests pass; full suite passes (149 unit, 34 integration, 75 contract); live `axdta:100000000008016` AxGet/Set validation and dependencies pass.
- Next: Re-run the M1.5 Set/durability/recovery sequence for the reviewed existing record.
- Postman: updated.

## P1D observability and row identity planning done (2026-10-08)

- Done: Added P1D-07 safe operational logging and P1D-08 stable DC/row addressing plans; D32 separates opaque RowId, positional legacy rowN, and persisted recordId.
- Decisions: Keep Axpert `dcN`; translate row order to legacy `rowN` and persistence identity to `axp_recid` only inside Integration.Axpert. No ARM-11.4 change.
- Next: Implement P1D-07 and P1D-08 through separate approved code plans before P2 Save.
- Postman: not applicable (planning only).

## P1C-05 done (2026-10-08)

- Done: Added batch FillGrid orchestration and a SELECT-only, parameterized, tenant-scoped executor with timeout and row cap; no ARM-11.4 changes.
- Real review: `axdtf` verifies AddRows 1/2/3, three applicable targets, repeated named-parameter normalization, PostgreSQL execution, and 60 HTTP FillGrid changes; D24 is confirmed.
- Tests: Serial build passes; 150 unit, 34 integration, and 107 contract tests pass; durability, Redis eviction recovery, and D28 pass. Next: P1D implementation. Postman: not applicable.

## P1D-08 done (2026-10-09)

- Done: Query, Set, AxGet, FillGrid, and Development fixtures now use opaque server-issued rowIds, authoritative dcIds, separate read-only recordIds, and positional legacy row translation only inside Integration.Axpert; no ARM-11.4 changes.
- Decisions: D32 implemented; new-row Set omits rowId and returns createdRows, while recordId/rowNo cannot authorize mutation; snapshot v2 preserves row order and deterministically migrates v1 identities.
- Tests: Serial build passes; 153 unit, 35 integration, and 110 contract tests pass; empty end-to-end project exits successfully.
- Next: P1D-07 operational logging, then remaining M1.6 readiness work. Postman: updated.

## P1D-07 done (2026-10-12)
- Done: Added safe structured API/stage/worker logs, Baseline/Diagnostic modes, durations/counts, and request-to-snapshot-worker correlation propagation; legacy HTML/value trace remains disabled.
- Decisions: Diagnostic adds operational metadata only; tokens, sessions, connections, SQL/parameter data, field values, bodies, and exception messages are excluded by the typed event contract and tests.
- Tests: Serial build passes; 154 unit, 35 integration, and 114 contract tests pass without restore; empty end-to-end project exits successfully.
- Next: Continue remaining M1.6 readiness work. Postman: not applicable.

## Repository bootstrap prepared (2026-10-12)

- Done: Configured the empty AxDataObject GitHub origin, documented commit ownership and reviewed ARM import workflow, and excluded discovered deployment credentials/configuration.
- Decisions: The current ARM tree is the user-owned initial baseline; future ARM imports remain separate reviewed commits and direct pulls into the parent tree are prohibited.
- Tests: Serial no-restore build passes; 154 unit, 35 integration, and 114 contract tests pass; empty end-to-end project exits successfully.
- Next: Approve and create the initial commit, then publish `main`. Postman: not applicable.

## P1D-01 done (2026-10-13)
- Done: Added pinned AxAuth JWT validation, live ARM Redis-session resolution, trusted tenant/user/role/SITENO context, and production real-adapter/state composition; no ARM-11.4 changes.
- Decisions: D29 implemented; Redis session liveness is authoritative, token/session/body disagreement fails authentication, and AXPERT_SESSIONID/SITENO remain outside HTTP identity and logs.
- Tests: Serial build passes; 154 unit, 35 integration, and 127 contract tests pass without restore; empty end-to-end project exits successfully.
- Next: P1D-02 deterministic build/dependency audit and P1D-03 CI/Redis verification. Postman: updated.

## P1D-05A/B planning prepared (2026-10-24)
- Done: Split field-engine design/measurement from implementation and recorded the AxTstructData parity trace, state/cache boundaries, capacity envelope, required reading, and handoff.
- Decisions: D33-D35 set operation-scoped runtime reuse, internal normalized source IDs with sparse originals, fail/re-Init definition handling, and evidence-gated cache/semaphore changes; D32 is unchanged.
- Tests: Documentation-only preparation; no product code or ARM-11.4 files changed and no tests run.
- Next: Run P1D-05A in a separate session, resolve measured limits/thresholds, then plan and run P1D-05B. Postman: not applicable.

## P1D-05A done (2026-10-24)
- Done: Recorded parity/rebuild rules, rejected unproven StructDef-cache reuse, measured v2 and sparse-v3 candidates through 350,000 fields, and produced the exact P1D-05B file/test plan.
- Decisions: Use repeated-name sparse JSON v3, 350,000 fields / 10,000 rows per grid / 32 MiB limits, one cache-bypassed runtime per operation, and retain the global gate pending isolation evidence.
- Tests: Characterizations and serial build pass; 154 unit, 35 integration, and 127 contract tests pass; envelope v2 was 56.71 MiB/888 ms versus sparse v3 25.13 MiB/277 ms.
- Next: Implement and verify P1D-05B against the recorded runtime, capacity, memory, and concurrency thresholds. Postman: not applicable.

## P1D-05B done (2026-10-26)
- Done: Added operation-scoped runtime/hydration/readback, reject-safe rebuild, normalized source IDs, and deterministic sparse snapshot v3; no ARM-11.4 changes.
- Decisions: Retain one cache-bypassed StructDef load and the global gate for the full operation; v1/v2 read compatibility remains and all new writes use v3.
- Tests: Serial build passes; 158 unit, 39 integration, and 136 contract tests pass; E2E exits successfully; 350,000 fields serialize to 25,716,825 bytes in 426.16 ms.
- Next: Continue remaining M1.6 readiness work. Postman: not applicable (no public HTTP or AXD contract change).

## ARM Dev import through 76080f7 done (2026-10-26)
- Done: Imported nine upstream Dev commits through 76080f7 as separate ARM commit 9092f4a; reconciled parser/session, AxPut/GenMap, queue, worker, and ADS changes.
- Decisions: Preserved fail-closed parser reads; fresh AxData operation runtimes restore trusted session globals and call PrepareExps without parser cache reuse.
- Tests: Nine affected ARM projects and serial AxData solution build pass; 158 unit, 39 integration, and 137 contract tests pass; E2E exits successfully.
- Next: Continue M1.6 readiness work. Postman: not applicable (no public AxData HTTP or AXD contract change).

## ARM Dev import through 5b42a46 done (2026-10-30)
- Done: Imported AxPut QA fixes and release16 version bump as ARM commit 396a05d; added sparse-grid and row-specific parser parity coverage.
- Decisions: Preserved fail-closed parser/tax handling and removed submitted/control values from new trace lines; no AxData production adapter change was required.
- Tests: AxStructDef, AxValidate, AxTstructData, and serial AxData solution builds pass; 158 unit, 39 integration, and 139 contract tests pass; E2E exits successfully.
- Next: Continue M1.6 readiness work. Postman: not applicable (legacy ARM behavior changed, not the AxData HTTP/AXD contract).

## ARM dependency workstream prepared (2026-10-30)
- Done: Added isolated ARM-DEP-00..04 task blocks for characterization, `a58`, Validate state/cache/results, targeted execution, and AxPut integration, plus a blocked two-step AxData handoff.
- Decisions: ARM behavior is proven first; existing AxData slice order is unchanged, final validation remains mandatory, and ARM field-value log redaction is out of scope.
- Tests: Documentation-only planning; no product code changed and no tests run.
- Next: Run ARM-DEP-00 in a fresh chat. Postman: not applicable.

## ARM-DEP-00 done (2026-10-30)
- Done: Added a package-free ARM characterization harness and report for Pascal `a58`, full Validate scans, cache/static state, parser failure, and AxPut parity; no production behavior changed.
- Decisions: Preserve downstream closure semantics as evidence only; structured/text parity, unknowns, cycles, label deduplication, and grid-row meaning remain ARM-DEP-01 stop conditions.
- Tests: 14 harness checks and serial AxTstructData build pass; exact counters are non-grid 3/3/2 and two-row grid 6/6/4 for refresh/validate/query.
- Next: Resolve the recorded ARM-DEP-01 ambiguities before implementing dependency projection. Postman: not applicable.

## ARM-DEP-01 done (2026-10-30)
- Done: AxStructDef now projects structured/text `a58` into typed direct/reverse metadata, compatibility closure/frame state, and diagnostics/fallback status; Validate/AxPut execution is unchanged.
- Decisions: Ordinary `e/s/f/ag` edges traverse in declaration order; `fg/dc/lbl/sf` remain distinct; unresolved targets and cycles require compatibility fallback; structured legacy `Dependents` stays Pascal-compatible.
- Tests: 17 package-free harness checks pass; serial no-restore AxStructDef, AxValidate, and AxTstructData builds pass with pre-existing NU1701 suppressed at the command line.
- Next: ARM-DEP-02 Validate state, cache, and result contracts. Postman: not applicable.

## ARM-DEP-02 done (2026-10-30)
- Done: Validate now owns invocation results and mutable state per runtime, exposes additive structured submission/dependency/final results, and replaces linked-fill query results per runtime/source/row/change wave without changing AxPut sequencing.
- Decisions: Existing string/`OperationResult` methods remain compatibility wrappers; valid transaction cache entries survive until their exact source/row query is re-executed, and existing ARM field-value logging is unchanged.
- Tests: 18 package-free dependency checks pass; serial no-restore AxStructDef, AxValidate, and AxTstructData builds pass with pre-existing NU1701 suppressed.
- Next: ARM-DEP-03 dependency-directed refresh and validation. Postman: not applicable.

## ARM-DEP-03 done (2026-10-30)
- Done: Added additive targeted dependency refresh/validation and full-current-value validation while preserving load, `ValidateData`, AxPut, and GenMap compatibility behavior.
- Decisions: Traverse resolved `e/s/f/ag` edges breadth-first; stop on unchanged value/source ID or failed validation; unsupported metadata/row mappings use warned compatibility fallback; ARM field-value logging is unchanged.
- Tests: 25 package-free dependency checks pass; serial no-restore AxStructDef, AxValidate, and AxTstructData builds pass with pre-existing NU1701 suppressed.
- Next: ARM-DEP-04 AxPut integration and ARM regression. Postman: not applicable.

## ARM-DEP-04 done (2026-10-30)
- Done: AxPut create/edit/GenMap now run targeted dependency work and mandatory cache-aware final validation before SQL; no AxData code or public contract changed.
- Decisions: Submission/GenMap value operations supply roots, deletes do not; exact runtime/source/row query results may be reused, and `AXV-DEP-W001` makes compatibility fallback trace-observable. ARM field-value logging is unchanged.
- Tests: 31 ARM checks and serial AxStructDef/AxValidate/AxTstructData builds pass; serial AxData build plus 158 unit, 39 integration, and 139 contract tests pass.
- Next: Use the recorded stable ARM contract for the separately approved AxData handoff. Postman: not applicable.

## AXD-DEP-01 done (2026-10-30)
- Done: AxData now projects authoritative ARM direct dependency kinds, declaration order, frame/grid/popup scope, resolution state, diagnostics, fallback status, and metadata-derived evaluation order; the Cexp/Deps heuristic is removed.
- Decisions: Application owns dependency DTOs; resolved ordinary edges define stable topological field order, while ARM diagnostics and compatibility fallback remain authoritative. No ARM-11.4 change.
- Tests: Serial no-restore build passes; 158 unit, 39 integration, and 143 contract tests pass; the empty E2E project exits successfully.
- Next: Plan AXD-DEP-02 consumption of targeted ARM validation. Postman: not applicable.

## AXD-DEP-02 done (2026-10-30)
- Done: Set now consumes ARM targeted refresh/validation with exact frame/row roots, safe structured errors, compatibility fallback, reject-safe rebuild, and transitive ordinary-edge blocking; no ARM-11.4 change.
- Decisions: One runtime/hydration/readback and the operation-wide gate remain; full Save validation stays separate. Controlled 10-field work fell from 10 to 0 full sweeps and 40 to 10 refreshes; SQL stayed 0 and field-stage latency was 56.538 ms versus 30.805 ms.
- Tests: AxData build passes without rebuilding locked ARM references; 159 unit, 39 integration, and 145 contract tests pass; E2E exits successfully; measured gate wait was 68.342 ms.
- Next: Continue remaining M1.6 readiness work. Postman: not applicable.

## ARM-DEP-04 create initialization fix done (2026-11-02)
- Done: New AxPut/GenMap runtimes now run one full initialization refresh; hydrated edits stay targeted, with a one-pass fallback when a required parent row is not initialized.
- Decision: Match Delphi parent-bound presence semantics; do not retry business validation failures or repeat final validation after a successful compatibility pass.
- Tests: 32 ARM checks, 159 unit, 39 integration, and 147 contract tests pass in Release without restore.
- Postman: not applicable; no HTTP request/response shape or AXD error code changed.

## ARM-DEP-04 parser/selection follow-up done (2026-11-02)
- Done: Decimal `abs` works, absent direct optional variables preserve the existing field without stale parser reads, and targeted select roots restore normalized source IDs before edit SQL.
- Decision: Match working Delphi optional-variable behavior narrowly; compound/other parser failures remain fail-closed.
- Tests: 32 ARM checks, 159 unit, 39 integration, and 150 contract tests pass in Release without restore.
- Postman: not applicable; no HTTP request/response shape or AXD error code changed.

## ARM-DEP-04 targeted grid-row binding fix done (2026-11-02)
- Done: Targeted roots and dependents now register their exact grid row before parser/query evaluation, preventing row 2 values from contaminating row 1 GenMap validation.
- Decision: Restore Delphi `RegRow` parity without adding a full-refresh fallback or widening the dependency traversal.
- Tests: focused two-row regression, 32 ARM checks, 159 unit, 39 integration, and 151 contract tests pass; serial build succeeds with existing warnings only.
- Postman: not applicable; no HTTP request/response shape or AXD error code changed.

## Validate parser guards restored (2026-11-02)
- Done: Restored all five fail-closed evaluation guards and the narrow missing-direct-variable compatibility branch after a local edit removed them.
- Tests: 8 focused regressions, 32 ARM checks, 151 contract, 159 unit, and 39 integration tests pass; serial build succeeds with existing warnings only.
- Postman: not applicable; no HTTP request/response shape or AXD error code changed.

## AxGet zero source-ID normalization fix done (2026-11-02)
- Done: Existing-record Init now normalizes legacy AxGet `0` source-ID companions to nullable AxData source IDs while rejecting negative, overflow, and nonnumeric values; no ARM-11.4 change.
- Tests: 12 focused AxGet contracts, 159 unit, and 39 integration tests pass; serial build succeeds. Full contracts retain two unrelated field-engine guard failures (`Failed_validation_expression...` first: expected Fail, actual Success).
- Postman: not applicable; no HTTP request/response shape or AXD error code changed.

## Temporary AxPut AxSave demonstration done (2026-11-03)
- Done: Added public `callIsFromAxSave`/`objectid`, temporary tenant/user-scoped Redis hydration/projection, Redis-before-SQL create/edit flow, and ObjectId responses; normal AxPut remains unchanged when disabled.
- Decision: This intentionally disposable ARM comparison is not the durable AxData Save design and must be removed before Phase 2 production integration.
- Tests: 11 focused characterization checks, 159 unit tests, and 39 integration tests pass; serial AxTstructData and AxData solution builds pass without restore with existing warnings.
- Postman: updated with disposable-schema happy and unknown-ObjectId negative requests.

## AxPut-owned Phase 2 Save planning accepted (2026-11-05)
- Done: Replaced the external AxData Save saga plan with nine session-ready AxPut-owned functional and durability slices.
- Decisions: D36 makes every AxPut call ObjectId-aware; missing IDs are generated, retention defaults true, `{}` may save, and known failure versions are never null.
- Next: Start P2-01 shared DataObject/request contracts; complete core flow through P2-05 before the final idempotency/outbox/worker slices.
- Postman: not applicable (planning only); existing user collection changes were preserved.

## Slice catalog aligned for AxPut-owned Phase 2 (2026-11-05)
- Done: Updated the slice catalog, readiness gate, PEG trusted-Save dependency, post-processing dispatcher dependency, and M2 completion point to P2-09.
- Status: Core functional review is P2-01..P2-05; durability is P2-06..P2-09; P1D-02/03/04/06 remain before full M1.6 closure.
- Next: Run P1D-02, then start P2-01 in separate sessions; P1D-04/P1D-06 may overlap P2-01 and P1D-03 must precede P2-05.
- Postman: not applicable (routing documentation only); existing user collection changes were preserved.

## P2-01 done (2026-11-05)
- Done: Added neutral shared v1/v2/v3 DataObject and AxPut contracts, secure ULIDs, byte-compatible AxData serialization, and ObjectId-aware AxPut binding; removed the disposable flag/Redis projection.
- Decisions: Retention null/omitted defaults true; supplied ObjectIds require `If-Match` or `expectedVersion`, matching when both are sent; shared-state hydration remains P2-02 and existing AxPut transaction/trace order is unchanged.
- Tests: Serial build, 15 ARM checks, 9 focused contracts, 14 snapshot checks, 159 unit, and 40 integration tests pass; full contracts are 158 passed/2 pre-existing parser-guard failures.
- Next: P2-02 ObjectId load/init, authorization, hydration, and payload overlay.
- Postman: updated; existing `ARMToken` edits preserved.

## P2-02 done (2026-11-06)
- Done: AxPut now loads tenant-scoped shared DataObjects, authorizes actor plus current transaction/DC/field access, checks version/StructDef hash, hydrates one runtime, and applies validated optional overlays before the unchanged SQL path.
- Decisions: ARM trace may add operation-relevant DataObject state/field values; AxData/worker logs remain value-free. P2-04 still owns atomic version claim and finalization.
- Tests: Serial build, 29 ARM checks, 20 focused contracts, 159 unit, 40 integration, and E2E pass; full contracts are 169 passed/2 pre-existing parser-guard failures.
- Next: P2-03 AxPut transaction parity and ObjectId-aware Save result. Postman: updated; existing `ARMToken` edits preserved.

## P2-03 done (2026-11-06)
- Done: Preserved AxPut transaction/response parity, added commit-gated stable row-to-record mappings and known ObjectId retention results, cached the parity trace, and retired the unreferenced legacy AxDataObject project.
- Decisions: P2-03 reports actual retained state without version advance; P2-04 owns claim/finalization/destruction. AxTstructData plus the fingerprinted parity contract are authoritative; Git history archives the discarded project.
- Tests: Serial builds, 35 ARM checks, 22 focused contracts, 159 unit, 40 integration, and E2E pass; full contracts are 171 passed/2 pre-existing parser-guard failures. The user-verified disposable PostgreSQL Postman happy and rollback review passes.
- Next: Continue P2-04 lifecycle finalization review. Postman: updated.

## P2-04 done (2026-11-06)
- Done: Added atomic Saving claims, rollback/ValidateOnly restoration, one-version Saved finalization, durable closure before active-key removal, and committed-success pending-finalization responses.
- Decisions: AxPut publishes lifecycle snapshots through the existing tenant stream; a persistent tenant-scoped closure marker prevents older SQL snapshots from recovering destroyed objects. No ARM SQL/validation/transaction-order change.
- Tests: Serial build, 26 focused contracts, 60 focused unit, 9 Redis lifecycle/race, 162 full unit, and 42 integration tests pass; E2E exits successfully. The user-verified disposable PostgreSQL Postman retained/destroyed review passes; full contracts retain two pre-existing parser-guard failures.
- Next: Complete P1D-03, then P2-05 core HTTP and real-provider review. Postman: updated.

## P1D-03 local verification accepted; CI pending (2026-11-06)
- Done: Added the Redis 6.2+ restart/redelivery harness and self-hosted workflow; the local `axdtf` existing/new-state chain passes FillGrid, persistence, eviction, recovery, and D28 on Memurai/PostgreSQL.
- Decision: The user approved a P2-05 prerequisite waiver because repository Settings are unavailable; CI remains unexecuted and is still required before M1.6 closure.
- Tests: Serial no-restore build, 9 Redis integration cases, Postman JSON, diff/secret checks, and the full local P1C-04 walkthrough pass.
- Next: P2-05 core HTTP and real-provider review; run `.github/workflows/axdata-e2e.yml` when an administrator supplies the runner. Postman: not applicable.

## AxData Init one-time on-load refresh done (2026-11-10)
- Done: Init now materializes row 1 for each new empty non-grid DC, hydrates one Axpert runtime, runs `RefreshFieldsOnLoad` exactly once before version-1 storage, reads refreshed non-save/default values back, and leaves empty grids rowless; Set/Get and AxPut production behavior are unchanged.
- Decisions: Current ARM missing/non-mandatory parser-variable compatibility is preserved; the five fail-closed parser guards are explicitly deferred to a later approved ARM change.
- Tests: serial API build, 7 focused Init unit, 6 focused loader/endpoint/adapter contracts, 32 ARM checks, 165 full unit, and 42 integration pass; full contracts are 179 passed with the 2 deferred parser-guard failures; empty E2E exits successfully.
- Next: implement the separately approved ARM parser-guard fix later. Postman: updated.

## AxData keyed/empty Init field parity done (2026-11-12)
- Done: keyed Init now loads an unrefreshed AxGet baseline; keyed and empty Init each run one AxValidate New/Existing pass, including saved form-load-generated fields without overwriting ordinary persisted values.
- Decisions: AxData owns exactly-once orchestration while AxValidate owns Accept/Calculate/default semantics; existing AxGet/AxPut/AxDelete defaults and deferred parser compatibility remain unchanged.
- Tests: serial build, 8 focused on-load, 7 Init unit, 16 loader/endpoint, 165 full unit, 42 integration, 32 ARM checks, and E2E pass; full contracts are 185 passed with 2 deferred parser-guard failures.
- Next: production review may exercise representative keyed and empty transactions; implement the separately approved parser guards later. Postman: not applicable.

## DataObject state redesign split approved (2026-11-13)
- Planned: AXD-STATE-01..06 split snapshot-worker deferral, version/StructDef bypass, positional compaction, owner removal, object-version removal, and final regression review into separate chats.
- Decisions: keep `AXD:{SCHEMANAME}:{OBJECTID}` with no migration; authorization and concurrency slices stop until enforceable server-side replacements are separately approved.
- Scope: documentation only; no AxData runtime or `ARM-11.4/` files changed. Tests: not run. Postman: not applicable.
- Next: run AXD-STATE-01 in a fresh chat, then continue in order.

## AXD-STATE-01 done (2026-11-13)
- Done: Set writes Redis directly; Redis misses return not found; snapshot transport/repository/worker requirements were removed from active composition, CI, walkthroughs, and configuration documentation.
- Decisions: Dormant snapshot source remains compiled but unwired; Redis is the only active DataObject-state store, and missing state is `404 / AXD-NOTFOUND-001` with no database recovery attempt.
- Tests: focused Set/Query/composition/endpoints pass; 165 unit and the full integration suite pass; contracts are 185 passed with the two documented parser-guard failures.
- Next: AXD-STATE-02. Postman: updated with the Redis-only happy path and exact-key deletion negative.

## AXD-STATE-02 done (2026-11-13)
- Done: Active state and AxPut no longer persist or enforce StructDef headers, versions, or hashes; every behavior path loads the current definition, while legacy v1/v2 snapshots remain readable.
- Decisions: D38 is implemented; `schemaVersion` remains only as an internal format discriminator pending AXD-STATE-03, `AXD-CONC-003` is removed, and clients cannot control binding bypass.
- Tests: no-restore solution build, 164 unit, 42 integration, E2E, and 184 contracts pass; the two documented parser-guard failures remain. Postman JSON and diff checks pass.
- Next: AXD-STATE-03 positional compaction. Postman: removed the obsolete StructDef-mismatch flow and documented the current-definition contract.

## AXD-STATE-03 done (2026-11-15)
- Done: DataObject state is current-only positional JSON: deterministic fields once per DC, aligned typed row values, sparse source IDs/originals, and only active workflow state; retained AxPut Save returns to `Active`.
- Decisions: no `schemaVersion`, backward reader, or migration; incompatible Redis state is cleared, read-only metadata is rebuilt from current StructDef, and files/`AxOpen` are omitted.
- Tests: no-restore build, 17 serializer capacity/round-trip, 63 focused contracts, 167 unit, 45 integration, and E2E pass; full contracts are 185 passed with the same 2 parser-guard failures.
- Next: AXD-STATE-04. Postman: retained Save → Query → Set and stale-version negative added; collection/template JSON validates.

## AXD-STATE-04 done (2026-11-15)
- Done: Removed `ownerUserName` from DataObject state, Redis/SQL serialization, and AxPut; trusted initiator identity now provides same-user access across live sessions.
- Decisions: D40 uses the D29-resolved username plus persisted `initiatorUserName`; ObjectId possession, a former owner value, or a different authenticated user grants no access.
- Tests: serial no-restore build, focused authorization/authentication/AxPut/serializer/Redis/SQL, 167 unit, 45 integration, and E2E pass; full contracts are 186 passed with the same 2 parser-guard failures.
- Next: AXD-STATE-05. Postman: added unrelated-session AxPut `403 / AXD-AUTH-002`; collection/template JSON validates.

## AXD-STATE-05 done (2026-11-16)
- Done: Removed object version, `If-Match`/`expectedVersion`, `AXD-CONC-001`, version CAS, and versioned snapshot/SQL fields; Set now commits only while its shared Redis lease token is still owned, while AxPut claims `Saving` before existing-object work.
- Decisions: D41 is implemented; overlap returns `409 / AXD-CONC-002`, sequential operations are accepted last-completed-wins, timed-out AxPut is not automatically retried, and `Idempotency-Key` remains future work.
- Tests: serial no-restore builds pass; 166 unit and 44 integration tests pass; contracts are 172 passed with the same 2 documented parser-guard failures; E2E exits successfully. Postman removes version chaining and includes manual Set/AxPut active-conflict assertions.
- Next: AXD-STATE-06 final regression review.

## AXD-STATE-03 corrective split approved (2026-11-16)
- Planned: AXD-STATE-03A corrects native values/array-pair sparse metadata, 03B establishes a clean Init baseline, and 03C removes active-state timestamps before final review.
- Decisions: D42-D44 fix the reviewed Redis shape; empty sparse properties are omitted and Redis TTL/PTTL is authoritative.
- Scope: documentation only; no runtime or ARM files changed. Tests: not run. Postman: not applicable.
- Next: run AXD-STATE-03A, 03B, and 03C in separate chats, with separate approval for each stated ARM cross-project section.

## AXD-STATE-03A done (2026-11-16)
- Done: Shared AxData/AxPut Redis state now uses native null/string/boolean/number values and omitted-when-empty sparse `[ordinal,value]` pair arrays through write, restore, claim, release, finalize, and close.
- Decisions: D42 implemented; current StructDef metadata controls business interpretation, strings are never type-guessed, and incompatible wrapper/object-map payloads fail closed with no migration.
- Tests: serial no-restore build, 13 focused contracts, 20 serializer, 7 Redis lifecycle, 166 unit, 48 integration, and E2E pass; full contracts are 173 passed with the same 2 documented parser-guard failures.
- Next: AXD-STATE-03B clean Init baseline. Postman: not applicable; no HTTP request/response or AXD error contract changed.

## AXD-STATE-03B done (2026-11-16)
- Done: Successful new/existing Init on-load values and source IDs become the clean baseline before Redis storage; collision retries reuse that accepted state, and retained Save finalization remains clean.
- Decisions: D43 implemented; later Set changes alone create sparse originals, reverting removes them, and current source IDs remain available without dirty baseline duplication.
- Tests: serial no-restore builds, focused Init/Set/serializer/AxPut contracts, 167 unit, 49 integration, and E2E pass; full contracts are 173 passed with the same 2 documented parser-guard failures.
- Next: AXD-STATE-03C. Postman: not applicable; no endpoint, request/response shape, or AXD error contract changed.

## AXD-STATE-03C done (2026-11-16)
- Done: Active Redis DataObjects and AxPut claim/release/final snapshots contain no creation, access, or expiry timestamps; Init still calculates and returns `expiresAt` without storing it.
- Decisions: D44 implemented; Redis TTL/PTTL is authoritative, and AxPut restores the captured positive TTL after release or retained finalization.
- Tests: focused paths and 43 ARM characterization checks pass; 167 unit and 50 integration pass; contracts are 173 passed with the same 2 documented parser-guard failures.
- Next: AXD-STATE-06 final regression review. Postman: not applicable.

## Set unpersisted-row correction (2026-11-16)
- Done: Set accepts server-issued active rows before Save, keeps edited unpersisted rows `New`, and removes them on delete; the Development lease is 60 seconds for real Axpert evaluation.
- Decisions: `rowId` is mutation identity; `persistedRecordId` remains nullable until Save and is not a Set eligibility check. No ARM-11.4 change.
- Tests: serial Application/API/unit builds, 32 focused Set tests, 170 full unit tests, and 13 Redis store/lock integration tests pass; legacy API dependencies retain pre-existing warnings. Next: AXD-STATE-06 final regression review. Postman: not applicable.

## AXD-STATE-03D done (2026-11-16)
- Done: Removed persisted `keyField`/`keyValue` from domain, Redis/shared snapshots, dormant SQL snapshots, and AxPut state; Init still accepts a business locator only for AxGet.
- Decisions: D45 implemented; canonical nullable `recordId` is the only persisted record locator, and ObjectId-aware AxPut derives the legacy `recordid` request pair server-side.
- Tests: no-restore production builds, 170 unit, 51 integration, 175 contracts (same 2 documented parser guards excluded), and all AxSave characterization checks pass. Next: AXD-STATE-06 final regression review. Postman: not applicable.

## Init runtime consolidation done (2026-11-16)
- Done: Real Init now uses one operation-scoped current StructDef/Validate/Parser runtime through AxGet baseline loading, on-load processing, and readback; new Init skips AxGet.
- Decisions: mutable legacy state remains request-local under the full-operation global gate; standalone Query/Set adapters and M1 fakes retain their existing ports. No ARM-11.4 change.
- Tests: serial no-restore Integration/API builds, 170 unit, and 176 contracts pass; the same 2 documented parser-guard failures remain. Next: AXD-STATE-06 final regression review. Postman: not applicable.

## AXD-STATE-03E done (2026-11-17)
- Done: Removed per-row source/original arrays; source-key values now use last-tilde `value~id`, while dirty existing rows use exact-length aligned `oldValues` with current-value copies at unchanged positions.
- Decisions: D46 implemented; `sourceKey` is stored once per field and validated against current StructDef, ID `0` is unresolved, ordinary tilde strings are literal, and clean/new/reverted rows omit `oldValues`.
- Tests: serial no-restore solution build, 22 serializer, 39 focused contracts, 172 unit, 50 integration, E2E, and AxSave characterization pass; full contracts are 180 passed with the same 2 documented parser-guard failures.
- Next: AXD-STATE-06 final regression review. Postman: not applicable; no HTTP or AXD error contract changed.

## AXD-STATE-03F done (2026-11-18)
- Done: Field metadata is stored once per DC as `[name,flags]` (`1` static read-only, `2` source key); Query/Get validates and projects from it without loading StructDef.
- Decisions: D47 implemented; live permissions remain mandatory, while Set and AxPut still load current StructDef and validate both stored flags.
- Tests: serial no-restore solution build, 25 serializer, 17 Query, 33 Set, 43 focused contracts, 172 unit, 53 integration, empty E2E, and 47 AxSave characterization checks pass; full contracts are 182 passed with the same 2 documented parser-guard failures.
- Next: AXD-STATE-06 final regression review. Postman: not applicable; no HTTP or AXD error contract changed.

## Init single-StructDef simplification done (2026-11-19)
- Done: Real Init now loads one mutable legacy StructDef and uses it directly for AxGet, DataObject construction, Validate/Parser, on-load processing, and readback; raw-props hashing, revision DTOs, the projected-definition cache, and preflight loads were removed.
- Decisions: D48 keeps the neutral Application definition only at the dependency boundary and retains the request-local legacy runtime gate; no ARM-11.4 change.
- Tests: serial no-restore solution build, 170 unit, 53 integration, and 180 contracts pass; the same 2 documented parser-guard failures remain excluded.
- Next: AXD-STATE-06 final regression review. Postman: not applicable; no HTTP request/response or AXD error contract changed.

## Init legacy-first final-state construction done (2026-11-19)
- Done: Real Init now completes baseline population and one on-load pass in its request-owned StructDef before constructing one clean DataObject; temporary state hydration/readback, JSON remodeling, and Init gate acquisition were removed.
- Decisions: D48 now follows AxTstructData runtime isolation for Init; Set/FillGrid gates remain, authorized AxGet shape and fail-closed unknown metadata checks are preserved, and no ARM-11.4 change was needed.
- Tests: serial no-restore API/Unit/Contract builds, 170 unit tests, and 181 contracts pass with the same 2 documented parser-guard failures excluded; focused Init/concurrency/composition verification passes. Next: AXD-STATE-06 final regression review. Postman: not applicable.

## Persistent active DataObject state (2026-11-19)
- Done: Removed active-state inactivity TTL, Init `expiresAt`, dead `ExpiresAtUtc` state metadata, AxPut PTTL capture/restore, and the Init diagnostic stage tracker. Active writes now remove any legacy expiry; concurrency lock leases remain bounded.
- Decisions: D49 supersedes D44's Redis-expiry rule because Redis has no active durable fallback. Documentation and Postman now state the persistent-key contract.
- Tests: serial no-restore solution build succeeds with zero errors; 7 focused Init unit, 3 Init HTTP contract, and 13 Redis store/lock/AxPut lifecycle tests pass. Postman assertions confirm Init omits `expiresAt`.

## Init payload and load-shape optimization (2026-11-19)
- Done: Real Init now projects only AxGet row/field presence, returns an initialization payload, and creates the final DataObject in the handler; production hydration/readback counters and dangling await semicolons were removed.
- Decisions: D48 parity order and fail-closed payload checks remain unchanged; the neutral port/M1 path retains its adapter state while production avoids temporary DataObject construction.
- Tests: serial no-restore solution build, 170 unit, 53 integration, and 181 contracts pass; the same 2 documented parser-guard contracts fail when included. Next: AXD-STATE-06 final regression. Postman: not applicable.

## Project-wide follow-up register (2026-11-23)
- Tracking: `docs/slices/axdata-follow-ups.md` now separates pending cross-cutting work, roadmap-owned deferrals, retained safeguards, and completed optimizations across all AxData areas.
- Scope: documentation only; no runtime, API, Postman, or ARM behavior changed. Tests: not run. Next: AXD-STATE-06 remains the next implementation slice.

## AXD-SET-01 done (2026-11-23)
- Done: Set now loads state once, owns one current StructDef/Validate/Parser runtime through metadata, reconstruction, and FillGrid, builds a fresh candidate, and performs one token-owned write.
- Decisions: Set/Save are initiator-only; generic administrator relation and AxPut's literal `admin` bypass are removed; D3 rejected-field rebuild/continuation remains.
- Tests: focused Set/runtime/FillGrid/auth/identity/AxPut pass; full 165 unit, 53 integration, 181 contracts (2 documented parser guards excluded), and AxSave characterization pass.
- Next: AXD-STATE-06 cross-flow regression and documentation closeout. Postman: not applicable; HTTP shapes, statuses, and AXD codes are unchanged.

## Init authorization wrapper cleanup (2026-11-24)
- Done: Init now uses the explicit create-permission result directly instead of wrapping it in the generic actor-relation authorization policy; denial order, logging, and errors are unchanged.
- Tests: serial no-restore Application build and 7 focused Init unit tests pass. Next: AXD-STATE-06 cross-flow regression. Postman: not applicable.

## Explicit Init/Get/Set POST endpoints (2026-11-24)
- Done: Replaced the mixed AxData routes with `POST /init`, `POST /get`, and `POST /set`; Get and Set now receive ObjectId in the JSON body, and Get retains full/selective projection.
- Decisions: Former route aliases are removed; health remains a GET probe; AxPut Save is unchanged. Body-bound ObjectId is shared with operational middleware without parsing or logging request bodies.
- Tests: serial no-restore solution build succeeds; 24 focused endpoint/middleware contracts and the real-data walkthrough syntax check pass; Postman JSON audit confirms 19 AxData requests on the three POST routes with body ObjectIds and 13 unchanged-route AxPut requests.
