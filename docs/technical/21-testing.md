## 21. Testing strategy

### 21.1 Unit tests

- Object state transitions and version increments.
- Row action validation.
- Permission filtering and approver restrictions.
- Set error collection and dependency skipping.
- FillGrid append/replace behavior.
- SaveOnApprove state-machine paths.
- Idempotency request hashing and duplicate results.
- Error mapping and serialization compatibility.

### 21.2 Adapter/contract tests

- StructDef loading and version identification.
- Validate/Parser input/output equivalence with existing behavior.
- AxGet loading and AxPut saving.
- GenMap/MdMap result and transaction behavior.
- Existing PEG routine and table behavior.
- Delphi print-service request/response behavior.

### 21.3 Integration tests

- Redis atomic compare/write and lock ownership.
- Snapshot version-guarded upsert.
- Database rollback when a child/GenMap/MdMap fails.
- Duplicate Save and duplicate PEG actions.
- Outbox commit and worker retry/deduplication.
- Redis loss followed by recovery from the configured AxData snapshot and PEG recovery source.
- TTL eviction with and without a durable snapshot.

### 21.4 Security tests

- Guessing another user's ObjectId.
- Cross-tenant ObjectId access.
- Approver accessing an unassigned task.
- Client attempting to activate internal permission bypass.
- Hidden/read-only field behavior.
- AFL secret leakage, path traversal, oversized files, and expired download links.

### 21.5 End-to-end scenarios

1. New transaction -> Set -> dependencies -> Save without PEG.
2. Existing transaction -> permission-filtered Get -> edit -> Save.
3. `SaveOnApprove=true` -> multi-level approval -> final Save -> print/notification.
4. Deferred Save validation failure at final approval -> recover/correct/retry.
5. `SaveOnApprove=false` -> PEG -> final approval -> `ApprovedAwaitingSave` -> explicit Save API.
6. Return for correction and resubmission.
7. Reject in both SaveOnApprove modes.
8. Recall by initiator/admin.
9. Duplicate client retry after response loss.
10. Concurrent same-user tabs with stale object version.
11. Redis restart and recovery from persisted snapshot/PEG tables.
12. Bulk Save using CallerId and Sequence.
