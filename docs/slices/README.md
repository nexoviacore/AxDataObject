# Slices

One slice = one Codex thread. Details for each slice are in the phase file; Codex reads only its own block.

## Run a slice

1. Check `docs/DECISIONS.md`: every decision in the slice's **Needs** must have an answer.
2. Start a new thread on the Local project (fresh context). Set the model and reasoning level for the slice's **Effort**:
   Light = smaller model, low reasoning; Standard = default; Deep = strongest model, high reasoning.
3. Paste:

```
Task: P1C-05 AxData-native FillGrid
Read docs/PROGRESS.md and the P1C-05 block in docs/slices/phase-1c-real-data-review.md, then the files that block lists, plus tools/postman/README.md.
If this slice touches an endpoint, update the Postman collection per the README rule.
Read docs/DECISIONS.md or docs/contracts/axpert-notes.md or docs/SALVAGE.md if required.
Plan first, list assumptions, wait for my OK.
```

4. Review the plan, approve, let Codex build and run unit tests. Run integration tests yourself when the slice says so.
5. Review the diff in the app's diff pane, commit, and confirm `docs/PROGRESS.md` and `docs/CODEMAP.md` were updated. If the slice changes or adds an HTTP endpoint or response shape, update tools/postman/ in the same slice: add or change the requests in the current milestone folder, with status and AXD-code assertions.
6. Close the thread. Next slice, new thread.

Run slices in order within a phase unless the phase file says they can run in parallel.
A slice should stay under about 10 changed files. If Codex's plan is bigger, split it before approving.
If the slice changes or adds an HTTP endpoint or response shape, update tools/postman/ in the same slice: add or change the requests in the current milestone folder, with status and AXD-code assertions.

## All slices

