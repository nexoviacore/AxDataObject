## 15. Delivery Strategy

### Phase 1 — Foundation

- Existing-code adapters, AxData domain model, Redis store, snapshot schema, authentication/ownership, Init, Get/query, Set, versioning, and tests.

### Phase 2 — Durable Save

- Save validation, transaction coordinator, AxPut adapter, GenMap/MdMap integration, idempotency, outbox, and database integration tests.

### Phase 3 — PEG

- PEG initiation and actions, active PEG state in Redis, existing PEG table integration where required, both `SaveOnApprove` paths, workflow recovery, and authorization tests.

### Phase 4 — Files and Post-Processing

- AFL integration, secure downloads, print service adapter, notifications, queue integrations, retries, and audit.

### Phase 5 — Production Hardening

- Load/failure testing, TTL/eviction/recovery, deployment scaling, observability dashboards, retention/cleanup jobs, and operational runbooks.
