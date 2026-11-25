namespace AxData.Application.Authorization;

[Flags]
public enum AxActorRelation
{
    Unrelated = 0,
    Initiator = 1,
    CurrentApprover = 2,
    Viewer = 4
}
