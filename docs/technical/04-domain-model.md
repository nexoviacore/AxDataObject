## 4. Core domain model

The following model is a design contract, not final C# syntax.

```csharp
public sealed class AxDataState
{
    public required string ObjectId { get; init; }
    public required string TenantId { get; init; }
    public required string TransId { get; init; }
    public long? RecordId { get; set; }

    public required string InitiatorUserName { get; init; }

    public DateTimeOffset CreatedAtUtc { get; init; }
    public DateTimeOffset LastAccessedAtUtc { get; set; }
    public AxDataLifecycleStatus Status { get; set; }

    public Dictionary<string, AxDataContainer> DataContainers { get; init; }
    public PegActiveState? Workflow { get; set; }
    public List<AxFileReference> Files { get; init; }
    public bool AxOpen { get; set; }
}
```

### 4.1 Data containers and rows

```csharp
public sealed class AxDataContainer
{
    public required string Name { get; init; }
    public bool IsGrid { get; init; }
    public List<AxDataFieldMetadata> Fields { get; init; }
    public List<AxDataRow> Rows { get; init; }
}

public sealed class AxDataFieldMetadata
{
    public required string Name { get; init; }
    public bool IsReadOnly { get; init; }
    public bool IsSourceKey { get; init; }
}

public sealed class AxDataRow
{
    // Stable AxData identity. Legacy rowN is derived from current array order only in the adapter.
    public required string RowId { get; init; }
    public long? PersistedRecordId { get; set; }
    public AxRowState State { get; set; }
    public Dictionary<string, AxFieldValue> Fields { get; init; }
}

public sealed class AxFieldValue
{
    public object? Value { get; set; }
    // Present in serialized state only while dirty.
    public object? OriginalValue { get; set; }
    // Internal normalized/source-key identity; never mutation authority by itself.
    public long? SourceId { get; set; }
    public long? OriginalSourceId { get; set; }
    public bool IsDirty { get; set; }
    public bool IsReadOnly { get; set; }
}
```

`dcN` remains the authoritative Axpert container identity. `RowId` is an immutable, opaque AxData identity; `rowN` is a transient 1-based position derived only at the Integration.Axpert boundary; `PersistedRecordId`/`axp_recid` is a separate nullable database identity. The container owns its rows and the row owns its fields, so DC, row, and record identity are not duplicated in every field value.

For a normalized field, `Value` is the display/business value and `SourceId` is its Axpert source-key ID. Existing-record Init restores both from AxGet. Clean fields treat current value/source ID as their baseline; dirty fields retain the original pair until changes are accepted. Client-supplied source IDs are not trusted without server-side resolution or revalidation.

Redis persistence is a compact positional projection of this domain shape, not the public/domain model itself. Each DC stores deterministic `[name,flags]` descriptors once, including empty DCs: bit `1` is static read-only and bit `2` is source key. Every row has an exact-length `values` array aligned to them. Source-key entries pack display value and non-negative ID as `value~id` using the last tilde, while ordinary values remain native scalars and are never split. Existing dirty rows add an exact-length `oldValues`; unchanged positions repeat the current value so real `null`/empty baselines remain unambiguous. Clean/new rows omit `oldValues`. Query/Get uses this field metadata plus fresh permissions and does not load StructDef. Set and Save load the current StructDef for business interpretation and validate both stored flags. Malformed, duplicate, missing, extra, or inconsistent positional data fails closed.

`KeyField`/`KeyValue` are request-scoped Init/AxGet locators and are not DataObject state. Existing Init retains only AxGet's resolved `RecordId`; a new object keeps it null until Save. Existing Query/Set and ObjectId-aware AxPut address Axpert by the trusted server-side `recordid` pair derived from that value.

### 4.1.1 Axpert runtime lifetime

`StructDef`, `Validate`, and `Parser` are mutable integration runtime objects, not domain state. Set creates one fresh operation-scoped runtime, hydrates it once from the AxData state, applies fields in authoritative evaluation order, reads the resulting state once, and discards it. It is never serialized, cached as an active object, or shared across requests/users. If a rejected field can leave the legacy runtime mutated, continuation rebuilds from the last accepted AxData state before evaluating another independent field.

Suggested lifecycle values:

- Object: `Active`, `PendingApproval`, `ApprovedAwaitingSave`, `Saved`, `Rejected`, `Recalled`, `Expired`, `Closed`.
- Row: `Unchanged`, `New`, `Edited`, `Deleted`.
- Workflow: `None`, `Pending`, `Approved`, `Rejected`, `Recalled` and an internal recoverable `FinalApprovalSaveFailed` or equivalent if needed.

The active Save matrix is: `Active` permits Set; `Saving` rejects Set while authorized Query returns all visible data as transaction-wide read-only; a retained successful Save returns to `Active`; a non-retained successful Save becomes `Closed` and the active Redis key is removed. `Saved` remains a defensive/legacy enum value but is not the retained AxPut final state.

### 4.2 Active PEG workflow state

The Redis AxData runtime state contains the active PEG information required for authorization, routing, recovery, and response construction:

```csharp
public sealed class PegActiveState
{
    public string WorkflowInstanceId { get; set; }
    public string? CurrentTaskId { get; set; }
    public string InitiatorUserName { get; set; }
    public string ApprovalStatus { get; set; }
    public int ApprovalLevel { get; set; }
    public IReadOnlyList<string> Approvers { get; set; }
    public long WorkflowVersion { get; set; }
    public DateTimeOffset PendingFromUtc { get; set; }
}
```

The active PEG state is maintained in Redis during the AxData lifecycle. Whether and when the same state/history is also written to `axactivetasks` and `axactivetaskstatus` is an explicit persistence-design decision that must be finalized after the existing PEG routines and recovery requirements are inspected. Approvers should be modeled internally as a collection even if a compatibility response exposes comma-separated names. Detailed, append-only approval actions/comments may remain in the PEG history store rather than expanding the active AxData document indefinitely.