| ID     | Slice                                                    | Effort   | Needs              | File                             |
| ------ | -------------------------------------------------------- | -------- | ------------------ | -------------------------------- |
| P0-01  | Decisions (you, no Codex)                                | you      |                    | phase-0-prep.md                  |
| P0-02  | Solution skeleton and guard rails                        | Light    |                    | phase-0-prep.md                  |
| P0-03  | Axpert discovery spike (you, no Codex)                   | you      |                    | phase-0-prep.md                  |
| P0-04  | Salvage assessment (report only, no new code)            | Standard |                    | phase-0-prep.md                  |
| P1-01  | Domain model, lifecycle, error catalog                   | Standard |                    | phase-1-foundation.md            |
| P1-02  | Snapshot serialization                                   | Standard | D8                 | phase-1-foundation.md            |
| P1-03  | Ports, in-memory fakes, sample fixture                   | Standard |                    | phase-1-foundation.md            |
| P1-04  | Redis store: atomic compare-and-set and TTL              | Deep     | D11                | phase-1-foundation.md            |
| P1-05  | Redis short lock (token-owned lease)                     | Deep     |                    | phase-1-foundation.md            |
| P1-06  | API conventions and cross-cutting                        | Standard | D4                 | phase-1-foundation.md            |
| P1-07  | Authorization policy (security tests first)              | Deep     |                    | phase-1-foundation.md            |
| P1-08  | Init                                                     | Standard | D4                 | phase-1-foundation.md            |
| P1-09  | Get / query                                              | Standard |                    | phase-1-foundation.md            |
| P1-10  | Set A: request handling, row actions, versioning         | Standard | D4, D7             | phase-1-foundation.md            |
| P1-11  | Set B: validate, dependencies, error collection          | Deep     | D3                 | phase-1-foundation.md            |
| P1-12  | Set C: FillGrid                                          | Standard |                    | phase-1-foundation.md            |
| P1-13  | Snapshot table and version-guarded upsert                | Deep     | D11                | phase-1-foundation.md            |
| P1-14  | Snapshot publish/consume and recovery on Redis miss      | Deep     | D5, D8             | phase-1-foundation.md            |
| P1-15  | Local composition and M1 real-infrastructure walkthrough | Standard |                    | phase-1-foundation.md            |
| P1B-01 | StructDef provider, cache, AxGet loader                  | Standard | D12                | phase-1b-real-read-adapters.md   |
| P1B-02 | Field engine adapter (Validate and Parser)               | Deep     |                    | phase-1b-real-read-adapters.md   |
| P1B-03 | FillGrid adapter                                         | Standard |                    | phase-1b-real-read-adapters.md   |
| P1C-00 | Compatibility gate (done)                                | Standard | D8, D25-27         | phase-1c-real-data-review.md     |
| P1C-01 | Real StructDef/runtime source, smoke harness (done)      | Standard | D8, D26            | phase-1c-real-data-review.md     |
| P1C-02 | Real authorization (done)                                | Deep     | D12, D19           | phase-1c-real-data-review.md     |
| P1C-03 | Real-data review composition                             | Standard | D25, D27           | phase-1c-real-data-review.md     |
| P1C-04 | HTTP walkthrough and report                              | Standard | D27, D28           | phase-1c-real-data-review.md     |
| P1C-05 | AxData-native FillGrid                                   | Deep     | D24                | phase-1c-real-data-review.md     |
| P1D-01 | Production authentication and composition                | Deep     |                    | phase-1d-readiness-gate.md       |
| P1D-02 | Deterministic build, dependency/secret audit             | Standard |                    | phase-1d-readiness-gate.md       |
| P1D-03 | E2E automation and Redis 6.2+ verification               | Standard | D31                | phase-1d-readiness-gate.md       |
| P1D-04 | DB provider matrix                                       | Standard |                    | phase-1d-readiness-gate.md       |
| P1D-05 | Field-engine performance                                 | Standard |                    | phase-1d-readiness-gate.md       |
| P1D-06 | ARM change regression                                    | Light    |                    | phase-1d-readiness-gate.md       |
| P2-01  | Idempotency store                                        | Deep     | D11                | phase-2-durable-save.md          |
| P2-02  | Outbox table and transactional writer                    | Standard | D2                 | phase-2-durable-save.md          |
| P2-03  | Outbox dispatcher worker                                 | Deep     |                    | phase-2-durable-save.md          |
| P2-04  | Unit of work and Save orchestrator (no-PEG path)         | Deep     | D10, D21, D28, D30 | phase-2-durable-save.md          |
| P2-04b | Reconciliation worker                                    | Deep     | D10, D21, D28, D30 | phase-2-durable-save.md          |
| P2-05  | Save endpoint and bulk save                              | Standard |                    | phase-2-durable-save.md          |
| P2-06  | Real transaction writer (AxPut, child, GenMap, MdMap)    | Deep     | D10                | phase-2-durable-save.md          |
| P3-01  | PEG state machine (pure logic)                           | Deep     |                    | phase-3-peg.md                   |
| P3-02  | PEG active state in Redis, versioned                     | Deep     |                    | phase-3-peg.md                   |
| P3-03  | PEG authorization (security tests first)                 | Deep     |                    | phase-3-peg.md                   |
| P3-04  | Submit                                                   | Standard |                    | phase-3-peg.md                   |
| P3-05  | Approve and Reject                                       | Deep     |                    | phase-3-peg.md                   |
| P3-06  | Return and Recall                                        | Standard |                    | phase-3-peg.md                   |
| P3-07  | Save state matrix and SaveOnApprove = false              | Deep     |                    | phase-3-peg.md                   |
| P3-08  | SaveOnApprove = true final approval                      | Deep     |                    | phase-3-peg.md                   |
| P3-09  | PEG history adapter and Redis/DB consistency             | Deep     | D6                 | phase-3-peg.md                   |
| P3-10  | PEG recovery                                             | Deep     |                    | phase-3-peg.md                   |
| P4-01  | File references and validation                           | Standard |                    | phase-4-files-post-processing.md |
| P4-02  | AFL adapter                                              | Deep     |                    | phase-4-files-post-processing.md |
| P4-03  | Secure downloads                                         | Standard |                    | phase-4-files-post-processing.md |
| P4-04  | Print request API and event                              | Standard | D2, D9             | phase-4-files-post-processing.md |
| P4-05  | Print worker and Delphi print-service adapter            | Standard |                    | phase-4-files-post-processing.md |
| P4-06  | Notification and integration workers                     | Standard |                    | phase-4-files-post-processing.md |
| P4-07  | Audit trail                                              | Standard |                    | phase-4-files-post-processing.md |
| P5-01  | Observability: metrics, traces, health checks            | Standard |                    | phase-5-hardening.md             |
| P5-02  | Eviction, TTL, retention, cleanup                        | Deep     |                    | phase-5-hardening.md             |
| P5-03  | Concurrency and failure tests                            | Standard |                    | phase-5-hardening.md             |
| P5-04  | Load test and size measurement                           | Standard |                    | phase-5-hardening.md             |
| P5-05  | Security suite and dependency/secret audit               | Deep     |                    | phase-5-hardening.md             |
| P5-06  | Deployment config and runbooks                           | Light    |                    | phase-5-hardening.md             |

Milestones (you review, no Codex): M1 after P1-15, M1.5 after P1C-05, M1.6 after P1D-06, M2 after P2-06, M3 after P3-10, M4 after P4-07.
