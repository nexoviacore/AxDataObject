# Phase 1: Foundation (everything runs against fakes)

Paths under **Read** are relative to `docs/`. Effort levels as in phase-0-prep.md.
Integration tests need Docker (Redis / SQL). The Codex sandbox may not reach Docker, so you run those.

### P1-01 Domain model, lifecycle, error catalog

Effort: Standard | After: P0-02
Read: technical/04-domain-model.md, technical/16-errors.md, architecture/05-core-concepts.md
Scope: src/AxData.Domain, tests/AxData.UnitTests
Build: AxDataState / Container / Row / FieldValue; lifecycle enums (object, row, workflow); version rules; AXD-\* error codes with their HTTP mapping as data.
Done when: state-transition, version-increment and row-action (n/e/d) rule tests pass. No infrastructure references.

### P1-02 Snapshot serialization

Effort: Standard | After: P1-01 | Needs: D8
Read: technical/18-serialization.md, technical/17-config.md (MaximumSerializedBytes, MaximumGridRows)
Scope: Domain or Infrastructure/Serialization, tests
Build: explicit `schemaVersion`; deterministic converters for dates, decimals, nulls, file references; never serialize services or credentials; size and grid-row limit checks.
Done when: round-trip test, a backward-read test with a v1 fixture, oversize rejection test.

### P1-03 Ports, in-memory fakes, sample fixture

Effort: Standard | After: P1-01
Read: technical/05-axpert-adapters.md, technical/03-dependency-direction.md
Scope: src/AxData.Application (ports), tests/AxData.TestSupport
Build: the five Axpert ports plus `IAxDataStore`, `IClock`, `IObjectIdGenerator`, `IUserContext`; in-memory fakes; a scripted sample StructDef (non-grid DC, grid DC, one dependency such as qty x rate = amount, one FillGrid).
Done when: a unit test sets a value through the fakes and receives the dependency change. Ports contain no Redis, SQL or Axpert types.
Note: this slice is what lets Phases 1 to 3 proceed without enterprise code.

### P1-04 Redis store: atomic compare-and-set and TTL

Effort: Deep, line-by-line review | After: P1-01, P1-02 | Needs: D11
Read: technical/08-redis.md (8.1 to 8.3), architecture/10-state-persistence.md (10.1)
Scope: src/AxData.Infrastructure/Redis, tests/AxData.IntegrationTests
Build: key `AXD:{tenantId}:{objectId}` (tenant always part of the key); write only if stored version equals expected (Lua or transaction); TTL refresh on authorized activity; a StaleVersion result, never a silent overwrite.
Done when (you run): stale write rejected; two concurrent writers means exactly one wins; TTL refreshed; another tenant's key is unreachable.

### P1-05 Redis short lock (token-owned lease)

Effort: Deep | After: P1-04
Read: technical/08-redis.md (8.2)
Build: acquire with a unique token and bounded lease; release only if the token matches; never held across user think-time.
Done when: an expired lease cannot release another holder's lock; the version check still protects when the lock expired.

### P1-06 API conventions and cross-cutting

Effort: Standard | After: P0-02 | Needs: D4
Read: technical/06.1-api-general.md, technical/16-errors.md, technical/20-observability.md
Scope: src/AxData.Api
Build: `/api/v1` base; correlation-id middleware; error to HTTP mapping including 409 with `currentVersion`; `If-Match` or `expectedVersion` (per D4) and `Idempotency-Key` parsing; trusted claims to `AxUserContext` (tenant and user never from the body); logging scopes with allow-listed fields only.
Done when: an unhandled exception returns a safe message plus correlation id (no stack, no SQL); body-supplied owner/tenant is ignored.

### P1-07 Authorization policy (security tests first)

Effort: Deep, line-by-line review | After: P1-01, P1-03
Read: technical/07-authorization.md, architecture/05-core-concepts.md (5.3), architecture/12-security.md
Scope: Application/Authorization, tests
Do first: write the security tests: guessing another user's ObjectId; cross-tenant access; unrelated user; approver gets Get and approval only.
Build: a pure policy (actor relation, operation) to allow/deny for every matrix cell. Relation resolved from state (owner/initiator, current approver from a faked PEG state, viewer, admin, unrelated). Tenant mismatch always denies.
Done when: table-driven tests cover every matrix cell; no infrastructure dependencies.

### P1-08 Init

Effort: Standard | After: P1-03, P1-04, P1-06, P1-07 | Needs: D4
Read: architecture/08-lifecycle.md (8.1), technical/06.2-initialize.md
Scope: Application/Init, Api
Build: `POST /api/v1/axdata`, new and existing key; StructDef through the port; crypto-random opaque ObjectId; version 1; state stored via `IAxDataStore`; response per 6.2.
Done when: tests with fakes for new, existing, unauthorized, unknown transId.

