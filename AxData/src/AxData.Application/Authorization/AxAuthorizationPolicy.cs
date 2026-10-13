namespace AxData.Application.Authorization;

public static class AxAuthorizationPolicy
{
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

        if (HasRelation(context, AxActorRelation.Administrator))
        {
            return operation != AxAuthorizationOperation.Set || context.IsEditable;
        }

        return operation switch
        {
            AxAuthorizationOperation.Query => HasAnyRelation(
                context,
                AxActorRelation.OwnerOrInitiator |
                AxActorRelation.CurrentApprover |
                AxActorRelation.Viewer),
            AxAuthorizationOperation.Set =>
                context.IsEditable && HasRelation(context, AxActorRelation.OwnerOrInitiator),
            AxAuthorizationOperation.Save => HasRelation(context, AxActorRelation.OwnerOrInitiator),
            AxAuthorizationOperation.Approve or
            AxAuthorizationOperation.Reject or
            AxAuthorizationOperation.Return => HasRelation(context, AxActorRelation.CurrentApprover),
            AxAuthorizationOperation.Recall => HasRelation(context, AxActorRelation.OwnerOrInitiator),
            AxAuthorizationOperation.Print => HasAnyRelation(
                context,
                AxActorRelation.OwnerOrInitiator |
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
