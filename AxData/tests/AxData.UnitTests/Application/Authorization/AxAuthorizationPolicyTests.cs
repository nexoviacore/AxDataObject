using AxData.Application.Authorization;
using AxData.Application.Ports;
using AxData.Domain.Lifecycle;
using AxData.Domain.Model;

namespace AxData.UnitTests.Application.Authorization;

public sealed class AxAuthorizationPolicyTests
{
    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void Initialize_requires_explicit_create_permission(bool hasPermission, bool expected)
    {
        var context = new AxAuthorizationContext(
            tenantMatches: true,
            AxActorRelation.Unrelated,
            hasPermission,
            isEditable: true);

        Assert.Equal(expected, AxAuthorizationPolicy.IsAllowed(context, AxAuthorizationOperation.Initialize));
    }

    public static TheoryData<AxActorRelation, AxAuthorizationOperation, bool> RelationMatrix => new()
    {
        { AxActorRelation.OwnerOrInitiator, AxAuthorizationOperation.Query, true },
        { AxActorRelation.OwnerOrInitiator, AxAuthorizationOperation.Set, true },
        { AxActorRelation.OwnerOrInitiator, AxAuthorizationOperation.Save, true },
        { AxActorRelation.OwnerOrInitiator, AxAuthorizationOperation.Approve, false },
        { AxActorRelation.OwnerOrInitiator, AxAuthorizationOperation.Reject, false },
        { AxActorRelation.OwnerOrInitiator, AxAuthorizationOperation.Return, false },
        { AxActorRelation.OwnerOrInitiator, AxAuthorizationOperation.Recall, true },
        { AxActorRelation.OwnerOrInitiator, AxAuthorizationOperation.Print, false },

        { AxActorRelation.CurrentApprover, AxAuthorizationOperation.Query, true },
        { AxActorRelation.CurrentApprover, AxAuthorizationOperation.Set, false },
        { AxActorRelation.CurrentApprover, AxAuthorizationOperation.Save, false },
        { AxActorRelation.CurrentApprover, AxAuthorizationOperation.Approve, true },
        { AxActorRelation.CurrentApprover, AxAuthorizationOperation.Reject, true },
        { AxActorRelation.CurrentApprover, AxAuthorizationOperation.Return, true },
        { AxActorRelation.CurrentApprover, AxAuthorizationOperation.Recall, false },
        { AxActorRelation.CurrentApprover, AxAuthorizationOperation.Print, false },

        { AxActorRelation.Viewer, AxAuthorizationOperation.Query, true },
        { AxActorRelation.Viewer, AxAuthorizationOperation.Set, false },
        { AxActorRelation.Viewer, AxAuthorizationOperation.Save, false },
        { AxActorRelation.Viewer, AxAuthorizationOperation.Approve, false },
        { AxActorRelation.Viewer, AxAuthorizationOperation.Reject, false },
        { AxActorRelation.Viewer, AxAuthorizationOperation.Return, false },
        { AxActorRelation.Viewer, AxAuthorizationOperation.Recall, false },
        { AxActorRelation.Viewer, AxAuthorizationOperation.Print, false },

        { AxActorRelation.Administrator, AxAuthorizationOperation.Query, true },
        { AxActorRelation.Administrator, AxAuthorizationOperation.Set, true },
        { AxActorRelation.Administrator, AxAuthorizationOperation.Save, true },
        { AxActorRelation.Administrator, AxAuthorizationOperation.Approve, true },
        { AxActorRelation.Administrator, AxAuthorizationOperation.Reject, true },
        { AxActorRelation.Administrator, AxAuthorizationOperation.Return, true },
        { AxActorRelation.Administrator, AxAuthorizationOperation.Recall, true },
        { AxActorRelation.Administrator, AxAuthorizationOperation.Print, true },

        { AxActorRelation.Unrelated, AxAuthorizationOperation.Query, false },
        { AxActorRelation.Unrelated, AxAuthorizationOperation.Set, false },
        { AxActorRelation.Unrelated, AxAuthorizationOperation.Save, false },
        { AxActorRelation.Unrelated, AxAuthorizationOperation.Approve, false },
        { AxActorRelation.Unrelated, AxAuthorizationOperation.Reject, false },
        { AxActorRelation.Unrelated, AxAuthorizationOperation.Return, false },
        { AxActorRelation.Unrelated, AxAuthorizationOperation.Recall, false },
        { AxActorRelation.Unrelated, AxAuthorizationOperation.Print, false }
    };

