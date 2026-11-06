# ARM-11.4 change ledger

Record every explicitly approved change made under `ARM-11.4/`. AxData remains the preferred location for
new behavior; this ledger documents the exceptional shared-project changes and their blast radius.

## 2026-09-30 — P1B-02 fail-closed Parser evaluation

- Files: `ARM-11.4/AxValidate/Validate.cs`, `ARM-11.4/AxValidate/Helpers/ValidateHelper.cs`.
- Reason: the field-engine adapter cannot safely wrap code that ignores `AxParser.Evaluate(false)`, reads
  `.Value`, or silently retains a prior calculated value; doing so could accept a failed validation or reuse an
  invalid calculation result.
- Behavioral impact: validation expressions, field calculations, and tax formulas now fail closed when parsing fails. Public
  signatures are unchanged; existing `AxValidate` consumers such as `AxTstructData` may now receive a
  validation/calculation failure where malformed expressions previously continued with an empty or stale value.
- Verification: seven focused P1B-02 regression/contract tests pass; the serial AxData solution build and full
  test suite pass without restore. The host's default parallel solution build exits without diagnostics while
  traversing the legacy project graph, so verification used `-m:1`.

## 2026-10-26 — Upstream Dev import through 76080f7

- Files: `ARM.VersionInfo.json`; `ARMAxpertJobsWorkerService/{JobScheduler.cs,RedisJobStore.cs,Worker.cs,appsettings.json}`;
  `ARMCachedSaveWorkerService/{Worker.cs,appsettings.json}`; `ARMCommon/Helpers/{Constants_SQL.cs,RedisCacheHelper.cs,RedisHelper.cs,Utils.cs}`;
  `ARMCommon/Helpers/RabbitMq/RabbitMQConsumer.cs`; `ARMCommon/Interface/{IRedisCacheHelper.cs,IRedisHelper.cs}`;
  `AxList/{Model/ADSDataRequest.cs,Repositories/ADSDataRepository.cs,Service/ADSDataService.cs}`; `AxParserApp/AxParser.cs`;
  `AxStructDef/{Models/TGenMap.cs,StructDef.cs}`; `AxTstructData/{Interface/IAxParserProvider.cs,Program.cs}`;
  `AxTstructData/Service/{AxDeleteService.cs,AxGetService.cs,AxParserProvider.cs,AxPutService.cs}`;
  `AxUtils/{Controllers/QueueStatusController.cs,Interface/IQueueStatusRepository.cs,Interface/IQueueStatusService.cs,Model/QueueStatusData.cs,Program.cs,Repositories/QueueStatusRepository.cs,Service/QueueStatusService.cs}`;
  and `AxValidate/Validate.cs`.
- Reason: deliberately import the complete upstream `Dev` range `a809567..76080f7` (nine commits) rather than
  pulling or merging the preserved legacy repository into the parent working tree. The range contains worker
  application-pool routing, parser/session reconstruction, AxPut/GenMap corrections, queue status, RabbitMQ
  concurrency, and ADS page-size behavior that cannot be reproduced by an AxData-only adapter.
- Behavioral impact: ARM adopts the upstream implementations. Reconciliation retains the existing fail-closed
  `AxParser.Evaluate` checks in `AxValidate/Validate.cs` and the unchanged tax-expression guard in
  `AxValidate/Helpers/ValidateHelper.cs`; AxGet and AxDelete already matched upstream. Parser reconstruction now
  restores session globals, uses session-bounded Redis expiry, and AxPut prepares expressions before validation.
- Verification: no secrets or machine-specific values were introduced by the two reviewed appsettings diffs.
  `dotnet build --nologo -v q --no-restore -m:1` passes for ARMCommon, AxParser, AxStructDef, AxValidate,
  AxTstructData, ARMAxpertJobsWorkerService, ARMCachedSaveWorkerService, AxList, and AxUtils.

## 2026-10-30 — Upstream Dev import through 5b42a46

- Files: `ARM-11.4/ARM.VersionInfo.json`; `ARM-11.4/AxStructDef/StructDef.cs`;
  `ARM-11.4/AxValidate/{Validate.cs,Helpers/ValidateHelper.cs}`;
  `ARM-11.4/AxTstructData/Repositories/AxPutRepository.cs`; and
  `ARM-11.4/AxTstructData/Service/{AxDeleteService.cs,AxGetService.cs,AxPutService.cs}`.
