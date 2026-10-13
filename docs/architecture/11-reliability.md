## 11. Transaction and Reliability Model

- Main Save, grids, children, GenMaps, and MdMaps commit together wherever they share a database transaction boundary.
- Any required failure rolls back the complete database unit of work within that transaction boundary.
- Save and mapping execution are protected by a durable idempotency key.
- `ObjectId + CallerId + Sequence`, or an explicit request key, identifies a logical bulk-save request.
- External work is recorded in a Transactional Outbox wherever it shares the same database transaction boundary as the business operation.
- Workers retry safely and deduplicate outbox deliveries.
- Operations involving Redis and database state must have defined consistency and recovery rules; Redis state must not be treated as a substitute for a committed business transaction.
