
using AgileConnect.Interfaces;
using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMCommon.Model;
using AutoMapper;

namespace AgileConnect.Services
{
    public class ARMUserService : IARMUsers
    {
        private readonly DataContext _context;
        private readonly IConfiguration _config;
        private readonly ITokenService _tokenService;
        private readonly IMapper _mapper;
        static string appConfigFilePath = string.Empty;

        public ARMUserService(DataContext context, IConfiguration configuration, ITokenService tokenService, IMapper mapper)
        {
            _context = context;
            _config = configuration;
            _tokenService = tokenService;
            _mapper = mapper;
            appConfigFilePath = "C:\\AgileConnect\\AppConfig.Json";
        }



        public async Task<ARMResult> AddUser(ARMUser user)
        {
            var group = Guid.NewGuid();//_context.UserGroups.FirstOrDefault(f => f.ID == login.GroupID);

            return await SaveUserToDatabase(user, true);
        }

        public async Task<ARMResult> SaveUserToDatabase(ARMUser user, bool isActive)
        {
            ARMResult resultData;

            try
            {
                await SaveUserAndGroup(user, isActive);
                return resultData = new ARMResult(true, "Success.");
            }
            catch (Exception ex)
            {
                return new ARMResult(false, ex.Message);
            }

        }

        public async Task SaveUserAndGroup(ARMUser model, bool isActive)
        {
            try
            {
                var userGuid = Guid.NewGuid();
                var UserGrpId = Guid.NewGuid();
                var user = new ARMUser()
                {
                    ID = userGuid,
                    appname = model.appname,
                    username = model.username,
                    password = "pfghjkl",
                    email = model.email,
                    mobileno = model.mobileno,
                    usergroupid = UserGrpId,
                    usergroup = model.usergroup,
                    isactive = isActive
                };
                _context.ARMUsers.Add(user);


                await _context.SaveChangesAsync();
            }

            catch (Exception ex)
            {
                var msg = ex;

            }

        }

        public bool UpdateUser(string id, ARMUser aRMUserResult)
        {
            if (Guid.Parse(id) != aRMUserResult.ID)
            {
                throw new KeyNotFoundException("User not found");
            }
            _context.Update(aRMUserResult);
            var i = _context.SaveChanges();
            if (i > 0)
            {
                return true;
            }
            else
                return false;

        }
        public bool Delete(string id)
        {
            var user = getUser(id);
            if (user == null)
                throw new KeyNotFoundException("User not found");

            _context.Remove(user);
            var i = _context.SaveChanges();
            if (i > 0)
            {
                return true;
            }
            else
                return false;

        }

        public ARMUser GetById(string id)
        {
            return getUser(id);
        }

        private ARMUser getUser(string id)
        {
            var user = _context.ARMUsers.Find(Guid.Parse(id));
            if (user == null)
                throw new KeyNotFoundException("User not found");
            return user;
        }

        public IEnumerable<ARMUser> UsersList()
        {
            var users = _context.ARMUsers.ToList();

            return users;
        }

        public IEnumerable<ARMUser> UsersList(string Appname)
        {

            var users = _context.ARMUsers.Where(p => p.appname == Appname).ToList();

            return users;
        }

    }
}