- Reason: import upstream commits `69ec7fc` and `5b42a46`, which contain the release16 AxPut QA fixes for
  row-aware validation, GenMap buffering/multi-record mapping, SQL parameter and value typing, cancelled
  transactions, key-value record lookup, and attachment retrieval behavior. These shared Axpert changes cannot
  be reproduced faithfully in an AxData-only adapter.
- Behavioral impact: ARM adopts the upstream sparse-row, per-row parser registration, GenMap, permission lookup,
  cancellation, and SQL conversion behavior. Reconciliation preserves the existing fail-closed parser/tax
  guards, removes upstream trailing whitespace, and omits submitted/control values from newly added trace lines.
- Verification: `AxStructDef`, `AxValidate`, and `AxTstructData` build without restore; the serial AxData solution
  build passes; 158 unit, 39 integration, and 139 contract tests pass; E2E exits successfully. Two contract tests
  cover highest-row sparse-grid submission and row-specific parser values during grid validation.

## 2026-10-30 — ARM-DEP-00 characterization harness

- Files: `ARM-11.4/AxDependency.Characterization/Invoke-Characterization.ps1`.
- Reason: freeze current StructDef dependency projection, Validate scanning/cache/static-state behavior, parser failure ordering, and AxPut parity before shared ARM dependency changes. This behavior must be proven in ARM and cannot be characterized through an AxData adapter.
- Behavioral impact: none. The additive package-free harness reads source files and runs deterministic characterization models; no production project references it.
- Verification: the harness passes 14 checks without restore and records exact controlled counters and ARM-DEP-01 stop conditions in `docs/reports/ARM-DEP-00-characterization.md`; the serial AxTstructData build passes with zero errors (existing warnings remain).

## 2026-10-30 — ARM-DEP-01 AxStructDef `a58` metadata

- Files: `ARM-11.4/AxStructDef/{StructDef.cs,Models/TDependencyMetadata.cs}` and
  `ARM-11.4/AxDependency.Characterization/{Invoke-Characterization.ps1,fixtures/ARM-DEP-01-branch.xml,fixtures/ARM-DEP-01-graph-cases.xml}`.
- Reason: AxStructDef owned the `a58` XML constant and legacy dependency members but did not project them during structure loading; ARM callers require authoritative metadata before dependency-directed Validate work can begin.
- Behavioral impact: structure loading now exposes additive typed direct/reverse dependency queries, stable downstream compatibility closures, frame metadata, and diagnostics/fallback status. Existing public members remain; Validate and AxPut execution order are unchanged.
- Verification: the package-free harness passes 17 checks, including the supplied `branch`, structured/text forms, order/deduplication, chain/diamond/cycle/unknown, special tags, frames, and no-`a58`; serial no-restore AxStructDef, AxValidate, and AxTstructData builds pass with pre-existing NU1701 suppressed at the command line.

## 2026-10-30 — ARM-DEP-02 Validate state, cache, and result contracts

- Files: `ARM-11.4/AxValidate/{Validate.cs,Dto/OperationResult.cs,Dto/ValidationAddress.cs,Dto/ValidationInvocationResult.cs}` and `ARM-11.4/AxDependency.Characterization/Invoke-Characterization.ps1`.
- Reason: mutable static/error state and an add-once linked-fill SQL cache made repeated and concurrent ARM validation unsafe; these shared runtime defects affect ARM callers directly and cannot be corrected by an AxData adapter.
- Behavioral impact: validation and submission invocations now expose additive structured results while existing string/`OperationResult` APIs remain wrappers; query results are invalidated and replaced per runtime/source/row/change wave, mutable execution state is instance-owned, and AxPut create/edit/GenMap sequencing is unchanged. Existing ARM field-value logging is unchanged.
- Verification: the package-free dependency harness passes 18 checks covering repeated calls, state isolation, cache replacement/reuse/failure, row/runtime boundaries, structured categories, parser failure ordering, and AxPut parity; serial no-restore AxStructDef, AxValidate, and AxTstructData builds pass with pre-existing NU1701 suppressed at the command line.

## 2026-10-30 — ARM-DEP-03 dependency-directed refresh and validation

