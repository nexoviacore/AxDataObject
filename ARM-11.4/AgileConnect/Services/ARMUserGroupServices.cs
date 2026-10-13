
using AgileConnect.Interfaces;
using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMCommon.Model;

namespace AgileConnect.Services
{
    public class ARMUserGroupServices : IARMUserGroups
    {
        private readonly DataContext _context;

        public ARMUserGroupServices(DataContext context, IConfiguration configuration, ITokenService tokenService)
        {
            _context = context;
        }
        public IEnumerable<ARMUserGroup> UserGroupList()
        {
            var users = _context.ARMUserGroups.ToList();

            return users;
        }


        public IEnumerable<ARMUserGroup> UserGroupList(string Appname)
        {
            // var users = _context.ARMUserGroups.ToList();
            var users = _context.ARMUserGroups.Where(p => p.AppName == Appname).ToList();

            return users;
        }

        public ARMUserGroup GetUserGroupById(string id)
        {
            return GetUserGroupBy(id);
        }

        private ARMUserGroup GetUserGroupBy(string id)
        {
            var user = _context.ARMUserGroups.Find(Guid.Parse(id));
            if (user == null)
                throw new KeyNotFoundException("User not found");
            return user;
        }

        public bool DeleteUserGroup(string id)
        {
            var user = GetUserGroupBy(id);
            if (user == null)
                throw new KeyNotFoundException("User not found");

            // _context.Entry(aRMUserResult).State= EntityState.Modified;
            _context.Remove(user);
            var i = _context.SaveChanges();
            if (i > 0)
            {
                return true;
            }
            else
                return false;

        }
    }
}
