## 13. Scalability and Performance Strategy

- Run multiple stateless API instances behind a load balancer.
- Use Redis for active-state reads/writes and short-lived distributed locks or atomic compare-and-set operations where required.
- Cache StructDef by tenant, TransId, and definition version; invalidate on definition publication/change.
- Avoid storing large binary file bodies inside long-lived Redis objects; store secure file references.
- Set configurable maximum AxData size, request size, grid-row count, and object inactivity TTL.
- Ensure a recoverable snapshot exists before allowing active-state eviction where recovery is required.
- Scale persistence, print, notification, and integration workers independently.
- Index `ObjectId`, tenant, owner, `RecordId`, status, workflow task, idempotency key, and outbox status fields.
- Measure serialization size/time, Redis latency, Set evaluation time, Save time, queue lag, and recovery success rate before introducing incremental/delta snapshots.
- Use short-lived object locks only for critical operations; do not hold locks for the entire user editing or approval lifecycle.
