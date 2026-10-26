# P1D-05 field-engine handoff

Use this handoff for the separate P1D-05A and P1D-05B sessions. The slice definitions and D33-D35 are authoritative; this report records the evidence and execution order that led to them.

## Scope and boundaries

- P1D-05A is documentation, parity characterization, baseline measurement, and final capacity/cache/semaphore acceptance criteria. It makes no product or ARM-11.4 change.
- P1D-05B implements the approved operation-scoped runtime and snapshot v3 plan after P1D-05A has no open decisions.
- No public API change is intended. Source IDs remain internal. Postman is not applicable unless an endpoint/request/response or AXD error changes.
- Default implementation boundary is AxData-only. Any ARM-11.4 change requires a separate Cross-project change plan, blast-radius analysis, explicit approval, and `docs/ARM-11.4-CHANGES.md` update.

Applicable `docs/SALVAGE.md` bucket is **Reuse with changes**:

- Runtime field and row behavior.
- Initialize/AxGet population.
- Set, validation, dependencies, and FillGrid.
- StructDef, Validate, and Parser integration.
- Redis active-state persistence.

## Verified current behavior

- AxTstructData AxPut creates one StructDef/Validate/Parser runtime for a transaction operation. For edit it loads AxGet data into that same runtime, establishes the old/current baseline with `SubmitValueOnLoad`, submits the changed payload, validates, and then reads the same runtime for persistence.
- AxData currently calls the field runtime for each submitted field. Each call enters a static one-slot semaphore, creates a runtime, hydrates the whole object, validates, and reads the whole object back.
- Current AxData runtime factories call legacy `StructDef.LoadStructure(..., refreshCache: true)`. AxTstructData uses the default cache-enabled call.
- The visible legacy cache key contains tenant schema and TransId, not the raw definition hash; no expiry or Builder invalidation was found in the inspected C# path. Cache use therefore needs coherency proof.
- AxGet emits `<field>_sourceid` companions. AxTstructData `SubmitValueOnLoad` restores current and old source IDs, but current AxData projection discards the companions.
- Snapshot v2 repeats current and original values for clean fields and is capped at 1 MiB. The characterization envelope is 50 DCs x 70 rows per DC x 100 fields = 350,000 field instances.

## AxTstructData parity trace required by P1D-05A

1. Create `Validate`.
2. Load one StructDef for the transaction operation.
3. Obtain and attach Parser.
4. Register `recordid`.
5. Set `LastSavedRecordid`.
6. Apply client/global date format.
7. Call `InitValidateAsync` and check its result.
8. For edit, call AxGet with the same Validate/runtime.
9. Submit loaded `axp_recid`, row actions, display values, and `_sourceid` companions through `SubmitValueOnLoad` to establish the baseline.
10. Reconcile submitted record IDs and submit changed fields in authoritative evaluation order.
11. Call `ValidateData` and check success before reading any calculated/parser value.
12. Read changed values and current/old source IDs from that runtime.

P1D-05 deliberately skips AxPut SQL generation/commit, child transactions, GenMap, and MdMap because they belong to the Save slices. It also skips cross-request runtime reuse because mutable Axpert runtime objects are operation-scoped under D33.

## P1D-05A deliverables

1. Capture a current baseline for small, medium, and capacity-envelope objects: StructDef loads, runtime creations, hydration/readback counts, semaphore wait, validation time, total Set latency/throughput, snapshot bytes, and approximate memory.
2. Prove or reject Builder invalidation/coherency of the legacy StructDef cache. If not proven, record refresh-once-per-operation as the P1D-05B policy.
3. Characterize whether a rejected field mutates legacy runtime state and specify the exact rebuild/checkpoint rule needed to preserve D3 independent-field continuation.
4. Measure deterministic snapshot-v3 candidates, including sparse originals/source IDs and optional per-DC field ordinals. Do not preselect MessagePack, compression, or multi-key partitioning.
5. Record the supported object-size limit and measurable P1D-05B acceptance thresholds. Keep the global gate unless evidence supports a narrower safe scope.
6. Finish with an implementation plan listing exact files and tests. Stop if any decision needed by P1D-05B remains open.

## P1D-05B intended flow

```text
authenticate -> authorize -> tenant-scoped load -> lock -> expectedVersion/hash check
-> create one fresh Axpert operation runtime -> hydrate once
-> apply fields in evaluation order -> validate/dependencies
-> rebuild from last accepted state only when a rejected field contaminated runtime
-> FillGrid through its existing port -> read state once
-> increment version -> atomic Redis CAS + snapshot stream -> discard runtime
```

Existing-record Init retains display value plus nullable Int64 source ID. New/default fields begin with null source ID until Axpert resolves them. Clients do not gain mutation authority by submitting an ID.

Snapshot v3 remains typed and deterministic, stores the StructDef content-hash reference rather than the definition body, reads v1/v2, and writes sparse originals/source IDs. `dcN`, opaque RowId, nullable recordId, and adapter-only positional `rowN` remain governed by D32.

## Expected implementation areas

- `AxData.Application/Ports/Axpert`: operation-session contracts with AxData-owned DTOs only.
- `AxData.Application/Set/SetAxData.cs`: one session per Set and safe independent-field continuation.
- `AxData.Domain/Model/AxFieldValue.cs`: normalized IDs and clean/dirty baseline invariants.
- `AxData.Integration.Axpert/AxGet`: retain and validate source-ID companions.
- `AxData.Integration.Axpert/FieldEngine`: parity initialization, one-time hydration/readback, and gate/cache policy.
- `AxData.Infrastructure/Serialization`: deterministic v3 writer plus v1/v2 readers/upcasters and measured limits.
- Unit, adapter/contract, Redis recovery, security, and capacity/concurrency tests listed in `docs/technical/21-testing.md` section 21.6.

Do not add a NuGet package, change the external API, or touch ARM-11.4 without the separately required approval.
