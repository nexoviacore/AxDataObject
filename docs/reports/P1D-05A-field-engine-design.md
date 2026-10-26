# P1D-05A field-engine parity, state, and performance design

## Outcome

P1D-05B can proceed without an open design decision. It will use one fresh Axpert runtime per Set operation, refresh StructDef once per operation, retain normalized source IDs internally, rebuild after any rejected evaluation when more independent fields remain, and write deterministic sparse JSON snapshot v3 with repeated field names. The global legacy runtime gate remains operation-wide until a later isolation test satisfies the narrowing criteria below.

No product or ARM-11.4 file changes are part of P1D-05A. The public API and AXD error catalog are unchanged, so the Postman pack is not applicable.

## Applicable salvage boundary

The applicable `Reuse with changes` items are:

- Runtime field and row behavior.
- Initialize/AxGet population.
- Set, validation, dependencies, and FillGrid.
- StructDef, Validate, and Parser integration.
- Redis active-state persistence.

Only behavior and call order are preserved. Mutable legacy types remain behind Application ports and are neither serialized nor shared.

## AxTstructData parity trace

P1D-05B must preserve this order inside one operation session:

1. Create `Validate` and load one StructDef.
2. Attach its Parser and register `recordid`.
3. Set `LastSavedRecordid` and the client/global date format.
4. Call `InitValidateAsync` and stop if it returns false.
5. For edit, load AxGet data through the same runtime.
6. Restore `axp_recid`, row actions, display values, and `_sourceid` companions with `SubmitValueOnLoad` so current and old values/IDs form the baseline.
7. Reconcile submitted record IDs and apply submitted fields in authoritative evaluation order.
8. Call `ValidateData` and check success before reading any parser/calculated value.
9. Read accepted current values and current/old source IDs from the same runtime.

AxPut SQL/commit, child transactions, GenMap, and MdMap remain outside this slice and belong to Save. A runtime is discarded at operation completion and is never reused across requests, objects, users, or tenants.

## Current baseline and cost model

Source inspection establishes the following exact current behavior for `S` submitted fields and `F` field instances in the loaded object:

| Metric | Current Set | P1D-05B successful Set |
|---|---:|---:|
| Global gate acquisitions | `S` | 1 |
| Runtime creations | `S` | 1 |
| Raw StructDef reads / cache-bypassed loads | `S` | 1 |
| Full-state hydration visits | `S × F` | `F` |
| Full-state readback visits | `S × F` | `F` |

The current adapter acquires a static one-slot semaphore, creates a runtime, hydrates the complete state, validates, reads the complete state, and releases the semaphore for each submitted field. Its field resolution also linearly scans legacy fields during hydration/readback. The capacity-envelope runtime cost is therefore not represented by snapshot time alone; P1D-05B must index legacy fields by `(frameNo, fieldName)` while creating the operation session.

The global gate bounds current runtime throughput to one field evaluation at a time across all tenants and objects. P1D-05B improves work inside the gate by moving acquisition to the operation boundary; it does not claim legacy thread safety.

A disposable three-field legacy-runtime fixture measured 0.135 ms median total adapter evaluation, 0.063 ms median `ValidateData`, and 7,413 evaluations/second over 20 post-warm-up iterations. These figures isolate in-memory adapter overhead; they do not include a real StructDef database load or a large-object traversal and must not be extrapolated as production Set latency. In a two-request contention check with a controlled 100 ms runtime-factory hold, the second request reached its factory after 98.09 ms and both completed in 204.14 ms. This confirms that wait time is approximately the preceding holder's remaining work and throughput is globally serialized.

## Snapshot characterization

The measurements used a disposable .NET 8 integration-test harness in Debug, three timed iterations after warm-up, and the median elapsed time. Values were 16-character strings; 10% of fields carried normalized source IDs. The 10%-dirty candidate additionally carried original values and original source IDs. `stateApproxBytes` is the retained managed-memory delta after constructing the current domain state and forcing collection. Results are characterization evidence, not a production latency SLA.

