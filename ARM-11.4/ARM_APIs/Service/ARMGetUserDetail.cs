using ARM_APIs.Interface;
using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMCommon.Model;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using static ARMCommon.Helpers.Constants;

namespace ARM_APIs.Service
{
    public class ARMGetUserDetail : IARMGetUserDetail
    {

        private readonly DataContext _context;
       private readonly IRedisHelper _redis;
      

        public ARMGetUserDetail(DataContext context, IRedisHelper redis)
        {
            _context = context;
            _redis = redis;
        }
        public async Task<ARMUser> GetUserProfileDetails(string ARMSessionId)
        {
            var dictSession = await _redis.HashGetAllDictAsync(ARMSessionId);
            var loginuser = await GetUserProfile(dictSession["APPNAME"], dictSession["USERNAME"], dictSession["USERGROUP"]);
            return loginuser;
        }

  
        private async Task<ARMUser> GetUserProfile(string appName, string username, string usergroup)
        {
            return await _context.ARMUsers.FirstOrDefaultAsync(f => f.username.ToLower() == username.ToLower() && f.appname.ToLower() == appName.ToLower() && f.usergroup.ToLower() == usergroup.ToLower());

        }

      
        public async Task<bool> UpdateUserProfileDetails(string ARMSessionId, string email, string mobileno)
        {
            var dictSession = await _redis.HashGetAllDictAsync(ARMSessionId);
            var loginuser = await GetUserProfile(dictSession["APPNAME"], dictSession["USERNAME"], dictSession["USERGROUP"]);
            loginuser.email = email;
            loginuser.mobileno = mobileno;
            _context.Update(loginuser);
            var updated = await _context.SaveChangesAsync();
            if (updated > 0)
            {
                return true;
            }

            else
            {
                return false;
            }
        }




        public async Task<bool> ValidateUserByEmail(string email)
        {
            var user = await _context.ARMUsers.FirstOrDefaultAsync(p => p.email.ToLower() == email.ToLower());
            if (user == null)
            {
                return false;
            }
            return true;
        }
        public async Task<bool> MobileExist(string mobile)
        {
         var user =  await _context.ARMUsers.FirstOrDefaultAsync(f => f.mobileno.ToLower() == mobile);
            if (user == null)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public async Task<bool> SaveLogindetailsToRedis(string otp, string regId)
        {
            var registrationDetails = new
            {
                otpattemptsleft = 3,
                otp = otp,
            };

            try
            {
                string key = $"{REDIS_PREFIX.ARMUPDATEUSERDETAIL.ToString()}_{regId}";
                await _redis.StringSetAsync(key, JsonConvert.SerializeObject(registrationDetails));
            }
            catch (Exception ex)
            {
                return false;
            }
            return true;
        }
    }
}
