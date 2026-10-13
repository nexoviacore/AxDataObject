## 16. Definition of Architectural Success

The architecture is considered implemented when:

- A client can initialize, modify, query, save, print, and complete applicable PEG actions using documented APIs.
- Existing StructDef, Validate, Parser, and Axpert persistence behavior are reused through adapters without unapproved changes.
- Data cannot be accessed merely by possessing an `ObjectId`.
- Concurrent/stale requests cannot silently overwrite newer state.
- Redis loss can be recovered from the latest valid durable snapshot within the defined retention period, where recovery is required.
- Save is atomic within its declared database transaction boundary and duplicate-safe.
- Both `SaveOnApprove` sequences behave exactly as specified.
- Workflow state and history have a defined recovery/persistence strategy and do not depend solely on transient API memory.
- External delivery failures do not roll back an already committed business transaction and can be retried without unintended duplication.
- Automated unit, integration, concurrency, security, recovery, and workflow tests pass.