| Shape | Fields | State approx. | Current v2 bytes / median | Sparse names, clean | Sparse ordinals, clean | Sparse ordinals, 10% dirty |
|---|---:|---:|---:|---:|---:|---:|
| 1 DC × 1 row × 10 | 10 | 2,600 B | 2,228 B / 0.07 ms | 1,025 B / 0.75 ms | 1,090 B / 0.03 ms | 1,178 B / 0.02 ms |
| 10 DC × 20 rows × 50 | 10,000 | 1,188,032 B | 1,708,508 B / 25.11 ms | 762,052 B / 12.83 ms | 715,702 B / 7.86 ms | 803,702 B / 8.82 ms |
| 50 DC × 70 rows × 100 | 350,000 | 39,443,200 B | 59,466,368 B / 888.39 ms | 26,352,612 B / 276.60 ms | 24,623,362 B / 195.66 ms | 27,703,362 B / 199.95 ms |

At the envelope, sparse repeated names reduce v2 by 55.7%. Ordinals save only another 6.6% and couple row payloads to a container-level schema. Repeated field names are therefore selected for v3. The measured clean payload is 25.13 MiB; adding the measured dirty-field cost to the repeated-name form projects 28.06 MiB at 10% dirty, below a 32 MiB cap. Values with larger content may still exceed the byte cap and fail before Redis publication.

MessagePack, compression, and multi-key partitioning remain rejected for P1D-05B: the measured sparse JSON form fits the characterized envelope, requires no package, keeps inspection/upcasting straightforward, and preserves the existing single-key CAS plus stream publication.

## Resolved state and failure rules

- A field stores typed current value plus nullable Int64 `SourceId`. Only dirty fields retain typed `OriginalValue` and nullable `OriginalSourceId`.
- Existing-record Init parses `<field>_sourceid` as a positive Int64 or null. Malformed or conflicting companions fail the AxGet load; they are never exposed as client mutation authority.
- Clean-field rehydration derives its baseline from current value/source ID. Dirty-field rehydration validates that at least value or source ID differs from its original pair.
- Container identity is written once per container; row ID, nullable persisted record ID, order, and state once per row. D32 remains unchanged.
- A failed submit, parser operation, or validation is considered contaminating because the legacy runtime is mutated before success is known. If another independent field remains, discard the runtime, create a fresh one, and hydrate the last accepted AxData checkpoint. If no eligible field remains, discard without rebuilding.
- The checkpoint advances only after a successful field evaluation. Dependents of a rejected source remain skipped under D3; accepted independent fields remain applied.
- Every false result is checked before any computed/current/source-ID value is read.

## StructDef cache and definition changes

The legacy cache key is schema + TransId + `ARM-AX-STRUCTDEF`. The inspected C# repository contains cache read/write sites but no Builder save/invalidation path and no hash/version component or expiry proof. Cache coherency is therefore **not proven**.

P1D-05B must first read the authoritative raw-definition hash, compare it with the object's stored hash, and return existing `AXD-CONC-003` on mismatch. On a match it calls `LoadStructure(..., refreshCache: true)` once for the operation. Authorization remains independent of the legacy definition cache. Enabling cache reuse requires separate Builder invalidation or hash-keyed cache evidence.

## Supported limits and P1D-05B acceptance thresholds

The supported v3 boundary is all of:

- At most 350,000 field instances per object.
- Existing maximum 10,000 rows in any one grid.
- At most 32 MiB deterministic serialized bytes.

The byte limit is authoritative for large values. Exceeding any limit fails before Redis CAS/stream publication and does not change state/version. The 1 MiB default is raised only with the v3 writer and all API/worker compositions must use the same options.

P1D-05B is accepted when the following repeatable checks pass on the same host/build configuration used for its before/after report:

