namespace AxData.Application.Authorization;

[Flags]
public enum AxActorRelation
{
    Unrelated = 0,
    OwnerOrInitiator = 1,
    CurrentApprover = 2,
    Viewer = 4,
    Administrator = 8
}
