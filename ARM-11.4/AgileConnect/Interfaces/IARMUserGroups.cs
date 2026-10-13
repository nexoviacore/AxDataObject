using ARMCommon.Model;

namespace AgileConnect.Interfaces
{
    public interface IARMUserGroups
    {
        bool DeleteUserGroup(string id);
        IEnumerable<ARMUserGroup> UserGroupList();
        IEnumerable<ARMUserGroup> UserGroupList(string Appname);
        ARMUserGroup GetUserGroupById(string id);
    }
}
