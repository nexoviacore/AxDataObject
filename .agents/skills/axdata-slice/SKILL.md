---
name: axdata-slice
description: Implement, review, diagnose, or complete an AxData task identified by a block in docs/slices. Use for named AxData phase/slice work; do not use for unrelated repository maintenance.
---

# AxData slice workflow

Keep the slice bounded and make repository context progressive rather than loading all design material.

## Discovery and plan

1. Read `docs/PROGRESS.md` and only the named block in the requested `docs/slices/` file.
2. Read only files that block explicitly lists. Use `docs/CODEMAP.md` to navigate `AxData/`; do not open unrelated design files.
3. If the work overlaps legacy Axpert behavior, an adapter, or a bug in an equivalent operation, read
   [references/axpert-parity.md](references/axpert-parity.md).
4. If it changes an HTTP endpoint, request/response shape, or AXD error code, read
   [references/http-postman.md](references/http-postman.md).
5. Read `docs/DECISIONS.md`, `docs/contracts/axpert-notes.md`, or `docs/SALVAGE.md` only when the slice or a conditional
   reference requires it. Stop for any needed open decision or "to be confirmed" item.
6. Before editing, present the files to create/change, assumptions, conditional requirements found above, and the
   `AGENTS.md` cross-project section for anything outside `AxData/`. Wait for explicit approval.

## Implementation and verification

- Preserve dependency direction and the authorization, concurrency, logging, and secret-handling rules in `AGENTS.md`.
- Prefer the smallest slice-complete diff. Do not refactor adjacent code.
- Add or update tests with every behavior change.
- Start with affected builds and focused tests. Run full suites only when the slice requires them, risk crosses a broad
  boundary, focused results expose wider impact, or the user requests them.
- Use `scripts/Invoke-SliceVerification.ps1` for repeatable local checks. It never restores packages and suppresses
  successful command output. Pass explicit projects and filters; do not invent a broad verification scope.

## Completion

- Update the Postman review pack when the HTTP reference applies.
- Append at most five lines to `docs/PROGRESS.md`: done, decisions, tests, next, and `Postman: updated | not applicable`.
- Add one `docs/CODEMAP.md` line for each new or moved type.
- Record every approved `ARM-11.4/` edit in `docs/ARM-11.4-CHANGES.md` with files, reason, impact, and verification.
- Report focused and full-suite results separately, including pre-existing failures. Do not claim checks that were not run.
