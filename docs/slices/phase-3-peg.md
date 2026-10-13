# Phase 3: PEG workflow

D14 decided: v1 scope is exactly what the slices below cover (Initiate/Approve/Reject/Return/Recall).
Forward/Skip/AmendmentApproved/AmendmentDiscarded/Checked are backlog — do not design or build them here;
they get their own slices after v1 ships.

Paths under **Read** are relative to `docs/`. Effort levels as in phase-0-prep.md.
D6 (PEG durability) must be answered before P3-09. P3-01 to P3-08 use a faked history repository.

### P3-01 PEG state machine (pure logic)
Effort: Deep | After: P1-01
Read: architecture/09-peg-lifecycle.md, technical/04-domain-model.md (4.2), technical/12-peg-consistency.md (12.1)
Scope: src/AxData.Peg
Build: transitions for Initiate, Approve (advance level or final), Reject, Return, Recall; invalid transitions return AXD-PEG; workflow version increments; approvers modeled as a collection; approver resolution through a port (fake).
Done when: exhaustive transition tests including multi-level.

### P3-02 PEG active state in Redis, versioned
Effort: Deep | After: P3-01, P1-04
Read: technical/12-peg-consistency.md (12.1), technical/08-redis.md (8.1)
Build: `IPegActiveStateStore` with the PEG state embedded in the AxData document (a single representation; record that choice in DECISIONS.md); compare-and-set on `workflowVersion`; AxData and PEG talk only through contracts.
Done when (you run): stale workflow version returns 409; two concurrent approvals means exactly one wins.

### P3-03 PEG authorization (security tests first)
Effort: Deep, line-by-line review | After: P3-02, P1-07
Read: technical/07-authorization.md, technical/06.8-peg-tasks.md, architecture/12-security.md
Do first: tests: approve an unassigned task; cross-tenant task; a client-sent bypass flag.
Build: task-based access (ObjectId alone never selects a task); approver gets Get and assigned-task actions only; recall by initiator or admin; the internal final-approval Save bypass reachable only from trusted server code.
Done when: all the tests above pass.

### P3-04 Submit
Effort: Standard | After: P3-03, P2-05
Read: technical/06.5-submit.md, architecture/09-peg-lifecycle.md (9.1)
Build: `POST .../submit`; PEG applicability through a port; initiate; object becomes PendingApproval; idempotent; kept distinct from PersistBusinessData.
Done when: the same key twice returns the same result; not-applicable returns a clear error.

### P3-05 Approve and Reject
Effort: Deep | After: P3-04
Read: technical/06.8-peg-tasks.md, technical/12-peg-consistency.md (12.1), architecture/09-peg-lifecycle.md (9.2, 9.4)
Build: endpoints; checks: user authorized, task open at the expected level, `expectedWorkflowVersion`, idempotency; intermediate approval advances the level; reject sets Rejected; history via the fake repository; notifications as outbox events.
Done when: a duplicate action with different input returns a conflict; multi-level approval reaches the final level.

### P3-06 Return and Recall
Effort: Standard | After: P3-05
Read: architecture/09-peg-lifecycle.md (9.5, 9.6), technical/06.8-peg-tasks.md
Done when: return to a valid target and resubmit; recall by initiator while pending; recall refused after approval.

### P3-07 Save state matrix and SaveOnApprove = false
Effort: Deep | After: P3-05, P2-05
Read: technical/06.6-save.md (matrix), technical/12-peg-consistency.md (12.3), architecture/09-peg-lifecycle.md (9.3, the false branch)
Build: every row of the matrix; final approval (false) marks the workflow Approved and the object ApprovedAwaitingSave, no writer call; a later explicit Save moves it to Saved and keeps the PEG state.
Done when: scenario 5 and the false branch of scenario 7 pass; approval and Save use different idempotency keys.

### P3-08 SaveOnApprove = true final approval
Effort: Deep (highest risk), line-by-line review | After: P3-07
Read: technical/12-peg-consistency.md (12.2), architecture/09-peg-lifecycle.md (9.3, the true branch), technical/11-save-orchestration.md
Build: the PEG service calls the trusted internal Save (skips permission and PEG-initiation checks only, never validation or idempotency); success then Approved; failure then not Approved with a recoverable `FinalApprovalPendingSave` state; retry is idempotent.
Done when: scenarios 3, 4 and the true branch of 7 pass; fault injection between the Save commit and the PEG update leaves no untracked saved record and no false Approved.

### P3-09 PEG history adapter and Redis/DB consistency
Effort: Deep | After: P3-08, P0-03 | Needs: D6
Read: contracts/axpert-notes.md, technical/12-peg-consistency.md (12.1, 12.4), technical/13-database.md (13.4)
Build: `IPegHistoryRepository` over `axactivetasks` / `axactivetaskstatus` as decided in D6; the write pattern chosen from 12.4; no new columns unless a schema gap analysis is approved.
Done when: contract tests match existing PEG behavior; a critical transition is never reported successful before the chosen durability point.

### P3-10 PEG recovery
Effort: Deep | After: P3-09, P1-14
Read: architecture/10-state-persistence.md (10.4), technical/12-peg-consistency.md
Build: restore PEG state alongside the snapshot after Redis loss; ApprovedAwaitingSave is preserved.
Done when: scenario 11 passes.

## Milestone M3 (you)
Scenarios 3 to 8 and 11 pass.

## Backlog (post-v1)
Forward, Skip, AmendmentApproved, AmendmentDiscarded, Checked — real PEG states with no slice yet (D14).
Scope these into new P3B-* slices once v1 ships: read the real transition/authorization rules for each from
`StoreDataRepository.cs`/`AxPegActionsService.cs` before writing them, the same way P3-01/P3-05 did for the
v1 states.
