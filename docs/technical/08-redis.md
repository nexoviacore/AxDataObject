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
