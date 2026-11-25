# Code map

Keep under ~150 lines. Codex updates this at the end of each slice: one line per new or moved type, `path: purpose`.
Group a folder into one line when it has more than 5 files. All paths below are relative to `AxData/`.

src/AxData.Api: HTTP endpoints, authentication, filters
src/AxData.Api/Controllers/AxDataController.cs: authenticated MVC controller for explicit POST Init, Get, and Set HTTP operations
src/AxData.Api/Contracts, Errors, Identity, Middleware, Routing: API conventions, trusted claims, safe errors, and correlation/logging scopes
src/AxData.Api/Identity/AxAuthAuthentication.cs: pinned AxAuth JWT validation and AXD authentication challenge responses
src/AxData.Api/Identity/ArmSessionConsistencyMiddleware.cs: transitional body ARMSessionId equality guard without trusting body identity
src/AxData.Api/Production/ProductionServices.cs: production Redis active-state and live Axpert adapter deployment composition; snapshot recovery is unwired
src/AxData.Api/Observability: Baseline/Diagnostic structured Microsoft logging over allow-listed operational events
src/AxData.Api/Middleware/RequestCompletionLoggingMiddleware.cs: correlation-aware HTTP status, duration, and body-addressed ObjectId completion events
src/AxData.Contracts: infrastructure-free shared DataObject snapshot, AxPut ObjectId/retention, and secure ULID contracts
src/AxData.Contracts/Snapshots/DataObjectSnapshotContracts.cs: current-only positional DTOs shared by AxData and AxTstructData, including compact `[name,flags]` field descriptors and aligned current/old values
src/AxData.Contracts/Snapshots/DataObjectSnapshotReader.cs: strict current positional reader for packed source IDs and optional aligned old values; legacy sparse properties fail closed
src/AxData.Contracts/Snapshots/DataObjectSnapshotWriter.cs: deterministic aligned-value wire writer used by AxData and ObjectId-aware AxPut
../ARM-11.4/AxTstructData/Interface/IAxPutDataObjectStore.cs: AxPut-local tenant-scoped shared DataObject load boundary
../ARM-11.4/AxTstructData/Model/AxPutDataObjectContext.cs: operation-scoped ObjectId/retention state, stable row IDs, claim state, committed record-ID mappings, and AXD decision contracts
../ARM-11.4/AxTstructData/Service/AxPutDataObjectStore.cs: Redis `AXD:{tenantId}:{objectId}` payload loader with identity validation and ARM trace
../ARM-11.4/AxTstructData/Service/AxPutDataObjectProjection.cs: snapshot authorization, runtime hydration, permission projection, overlay validation, lifecycle projection, and stable row-to-record identity capture
src/AxData.Api/Development/M1DevelopmentServices.cs: opt-in Development-only real Redis/Postgres state composition with deterministic fake Axpert adapters
src/AxData.Api/Development/DevelopmentIdentity.cs: trusted configured Development identity middleware shared by fake M1 and RealDataReview
src/AxData.Api/Development/RealDataReviewServices.cs: Development-only real Axpert composition, host allowlist, separate state storage, and startup read-only guard
src/AxData.Application: use cases, orchestration, authorization, ports
src/AxData.Application/Init/InitializeAxData.cs: authorized new/existing transaction initialization and create-only state persistence
src/AxData.Application/Query/QueryAxData.cs: tenant-scoped permission-filtered complete/selective query projection from stored ordered field metadata without a StructDef load
src/AxData.Application/Set/SetAxData.cs: authorized Set orchestration, exact-StructDef validation, row actions, server-owned locking, and one token-verified persistence write
src/AxData.Application/Authorization: pure tenant, actor-relation, operation-permission, and editability authorization policy
src/AxData.Application/Ports/Axpert: Axpert-neutral StructDef (including authoritative dependency metadata and evaluation order), load, field evaluation, FillGrid, and transaction-writer contracts
src/AxData.Application/Ports/Axpert/AxpertContracts.cs: neutral StructDef and operation contracts plus the post-runtime initialization payload consumed by the final-state handler
src/AxData.Application/Ports: tenant-scoped state store, trusted user context, clock, and ObjectId generation contracts
src/AxData.Application/Ports/Identity/AxpertSession.cs: trusted live ARM session resolution contract with internal site metadata
src/AxData.Application/Ports/Observability/OperationalLogging.cs: infrastructure-neutral safe event schema, levels, detail modes, and sink port
src/AxData.Application/Ports/System/CryptographicObjectIdGenerator.cs: cryptographically random ULID ObjectIds and system clock
src/AxData.Application/Ports/System/CryptographicRowIdGenerator.cs: cryptographically random opaque row identities independent of persistence IDs and row positions
src/AxData.Application/Ports/State/IAxDataLock.cs: short object-lock contract with asynchronously releasable owned leases
src/AxData.Application/Ports/Snapshots/SnapshotContracts.cs: correlation-carrying snapshot envelope, Redis transport, delivery, and durable-repository contracts
src/AxData.Application/Recovery/AxDataRecoveryService.cs: dormant future-use snapshot recovery service; not registered by active API composition
src/AxData.Domain: AxData model, rules, result/error types
src/AxData.Domain/Model: version-free aggregate state, containers with ordered static-read-only/source-key field metadata, rows, fields, PEG projection, and file references
src/AxData.Domain/Lifecycle: object/row/workflow states and n/e/d compatibility action mapping
src/AxData.Domain/Errors: immutable AXD error catalog and HTTP mappings
src/AxData.Infrastructure: Redis, SQL, queues, outbox, file storage
src/AxData.Infrastructure/Redis: tenant-scoped Redis state store with token-owned atomic writes and inactivity TTL
src/AxData.Infrastructure/Redis/RedisAxDataLock.cs: token-owned Redis object lease with bounded expiry and compare-delete release
src/AxData.Infrastructure/Redis/RedisSnapshotTransport.cs: snapshot stream publication, consumer-group delivery, DLQ, and acknowledged-offset trimming
src/AxData.Infrastructure/Serialization: deterministic current-only positional state, packed source-key values, aligned dirty baselines, and size/grid limit enforcement
src/AxData.Infrastructure/Serialization/AxDataSnapshotSerializer.cs: deterministic current-only positional writer and strict domain rehydration for packed source IDs and aligned old values
src/AxData.Infrastructure/Sql: dormant future-use last-delivery-wins snapshot writes and provider-specific table migrations
src/AxData.Integration.Axpert: adapters for StructDef/Validate/Parser/AxGet/AxPut
src/AxData.Integration.Axpert/AxpertRecordKeyMapping.cs: consistent business-key and record-ID mapping for legacy permission/AxGet calls
src/AxData.Integration.Axpert/StructDef/AxpertStructDefProvider.cs: identity-validating current neutral StructDef provider without revision binding or a projected-definition cache
src/AxData.Integration.Axpert/StructDef/AxpertStructDefSource.cs: one-load current mutable StructDef source and authoritative ARM dependency projection
src/AxData.Integration.Axpert/Runtime/AxpertRuntimeContextFactory.cs: scoped trusted review session, tenant/schema validation, and Axpert Utils initialization
src/AxData.Integration.Axpert/Runtime/AxpertSessionResolver.cs: live per-request ARM Redis session, tenant schema, user, role, and SITENO resolution
src/AxData.Integration.Axpert/Runtime/ProductionAxpertRuntimeContextFactory.cs: authenticated session-backed production Utils runtime for real adapters
src/AxData.Integration.Axpert/Runtime/AxpertReviewStartupGuard.cs: passive PostgreSQL privilege audit that rejects writable review identities at startup
src/AxData.Integration.Axpert/Runtime/NoOpRedisCache.cs: in-process cache sink that keeps database-only review loading Redis-free
src/AxData.Integration.Axpert/AxGet/AxpertDataLoader.cs: neutral AxGet state loader plus compact real-Init row/field-presence shape and final initialization-payload builder
src/AxData.Integration.Axpert/AxGet/AxpertDataSource.cs: real AxGet service invocation, standalone value projection, and compact real-Init load-shape projection
src/AxData.Integration.Axpert/Initialization/AxpertInitializer.cs: operation-scoped current StructDef, AxGet, Validate/Parser, and on-load Init adapter with one mutable legacy runtime
src/AxData.Integration.Axpert/Authorization/AxpertAuthorizationProvider.cs: real create/view/edit permission queries, DataObject-field-map normalization for Query, user precedence, visibility normalization, and fail-closed authorization
src/AxData.Integration.Axpert/FieldEngine: checked Validate/Parser runtime binding and fail-closed field evaluation adapter
src/AxData.Integration.Axpert/FieldEngine/AxpertFieldRuntimeFactory.cs: single-load, cache-bypassed Validate/Parser runtime construction
src/AxData.Integration.Axpert/FillGrid: TFg normalization, applicability, parameter binding, result-safe query mapping, and declarative FillGrid adapter
src/AxData.Integration.Axpert/FillGrid/AxpertFillGridRuntimeFactory.cs: tenant/version-checked real TFg runtime construction
src/AxData.Integration.Axpert/FillGrid/AxpertFillGridSqlSource.cs: safe raw StructDef XML recovery for SQL omitted by legacy TFg projection
src/AxData.Integration.Axpert/FillGrid/AxpertFillGridQueryExecutor.cs: SELECT-only parameterized PostgreSQL execution with timeout and row cap
src/AxData.Integration.Axpert/Development/M1DevelopmentAxpert.cs: deterministic local Axpert fakes for the M1 HTTP walkthrough
src/AxData.Peg: PEG application/domain, Redis state, persistence adapters
src/AxData.Workers: deferred worker host plus dormant snapshot consumer source
src/AxData.Workers/Snapshots/SnapshotConsumer.cs: snapshot persistence consumer with exponential retry, acknowledgement, and poison dead-lettering
src/AxData.Workers/Observability/ConsoleOperationalEventSink.cs: filtered structured-JSON worker event sink
src/AxData.Workers/Program.cs: inert host reporting that DataObject snapshot persistence is deferred
tests/AxData.TestSupport/Fakes: scriptable Axpert, state-store, identity, clock, and ObjectId test doubles
tests/AxData.TestSupport/Fixtures/SampleStructDefFixture.cs: non-grid/grid sample metadata with dependency and FillGrid definitions
tests/AxData.UnitTests/Application/PortsAndFakesTests.cs: port-fake dependency, failure safety, fixture-shape, and tenant tests
tests/AxData.UnitTests/Application/Init/InitializeAxDataTests.cs: new/existing, authorization, unknown-transaction, and persistence orchestration tests
tests/AxData.UnitTests/Application/Query/QueryAxDataTests.cs: selection, visibility, read-only, approver, denial, and tenant-isolation query tests
tests/AxData.UnitTests/Application/Set/SetAxDataTests.cs: Set row-action, permission, security, active-conflict, and owned-write tests
tests/AxData.UnitTests/Application/Recovery/AxDataRecoveryServiceTests.cs: recovery lifecycle, permanent/transient failure, exact-definition, authorization, and tenant-isolation tests
tests/AxData.UnitTests/Application/Authorization/AxAuthorizationPolicyTests.cs: complete relation-operation matrix and ObjectId/tenant/PEG security tests
tests/AxData.UnitTests/DependencyDirectionTests.cs: verifies Domain/Application do not reference infrastructure or Axpert dependencies
tests/AxData.UnitTests/Domain: lifecycle, row-action, field-value, and error-catalog tests
tests/AxData.IntegrationTests/Serialization: aligned-value/source-key round trips, deterministic limits, corruption rejection, and isolated capacity measurements
tests/AxData.IntegrationTests/Redis/RedisAxDataStoreTests.cs: real-Redis create/owned-write, TTL refresh, exact-key, and tenant-isolation tests
tests/AxData.IntegrationTests/Redis/RedisAxDataLockTests.cs: real-Redis lease ownership, expiry, reacquisition, and expired-token write rejection tests
tests/AxData.IntegrationTests/Sql: dormant snapshot dialect, JSON binding, last-delivery-wins, and tenant-isolation integration-contract tests
tests/AxData.IntegrationTests/Workers/SnapshotConsumerTests.cs: retry, acknowledgement, and poison-envelope dead-letter tests
tests/AxData.ContractTests: API contracts (correlation, errors, identity, headers, Init/Get/Set endpoints), Axpert adapter contracts (StructDef, AxGet, authorization, field engine, FillGrid), M1 identity, and the opt-in RealDataReviewSmokeTests harness
tests/AxData.ContractTests/OperationalLoggingContractTests.cs: Baseline filtering and sensitive-value exclusion for structured operational logs
tests/AxData.ContractTests/AxPutRequestContractTests.cs: ObjectId, retention, Data/SubmitData, ULID, and response-omission contracts
tests/AxData.ContractTests/AxPutDataObjectProjectionTests.cs: shared-state normalization, authorization, hydration, overlay, identity, lifecycle, and current-StructDef contracts
tests/AxData.ContractTests/AxPutEndpointErrorContractTests.cs: AxPut ObjectId HTTP status, AXD code, ObjectId, and version-omission response contracts
tests/AxData.UnitTests/Integration/M1DevelopmentAxpertTests.cs: M1 fake fixture, tenant denial, dependency, and read-only behavior
tools/http/m1-recovery.http: executable Init/Set/Get/evict/recover real-infrastructure walkthrough
tools/real-data-review/Invoke-P1C04Walkthrough.ps1: value-free P1C-04 HTTP, durability/recovery, and D28 mismatch automation
tools/postman/: secret-free M1.5 Postman review collection, environment template, and manual review instructions
