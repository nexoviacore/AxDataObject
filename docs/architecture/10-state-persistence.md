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
- Object identity and lifecycle metadata
- Lifecycle information

Active objects have no inactivity TTL while Redis is the only state store. Init, Set, and retained Save keep the active key persistent until an explicit lifecycle action removes or closes it. Short-lived concurrency leases remain separately bounded.

### 10.2 Snapshot Persistence (future use)

Durable DataObject snapshot publication, consumption, and SQL recovery are deferred. Redis is the only active DataObject-state store in the current development scope. Set writes the complete state directly to the tenant-scoped Redis key; no snapshot stream or persistence worker is active or required.

The dormant snapshot source remains compiled for possible future use, but it is not registered by API composition or started by deployment and walkthrough tooling.

```text
Set Request
    ↓
Verify the server-owned lease token and update Redis once
    ↓
Set completed
```

If durable snapshot recovery is reintroduced, its transport, persistence, version guard, and operational requirements must be approved and activated as one complete path rather than partially wired.

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
- Explicit AxPut Save

the system must ensure that the required state has been successfully persisted according to the applicable operation's persistence rules before returning success.

For `SaveOnApprove = true`, the final approval must successfully persist the AxData transaction before the workflow is marked as successfully Approved.

For `SaveOnApprove = false`, PEG approval and business-data persistence remain separate operations. The business data is persisted only when AxPut is invoked explicitly.

Notifications and external integrations remain asynchronous through the Transactional Outbox and do not need to complete before the API returns success.

### 10.4 Recovery (future use)

There is no active database recovery path. When an `ObjectId` is absent from its authenticated tenant's Redis scope, Get, Query, Set, and ObjectId-aware Save treat it as not found (`404 / AXD-NOTFOUND-001`) without querying `AxDataObjectInstance` or restoring Redis state.

Any future recovery mechanism must validate tenant, lifecycle, and authorization before restoration and must ensure that ObjectId possession is never sufficient authority.

The exact durable persistence and recovery strategy for PEG state, including the use of `axactivetasks` and `axactivetaskstatus`, will be finalized as part of the detailed PEG persistence design.
