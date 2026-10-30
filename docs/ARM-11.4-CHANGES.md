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
