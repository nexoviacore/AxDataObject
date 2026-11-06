namespace AxData.Domain.Lifecycle;

public enum AxDataLifecycleStatus
{
    Active,
    Saving,
    PendingApproval,
    ApprovedAwaitingSave,
    Saved,
    Rejected,
    Recalled,
    Expired,
    Closed
}