- Files: `ARM-11.4/AxValidate/Validate.cs` and `ARM-11.4/AxDependency.Characterization/Invoke-Characterization.ps1`.
- Reason: Validate had authoritative ordered dependency metadata and runtime-safe state, but its only refresh/validation path still scanned every frame, row, and field. Targeted execution must remain inside ARM because it uses Validate's private parser, linked-query cache, source IDs, and mutable StructDef values.
- Behavioral impact: additive targeted and full-current-value APIs validate submitted roots, refresh only reachable ordinary dependents in stable breadth-first order, deduplicate diamonds, stop unchanged/failed paths, apply proven row rules, and use warned compatibility fallback for unsupported metadata or row mappings. Existing `ValidateData`, load refresh, AxPut/GenMap sequencing, and ARM field-value logging are unchanged.
- Verification: the package-free dependency harness passes 25 checks covering branch/query counters, chain/diamond order, unchanged stops, failure isolation, row rules, fallback, full validation, parser/SQL failure guards, compatibility entry points, and AxPut parity; serial no-restore AxStructDef, AxValidate, and AxTstructData builds pass with pre-existing NU1701 suppressed.

## 2026-10-30 — ARM-DEP-04 AxPut integration

- Files: `ARM-11.4/AxTstructData/Service/AxPutService.cs`, `ARM-11.4/AxValidate/Validate.cs`, and `ARM-11.4/AxDependency.Characterization/Invoke-Characterization.ps1`.
- Reason: current ARM AxPut create/edit/GenMap flows still invoked compatibility full-scan validation and did not consume the accepted targeted dependency API. This production orchestration and its runtime query cache cannot be changed through an AxData adapter.
- Behavioral impact: successful submission or mapping now runs targeted dependency refresh/validation and mandatory full current-value validation before SQL construction. AxPut uses an additive cache-aware final-validation overload; the existing parameterless API, public AxPut contract, persistence semantics, and existing ARM field-value logging are unchanged. Compatibility fallback is trace-observable through `AXV-DEP-W001`.
- Verification: 31 package-free ARM checks pass, including create/edit, independent transactions, parent/GenMap and queued records, deleted rows, failure boundaries, state isolation, untouched invalid data, fallback, and sparse counter/persistence parity. Serial no-restore AxStructDef, AxValidate, and AxTstructData builds pass; the serial AxData build and 158 unit, 39 integration, and 139 contract tests pass unchanged. Full evidence and the stable handoff contract are in `docs/reports/ARM-DEP-04-verification.md`.

## 2026-11-02 — ARM-DEP-04 create initialization and parent-bound fallback fix

- Files: `ARM-11.4/AxTstructData/Service/AxPutService.cs`, `ARM-11.4/AxValidate/Validate.cs`, and `ARM-11.4/AxDependency.Characterization/Invoke-Characterization.ps1`.
- Reason: direct AxPut create was sent through the hydrated-edit targeted path, so session-derived parents such as `company` could remain uninitialized before customer/item linked queries. The working legacy path first performed a full initialization refresh. This shared ARM orchestration/state check cannot be reproduced by an AxData adapter.
- Behavioral impact: create and new-record GenMap run exactly one full initialization refresh/validation; hydrated edits remain targeted plus final validation. Missing or unsupported required parent rows select one compatibility full pass, while business validation failures are not retried and a completed fallback is not followed by a second full scan.
- Verification: 32 package-free ARM checks, 159 unit tests, 39 integration tests, and 147 contract tests pass in Release without restore. Focused contracts prove session-derived parent initialization and the parent-bound fallback; `git diff --check` passes. Postman collection changes are not applicable because the HTTP contract and AXD codes are unchanged.

## 2026-11-02 — ARM-DEP-04 parser and targeted selection follow-up

- Files: `ARM-11.4/AxParserApp/AxParser.cs`, `ARM-11.4/AxValidate/Validate.cs`, and `ARM-11.4/AxDependency.Characterization/Invoke-Characterization.ps1`.
- Reason: the create full pass exposed legacy optional-variable and decimal-`abs` behavior, while targeted edit validated customer text without restoring its normalized source ID and generated `customer = 0`. These shared parser/Validate defects cannot be corrected in an AxData-only adapter.
- Behavioral impact: `abs` accepts decimal operands; a missing direct optional variable such as `m_einvoice` keeps the field's pre-evaluation value without reading stale parser output, while other expression failures remain fail-closed. Targeted select roots now refresh once to resolve their source ID before validation.
- Verification: 32 ARM characterization checks, 159 unit, 39 integration, and 150 contract tests pass in Release without restore. Focused contracts cover decimal `abs`, safe missing-direct-variable behavior, and source-ID restoration. Postman is not applicable because no HTTP or AXD contract changed.

## 2026-11-02 — ARM-DEP-04 targeted grid-row binding fix

