# Code map

Keep under ~150 lines. Codex updates this at the end of each slice: one line per new or moved type, `path: purpose`.
Group a folder into one line when it has more than 5 files. All paths below are relative to `AxData/`.

src/AxData.Api: HTTP endpoints, authentication, filters
src/AxData.Api/Contracts, Errors, Identity, Middleware, Requests, Routing: versioned API conventions, trusted claims, safe errors, correlation/logging scopes, and request metadata parsing
src/AxData.Api/Observability: Baseline/Diagnostic structured Microsoft logging over allow-listed operational events
src/AxData.Api/Middleware/RequestCompletionLoggingMiddleware.cs: correlation-aware HTTP status and duration completion events
src/AxData.Api/Development/M1DevelopmentServices.cs: opt-in Development-only real Redis/Postgres state composition with deterministic fake Axpert adapters
src/AxData.Api/Development/DevelopmentIdentity.cs: trusted configured Development identity middleware shared by fake M1 and RealDataReview
src/AxData.Api/Development/RealDataReviewServices.cs: Development-only real Axpert composition, host allowlist, separate state storage, and startup read-only guard
src/AxData.Application: use cases, orchestration, authorization, ports
src/AxData.Application/Init/InitializeAxData.cs: authorized new/existing transaction initialization and create-only state persistence
src/AxData.Application/Query/QueryAxData.cs: tenant-scoped permission-filtered complete/selective query projection with exact-StructDef fail-closed validation
src/AxData.Application/Set/SetAxData.cs: authorized Set orchestration, exact-StructDef validation, row actions, locking, version checks, and single CAS persistence
src/AxData.Application/Authorization: pure tenant, actor-relation, operation-permission, and editability authorization policy
src/AxData.Application/Ports/Axpert: Axpert-neutral StructDef (including evaluation order), load, field evaluation, FillGrid, and transaction-writer contracts
src/AxData.Application/Ports: tenant-scoped state store, trusted user context, clock, and ObjectId generation contracts
src/AxData.Application/Ports/Observability/OperationalLogging.cs: infrastructure-neutral safe event schema, levels, detail modes, and sink port
src/AxData.Application/Ports/System/CryptographicObjectIdGenerator.cs: cryptographically random ULID ObjectIds and system clock
src/AxData.Application/Ports/System/CryptographicRowIdGenerator.cs: cryptographically random opaque row identities independent of persistence IDs and row positions
src/AxData.Application/Ports/State/IAxDataLock.cs: short object-lock contract with asynchronously releasable owned leases
src/AxData.Application/Ports/Snapshots/SnapshotContracts.cs: correlation-carrying snapshot envelope, Redis transport, delivery, and durable-repository contracts
src/AxData.Application/Recovery/AxDataRecoveryService.cs: authorized tenant-scoped durable recovery with lifecycle and exact-StructDef validation
src/AxData.Domain: AxData model, rules, result/error types
src/AxData.Domain/Model: aggregate state, containers, rows, fields, PEG projection, and file references
src/AxData.Domain/Lifecycle: object/row/workflow states and n/e/d compatibility action mapping
src/AxData.Domain/Errors: immutable AXD error catalog and HTTP mappings
src/AxData.Infrastructure: Redis, SQL, queues, outbox, file storage
src/AxData.Infrastructure/Redis: tenant-scoped Redis state store with atomic version CAS and inactivity TTL
src/AxData.Infrastructure/Redis/RedisAxDataLock.cs: token-owned Redis object lease with bounded expiry and compare-delete release
src/AxData.Infrastructure/Redis/RedisSnapshotTransport.cs: atomic state-CAS/stream publication, consumer-group delivery, durability-gated TTL, DLQ, and acknowledged-offset trimming
src/AxData.Infrastructure/Serialization: deterministic, versioned AxData snapshot contracts, typed values, and size/grid limit enforcement
src/AxData.Infrastructure/Serialization/AxDataSnapshotV2.cs: dcId/rowId snapshot schema preserving semantic row array order with separate nullable recordId
src/AxData.Infrastructure/Sql: ARMCommon-backed snapshot writes, three-provider atomic version guards, and provider-specific table migrations including PostgreSQL UTC timestamptz upgrade
src/AxData.Integration.Axpert: adapters for StructDef/Validate/Parser/AxGet/AxPut
src/AxData.Integration.Axpert/AxpertRecordKeyMapping.cs: consistent business-key and record-ID mapping for legacy permission/AxGet calls
src/AxData.Integration.Axpert/StructDef/AxpertStructDefProvider.cs: immutable versioned StructDef provider and tenant-aware cache over an authoritative Axpert source
src/AxData.Integration.Axpert/StructDef/AxpertStructDefSource.cs: tenant-scoped current tstructs loader, raw-props SHA-256 revision, cache-bypassed StructDef projection
src/AxData.Integration.Axpert/Runtime/AxpertRuntimeContextFactory.cs: scoped trusted review session, tenant/schema validation, and Axpert Utils initialization
src/AxData.Integration.Axpert/Runtime/AxpertReviewStartupGuard.cs: passive PostgreSQL privilege audit that rejects writable review identities at startup
src/AxData.Integration.Axpert/Runtime/NoOpRedisCache.cs: in-process cache sink that keeps database-only review loading Redis-free
src/AxData.Integration.Axpert/AxGet/AxpertDataLoader.cs: result-safe AxGet request adapter and StructDef-validated AxData state loader
src/AxData.Integration.Axpert/AxGet/AxpertDataSource.cs: real AxGet service invocation and result-safe legacy payload normalization
src/AxData.Integration.Axpert/Authorization/AxpertAuthorizationProvider.cs: real create/view/edit permission queries, user precedence, visibility normalization, and fail-closed authorization
src/AxData.Integration.Axpert/FieldEngine: checked Validate/Parser runtime binding and fail-closed field evaluation adapter
src/AxData.Integration.Axpert/FieldEngine/AxpertFieldRuntimeFactory.cs: content-hash-checked, cache-bypassed Validate/Parser runtime construction
src/AxData.Integration.Axpert/FillGrid: TFg normalization, applicability, parameter binding, result-safe query mapping, and declarative FillGrid adapter
src/AxData.Integration.Axpert/FillGrid/AxpertFillGridRuntimeFactory.cs: tenant/version-checked real TFg runtime construction
src/AxData.Integration.Axpert/FillGrid/AxpertFillGridSqlSource.cs: safe raw StructDef XML recovery for SQL omitted by legacy TFg projection
src/AxData.Integration.Axpert/FillGrid/AxpertFillGridQueryExecutor.cs: SELECT-only parameterized PostgreSQL execution with timeout and row cap
src/AxData.Integration.Axpert/Development/M1DevelopmentAxpert.cs: deterministic local Axpert fakes for the M1 HTTP walkthrough
src/AxData.Peg: PEG application/domain, Redis state, persistence adapters
src/AxData.Workers: snapshot, outbox, print, notification workers
src/AxData.Workers/Snapshots/SnapshotConsumer.cs: version-guarded snapshot persistence consumer with exponential retry, acknowledgement, and poison dead-lettering
src/AxData.Workers/Observability/ConsoleOperationalEventSink.cs: filtered structured-JSON worker event sink
src/AxData.Workers/Program.cs: environment-configured Redis-to-Postgres snapshot worker host
tests/AxData.TestSupport/Fakes: scriptable Axpert, state-store, identity, clock, and ObjectId test doubles
tests/AxData.TestSupport/Fixtures/SampleStructDefFixture.cs: non-grid/grid sample metadata with dependency and FillGrid definitions
tests/AxData.UnitTests/Application/PortsAndFakesTests.cs: port-fake dependency, failure safety, fixture-shape, tenant, and version tests
tests/AxData.UnitTests/Application/Init/InitializeAxDataTests.cs: new/existing, authorization, unknown-transaction, and persistence orchestration tests
tests/AxData.UnitTests/Application/Query/QueryAxDataTests.cs: selection, visibility, read-only, approver, denial, and tenant-isolation query tests
tests/AxData.UnitTests/Application/Set/SetAxDataTests.cs: Set row-action, permission, security, locking, stale-version, and CAS tests
tests/AxData.UnitTests/Application/Recovery/AxDataRecoveryServiceTests.cs: recovery lifecycle, permanent/transient failure, exact-definition, authorization, and tenant-isolation tests
tests/AxData.UnitTests/Application/Authorization/AxAuthorizationPolicyTests.cs: complete relation-operation matrix and ObjectId/tenant/PEG security tests
tests/AxData.UnitTests/DependencyDirectionTests.cs: verifies Domain/Application do not reference infrastructure or Axpert dependencies
tests/AxData.UnitTests/Domain: lifecycle, version, row-action, field-value, and error-catalog tests
tests/AxData.IntegrationTests/Serialization: snapshot round-trip, v1 fixture compatibility, determinism, and rejection tests
tests/AxData.IntegrationTests/Redis/RedisAxDataStoreTests.cs: real-Redis stale/concurrent CAS, TTL refresh, and tenant-isolation tests
tests/AxData.IntegrationTests/Redis/RedisAxDataLockTests.cs: real-Redis lease ownership, expiry, reacquisition, and stale-write tests
tests/AxData.IntegrationTests/Sql: snapshot dialect, JSON binding, stale/duplicate delivery, and tenant-isolation integration-contract tests
tests/AxData.IntegrationTests/Workers/SnapshotConsumerTests.cs: retry, acknowledgement, and poison-envelope dead-letter tests
tests/AxData.ContractTests: API contracts (correlation, errors, identity, headers, Query/Set endpoints), Axpert adapter contracts (StructDef, AxGet, authorization, field engine, FillGrid), M1 identity, and the opt-in RealDataReviewSmokeTests harness
tests/AxData.ContractTests/OperationalLoggingContractTests.cs: Baseline filtering and sensitive-value exclusion for structured operational logs
tests/AxData.UnitTests/Integration/M1DevelopmentAxpertTests.cs: M1 fake fixture, tenant denial, dependency, and read-only behavior
tools/http/m1-recovery.http: executable Init/Set/Get/evict/recover real-infrastructure walkthrough
tools/real-data-review/Invoke-P1C04Walkthrough.ps1: value-free P1C-04 HTTP, durability/recovery, and D28 mismatch automation
tools/postman/: secret-free M1.5 Postman review collection, environment template, and manual review instructions
