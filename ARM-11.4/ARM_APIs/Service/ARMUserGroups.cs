using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ARM_APIs.Interface;
using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMCommon.Model;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace ARM_APIs.Service
{
    public class ARMUserGroups : IARMUserGroups
    {
        private readonly DataContext _context;
        private readonly IRedisHelper _redis;

        public ARMUserGroups(DataContext context, IRedisHelper redis)
        {
            _context = context;
            _redis = redis;
        }

        public async Task<object> GetARMUserGroups(string appname)
        {
            ARMResult result;
            UserGroupDetails userGroupDetail = new UserGroupDetails();
            userGroupDetail.userGroupNames = new List<UserGrouplIst>();

            string rediskey = "ARMActiveUserGroups-" + appname;
            string appConfig = await _redis.StringGetAsync(rediskey);
            if (!string.IsNullOrEmpty(appConfig))
            {
                JArray userGroupArray = JArray.Parse(appConfig);
                var userGroups = userGroupArray.Select(c => (UserGroupName: c["Name"], UserGroupType: c["GroupType"]));

                foreach (var user in userGroups)
                {
                    userGroupDetail.userGroupNames.Add(new UserGrouplIst
                    {
                        usergroup = user.UserGroupName.ToString(),
                        grouptype = user.UserGroupType.ToString()
                    });
                }

                if (userGroupDetail.userGroupNames.Count == 0)
                {
                    return Constants.RESULTS.ERROR;
                }
                else
                {
                    result = new ARMResult();
                    result.result.Add("message", "SUCCESS");
                    result.result.Add("data", userGroupDetail.userGroupNames);
                    return result;
                }
            }
            else
            {
                var usersGroups = _context.ARMUserGroups.Where(p => p.AppName == appname && p.IsActive);
                var userGroupsList = usersGroups.Select(x => new { x.Name, x.GroupType }).ToList();

                if (userGroupsList == null || userGroupsList.Count == 0)
                {
                    return Constants.RESULTS.ERROR;
                }

                foreach (var user in userGroupsList)
                {
                    userGroupDetail.userGroupNames.Add(new UserGrouplIst
                    {
                        usergroup = user.Name.ToString(),
                        grouptype = user.GroupType.ToString()
                    });
                }

                result = new ARMResult();
                result.result.Add("message", "SUCCESS");
                result.result.Add("data", userGroupDetail.userGroupNames);
                return result;

            }

        }

    }

}
