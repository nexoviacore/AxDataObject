
using ARMCommon.Model;

namespace AgileConnect.Interfaces
{
    public interface IARMUsers
    {

        ARMUser GetById(string id);
        abstract Task<ARMResult> AddUser(ARMUser user);
        abstract Task<ARMResult> SaveUserToDatabase(ARMUser user, bool isActive);
        bool Delete(string id);
        abstract Task SaveUserAndGroup(ARMUser user, bool isActive);
        bool UpdateUser(string id, ARMUser aRMUserResult);
        IEnumerable<ARMUser> UsersList();
        IEnumerable<ARMUser> UsersList(string Appname);

    }
}