- Files: `ARM-11.4/AxValidate/Validate.cs` and `AxData/tests/AxData.ContractTests/AxpertFieldEngineContractTests.cs`.
- Reason: targeted validation refreshed grid roots and dependents without first registering their row in the parser. A generated `vchtr` edit consequently evaluated row 1 `subledgername` with row 2 `controlaccount` and `mg_accountid`; this shared Validate state transition cannot be corrected by an AxData adapter.
- Behavioral impact: each targeted root and queued dependent now calls the existing Delphi-parity `RegRow` operation before refresh/evaluation. Targeted scope, dependency order, fallback rules, and final validation are unchanged; no full refresh was added.
- Verification: the focused two-row targeted regression, all 151 contract tests, 159 unit tests, 39 integration tests, and all 32 ARM characterization checks pass without restore; the serial contract-project build succeeds with existing warnings only. Postman is not applicable because no HTTP or AXD contract changed.

## 2026-11-02 — Restore Validate parser failure guards

- Files: `ARM-11.4/AxValidate/Validate.cs`.
- Reason: a later local edit changed all five guarded parser evaluations back to success-only branches, orphaning the optional-variable compatibility helper and allowing failed expressions to continue with prior state.
- Behavioral impact: general expression and validation failures again fail closed before `Parser.Value` is read; only an absent direct optional variable such as `m_einvoice` preserves the field's pre-evaluation value. The dependency, refresh, and row-binding fixes are unchanged.
- Verification: 8 focused regressions, 32 ARM characterization checks, 151 contract tests, 159 unit tests, and 39 integration tests pass without restore; the serial contract-project build succeeds with existing warnings only. Postman is not applicable because no HTTP or AXD contract changed.

## 2026-11-03 — Temporary AxPut-owned AxSave demonstration

- Files: `ARM-11.4/AxTstructData/{Controllers/AxPutController.cs,Model/TstructData.cs,Model/AxSaveDataObject.cs,Service/AxPutService.cs}` and `ARM-11.4/AxSave.Characterization/Invoke-Characterization.ps1`.
- Reason: implement the team-requested temporary comparison flow inside AxPut before the durable AxData Save saga. This intentionally cannot be implemented as an AxData-only adapter because the experiment is specifically measuring the consequences of making AxPut own ObjectId and Redis state.
- Behavioral impact: `AxPutInput` publicly accepts `callIsFromAxSave` and `objectid`. When enabled, AxPut generates a missing ObjectId or loads the supplied user's tenant-scoped temporary object, hydrates StructDef, applies the payload, runs existing refresh/final validation, writes the projected object to an `AXSAVE` Redis key, then prepares/executes SQL. Responses retain the existing shape and add `objectid`; ordinary AxPut is unchanged when the flag is false. This is temporary and must be removed before production AxSave.
- Verification: the dedicated characterization passes 11 flow/order checks; serial AxTstructData and AxData solution builds, 159 unit tests, and 39 integration tests pass without restore with existing warnings. The broader ARM dependency harness remains blocked by the pre-existing missing `IsUnavailableDirectVariableExpression` call in the pushed baseline. Postman adds a disposable-schema happy path and unknown-ObjectId negative.

## 2026-11-05 — P2-01 shared DataObject and AxPut request contracts

- Files: `ARM-11.4/AxTstructData/{AxTstructData.csproj,Controllers/AxPutController.cs,Model/TstructData.cs,Service/AxPutService.cs}`, removed `ARM-11.4/AxTstructData/Model/AxSaveDataObject.cs`, and updated `ARM-11.4/AxSave.Characterization/Invoke-Characterization.ps1`.
- Reason: D36 makes AxTstructData the public ObjectId-aware Save owner, so its wire binding must consume the neutral contract and cannot be changed through an AxData-only adapter. The temporary flag and Redis-before-SQL projection were explicitly disposable.
- Behavioral impact: every AxPut call now uses supplied/generated ObjectId contract fields, nullable retention defaulting true, and matching `If-Match`/`expectedVersion` rules after trusted session resolution. Unknown versions are omitted. The existing AxPut load/validation/SQL/commit/rollback/attachment/logging order is unchanged; shared-state hydration remains P2-02.
- Verification: the package-free characterization passes 15 checks; the serial AxData solution build passes with existing warnings; 9 focused AxPut contract tests, 14 snapshot tests, 159 unit tests, and 40 integration tests pass. The full contract run has 158 passes and retains the two previously recorded unrelated parser-guard failures, first `Failed_validation_expression_is_not_allowed_to_read_parser_value` (expected `Fail`, actual `Success`).

## 2026-11-06 — P2-02 ObjectId load, authorization, hydration, and overlay

