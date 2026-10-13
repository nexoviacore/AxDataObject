# Phase 1C: Real-data review

Goal: demonstrate Init, Get, Set, real permissions, StructDef, AxGet, Validate/Parser,
dependencies, FillGrid, snapshot durability, and recovery on sanitized staging data.
Save/AxPut and PEG remain out of scope.

## P1C-00 Compatibility gate

Read: contracts/axpert-notes.md, SALVAGE.md (Reuse with changes: Init/AxGet, Set,
StructDef/Validate/Parser, authorization, Redis state).

- Production authentication is deferred; authorization remains mandatory.
- Review identity is Development-only and trusted configuration, never request data.
- No new ARM-11.4 changes beyond those approved in docs/ARM-11.4-CHANGES.md. Anything else: stop and propose a separately approved change.
- Missing configuration or infrastructure fails explicitly.
- Review data must be sanitized staging data and the database user must be read-only.

Findings:

- `StructDef.LoadStructure`, AxGet load/permission methods, Validate, and Parser are public.
- `Utils`, StructDef, Validate, and Parser are mutable and must be scoped per operation/tenant.
- `tstructs.props`, concatenated in `blobno` order, is the authoritative definition content.
- SHA-256 of the compressed `props` bytes is the revision identity. `updatedon` is informational.
- `Struct.VersionNo` reads XML `vno`; the .NET builder writes `11.4`. It is informational,
  not a definition revision, and no reviewed .NET save path increments it.
- `runtimetstruct` and `runtimemod` are parsed as true only for `Y`; `T` is false and does
  not select another loader path.
- Review loading uses `LoadStructure(..., refreshCache: true)` after reading and hashing the
  database row. Null is a hard failure.
- `refreshCache: true` still writes page-title and StructDef cache entries. The review runtime
  supplies an in-process cache sink and pre-seeded trusted session so this path makes no Redis call.
- The cloud testing connection is used only from local uncommitted configuration. The runtime sets
  PostgreSQL `default_transaction_read_only` before selecting the tenant schema.
- AxGet emits generated `<select-field>_sourceid` companions that are not StructDef fields. The
  Phase 1 read adapter omits them; preserving lookup IDs for Save round-tripping is Phase 2 work.
- Two connections: read-only Axpert (review data) and a separate writable local AxData store (snapshots, D27).

## P1C-01 Real StructDef/runtime source and smoke harness

Build: tenant-scoped runtime context, current-row StructDef source, AxGet source, and field
runtime factory. Store the content hash in the existing StructDefVersion snapshot field.
On later calls, re-read the current row and fail closed while reporting a hash mismatch;
pin/reload/migration policy is Phase 2 work.

Tests: current-row hash, changed-hash detection, tenant/transaction isolation, concurrent
two-tenant isolation, fail-closed loader/parser behavior, existing-record load, and the
permanent concrete-Axpert namespace boundary test.

Status: the `uat2` / `dtchk` smoke loads the current StructDef and expected record, initializes
Validate/Parser, and confirms an in-memory `qty` Set produces the `amount` dependency change.

Done when: the no-HTTP/no-AxData-Redis smoke harness loads the review StructDef and record,
runs Validate/Parser and dependencies, or records a concrete incompatibility.

## P1C-02 Real authorization

Build `Authorization/AxpertAuthorizationProvider.cs` over create/view/edit permission calls.
Test allow/deny create, DC/field include/exclude, read-only, user/role precedence,
cross-tenant access, and unrelated users against real permission data.

Status: implemented with fail-closed normalization, user-over-role precedence, hidden-node
omission, and full business-key forwarding (`keyfield`, `keyvalue`, and record id). The live
`uat2/dtchk` record `100000000008013`, addressed by `recordid`, grants full-control view/edit
for `admin/default`; cross-tenant and unrelated identities are denied. The complete read-only
StructDef -> AxGet -> Validate/Parser -> dependency smoke also passes for this record.

## P1C-03 Real-data review composition

Split fake M1 and real-review composition. RealDataReview is Development-only, checks the
database-host allowlist and read-only database user, and supplies a trusted configured identity.
Production authentication remains deferred.
Done when: RealDataReview composition refuses to start in non-Development, with a non-allowlisted
host, or with a writable Axpert connection (one test each); fake M1 composition still passes.

## P1C-04 HTTP walkthrough and report

Automate trusted identity -> permission -> StructDef -> AxGet/new state -> Init -> Get -> Set
-> Validate/Parser -> dependencies -> Redis snapshot -> SQL durability (local store, not the
Axpert DB) -> explicit Redis-key delete -> recovery Get. Requires Redis 6.2+;
the host's Memurai 3.0 cannot run the stream cases (verify: mumurai runs in port 6385 & memurai_version:8.2.0
redis_version:8.2.10. Update in necessary places). Report only safe identifiers, content
hash, informational updatedon, record/version identifiers, and success counts; never values.
Done when: one command runs the full chain, and a hash-mismatch run fails closed (D28 path).

## P1C-05 AxData-native FillGrid

Implement query execution through `IDbHelper`; no legacy execution engine exists. Verify D24
against real AddRows 1/2/3 definitions, multiple applicable definitions, duplicate parameters,
parameterized SQL, tenant schema isolation, and PostgreSQL dialect behavior. Re-run P1C-04 with
FillGrid and capture characterization snapshots for future native replacements.
Safety rails (FillGrid SQL is tenant-authored): SELECT-only guard, parameters only, statement
timeout, row cap, tenant schema from trusted context only, read-only connection.
Done when: AddRows 1/2/3 verified against real definitions (D24 confirmed or revised in
DECISIONS.md) and P1C-04 re-run with FillGrid passes.

Exit = milestone M1.5 (you review): real data through Init/Get/Set/recovery. Then Phase 1D.
