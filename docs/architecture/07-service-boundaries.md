## 7. Service boundaries and deployment

### 7.1 AxData Module

AxData owns:

- AxData object initialization, lifecycle management, and recovery.
- `AxDataObjectInstance` runtime state and transaction context.
- Transaction data and version management.
- `Set` / `Get` behavior.
- Authorization enforcement for transaction, DC, field, and method-level access, based on the permissions/context provided by the applicable authorization/PEG layer.
- Validation and parser orchestration.
- `FillGrid` execution.
- `Save`, [On persistent save : `GenMap`, `MdMap`, and child-transaction coordination.]
- AxData runtime-state snapshot creation, retrieval, and lifecycle management through Redis.
- Coordination of AxData state between API calls using `ObjectId`.
- Print requests associated with an AxData object.
- Coordination with the Transactional Outbox for asynchronous processing where required.
