## 11. Transaction and Reliability Model

- AxTstructData AxPut owns business Save and its provider transaction; AxData owns Init/Get/Set/Query and shared DataObject state.
- Every AxPut request is ObjectId-aware. Missing ObjectId generates a secure ULID; omitted retention defaults to retain.
- Main Save, grids, children, GenMaps, and MdMaps commit together wherever they share AxPut's database transaction.
- Shared server-owned exclusion prevents overlapping Set/AxPut mutation. Set's final write verifies its lease token; sequential operations are accepted last-completed-wins, and every operation uses the current StructDef.
- Redis is the only active DataObject-state store in the current development scope. It is not evidence of a committed business Save; DataObject retention/destruction occurs only after successful database commit, and durable closure still prevents a destroyed object from becoming active again.
- DataObject snapshot publication, consumption, and SQL recovery are future use and are not active deployment requirements. This does not defer AxPut idempotency, outbox, dispatch, reconciliation, or state-finalization durability.
- Core Save slices deliver state hydration, optional payload overlay, empty-payload Save, validation, transaction parity, response, and retention before reliability infrastructure is added.
- Production durability is incomplete until idempotency, transactional outbox, dispatcher, retry/backoff, poison handling, and recovery workers pass their slices.
- Idempotency completion and outbox rows ultimately join AxPut's transaction. Required work targeting another database/service is delivered asynchronously; no cross-database atomicity is claimed.
- PEG approval is not business Save. Trusted final approval invokes the same AxPut-owned Save behavior without any client bypass flag.
