## 20. Observability implementation

- Use structured logging scopes for correlation ID, tenant, user, ObjectId, TransId, object version, workflow task, and idempotency key.
- Redact data values by default; whitelist safe metadata rather than blacklist secrets.
- Trace API -> Redis -> Axpert engine -> SQL -> outbox/worker operations.
- Emit metrics for Set field count/evaluation duration, serialized bytes, conflicts, validation errors, Save duration/rollbacks, PEG transitions, queue lag, and worker retries.
- Add health checks for Redis, primary database, PEG tables/routines, queue, file storage, and print-service reachability, distinguishing readiness from liveness.
