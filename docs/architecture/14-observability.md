## 14. Observability and Operations

Every request should carry a correlation/trace identifier. Structured logs and metrics must include safe identifiers such as tenant, TransId, ObjectId, workflow instance/task, object version, session identifier where applicable, and idempotency key; sensitive field values and credentials must not be logged.

Key operational signals include:

- API latency and error rate by operation.
- Validation/business errors separately from system errors.
- Redis hit, miss, conflict, eviction, and recovery rates.
- AxData serialized size distribution.
- PEG transition failures and tasks stuck at a level.
- Save rollback and duplicate-request counts.
- Snapshot and outbox queue lag, retries, and dead-letter count.
- Print and notification processing outcomes.
- Trace/span health and distributed request latency across API and database operations.
