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

## P2-06 D30 spike planning done (2026-10-05)

- Done: Traced create/edit selection, supplied RecordId handling, StructDef main-table resolution, and the SQL-only existence probe.
- Finding: Builder transaction-table DDL is absent from this repository, so main-key uniqueness requires supplied Builder source or disposable-schema catalog proof.
- Tests: Added a plan only; no tests or ARM-11.4 files changed. All live AxPut/provider steps are user-run.
- Next: Run the disposable-schema constraint, create replay, late-commit, and edit-replay characterization before implementing D30 reconciliation.

## P2-06 site-number trace done (2026-10-05)

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
