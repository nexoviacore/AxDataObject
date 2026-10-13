# Phase 2: Durable Save

Paths under **Read** are relative to `docs/`. Effort levels as in phase-0-prep.md. DB integration tests: you run.

### P2-01 Idempotency store

Effort: Deep | After: P0-02 | Needs: D11
Read: technical/11-save-orchestration.md (11.1), technical/13-database.md (13.2)
Build: table and repository; request hash; states InProgress / Completed / FailedRetryable / FailedFinal / NeedsReview; lease with takeover of an expired InProgress (takeover only marks it for reconciliation, never re-runs the writer); same key with a different hash returns AXD-IDEMP. The lease duration must exceed the writer's command timeout plus a margin (D30); expiry only makes the entry eligible for reconciliation.
Done when: unit tests for hashing and conflict; DB test: concurrent insert leaves exactly one InProgress; an expired lease is reclaimable for reconciliation.

### P2-02 Outbox table and transactional writer

Effort: Standard | After: P2-01 | Needs: D2 (answered: outbox)
Read: technical/13-database.md (13.3), technical/11-save-orchestration.md (11.3), architecture/11-reliability.md
Build: `AxOutbox` table; a writer that joins AxData's own local transaction (never AxPut's); deduplication key.
Done when: a rollback of the local transaction removes the outbox rows.

### P2-03 Outbox dispatcher worker

Effort: Deep | After: P2-02
Read: technical/13-database.md (13.3), architecture/13-scalability.md
Scope: src/AxData.Workers
Build: claim bounded batches with DB-safe locking; deliver through a sink port; retry with backoff; deduplicate; dead-letter or review state; sinks faked.
Done when: two workers never deliver the same row; a failed delivery is retried; a poison message is dead-lettered.

### P2-04 Save orchestrator as external saga (no-PEG path)

Effort: Deep, line-by-line review | After: P2-02, P1-14, P1D-02 | Needs: D10, D21, D28, D30
Read: technical/11-save-orchestration.md, technical/06.6-save.md (first row of the matrix only), architecture/11-reliability.md
Build, in this order (D21), against a FAKE writer with fault injection: 0. Check the StructDef hash (D28); mismatch fails closed before any write.

1. CAS the object lifecycle to Saving (blocks concurrent Set). Do not hold a lock lease across the writer call.
2. For NEW records, reserve the main record ID (D30) through the Integration.Axpert adapter that calls StructDef.GetRecordIdsFromDB on a writable connection (site number from the live session). Store it as Int64. Then Local transaction: reserve idempotency InProgress with the reserved ID; same hash Completed returns the stored result; different hash conflicts. Commit.
3. Final validation, then call the writer with a bounded command timeout (D10), passing the reserved ID as RecordId.
4. Second local transaction: store outcome (including RecordId mappings for tmp: rows, D7), insert the outbox row, mark Completed. Commit.
5. Update Redis (RecordId, status, version).
   Done when: a duplicate Save returns the stored result and the writer ran once; a writer failure leaves no outbox row and no Completed; a crash after step 3 and before step 4 leaves InProgress and is picked up by P2-04b; a fault after step 4 and before step 5 is recoverable; StructDef hash mismatch is rejected. retry after a timeout reuses the same reserved ID.

### P2-04b Reconciliation worker (new)

Effort: Deep | After: P2-04 | Needs: D21, D30, P2-06 spike
Read: DECISIONS.md D21 and D30, contracts/axpert-notes.md
Scope: src/AxData.Workers
Build: find stale InProgress entries past the lease. NEW records: run the existence probe (system context, read-only, boolean); present -> complete the entry (outcome + outbox + Completed); authoritatively absent -> FailedRetryable (retry reuses the reserved ID); error/ambiguous -> NeedsReview. EDIT/DELETE entries -> NeedsReview, never retried. Never blind-retry the writer.
Done when: fake tests cover committed-but-unrecorded, never-committed, in-flight (lease not yet expired), probe failure, and edit cases; two workers never reconcile the same entry; no user session is needed.
Done when: fake-writer tests cover committed-but-unrecorded, never-committed, and unprovable cases; two workers never reconcile the same entry.

### P2-05 Save endpoint and bulk save

Effort: Standard | After: P2-04b, P1D-01, P1D-03
Read: technical/06.6-save.md, technical/11-save-orchestration.md (11.1 key selection)
Build: `POST .../save` with expected version and Idempotency-Key; `responseFields`; bulk key from tenant + ObjectId + CallerId + Sequence; each bulk item is its own saga with a per-item result (there is no all-or-nothing, because AxPut commits per record); PEG rows of the matrix return the defined "not allowed yet" error until Phase 3.
Done when: end-to-end scenarios 1, 9 and 12 from technical/21-testing.md pass with fakes; a bulk call with one failing item returns mixed results.

### P2-06 Real transaction writer (AxPut, child, GenMap, MdMap)

Effort: Deep | After: P2-04, P0-03, P1B-01 | Needs: D10, D20, D30
Read: contracts/axpert-notes.md, technical/11-save-orchestration.md (11.2), technical/21-testing.md (21.2, 21.3)
Build: Spike first, on a DISPOSABLE test schema (review data stays read-only, D27): (1) confirm a nonzero caller RecordId is treated as create, a replay of the same payload and ID fails without a second row, and recordid is unique in the table; (2) get the FN_AXDBPUT_GETRECID body per provider (the function is not in the repo); (3) build the existence probe from the StructDef main table name. Do the D20 audit after. first the D20 line-by-line audit of AxDataObject vs AxTstructData's AxPutService/SaveData. Then `IAxpertTransactionWriter` that calls AxPut as an external self-committing step; map returned recordIds for tmp: rows (D7). Child, GenMap and MdMap behavior follows what SaveData does inside its own transaction; anything that targets another DB or service goes through the outbox (D2). Do not modify AxPut without review.
Done when (you run): a child failure leaves no main record (SaveData's own rollback); success returns correct recordId mappings; GenMap/MdMap effects happen once per operation key; D30 reconciliation works against a real committed record.
(4) Oracle/SQL Server: obtain the allocator and a primary-key/unique proof for each deployed provider; until then new-record ambiguity on that provider goes to NeedsReview (D30). PostgreSQL is already confirmed. (5) Run notes §8.4 steps 2-5 on a disposable PostgreSQL schema.

## Milestone M2 (you)

Needs M1.6 (Phase 1D) done. Scenarios 1, 2, 9, 12 pass with real adapters, including a crash-between-AxPut-and-record test.
