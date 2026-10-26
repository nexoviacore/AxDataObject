## 10. State and Persistence Strategy

### 10.1 Redis Active State

Redis contains the complete serialized active AxData object, including its runtime state and active PEG workflow state, for fast access.

"Runtime state" means the durable AxData values and lifecycle metadata needed to reconstruct processing. Redis does not contain mutable Axpert service objects such as `StructDef`, `Validate`, or `Parser`. A fresh Axpert runtime is created for one operation, hydrated from AxData state, used for that operation, and then discarded.

API nodes remain stateless and can access the active object using `ObjectId`.

Redis entries include:

- `ObjectId`
- Tenant/context information
- AxData runtime state
- Active PEG workflow state
- Object/version metadata
- Lifecycle information

Active objects have a configurable inactivity TTL. The TTL is refreshed based on configured object activity and the object is removed from Redis after the configured period of inactivity.

### 10.2 Snapshot Persistence

After each successfully completed Set operation, the service publishes a full snapshot of the current AxData object to `AxPersist`.

The persistence consumer upserts the latest snapshot into `AxDataObjectInstance`, maintaining one current recoverable snapshot per `ObjectId`.

```text
Set Request
    ↓
Update Redis
    ↓
Set completed
    ↓
Full Snapshot
    ↓
AxPersist
    ↓
AxDataObjectInstance
```

Full-snapshot persistence is the first-release strategy because it provides a simple and reliable recovery model.

Change-only/delta persistence may be considered later if performance and load testing demonstrate that full snapshots introduce significant processing, storage, or network overhead.

Snapshot field state is sparse: a clean field stores its current value and, when applicable, its normalized source ID. Original value/source ID are stored only while the field is dirty. Data-container identity is stored once per container, and row identity plus persisted record identity are stored once per row rather than repeated for every field. P1D-05A characterizes the 50 DC x 70 rows x 100 fields capacity envelope before P1D-05B fixes the v3 wire representation and supported limits.

### 10.3 Critical Persistence

Critical workflow and business-state transitions must have a clearly defined durability point before the API reports successful completion.

For operations such as:

- PEG initiation
- Approve
- Reject
- Return
- Recall
- Explicit AxData Save

the system must ensure that the required state has been successfully persisted according to the applicable operation's persistence rules before returning success.

For `SaveOnApprove = true`, the final approval must successfully persist the AxData transaction before the workflow is marked as successfully Approved.

For `SaveOnApprove = false`, PEG approval and AxData business-data persistence remain separate operations. The business data is persisted only when the explicit AxData Save API is invoked.

Notifications and external integrations remain asynchronous through the Transactional Outbox and do not need to complete before the API returns success.

### 10.4 Recovery

When an `ObjectId` is not available in Redis:

1. Look up the latest valid `AxDataObjectInstance` snapshot within the authenticated tenant and authorization scope.
2. Validate the snapshot version, lifecycle state, and access permissions.
3. Restore the AxData runtime state to Redis.
4. Restore the active PEG workflow state associated with the snapshot, where available.
5. Continue the requested operation if the object is valid and recoverable.
6. Otherwise, return a defined `Expired`, `NotFound`, or `RecoveryFailed` response.

The recovery mechanism must ensure that an object belonging to another tenant or unauthorized user cannot be restored or accessed merely by supplying its `ObjectId`.

The exact durable persistence and recovery strategy for PEG state, including the use of `axactivetasks` and `axactivetaskstatus`, will be finalized as part of the detailed PEG persistence design.
