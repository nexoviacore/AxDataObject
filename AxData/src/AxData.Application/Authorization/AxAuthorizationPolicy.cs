namespace AxData.Application.Authorization;

public static class AxAuthorizationPolicy
{
    /// <summary>Evaluates whether the actor relationship satisfies the requested AxData operation.</summary>
    /// <remarks>Administrative access is explicit; an object reference alone never grants access.</remarks>
    public static bool IsAllowed(
        AxAuthorizationContext context,
        AxAuthorizationOperation operation)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!context.TenantMatches || !context.HasOperationPermission)
        {
            return false;
        }

        if (operation == AxAuthorizationOperation.Initialize)
        {
            return true;
        }

        return operation switch
        {
            AxAuthorizationOperation.Query => HasAnyRelation(
                context,
                AxActorRelation.Initiator |
                AxActorRelation.CurrentApprover |
                AxActorRelation.Viewer),
            AxAuthorizationOperation.Set =>
                context.IsEditable && HasRelation(context, AxActorRelation.Initiator),
            AxAuthorizationOperation.Save => HasRelation(context, AxActorRelation.Initiator),
            AxAuthorizationOperation.Approve or
            AxAuthorizationOperation.Reject or
            AxAuthorizationOperation.Return => HasRelation(context, AxActorRelation.CurrentApprover),
            AxAuthorizationOperation.Recall => HasRelation(context, AxActorRelation.Initiator),
            AxAuthorizationOperation.Print => HasAnyRelation(
                context,
                AxActorRelation.Initiator |
                AxActorRelation.CurrentApprover |
                AxActorRelation.Viewer),
            _ => false
        };
    }

    private static bool HasRelation(AxAuthorizationContext context, AxActorRelation relation) =>
        (context.Relations & relation) == relation;

    private static bool HasAnyRelation(AxAuthorizationContext context, AxActorRelation relations) =>
        (context.Relations & relations) != AxActorRelation.Unrelated;
}