    [Theory]
    [MemberData(nameof(RelationMatrix))]
    public void Relation_matrix_is_enforced(
        AxActorRelation relation,
        AxAuthorizationOperation operation,
        bool expected)
    {
        var hasPermission = operation != AxAuthorizationOperation.Print ||
                            relation == AxActorRelation.Administrator;
        var context = new AxAuthorizationContext(true, relation, hasPermission, isEditable: true);

        Assert.Equal(expected, AxAuthorizationPolicy.IsAllowed(context, operation));
    }

    [Theory]
    [InlineData(AxActorRelation.OwnerOrInitiator)]
    [InlineData(AxActorRelation.CurrentApprover)]
    [InlineData(AxActorRelation.Viewer)]
    public void Print_requires_an_explicit_permission(AxActorRelation relation)
    {
        var denied = new AxAuthorizationContext(true, relation, hasOperationPermission: false, isEditable: true);
        var allowed = denied with { HasOperationPermission = true };

        Assert.False(AxAuthorizationPolicy.IsAllowed(denied, AxAuthorizationOperation.Print));
        Assert.True(AxAuthorizationPolicy.IsAllowed(allowed, AxAuthorizationOperation.Print));
    }

    [Fact]
    public void Set_requires_an_editable_object()
    {
        var context = new AxAuthorizationContext(
            tenantMatches: true,
            AxActorRelation.OwnerOrInitiator,
            hasOperationPermission: true,
            isEditable: false);

        Assert.False(AxAuthorizationPolicy.IsAllowed(context, AxAuthorizationOperation.Set));
    }

    [Fact]
    public void Missing_operation_permission_denies_even_an_administrator()
    {
        var context = new AxAuthorizationContext(
            tenantMatches: true,
            AxActorRelation.Administrator,
            hasOperationPermission: false,
            isEditable: true);

        Assert.False(AxAuthorizationPolicy.IsAllowed(context, AxAuthorizationOperation.Query));
    }

