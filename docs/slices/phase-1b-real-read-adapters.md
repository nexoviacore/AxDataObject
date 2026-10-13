# Phase 1B: Real read-side adapters (the only place enterprise code is touched)

Paths under **Read** are relative to `docs/`. Effort levels as in phase-0-prep.md.
Codex sees only `docs/contracts/axpert-notes.md` and the adapter project. Never the enterprise repo.
You reference the enterprise assemblies from `AxData.Integration.Axpert` only. You may write the glue code yourself in Visual Studio and use Codex for mapping code and contract tests.
After: P0-03 and P1-03 (best started after milestone M1)

### P1B-01 StructDef provider, cache, AxGet loader
Effort: Standard | Needs: D12
Read: contracts/axpert-notes.md, technical/05-axpert-adapters.md, technical/19-structdef-cache.md
Scope: src/AxData.Integration.Axpert, tests/AxData.ContractTests
Build: `IStructDefProvider` (cache key = tenantId + transId + published definition version, never TransId alone); `IAxpertDataLoader`.
Done when: contract tests pass against the sample StructDef. Domain and Application are unchanged.

### P1B-02 Field engine adapter (Validate and Parser)
Effort: Deep | After: P1B-01
Read: contracts/axpert-notes.md, technical/05-axpert-adapters.md, technical/21-testing.md (21.2)
Build: `IAxpertFieldEngine` over the existing Validate/Parser.
Done when: equivalence tests match existing Axpert results for a list of sample cases you provide (a valid dependency, an invalid value, a calculation).

### P1B-03 FillGrid adapter
Effort: Standard | After: P1B-01
Read: contracts/axpert-notes.md, technical/10-fillgrid.md
Build: returns the declarative `FillGridResult`; the application layer still applies it.
Done when: contract tests for Append and Replace pass. Then swap fakes for real adapters in a test host and rerun M1.
