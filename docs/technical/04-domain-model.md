## 4. Core domain model

The following model is a design contract, not final C# syntax.

```csharp
public sealed class AxDataState
{
    public required string ObjectId { get; init; }
    public required string TenantId { get; init; }
    public required string TransId { get; init; }
    public string? KeyField { get; set; }
    public string? KeyValue { get; set; }
    public long? RecordId { get; set; }

    public required string OwnerUserName { get; init; }
    public string InitiatorUserName { get; init; }
    public long Version { get; set; }
    public string StructDefVersion { get; init; }

    public DateTimeOffset CreatedAtUtc { get; init; }
    public DateTimeOffset LastAccessedAtUtc { get; set; }
    public DateTimeOffset? ExpiresAtUtc { get; set; }
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
    public List<AxDataRow> Rows { get; init; }
}

public sealed class AxDataRow
{
    // Stable identity is required for edit/delete; exact Axpert key is to be confirmed.
    public required string RowId { get; init; }
    public long? PersistedRecordId { get; set; }
    public AxRowState State { get; set; }
    public Dictionary<string, AxFieldValue> Fields { get; init; }
}

public sealed class AxFieldValue
{
    public object? Value { get; set; }
    public object? OriginalValue { get; set; }
    public bool IsDirty { get; set; }
    public bool IsReadOnly { get; set; }
}
```

Suggested lifecycle values:

- Object: `Active`, `PendingApproval`, `ApprovedAwaitingSave`, `Saved`, `Rejected`, `Recalled`, `Expired`, `Closed`.
- Row: `Unchanged`, `New`, `Edited`, `Deleted`.
- Workflow: `None`, `Pending`, `Approved`, `Rejected`, `Recalled` and an internal recoverable `FinalApprovalSaveFailed` or equivalent if needed.

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
