## 8. Redis design

### 8.1 Keys

Proposed namespaced keys:

```text
AXD:{tenantId}:{objectId}             serialized AxData state
AXDLOCK:{tenantId}:{objectId}         short operation lock
AXDIDEMP:{tenantId}:{operationKey}    optional short result cache; DB remains durable
PEG:{tenantId}:{workflowInstanceId}   optional indexed copy of active PEG state
```

The logical requirement `AXD + ObjectId` is preserved while adding tenant isolation and operational readability. The active PEG state is embedded in the AxData document; the separate `PEG:` key is optional when task-based lookup or independent PEG scaling requires it. If both representations exist, they must be versioned and updated through one defined consistency operation.

### 8.2 Atomic update

Set and other mutating operations use both a short lease and an object version:

```text
Acquire lock with unique token and bounded lease
    -> read state
    -> authorize
    -> compare expected version
    -> execute operation
    -> increment version
    -> write complete state atomically and refresh TTL
    -> release only the lock owned by this token
```

A Lua script, Redis transaction, or storage abstraction must atomically compare the stored version and write the new state. A stale request returns HTTP `409 Conflict` with the current version; it never silently overwrites the object.

The lock prevents overlapping evaluation work. Versioning remains required because locks can expire and requests can retry.

### 8.3 Expiry and eviction

- TTL is refreshed on authorized activity.
- Eviction is allowed only after a durable recoverable snapshot exists.
- LRU capacity management treats Redis removal as eviction, not durable deletion.
- Completed/rejected/recalled objects follow retention configuration.
- `AxOpen` modifies logical cleanup timing but does not bypass infrastructure hard limits indefinitely.

### 8.4 Active-state size and definition cache boundary

The active `AXD:` value stores AxData state only; it never stores mutable `StructDef`, `Validate`, or `Parser` instances. Snapshot schema v3 uses sparse original values/source IDs and avoids repeating container/row identity per field. P1D-05A measures the agreed 50 DC x 70 rows x 100 fields envelope and records the supported byte/memory limit before P1D-05B changes production limits or storage layout.

The legacy StructDef Redis entry is a separate definition cache. It is not AxData durable state and must not weaken the current raw-definition hash check. AxData may reuse it only when Builder invalidation or equivalent hash coherency is demonstrated; otherwise AxData refreshes StructDef once per operation. Splitting one AxData object across Redis keys is deferred unless measurement proves a compact single-value snapshot cannot meet the capacity envelope, because multi-key storage complicates atomic version CAS and stream publication.