### P1-09 Get / query

Effort: Standard, review the filtering | After: P1-08
Read: architecture/08-lifecycle.md (8.3), technical/06.4-query.md, technical/07-authorization.md
Build: select shape (`*` or DC/field list); hidden DC/field omitted before serialization; read-only metadata; `PendingApproval` only for the current approver.
Done when: a hidden field is absent from the JSON (not null, not masked); viewer sees read-only; unauthorized gets the standard error shape.

### P1-10 Set A: request handling, row actions, versioning

Effort: Standard | After: P1-08, P1-05 | Needs: D4, D7
Read: technical/06.3-set.md, technical/09-set-algorithm.md (steps up to field permission), architecture/08-lifecycle.md (8.2)
Build: parse request; resolve DC/row against StructDef; n/e/d rules; field write permission; expected version vs current (409); lock; apply raw values with the fake engine returning OK; one version increment through compare-and-set.
Done when: unknown DC/field returns AXD-REQ; deleting an unknown row fails; stale version returns 409 with current version; read-only field rejected.

### P1-11 Set B: validate, dependencies, error collection

Effort: Deep | After: P1-10 | Needs: D3
Read: technical/09-set-algorithm.md, technical/05-axpert-adapters.md (IAxpertFieldEngine), technical/06.3-set.md
Build: deterministic order (StructDef order if provided); per-field call to `IAxpertFieldEngine`; on invalid record a structured error, skip dependencies sourced from that field, continue independent fields; `GetDeps` returns `changed` with `source: dependency`; apply the D3 semantics.
Done when: an invalid field does not trigger its dependents; independent fields still evaluate; all errors collected; GetDeps returns only dependency-changed fields.

### P1-12 Set C: FillGrid

Effort: Standard | After: P1-11
Read: technical/10-fillgrid.md, technical/09-set-algorithm.md (FillGrid steps)
Build: collect changed parents; call `IAxpertFillGridEngine`; apply Append/Replace in the application layer; one version increment per Set.
Done when: append vs replace; runs when the target grid is empty; errors surfaced; exactly one Redis write per Set.

### P1-13 Snapshot table and version-guarded upsert

Effort: Deep | After: P1-02 | Needs: D11
Read: technical/13-database.md (13.1), technical/14-snapshot-queue.md
Scope: Infrastructure/Sql, migrations, integration tests
Build: `AxDataObjectInstance` (one row per tenant + ObjectId, indexes per architecture/13-scalability.md); upsert only if incoming version is greater than stored; duplicate event ids harmless.
Done when (you run): v12 cannot overwrite v13; duplicate event is a no-op; another tenant's row untouched.

### P1-14 Snapshot publish/consume and recovery on Redis miss

Effort: Deep | After: P1-12, P1-13 | Needs: D5
Read: architecture/10-state-persistence.md (10.2, 10.4), technical/14-snapshot-queue.md, architecture/13-scalability.md
Build: an `ISnapshotTransport` port (Application) and `SnapshotEnvelope` DTO; a Redis Streams implementation
whose CAS write and stream append are one atomic Lua operation; a consumer (in `AxData.Workers`) that performs
the P1-13 version-guarded SQL upsert per envelope, XACKs on success, retries with backoff, and dead-letters
after repeated failure; stream trimming by acknowledged offset only; on a Redis miss, load the latest snapshot
inside tenant and authorization scope, validate version and lifecycle, restore to Redis, otherwise return
Expired / NotFound / RecoveryFailed; no eviction unless a durable snapshot at or above the active version exists.
Done when: recovery restores state; another tenant or unauthorized user cannot recover by ObjectId; missing
snapshot returns the defined error; a killed/restarted consumer does not lose or duplicate-corrupt a snapshot
(P1-13's version guard absorbs redelivery); a poison envelope dead-letters instead of blocking the stream.

### P1-15 Local composition and M1 real-infrastructure walkthrough

Effort: Standard, whole-diff review | After: P1-14
Read: technical/21-testing.md (21.4 only)
Build: API startup registrations for the real Redis state store, Redis Streams transport, SQL snapshot store,
and lock (Axpert ports stay fake — P1B hasn't happened yet); a runnable `AxData.Workers` host running the Redis
Stream consumer against Postgres; local-dev config (Redis/Postgres connection strings) via user-secrets or
`appsettings.Development.json` — no credentials committed; an `.http` file with Init/Set/Get requests plus the
manual Redis-delete recovery step.
Done when: API and worker both start against real local Redis + Postgres; the M1 walkthrough below passes
end-to-end; the recovery Get succeeds after deleting the tenant-scoped Redis key; build/tests pass; and the whole Phase 1 diff is reviewed once.

With fakes, real Redis and real DB: Init, Set, Get over HTTP; evict from Redis; Get recovers. Save a `.http` file for it. Review the whole diff once before Phase 2.
