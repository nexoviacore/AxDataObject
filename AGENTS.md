# AxData: agent instructions

Headless, metadata-driven transaction runtime (AxData Object) plus a PEG workflow module, as a NEW .NET solution.
Environment: Windows + PowerShell. Use PowerShell syntax; do not assume bash tools.

## How to work (token discipline)

- Start of a task: read `docs/PROGRESS.md` and ONLY the slice block named in the task (`docs/slices/`).
  Read ONLY the design files that block lists (`docs/architecture/`, `docs/technical/`, map in `docs/INDEX.md`).
  If the slice adds or changes an HTTP endpoint or response shape, also read `tools/postman/README.md`.
  Do not open any other design file.
- When the slice's work overlaps something the old `AxDataObject` project did (Init, Set/validation, Query,
  Auth, Redis state, Save/GenMap/MdMap, PEG), check `docs/SALVAGE.md` and name the applicable bucket and item
  in the plan before writing code. A "Reuse with changes" item says what behavior to preserve and what to
  rebuild — never license to copy the old type wholesale.
- Navigate `AxData/` with `docs/CODEMAP.md`. Navigate `ARM-11.4/` with `rg` for the specific symbol, method, or
  table name you need — never an open-ended directory read or a full-file read of a large existing file.
- If you need to understand how another ARM-11.4 project behaves and the answer isn't obvious from a targeted
  search, summarize what you found and ask me directly instead of proceeding on an assumption.
- Plan first: list the files you will create/change and your assumptions. Wait for my approval before writing code.
- If the design says "to be confirmed", or a decision the slice needs is still open in `docs/DECISIONS.md`,
  STOP and ask. Do not guess.
- Stay inside the slice's scope. No drive-by refactors. Small diffs. Add or update tests with every change.
- Build/test quietly: `dotnet build --nologo -v q`, `dotnet test --nologo -v q`.
  On failure, report only the first error and its cause.
- Do not run restore or other network commands; ask me.
- Postman review pack: any slice that adds or changes an HTTP endpoint, request/response shape, or AXD error
  code must update `tools/postman/` in the same slice. Add or change requests in the current milestone folder
  (happy path plus at least one negative), assert status and AXD error code, and chain `objectId`/`version`
  via test scripts. Take request shapes from the contract tests, never invent them. Do not read the whole
  collection JSON; use `rg` for the request you are changing. Non-HTTP steps (evict Redis, worker, user switch)
  go in the README as manual steps. Never put secrets, tenant connections, or real record IDs in the
  collection or the committed environment template.
- Adapters over existing Axpert behavior: before planning, find the existing working caller of that behavior
  (e.g. the AxPut/AxGet path in AxTstructData) and include a "Parity trace" in the plan: every state
  initialization, variable registration, and call order that caller performs before the operation. Any step
  the adapter deliberately skips must be listed with a reason.
- Bug fixes: the plan must state the root cause with evidence, and the first thing compared must be the
  working legacy path for the same operation. Do not propose a change under `ARM-11.4/` until adapter
  parity has been ruled out. If two explanations fit, list both and say which check separates them.
- End of a task: append at most 5 lines to `docs/PROGRESS.md` (done / decisions / next, plus
  `Postman: updated | not applicable`) and update `docs/CODEMAP.md` for new or moved types
  (one line each: path, purpose).

## Hard rules

- Repo layout: `AxData/` is the new solution. `ARM-11.4/` holds the existing projects (AxValidate, AxStructDef,
  AxParser, AxTstructData, AxDataObject, Common, ...). `AxDataObject` is the old, incomplete AxData implementation
  that this project replaces — read it, don't build on it.
- You may read anywhere in `ARM-11.4/` to understand existing behavior. You may NOT write to anything outside
  `AxData/` unless I've explicitly approved that specific change.
- Every explicitly approved change under `ARM-11.4/` must be recorded in `docs/ARM-11.4-CHANGES.md` in the
  same task, including the changed files, reason, behavioral impact, and verification. Keep this ledger current
  for all future cross-project changes.
- A plan that touches anything outside `AxData/` must have its own "Cross-project change" section: which
  project/file, why a port/adapter in `AxData` can't do this instead, and the blast radius (what else calls the
  code you'd change). Wait for my explicit go-ahead on that section specifically, even after the rest of the plan
  is approved. Default assumption: prefer an adapter in `AxData` over touching shared code.
- Treat AxStructDef/AxValidate/AxParser/Common as especially high blast-radius (AxTstructData and others depend
  on them) — proposing a change there needs a clearly stated reason a port can't work, not just convenience.
- Never read a computed/parsed value without checking that operation's success result first (AxParser in
  the existing code swallows exceptions and returns false while some callers still read the stale value —
  do not reproduce that pattern anywhere in AxData).
- Axpert behaviour (StructDef, Validate, Parser, AxGet, AxPut, PEG routines) is reached ONLY through ports in
  `AxData.Application`, implemented in `AxData.Integration.Axpert`. Until real adapters exist, use in-memory fakes.
- Dependency direction: Api -> Application -> Domain. Domain and Application must not reference Redis, SQL providers,
  queue SDKs, or concrete Axpert classes.
- ObjectId is a reference, never a credential. Every ObjectId operation follows this order:
  authenticate -> resolve tenant -> load by tenant-scoped key `AXD:{tenantId}:{objectId}` -> check owner / current
  approver / admin -> apply transaction/DC/field permission -> execute.
- Hidden DCs/fields are omitted from responses, never masked.
- Mutations use a short lock plus a version compare-and-set. A stale version returns 409 with the current version.
  Never overwrite silently.
- Save is idempotent and transactional. PEG approval is NOT a business Save. Follow `docs/architecture/09-peg-lifecycle.md`
  for SaveOnApprove exactly.
- Clients can never send an internal bypass flag. Bypass is trusted server code only.
- Never log field values, secrets, or tokens. Log safe identifiers only (tenant, TransId, ObjectId, version, trace id).
- No secrets in code or committed config. Use environment variables or user-secrets.
- Do not add NuGet packages without asking. Pin versions (central package management).

## Conventions

- .NET version: 8.0
- Nullable enabled. Async everywhere with CancellationToken. Sealed classes by default. LF line endings.
- Security tests from `docs/technical/21-testing.md` (21.4) are required for any authorization change.
