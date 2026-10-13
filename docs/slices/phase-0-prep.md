# Phase 0: Prep

Paths under **Read** are relative to `docs/`.
Effort: Light = smaller model, low reasoning. Standard = default. Deep = strongest model, high reasoning, tests first, you review line by line.

### P0-01 Decisions (you, no Codex)
After: nothing
Do: answer D1, D3, D4, D7, D11 in `docs/DECISIONS.md` before Phase 1 starts. Each row says which slice needs it.
Done when: those rows have an answer.

### P0-02 Solution skeleton and guard rails
Effort: Light | Run: you run `dotnet restore` first, then Codex builds and tests | After: P0-01 (D1)
Read: technical/02-solution-structure.md, technical/03-dependency-direction.md
Scope: repo root, src/*, tests/*
Build:
- `AxData.sln` with the 7 src projects and 4 test projects from technical/02, plus `tests/AxData.TestSupport` (fakes).
- Project references exactly per the dependency direction.
- `Directory.Build.props` (nullable, warnings as errors, analyzers), `Directory.Packages.props` (central versions).
- `.gitignore`, `.gitattributes` (`* text=auto`), `.editorconfig` (LF). This stops CRLF noise in diffs.
- An architecture test that fails if Domain or Application reference Redis, SQL, queue SDKs or Integration.Axpert.
- Empty health endpoint in Api.
Done when: build and test pass; the architecture test demonstrably fails when a forbidden reference is added (prove once, revert).

### P0-03 Axpert discovery spike (you, no Codex)
After: nothing (do it while P0-02 and Phase 1 run)
Read: technical/22-entry-criteria.md
Do: write `docs/contracts/axpert-notes.md`, one to two pages, signatures and shapes only, no source code:
1. Does AxPut commit internally, or can you pass in a connection/transaction? (D10)
2. Callable signatures for StructDef load, AxGet load, Validate/Parser set-and-refresh, FillGrid, GenMap/MdMap.
3. Schemas of `axactivetasks` / `axactivetaskstatus` and the PEG routines.
4. Tenant and user context contract; database provider (D11); permission source (D12).
5. One sanitized sample StructDef: non-grid DC, grid DC, one dependency, one FillGrid.
Done when: the note exists and D10, D11, D12 are answered.

### P0-04 Salvage assessment (report only, no new code)
Effort: Standard | After: P0-01 | Run alongside P0-02/P0-03
Read: docs/architecture/ and docs/technical/ (whichever sections match what you find in old-AxData), then
  `ARM-11.4/AxDataObject/` in full, and `ARM-11.4/AxTstructData/` for the specific flows AxDataObject mirrors.
  Use `rg` to jump to the relevant StructDef/Validate/Parser/Common calls from there — don't read those projects
  end to end.
Do: write `docs/SALVAGE.md` with four sections:
1. **Reuse as-is** — code that already matches the new design; where it would live in `AxData/`.
2. **Reuse with changes** — close but needs rework; what changes and why.
3. **Discard** — conflicts with the new design (state model, Redis/version handling, PEG split, auth order,
   etc.); one line on why.
4. **Open questions** — anything ambiguous. Do not decide these yourself.
This is a read + report slice: no files under `AxData/` are created or edited.
Done when: `docs/SALVAGE.md` exists and every later Phase 1-3 slice that could reuse old-AxData code names the
  relevant SALVAGE.md bucket in its own plan before Codex writes it.
