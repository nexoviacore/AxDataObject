using AxData.Application.Ports;
using AxData.Domain.Lifecycle;
using AxData.Domain.Model;

namespace AxData.Application.Authorization;

public sealed record AxAuthorizationContext
{
    public AxAuthorizationContext(
        bool tenantMatches,
        AxActorRelation relations,
        bool hasOperationPermission,
        bool isEditable)
    {
        TenantMatches = tenantMatches;
        Relations = relations;
        HasOperationPermission = hasOperationPermission;
        IsEditable = isEditable;
    }

    public bool TenantMatches { get; init; }

    public AxActorRelation Relations { get; init; }

    public bool HasOperationPermission { get; init; }

    public bool IsEditable { get; init; }

    public static AxAuthorizationContext FromState(
        AxUserContext actor,
        AxDataState state,
        bool hasViewerAccess,
        bool hasOperationPermission,
        bool isEditable)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(state);

        var tenantMatches = string.Equals(actor.TenantId, state.TenantId, StringComparison.Ordinal);
        if (!tenantMatches)
        {
            return new AxAuthorizationContext(
                tenantMatches: false,
                AxActorRelation.Unrelated,
                hasOperationPermission,
                isEditable);
        }

        var relations = AxActorRelation.Unrelated;

        if (string.Equals(actor.UserName, state.OwnerUserName, StringComparison.Ordinal) ||
            string.Equals(actor.UserName, state.InitiatorUserName, StringComparison.Ordinal))
        {
            relations |= AxActorRelation.OwnerOrInitiator;
        }

        if (IsCurrentApprover(actor.UserName, state.Workflow))
        {
            relations |= AxActorRelation.CurrentApprover;
        }

        if (hasViewerAccess)
        {
            relations |= AxActorRelation.Viewer;
        }

        if (actor.IsAdministrator)
        {
            relations |= AxActorRelation.Administrator;
        }

        return new AxAuthorizationContext(
            tenantMatches: true,
            relations,
            hasOperationPermission,
            isEditable);
    }

    private static bool IsCurrentApprover(string userName, PegActiveState? workflow) =>
        workflow is
        {
            Status: PegWorkflowStatus.Pending,
            CurrentTaskId: not null
        } &&
        !string.IsNullOrWhiteSpace(workflow.CurrentTaskId) &&
        workflow.Approvers.Any(approver => string.Equals(approver, userName, StringComparison.Ordinal));
}
