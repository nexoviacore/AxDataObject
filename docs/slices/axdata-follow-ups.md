# AxData follow-up register

This is the project-wide register for agreed reviews, optimizations, known gaps, and deliberately retained behavior.
It is not limited to Init. Scheduled feature work remains in the [slice roadmap](README.md); this file tracks work that
crosses slices or could otherwise be mistaken for unfinished cleanup.

## Status rules

- **Pending**: agreed work or investigation that still needs its own approved plan.
- **Deferred**: intentionally postponed until its stated prerequisite or owning slice.
- **Retained**: reviewed and kept by design; do not reopen without new evidence.
- **Completed**: implemented and recorded here to prevent the same concern being raised as pending.

Update this register when a review changes status. Record behavioral decisions in `docs/DECISIONS.md`, implementation
results in `docs/PROGRESS.md`, and any approved `ARM-11.4/` edits in `docs/ARM-11.4-CHANGES.md`.

## Pending

| ID | Area | Follow-up | Why it remains | Completion evidence |
| --- | --- | --- | --- | --- |
| F-01 | Cross-flow | Complete AXD-STATE-06 final regression and documentation closeout after AXD-SET-01. | The state redesign implementations are complete, but Set reconstruction/authorization consolidation must land before their final cross-flow review. | AXD-SET-01 and AXD-STATE-06 results in `docs/PROGRESS.md`. |
| F-02 | Persistence | Decide whether to delete the dormant snapshot transport, SQL snapshot, recovery service, and worker code or keep it explicitly dormant. | D37 disabled and unwired durable recovery; unused source still increases maintenance surface. Removal is a separate broad change and must not imply that Redis has a fallback. | Decision plus focused build/tests; update architecture and slice documents. |
| F-03 | Verification | Resolve the two documented parser-guard contract failures. | Recent full contract runs consistently pass all other contracts while these two remain excluded/failing. Their root cause and intended parity behavior need a bounded review. | Both contracts pass without exclusion, or an approved decision records why their expectations change. |
| F-04 | Init adapters | Review whether the M1/in-memory port path can construct its final state directly instead of using temporary state plus `AcceptInitializedBaseline`. | Production Init no longer uses the temporary construction path; the remaining path is test/development composition and should change only if it becomes simpler without weakening its contract coverage. | Focused Init unit/contract tests and updated test-support design. |
| F-05 | StructDef boundary | Review the neutral `StructDef`/handle contract used outside production Init and remove projections that no active Application consumer needs. | AXD-SET-01 owns the Set consumer. Remaining neutral ports still require consumer-by-consumer evidence rather than global deletion. | AXD-SET-01 result plus usage audit and affected adapter tests for remaining consumers. |
| F-06 | Runtime composition | Audit `AxpertRuntimeContextFactory` and `ProductionAxpertRuntimeContextFactory` for genuinely duplicated logic. | The factories currently represent different trust/session sources. Share only proven common mechanics; do not merge review/development identity with production authentication. | Composition tests demonstrate unchanged trust boundaries. |
| F-07 | Request context | Audit `DevelopmentContext` and app/user/session consistency checks for redundancy. | Authentication and live-session consistency are required, but development-only composition and duplicate checks may be simplified after call-path evidence. | Security tests required by `docs/technical/21-testing.md` plus production/review composition tests. |
| F-08 | Date format | Verify whether `AxClient_DateFormat` can always be derived from `common.ApiDateFormat` before removing request/client handling. | This is legacy-parity behavior and must be checked against AxTstructData and live adapter expectations before simplification. | Parity evidence and affected adapter/contract tests. |
| F-09 | Redis-only operation | Add explicit capacity, persistence, eviction, backup, and state-loss operating guidance for persistent active keys. | D49 correctly removed active TTL while no durable fallback exists, but persistent Redis is now the only active copy and needs an operational contract. | Owning hardening/runbook slice documents deployment requirements and failure behavior. |
| F-10 | Save retries | Decide and implement the future `Idempotency-Key` contract if automatic/client retries are required. | D41 intentionally leaves this unimplemented; a timed-out AxPut has an unknown commit outcome and must not currently be retried automatically. | Approved API/storage design, security/concurrency tests, and Postman coverage. |

## Deferred to the slice roadmap

| Area | Owner | Reason |
| --- | --- | --- |
| Set state reconstruction and authorization consolidation | AXD-SET-01 | The approved Set flow, single-runtime work, administrator removal, and AxPut alignment now have a dedicated ordered slice. |
| PEG lifecycle and recovery | P3-01 through P3-10 | Already decomposed into ordered feature slices with authorization and consistency requirements. |
| Files, print, notifications, integrations, and audit | P4-01 through P4-07 | Future feature work, not cleanup debt. |
| Metrics, eviction policy, load/security testing, and deployment runbooks | P5-01 through P5-06 | Hardening work belongs in the existing roadmap; F-09 must be addressed there if not earlier. |

## Retained by design

| Area | Retained behavior | Why |
| --- | --- | --- |
| Cancellation | Pass `CancellationToken` to cancellable calls and keep explicit checkpoints before CPU-only or non-token-aware work. | Passing a token does not itself stop the parent method; each operation must observe it. |
| Legacy commands | Keep `.ToCommand()` where legacy Axpert APIs require their command representation. | It is an adapter conversion, not redundant async plumbing. |
| Authentication | Authenticate, resolve the live tenant/session, and enforce required identity consistency before state access. | ObjectId is never a credential; removing these checks would weaken the required trust boundary. |
| Authorization | Keep transaction permission and existing-record `CheckTstructViewAccess` checks. | General permission and record/view access answer different authorization questions. |
| Concurrency | Keep Set/FillGrid process gates where legacy shared-state evidence requires them, and keep short token-owned Redis leases for per-object exclusion. | Init request-local runtime evidence does not prove that other legacy paths are safe without their guards. Coordination-key TTLs are unrelated to the removed active-state TTL. |
| Validation | Keep fail-closed checks for malformed StructDef/payload data, unknown containers, field order/flags, and source-key metadata. | Constructor initialization does not prove that external or legacy-produced content is valid. |
| State shape | Keep compact field metadata beside row values. | Field name/flags describe the schema needed by Query/Get; row values are instance data, not duplicates. |
| Baseline | Accept successful Init/on-load output as the clean baseline once. | Later Set changes need a stable baseline for dirty tracking and revert behavior; temporary production hydration/readback is not required. |
| Async style | Do not add `ConfigureAwait(false)` mechanically in AxData. | It is unnecessary for correctness in the current ASP.NET Core flow; remove isolated no-value uses only as part of touched code. |

## Completed

| Area | Result | Record |
| --- | --- | --- |
| Real Init StructDef flow | One mutable legacy StructDef load is used directly; hashes, revision DTOs, projected cache, and preflight reloads were removed. | D48 and `docs/PROGRESS.md` entries dated 2026-11-19. |
| Real Init state construction | AxGet and on-load processing finish before one final clean DataObject is built; production temporary hydration/readback and stage tracking were removed. | D48 and the Init completion entries in `docs/PROGRESS.md`. |
| Active-state lifetime | Active DataObject TTL, `expiresAt`, and AxPut PTTL capture/restore were removed; active keys persist until explicit lifecycle removal. | D49 and the persistent-state entry in `docs/PROGRESS.md`. |
| Redis coordination | Short lease TTLs remain only for bounded coordination/locking. | D41 and D49. |

