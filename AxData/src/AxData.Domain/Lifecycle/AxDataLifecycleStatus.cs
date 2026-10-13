namespace AxData.Domain.Lifecycle;

public enum AxDataLifecycleStatus
{
    Active,
    PendingApproval,
    ApprovedAwaitingSave,
    Saved,
    Rejected,
    Recalled,
    Expired,
    Closed
}