- A successful Set creates one runtime, performs one StructDef load, hydrates once, reads back once, and acquires the gate once, regardless of submitted-field count.
- With one rejected field followed by an independent valid field, counts are exactly two runtime creations, two loads, two hydrations, one final readback, and one operation-wide gate acquisition; no rejected value or dependent leaks.
- A 10-field successful Set performs no more than 1.5 times the field-runtime elapsed work of the same object's one-field Set, and achieves at least 5 times the current per-field-runtime throughput.
- Snapshot v3 at the characterized envelope is at most 32 MiB and its median serialization time is at most 500 ms; v3 must remain deterministic across repeated writes.
- The capacity operation's incremental managed-memory peak is at most 256 MiB and retained memory after discarding the runtime and forcing collection is at most 10 MiB above the pre-operation baseline.
- Two simultaneous Sets for different objects and tenants remain isolated. Keep the global gate. It may be narrowed only if a separate 100-iteration parallel test has zero cross-runtime contamination/failures and at least 1.8 times throughput versus the global gate; no such evidence exists in P1D-05A.
- v1/v2 fixtures recover, new writes are v3, a stale definition hash remains `AXD-CONC-003`, and oversized state is rejected without a write.

## P1D-05B implementation plan

No ARM-11.4 change is planned.

Application and Domain:

- `AxData/src/AxData.Application/Ports/Axpert/AxpertPorts.cs` and `AxpertContracts.cs`: replace per-field evaluation with an AxData-owned operation-session contract and result DTOs.
- `AxData/src/AxData.Application/Set/SetAxData.cs`: acquire one session, apply authoritative order, checkpoint accepted state, rebuild after rejection only when needed, and dispose before FillGrid/persistence.
- `AxData/src/AxData.Domain/Model/AxFieldValue.cs`: add current/original source IDs and enforce clean/dirty pair invariants.

Integration and serialization:

- `AxData/src/AxData.Integration.Axpert/AxGet/AxpertDataSource.cs` and `AxpertDataLoader.cs`: retain and validate `_sourceid` companions.
- `AxData/src/AxData.Integration.Axpert/FieldEngine/AxpertFieldRuntime.cs`, `AxpertFieldRuntimeFactory.cs`, and `AxpertFieldEngine.cs`: implement the parity initialization, operation-wide gate, indexed field lookup, one-time hydration/readback, result checking, and explicit rebuild.
- `AxData/src/AxData.Infrastructure/Serialization/AxDataSnapshotV1.cs`, `AxDataSnapshotV2.cs`, a new `AxDataSnapshotV3.cs`, `AxDataSnapshotSerializer.cs`, and `AxDataSnapshotOptions.cs`: retain v1/v2 readers, add deterministic sparse v3, enforce field/row/byte limits, and make 32 MiB the consistent API/worker default.

Tests and evidence:

- `AxData/tests/AxData.UnitTests/Application/Set/SetAxDataTests.cs`: session counts, evaluation order, accepted checkpoint, rejected contamination, dependent skipping, stale hash, and no-write limits.
- `AxData/tests/AxData.ContractTests/AxpertDataLoaderContractTests.cs`: source-ID preservation, null/malformed/overflow companions, and client-ID non-authority.
- `AxData/tests/AxData.ContractTests/AxpertFieldEngineContractTests.cs`: full parity order, checked failures, source IDs, record IDs, rebuild isolation, runtime counts, and concurrency.
- `AxData/tests/AxData.IntegrationTests/Serialization/AxDataSnapshotSerializerTests.cs`: deterministic v3, sparse clean/dirty fields, source IDs, v1/v2 upcasting, all limits, and the three characterization shapes.
- `AxData/tests/AxData.IntegrationTests/Redis/RedisAxDataStoreTests.cs` and `AxData/tests/AxData.UnitTests/Application/Recovery/AxDataRecoveryServiceTests.cs`: v3 CAS/recovery and v1/v2 recovery.
- Re-run quiet serial build and the full unit, integration, contract, and end-to-end suites without restore. Record before/after counts, durations, bytes, memory, throughput, and semaphore wait in the P1D-05B report.
