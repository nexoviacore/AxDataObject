# Phase 4: Files and post-processing

Paths under **Read** are relative to `docs/`. Effort levels as in phase-0-prep.md.
P4-01, P4-02, P4-03 touch different code and can run in parallel threads if you want.

### P4-01 File references and validation

Effort: Standard | After: P1-01
Read: technical/15-files-afl.md, architecture/12-security.md, contracts/axpert-notes.md §6.1: attachment handling runs before the final commit, outside the DB transaction; folder files persist after a DB rollback (including ValidateOnly); the attachment result is ignored by SaveData. Design file Save around this.
Build: `AxFileReference` tokens (no file bodies in Redis); validate tenant, object, field, type, size, path traversal, allowed roots; strict inline limit.
Done when: path traversal, oversize, wrong type and another tenant's token are all rejected.

### P4-02 AFL adapter

Effort: Deep (secrets) | After: P4-01
Read: technical/15-files-afl.md (15.2), architecture/12-security.md
Build: resolve an authorized AFL name; credentials from the secret provider, server-side only.
Done when: a leak test serializes state, logs and queue messages and finds no credential.

### P4-03 Secure downloads

Effort: Standard | After: P4-01
Read: technical/15-files-afl.md (15.2), architecture/12-security.md
Build: short-lived signed or authenticated links scoped to one artifact, with a tenant + ObjectId check.
Done when: expired link, wrong tenant and a different artifact are all denied.

### P4-04 Print request API and event

Effort: Standard | After: P2-03, P4-03 | Needs: D2, D9
Read: technical/06.7-print.md, architecture/08-lifecycle.md (8.5)
Build: `POST .../print` (idempotent); authorization by ObjectId plus explicit print permission (approvers denied by default); enqueue a print event; response shape per D9.
Done when: API tests with fakes; unauthorized user denied.

### P4-05 Print worker and Delphi print-service adapter

Effort: Standard | After: P4-04
Read: technical/06.7-print.md, architecture/02-existing-capabilities.md
Build: worker consuming print events; HTTP adapter behind a port to the Delphi service; artifact stored and linked through P4-03.
Done when: contract tests with a recorded sample; retry and dedupe work; failure is dead-lettered.

### P4-06 Notification and integration workers

Effort: Standard | After: P2-03
Read: technical/13-database.md (13.3), architecture/07.2-peg-module.md
Build: reuse the P2-03 dispatcher; sinks for notifications and integration messages; idempotent delivery.
Done when: duplicate events deliver once; permanent failures dead-lettered.

### P4-07 Audit trail

Effort: Standard | After: P3-05
Read: architecture/12-security.md, architecture/14-observability.md
Build: append-only audit for security-sensitive actions and PEG transitions. Never field values.
Done when: each PEG transition and each denied access writes an audit record.

## Milestone M4 (you)

Scenario 3 end to end including print and notification.
