## 8. Redis design

### 8.1 Keys

Proposed namespaced keys:

```text
AXD:{tenantId}:{objectId}             serialized AxData state
AXD:LOCK:{tenantId}:OBJECT            shared Set/AxPut operation lock
AXDIDEMP:{tenantId}:{operationKey}    optional short result cache; DB remains durable
PEG:{tenantId}:{workflowInstanceId}   optional indexed copy of active PEG state
```

The logical requirement `AXD + ObjectId` is preserved while adding tenant isolation and operational readability. The active PEG state is embedded in the AxData document; the separate `PEG:` key is optional when task-based lookup or independent PEG scaling requires it. If both representations exist, they must be versioned and updated through one defined consistency operation.

### 8.2 Atomic update

Set uses a short server-owned lease:

```text
Acquire lock with unique token and bounded lease
    -> read state
    -> authorize
    -> execute operation
    -> atomically verify the lease token, write complete state, and refresh TTL
    -> release only the lock owned by this token
```

The final Lua write verifies that the lock still contains the holder's unique token. An expired or replaced holder cannot commit. AxPut uses the same lock key to establish a persistent `Saving` claim before hydration and SQL; Set observes that state and returns `409 / AXD-CONC-002`. A competing AxPut claim does the same when Set holds the lease.

Sequential requests are distinct operations and the last completed operation wins. There is no stale-tab detector. A timed-out AxPut request is not automatically retried because the server cannot infer whether its business transaction committed.

### 8.3 Persistence and eviction

- TTL is refreshed on authorized activity.
- Active payloads contain no creation, access, or expiry timestamps, and active keys have no inactivity TTL. Init, Set, AxPut claim/release, and retained finalization leave the key persistent.
- Short-lived lock leases keep their bounded TTL because they are coordination keys, not DataObject storage.
- Redis eviction or manual deletion removes the only active DataObject copy in the current scope; a later lookup returns `404 / AXD-NOTFOUND-001` and does not attempt database recovery.
- Capacity and TTL settings must therefore be chosen with this explicit loss-of-active-state behavior in mind until durable snapshot recovery is reintroduced.
- Completed/rejected/recalled objects follow retention configuration.
- `AxOpen` modifies logical cleanup timing but does not bypass infrastructure hard limits indefinitely.

### 8.4 Active-state size and definition cache boundary

The active `AXD:` value stores AxData state only; it never stores mutable `StructDef`, `Validate`, or `Parser` instances. The current payload has no format/schema discriminator and no compatibility reader: deployments clear incompatible active objects rather than migrate them. Each DC stores ordered `[name,flags]` descriptors once, with static read-only bit `1` and source-key bit `2`. Rows store aligned typed values, with source-key display value and non-negative ID packed as `value~id`, plus optional aligned `oldValues` only while an existing row is dirty. Init/AxGet business-key locators, per-row source/original arrays, data types, files, and AxOpen are omitted; existing state retains only the resolved `recordId`, Query/Get uses stored field metadata plus fresh permissions without StructDef, Set/Save validate the flags against current StructDef, and active PEG state is included only when present.

The unchanged hash key remains `AXD:{SCHEMANAME}:{OBJECTID}`. One compact value preserves atomic replacement and the 50 DC x 70 rows x 100 fields (350,000 instances) capacity envelope. The legacy StructDef cache is separate from DataObject state; active operations load the current definition without a stored definition binding. Snapshot stream publication remains deferred.
