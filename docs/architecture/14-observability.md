## 14. Observability and Operations

Every request should carry a correlation/trace identifier. AxData structured logs and metrics may include safe identifiers such as tenant, TransId, ObjectId, workflow instance/task, and correlation ID; they do not include object versions, session identifiers, request bodies, field values, tokens, or credentials. The narrowly scoped AxTstructData trace exception remains documented separately.

Key operational signals include:

- API latency and error rate by operation.
- Validation/business errors separately from system errors.
- Redis hit, miss, conflict, and eviction rates.
- AxData serialized size distribution.
- PEG transition failures and tasks stuck at a level.
- Save rollback and duplicate-request counts.
- Outbox queue lag, retries, and dead-letter count when that future path is implemented; dormant snapshot transport is not an active operational dependency.
- Print and notification processing outcomes.
- Trace/span health and distributed request latency across API and database operations.
