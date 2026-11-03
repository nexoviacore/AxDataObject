# ARM dependency workstream

This workstream restores dependency-driven field refresh and validation in the existing ARM-11.4 flow before AxData adopts it. It is deliberately separate from `docs/slices/`; do not add these tasks to the AxData phase order or combine them with an active AxData slice.

One task equals one Codex chat. Run ARM tasks in order. Start the AxData adoption handoff only after ARM-DEP-04 is accepted.

## Working rules

- At the start of a task, read `docs/PROGRESS.md` and only the named block in `docs/arm-tasks/arm-dependency-workstream.md`.
- Read only the additional documentation and source locations named by that block.
- Plan first, list exact files and assumptions, and wait for approval before editing.
- Every ARM implementation task has an explicitly approved cross-project scope, but its plan must still restate the files, reason, and blast radius.
- Record every ARM-11.4 change in `docs/ARM-11.4-CHANGES.md` in the same task.
- Do not add NuGet packages or run restore/network commands without approval.
- Build and test quietly and report only the first failure and its cause.
- Existing ARM dependency/validation trace payloads are not subject to AxData's no-field-value logging rule. Log redaction is not part of this workstream.
- Postman is not applicable unless a task unexpectedly changes an HTTP endpoint, response shape, or error contract.

## Task order

| ID | Purpose | Needs | Effort |
| --- | --- | --- | --- |
| ARM-DEP-00 | Characterize parity and establish the ARM test harness | — | Standard |
| ARM-DEP-01 | Parse and model `a58` dependency metadata in AxStructDef | ARM-DEP-00 | Deep |
| ARM-DEP-02 | Correct Validate state, SQL-result cache lifetime, and result contracts | ARM-DEP-01 | Deep |
| ARM-DEP-03 | Add dependency-directed refresh and validation execution | ARM-DEP-02 | Deep |
| ARM-DEP-04 | Integrate AxPut create/edit/GenMap and run ARM regression | ARM-DEP-03 | Deep |

## Paste-ready prompts

Replace `<ID>` with one task ID and use a fresh chat:

```text
Task: <ID>
Read docs/PROGRESS.md and only the <ID> block in docs/arm-tasks/arm-dependency-workstream.md, then only the files and references that block lists.
This is an ARM-first task. Do not change AxData product code or the existing AxData slice files.
Plan first with root cause/parity evidence, exact files, assumptions, tests, and the Cross-project change section. Wait for my approval before editing.
Existing ARM field-value logging is out of scope; do not apply the AxData-only log-redaction rule.
```

After ARM-DEP-04 is accepted, use the prompts in `docs/arm-tasks/axdata-adoption-handoff.md` for the separately scheduled AxData work.
