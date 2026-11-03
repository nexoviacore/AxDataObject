# AxData adoption handoff for ARM dependency work

This is a blocked handoff, not an active AxData slice. Do not add it to `docs/slices/README.md` or start it while ARM-DEP-00 through ARM-DEP-04 are incomplete. When ARM-DEP-04 is accepted, schedule these tasks deliberately against the then-current AxData roadmap so they do not overlap another field-engine slice.

Applicable decisions and boundaries remain D3, D26, D28, and D33-D35. Applicable `docs/SALVAGE.md` bucket is **Reuse with changes** for Set/validation/dependencies and StructDef/Validate/Parser integration.

## AXD-DEP-01 — project authoritative ARM dependency metadata

**Blocked by:** ARM-DEP-04 accepted and its stable contract recorded.  
**Effort:** Deep.

### Objective

Expose the authoritative ARM direct dependency graph through AxData-owned Application contracts without leaking concrete ARM types outside `AxData.Integration.Axpert`.

### Expected scope

- `AxData/src/AxData.Integration.Axpert/StructDef/AxpertStructDefSource.cs`.
- `AxData/src/AxData.Application/Ports/Axpert/AxpertContracts.cs`.
- StructDef provider/contract tests and fixtures.
- `docs/CODEMAP.md`, `docs/PROGRESS.md`, and Postman only if an external contract changes.

### Required behavior

- Preserve ARM edge direction: source field to direct downstream target.
- Preserve dependency kind and proven row/frame scope required by Set.
- Do not interpret compatibility `TField.Deps` as target inputs.
- Remove the `Cexp` text regex as the authoritative dependency source. A fallback may exist only if ARM-DEP-04 explicitly defines and tests it.
- Validate unknown targets/cycles according to the accepted ARM contract.
- Derive stable evaluation order from metadata rather than display order where required.
- No HTTP or snapshot-shape change is intended.

### Session prompt

```text
Task: AXD-DEP-01
ARM-DEP-04 is complete. Read docs/PROGRESS.md, this AXD-DEP-01 block, the ARM-DEP-04 verification report/contract, and only the AxData files listed here. Read D3, D26, D28, D33-D35 and the named SALVAGE bucket. Plan first and wait for approval. Do not modify ARM-11.4 unless a new separately approved cross-project plan proves the accepted ARM contract is insufficient.
```

## AXD-DEP-02 — consume targeted ARM validation in Set

**Blocked by:** AXD-DEP-01 accepted.  
**Effort:** Deep.

### Objective

Replace AxData's per-submission full `ValidateData()` sweep with the accepted ARM targeted operation while preserving P1D-05B operation-scoped hydration/readback and D3 continuation.

### Expected scope

- `AxData/src/AxData.Integration.Axpert/FieldEngine/AxpertFieldEngine.cs`.
- AxData Application port contracts only if AXD-DEP-01 did not fully define the operation result.
- `AxData/src/AxData.Application/Set/SetAxData.cs` only for orchestration changes proven necessary.
- Unit, adapter/contract, concurrency, recovery, and performance tests.
- `docs/CODEMAP.md`, `docs/PROGRESS.md`, and Postman only if an external contract changes.

### Required behavior

- One fresh runtime, one hydration, and one readback per Set operation remain authoritative.
- Apply submitted fields in authoritative order.
- Use targeted dependency refresh/validation, rebuilding only after a rejected field contaminates runtime state.
- Failed sources block their dependents; independent fields continue per D3.
- Full final Save validation remains separate; Set does not weaken Save.
- Preserve source IDs, read-only dependency updates, FillGrid port separation, StructDef hash checks, version CAS, and existing AXD errors.
- Measure full-sweep calls, refreshed fields, SQL queries, gate wait, and Set latency before/after.

### Session prompt

```text
Task: AXD-DEP-02
AXD-DEP-01 is complete. Read docs/PROGRESS.md, this AXD-DEP-02 block, the accepted AXD-DEP-01 contract, D3/D28/D33-D35, and only the listed FieldEngine/Set/tests. Plan first and wait for approval. Keep ARM-11.4 unchanged unless a new explicit Cross-project change section is approved.
```
