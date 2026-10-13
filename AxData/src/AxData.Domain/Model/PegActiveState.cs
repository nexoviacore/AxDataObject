using AxData.Domain.Lifecycle;

namespace AxData.Domain.Model;

public sealed class PegActiveState
{
    public required string WorkflowInstanceId { get; init; }

    public string? CurrentTaskId { get; set; }

    public required string InitiatorUserName { get; init; }

    public PegWorkflowStatus Status { get; set; }

    public int ApprovalLevel { get; set; }

    public IReadOnlyList<string> Approvers { get; set; } = [];

    public long WorkflowVersion { get; set; }

    public DateTimeOffset PendingFromUtc { get; set; }
}
