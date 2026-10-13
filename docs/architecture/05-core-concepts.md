## 5. Core concepts

### 5.1 StructDef

StructDef is the immutable/versioned definition of a tstruct for the lifetime of an AxData operation. It describes the transaction structure and behavior.

### 5.2 AxData object

An AxData object is one active transaction instance. It contains:

- Identity: tenant/instance, ObjectId, TransId, key field/value, and RecordId.
- Ownership: initiator/owner and authenticated user context.
- Data: DCs, rows, row actions, fields, values, and relevant metadata.
- Runtime state: object version, dirty state, calculated/dependent values, and lifecycle timestamps.
- Workflow projection: approval status, level, and current approvers for fast access. PEG remains the authority for workflow history.
- References: files, print outputs, and related transaction identifiers.

### 5.3 ObjectId

`ObjectId` is an opaque, server-generated identifier used to reference the same `AxDataObjectInstance` across multiple API calls.

The corresponding `AxDataObjectInstance` is stored in **Redis as shared runtime state**, rather than being maintained in the API server's process memory. This allows the AxData instance to be accessed consistently across API requests and multiple API instances.

Possession or knowledge of an `ObjectId` **does not grant access or authorization**.

For every API call involving an `ObjectId`, the server must:

1. Authenticate the requesting user.
2. Validate the tenant/instance context.
3. Retrieve or validate the corresponding AxData object.
4. Validate that the user is authorized to access the object.
5. Where a PEG workflow is attached, validate the user's current PEG role/task authorization.

Access should be limited to the authorized **initiator/creator and the current approver(s)** as determined by the applicable PEG workflow and authorization rules.

Example:

```text
ObjectId = OBJ789
Tenant   = TEN001
Creator  = U123
Approver = U456
PEG Task = TASK1001
Status   = Pending Approval
```

Access:

```text
U123 → Creator/Initiator → Allowed
U456 → Current Approver  → Allowed
U999 → No authorization → Denied
```

The `ObjectId` should therefore be treated only as a **reference to runtime state**, never as a security token or authorization mechanism.

### 5.4 Data containers and rows

The request contains one node for each submitted DC and one node per row. Non-grid DCs contain one logical row. Grid DCs may contain multiple rows. Each submitted row carries an action:

- `n`: add a new row.
- `e`: edit an existing row.
- `d`: delete an existing row.
