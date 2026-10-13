# Phase 5: Production hardening

Paths under **Read** are relative to `docs/`. Effort levels as in phase-0-prep.md. Integration and load tests: you run.

### P5-01 Observability: metrics, traces, health checks
Effort: Standard | After: P4-05
Read: technical/20-observability.md, architecture/14-observability.md
Build: build on the safe structured logging and correlation propagation from P1D-07; add metrics listed in technical/20; distributed spans from API to Redis to engine to SQL to worker; separate readiness and liveness checks for Redis, DB, PEG tables, queue, file storage, print service.
Done when: a failing dependency flips readiness, not liveness.

### P5-02 Eviction, TTL, retention, cleanup
Effort: Deep | After: P3-10
Read: technical/08-redis.md (8.3), architecture/10-state-persistence.md, architecture/13-scalability.md
Build: retention config for completed and rejected objects; `AxOpen` maximum retention; cleanup jobs; sweep for expired InProgress idempotency rows; eviction only after a snapshot exists.
Done when: an object is evictable only with a durable snapshot; cleanup removes only what retention allows.

### P5-03 Concurrency and failure tests
Effort: Standard | After: P3-10
Read: technical/21-testing.md (21.3, 21.5)
Build: scenarios 9 to 12; lock expiry mid-Set; duplicate PEG actions; response-loss retry; Redis restart.
Done when: all pass repeatedly (run them 20 times to catch flakiness).

### P5-04 Load test and size measurement
Effort: Standard | After: P5-03
Read: architecture/13-scalability.md, technical/17-config.md
Build: measure serialization size and time, Redis latency, Set time, Save time, queue lag. Write a short report. Delta snapshots only if the numbers justify them (architecture/04-non-goals.md).
Done when: the report exists with numbers.

### P5-05 Security suite and dependency/secret audit
Effort: Deep | After: P5-03
Read: technical/21-testing.md (21.4), architecture/12-security.md
Build: complete every 21.4 test; NuGet vulnerability audit; secret scan in CI; log-redaction tests.
Done when: the full 21.4 list passes; the audit and secret scan run in CI.

### P5-06 Deployment config and runbooks
Effort: Light | After: P5-01
Read: technical/17-config.md, architecture/07.3-initial-deployment.md, architecture/13-scalability.md
Build: environment config per technical/17 (secrets from the platform provider); worker scaling notes; runbooks: Redis loss, stuck PEG tasks, dead-letter review, stale InProgress cleanup.
Done when: a teammate can follow each runbook without you.
