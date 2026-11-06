# AxData: agent instructions

Headless, metadata-driven transaction runtime (AxData Object) plus a PEG workflow module, as a NEW .NET solution.
Environment: Windows + PowerShell. Use PowerShell syntax; do not assume bash tools.

## Workflow

- For work named by a block in `docs/slices/`, use the repository skill `$axdata-slice`; it owns the selective-reading,
  legacy-parity, Postman, verification, and completion workflow.
- For every change, plan first: list files and assumptions, identify any change outside `AxData/`, and wait for approval.
- Stay in scope, keep diffs small, and add or update tests with every behavior change.
- Build and test quietly without restore. Ask before any restore, network command, or new NuGet package.
- On failure, report only the first error and its cause.

## Hard rules

- Repo layout: `AxData/` is the new solution. `ARM-11.4/` holds the existing projects (AxValidate, AxStructDef,
  AxParser, AxTstructData, Common, ...). The old, incomplete `AxDataObject` project was retired after P0-04;
  use `docs/SALVAGE.md`, cached parity contracts, and Git history instead of restoring or rereading it.
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
- AxData baseline/diagnostic logs never contain field values, bodies, secrets, or tokens; log safe identifiers only
  (tenant, TransId, ObjectId, version, trace id). This restriction is unchanged outside `ARM-11.4`. Inside
  `ARM-11.4` only, ObjectId-aware AxPut Save preserves the current AxTstructData trace behavior and may add
  operation-relevant trace messages containing DataObject state, request/result content, session identifiers,
  and field values. Never extend this exception to AxData or workers, and do not add tokens, connection strings,
  or unrelated secrets to new trace messages.
- No secrets in code or committed config. Use environment variables or user-secrets.
- Pin NuGet versions through central package management.

## Conventions

- .NET version: 8.0
- Nullable enabled. Async everywhere with CancellationToken. Sealed classes by default. LF line endings.
- Security tests from `docs/technical/21-testing.md` (21.4) are required for any authorization change.