- Files: `ARM-11.4/AxTstructData/{Controllers/AxPutController.cs,Interface/IAxPutRepository.cs,Interface/IAxPutDataObjectStore.cs,Model/AxPutDataObjectContext.cs,Program.cs,Repositories/AxPutRepository.cs,Service/AxPutService.cs,Service/AxPutDataObjectProjection.cs,Service/AxPutDataObjectStore.cs}` and `ARM-11.4/AxSave.Characterization/Invoke-Characterization.ps1`.
- Reason: D36 assigns public business Save to AxPut, so tenant-scoped shared-state loading, trusted actor/permission checks, StructDef revision validation, and legacy runtime hydration must occur before AxPut's existing SQL path and cannot be implemented by an AxData-only adapter.
- Behavioral impact: existing ObjectIds load schema-v1/v2/v3 state from `AXD:{tenantId}:{objectId}`, deny unrelated/cross-tenant access, enforce expected version/current StructDef hash and current transaction/DC/field permissions, hydrate one fresh runtime, and overlay only validated payload values. Missing/empty payload saves stored state; generated ObjectIds retain the existing default/autogenerated initialization. Existing SQL/transaction order is unchanged. AxTstructData trace retains existing messages and adds approved DataObject payload, hydration, and overlay state/value messages; AxData/worker logging is unchanged.
- Verification: serial AxData solution and AxTstructData/contract builds pass; 29 package-free ARM checks and 20 focused contracts pass. Full suites pass 159 unit, 40 integration, and E2E; full contracts are 169 passed with the same two pre-existing parser-guard failures. Postman collection/template JSON validates locally.

## 2026-11-06 — P2-03 AxPut transaction parity and legacy AxDataObject retirement

- Files: `ARM-11.4/AxTstructData/{Controllers/AxPutController.cs,Model/AxPutDataObjectContext.cs,Service/AxPutService.cs,Service/AxPutDataObjectProjection.cs}`, `ARM-11.4/AxSave.Characterization/Invoke-Characterization.ps1`, and removed all 13 files under `ARM-11.4/AxDataObject/`.
- Reason: D36 requires record-ID capture and ObjectId result metadata inside the self-committing AxPut path. The retired AxDataObject project had no solution, project, or C# consumers and its incomplete Save copy was already classified as discard; an AxData adapter cannot observe AxPut's assigned IDs or final transaction outcome.
- Behavioral impact: AxPut preserves its existing SQL/child/GenMap/MdMap/attachment/commit/response order, assigns new grid/non-grid record IDs back into the runtime, captures primary transaction record IDs against stable AxData row IDs, publishes mappings only after commit, and returns known numeric version plus `dataObjectRetained=true` without P2-04 Redis finalization. The deleted legacy project is recoverable from Git history; cached parity fingerprints replace repeated rereads.
- Verification: 35 ARM characterization checks and 22 focused AxPut contracts pass; serial AxTstructData and AxData solution builds pass without restore; 159 unit and 40 integration tests pass, E2E exits successfully, and full contracts are 171 passed with the same two pre-existing parser-guard failures. Postman JSON and `git diff --check` pass. Live disposable-schema execution remains pending reviewed local values.

## 2026-11-06 — P2-04 version claim, finalization, and retention/destruction

- Files: `ARM-11.4/AxTstructData/{Interface/IAxPutDataObjectStore.cs,Model/AxPutDataObjectContext.cs,Service/AxPutService.cs,Service/AxPutDataObjectProjection.cs,Service/AxPutDataObjectStore.cs}`.
- Reason: only AxPut knows the provider transaction's actual commit outcome, so the shared-state claim must surround its existing Save sequence and finalization/destruction must follow that commit; an AxData-only adapter cannot provide this ordering.
- Behavioral impact: ObjectId Save atomically claims the expected version as `Saving`, restores prior usable state on rollback/ValidateOnly, and after commit publishes one-version `Saved` or durable `Closed` state with record mappings. Destruction writes a tenant-scoped closure marker before active-key removal. A post-commit state failure returns the successful business result with `dataObjectFinalizationPending=true`; existing SQL, child/GenMap/MdMap, attachment, and commit order is unchanged.
- Verification: serial no-restore solution and focused project builds pass; 26 focused AxPut contracts, 60 focused unit tests, 9 Redis lifecycle/race tests, 162 full unit tests, and 42 full integration tests pass. E2E exits successfully; full contracts are 175 passed with the same two pre-existing parser-guard failures. Postman JSON and `git diff --check` pass. Disposable-schema retained/destroyed review remains pending.
