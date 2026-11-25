## 13. Scalability and Performance Strategy

- Run multiple stateless API instances behind a load balancer.
- Use Redis for active-state reads/writes, short-lived token-owned Set leases, and persistent AxPut `Saving` claims.
- Cache StructDef only by trusted tenant/transaction identity with proven coherency; active DataObjects are not bound to a stored definition version.
- Avoid storing large binary file bodies inside long-lived Redis objects; store secure file references.
- Set configurable maximum AxData size, request size, and grid-row count; monitor persistent active-key count and Redis capacity.
- Treat active-state eviction as loss of the active DataObject until a separately approved recovery design is reactivated.
- Scale persistence, print, notification, and integration workers independently.
- Index `ObjectId`, tenant, initiator, `RecordId`, status, workflow task, idempotency key, and outbox status fields where those future durable stores are active.
- Measure serialization size/time, Redis latency, Set evaluation time, Save time, queue lag, and recovery success rate before introducing incremental/delta snapshots.
- Use short-lived Set locks only for critical operations; AxPut's persistent `Saving` claim spans its business Save and is finalized or released by its token owner.
