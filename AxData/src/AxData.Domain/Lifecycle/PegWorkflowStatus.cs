namespace AxData.Domain.Lifecycle;

public enum PegWorkflowStatus
{
    None,
    Pending,
    Approved,
    Rejected,
    Recalled,
    FinalApprovalSaveFailed
}