    [Fact]
    public void Guessing_another_users_object_id_does_not_authorize_access()
    {
        var state = CreateState(owner: "owner-a", initiator: "owner-a");
        var actor = CreateActor("tenant-a", "user-b");
        var context = AxAuthorizationContext.FromState(
            actor,
            state,
            hasViewerAccess: false,
            hasOperationPermission: true,
            isEditable: true);

        Assert.Equal(AxActorRelation.Unrelated, context.Relations);
        Assert.False(AxAuthorizationPolicy.IsAllowed(context, AxAuthorizationOperation.Query));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Cross_tenant_access_always_denies(bool isAdministrator)
    {
        var state = CreateState(owner: "same-user", initiator: "same-user");
        var actor = CreateActor("tenant-b", "same-user", isAdministrator);
        var context = AxAuthorizationContext.FromState(
            actor,
            state,
            hasViewerAccess: true,
            hasOperationPermission: true,
            isEditable: true);

        Assert.False(context.TenantMatches);
        Assert.Equal(AxActorRelation.Unrelated, context.Relations);
        Assert.All(
            Enum.GetValues<AxAuthorizationOperation>(),
            operation => Assert.False(AxAuthorizationPolicy.IsAllowed(context, operation)));
    }

    [Fact]
    public void Assigned_pending_approver_gets_query_and_workflow_actions_only_by_default()
    {
        var state = CreateState(owner: "owner", initiator: "owner");
        state.Workflow = CreatePendingWorkflow("approver");
        var context = AxAuthorizationContext.FromState(
            CreateActor("tenant-a", "approver"),
            state,
            hasViewerAccess: false,
            hasOperationPermission: true,
            isEditable: true);

        Assert.Equal(AxActorRelation.CurrentApprover, context.Relations);
        Assert.True(AxAuthorizationPolicy.IsAllowed(context, AxAuthorizationOperation.Query));
        Assert.True(AxAuthorizationPolicy.IsAllowed(context, AxAuthorizationOperation.Approve));
        Assert.True(AxAuthorizationPolicy.IsAllowed(context, AxAuthorizationOperation.Reject));
        Assert.True(AxAuthorizationPolicy.IsAllowed(context, AxAuthorizationOperation.Return));
        Assert.False(AxAuthorizationPolicy.IsAllowed(context, AxAuthorizationOperation.Set));
        Assert.False(AxAuthorizationPolicy.IsAllowed(context, AxAuthorizationOperation.Save));
        Assert.False(AxAuthorizationPolicy.IsAllowed(context, AxAuthorizationOperation.Recall));
    }

    [Fact]
    public void Approver_cannot_access_a_task_assigned_to_someone_else()
    {
        var state = CreateState(owner: "owner", initiator: "owner");
        state.Workflow = CreatePendingWorkflow("assigned-approver");
        var context = AxAuthorizationContext.FromState(
            CreateActor("tenant-a", "different-approver"),
            state,
            hasViewerAccess: false,
            hasOperationPermission: true,
            isEditable: true);

        Assert.Equal(AxActorRelation.Unrelated, context.Relations);
        Assert.False(AxAuthorizationPolicy.IsAllowed(context, AxAuthorizationOperation.Query));
        Assert.False(AxAuthorizationPolicy.IsAllowed(context, AxAuthorizationOperation.Approve));
    }

    [Theory]
    [InlineData(null, PegWorkflowStatus.Pending)]
    [InlineData("task-1", PegWorkflowStatus.Approved)]
    public void Approver_without_a_current_pending_assignment_is_unrelated(
        string? currentTaskId,
        PegWorkflowStatus status)
    {
        var state = CreateState(owner: "owner", initiator: "owner");
        state.Workflow = CreatePendingWorkflow("approver");
        state.Workflow.CurrentTaskId = currentTaskId;
        state.Workflow.Status = status;

        var context = AxAuthorizationContext.FromState(
            CreateActor("tenant-a", "approver"),
            state,
            hasViewerAccess: false,
            hasOperationPermission: true,
            isEditable: true);

        Assert.Equal(AxActorRelation.Unrelated, context.Relations);
        Assert.False(AxAuthorizationPolicy.IsAllowed(context, AxAuthorizationOperation.Query));
        Assert.False(AxAuthorizationPolicy.IsAllowed(context, AxAuthorizationOperation.Approve));
    }

    [Fact]
    public void Owner_who_is_also_the_current_approver_keeps_both_relations()
    {
        var state = CreateState(owner: "owner", initiator: "owner");
        state.Workflow = CreatePendingWorkflow("owner");

        var context = AxAuthorizationContext.FromState(
            CreateActor("tenant-a", "owner"),
            state,
            hasViewerAccess: false,
            hasOperationPermission: true,
            isEditable: true);

        Assert.True(context.Relations.HasFlag(AxActorRelation.OwnerOrInitiator));
        Assert.True(context.Relations.HasFlag(AxActorRelation.CurrentApprover));
        Assert.True(AxAuthorizationPolicy.IsAllowed(context, AxAuthorizationOperation.Save));
        Assert.True(AxAuthorizationPolicy.IsAllowed(context, AxAuthorizationOperation.Approve));
    }

    [Fact]
    public void Public_policy_contract_has_no_bypass_input()
    {
        var publicNames = typeof(AxAuthorizationContext)
            .GetProperties()
            .Select(property => property.Name)
            .Concat(typeof(AxAuthorizationPolicy)
                .GetMethods()
                .SelectMany(method => method.GetParameters())
                .Select(parameter => parameter.Name ?? string.Empty));

        Assert.DoesNotContain(publicNames, name => name.Contains("bypass", StringComparison.OrdinalIgnoreCase));
    }

    private static AxUserContext CreateActor(string tenantId, string userName, bool isAdministrator = false) =>
        new(tenantId, userName, new HashSet<string>(StringComparer.Ordinal), isAdministrator);

    private static AxDataState CreateState(string owner, string initiator) =>
        new(
            "object-1",
            "tenant-a",
            "sales-order",
            owner,
            initiator,
            "struct-v1",
            DateTimeOffset.Parse("2026-01-01T00:00:00Z"));

    private static PegActiveState CreatePendingWorkflow(string approver) => new()
    {
        WorkflowInstanceId = "workflow-1",
        CurrentTaskId = "task-1",
        InitiatorUserName = "owner",
        Status = PegWorkflowStatus.Pending,
        ApprovalLevel = 1,
        Approvers = [approver],
        WorkflowVersion = 1,
        PendingFromUtc = DateTimeOffset.Parse("2026-01-01T00:00:00Z")
    };
}
