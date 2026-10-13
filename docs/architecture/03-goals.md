## 3. Goals

- Create and load an in-memory transaction from a StructDef.
- Allow partial or complete Set operations across DCs and rows.
- Apply permissions, validation, field refresh, calculations, and FillGrid rules.
- Return selected fields, selected DCs, or the complete permission-filtered transaction.
- Save main, grid, child, GenMap, and MdMap changes transactionally and idempotently.
- Support PEG initiation, Approve, Reject, Return, Recall, and multi-level approval.
- Support both `SaveOnApprove` modes exactly as defined in this document.
- Maintain active state in Redis and recover it from durable database storage.
- Generate print forms and dispatch notifications/integration messages reliably.
- Scale API nodes horizontally without relying on process-local session state.
- Preserve user, transaction, field, and workflow security at every endpoint.
