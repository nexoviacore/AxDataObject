
using AgileConnect.Filter;
using AgileConnect.Interfaces;
using ARMCommon.Helpers;
using ARMCommon.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgileConnect.Controllers
{
    [ServiceFilter(typeof(SessionActionFilter))]
    [Authorize]

    public class ARMUsersController : Controller
    {


        private readonly DataContext _context;
        private readonly IARMUsers _ARMUsers;
        private readonly Utils _common;

        public ARMUsersController(DataContext context, IARMUsers ARMUsers, Utils common)
        {
            _context = context;
            _ARMUsers = ARMUsers;
            _common = common;
        }

        [HttpGet]
        public IActionResult Create(string appName)
        {
            ARMUser userGroup = new ARMUser();

            string strApp = appName;

            var userGrouplists = _context.ARMUserGroups.GroupBy(x => x.Name)
                                              .Select(y => y.FirstOrDefault())
                                              .ToList();
            var userGrouplist = userGrouplists.Select(u => u.Name).ToList();


            string groupStr = (string.Join(",", userGrouplist.Select(x => x.ToString()).ToArray()));


            List<string> strAppList = strApp.Split(',').ToList();
            List<string> Usergroupslist = groupStr.Split(',').ToList();

            userGroup.app1 = new List<App>();
            userGroup.UserGroupName1 = new List<UserGroupName>();

            foreach (string appItem in strAppList)
            {
                userGroup.app1.Add(new App() { ID = appItem, Name = appItem });

            };
            foreach (string userGroupItem in Usergroupslist)
            {
                userGroup.UserGroupName1.Add(new UserGroupName() { ID = userGroupItem, Name = userGroupItem });

            };

            return View(userGroup);
        }
        [HttpPost]
        public async Task<IActionResult> Create(string appName, ARMUser user)
        {
            ARMResult result;
            if (string.IsNullOrEmpty(user.username) || string.IsNullOrEmpty(user.appname))
            {


                result = new ARMResult(false, "Required fields (Username/Usergroup details is missing");
                return BadRequest(result);
            }

            var response = await _ARMUsers.AddUser(user);

            if (response == null)
            {
                return BadRequest(response.result);
            }
            else
            {
                return RedirectToAction("ListByApp", "ARMUsers", new { Appname = appName });
            }
            return View();
        }

        [HttpGet]
        public IActionResult Edit(string id)
        {

            var user = _ARMUsers.GetById(id);


            string str = GetAppList();

            var userGrouplists = _context.ARMUserGroups.GroupBy(x => x.Name)
                                          .Select(y => y.FirstOrDefault())
                                          .ToList();

            var Grouplist = userGrouplists.Select(u => u.Name).ToList();

            string grpstr = (string.Join(",", Grouplist.Select(x => x.ToString()).ToArray()));


            List<string> strList = str.Split(',').ToList();
            List<string> grplist = grpstr.Split(',').ToList();

            user.app1 = new List<App>();
            user.UserGroupName1 = new List<UserGroupName>();

            foreach (string appItem in strList)
            {
                user.app1.Add(new App() { ID = appItem, Name = appItem });

            };
            foreach (string userGroupItem in grplist)
            {
                user.UserGroupName1.Add(new UserGroupName() { ID = userGroupItem, Name = userGroupItem });

            };
            return View(user);
        }

        [HttpGet]
        public IActionResult Delete(string id)
        {
            var user = _ARMUsers.GetById(id);
            var appName = user.appname;

            var result = _ARMUsers.Delete(id);
            if (result)
                return RedirectToAction("ListByApp", "ARMUsers", new { Appname = appName });

            return View();
        }


        [HttpPost]
        public IActionResult Edit(string id, ARMUser aRMUserResult)
        {
            string appName = aRMUserResult.appname;
            var result = _ARMUsers.UpdateUser(id, aRMUserResult);
            if (result)
                return RedirectToAction("ListByApp", "ARMUsers", new { Appname = appName });

            return View(aRMUserResult);
        }


        public IActionResult List()
        {

            var usersList = _ARMUsers.UsersList();
            return View(usersList);
        }

        public IActionResult ListByApp(string Appname)
        {
            ViewBag.result = Appname;
            var usersList = _ARMUsers.UsersList(Appname);
            return View(usersList);
        }

        public async Task<IActionResult> ARMAddUser(ARMUser user)
        {
            ARMResult resultData;


            if (string.IsNullOrEmpty(user.username) || string.IsNullOrEmpty(user.appname))
            {
                resultData = new ARMResult(false, "Username/Usergroup details is missing.");
                return BadRequest(resultData);

            }

            var response = await _ARMUsers.AddUser(user);

            if (response == null)
            {
                return BadRequest(response.result);
            }
            else
            {
                return Ok(response.result);
            }

        }


        [HttpGet("{id}")]
        public IActionResult GetById(string id)
        {
            var user = _ARMUsers.GetById(id);
            return Ok(user);
        }

        private string GetAppList()
        {
            var appList = _context.ARMApps.Select(u => u.AppName).ToList();

            string appnames = (string.Join(",", appList.Select(x => x.ToString()).ToArray()));

            return appnames;
        }


        [HttpGet]
        public async Task<RedirectToActionResult> ImportUsers(string appName, ARMUser user)
        {
            int importedCount = 0;
            int skippedCount = 0;

            var response = new ARMResult();
            var Axpertusers = _context.AxpertUsers.Where(p => p.AppName == user.appname).ToList();

            if (Axpertusers == null)
                throw new KeyNotFoundException("Users does not  exist");

            foreach (var i in Axpertusers)
            {
                user.appname = i.AppName;
                user.username = i.UserName;
                user.email = i.Email;
                user.mobileno = i.MobileNo;
                user.usergroup = "HR";
                user.isactive = i.IsActive;
                var userstoAdd = _context.ARMUsers.Where(p => p.email == i.Email).ToList();


                if (userstoAdd.Count > 0)
                {
                    skippedCount++;
                    continue;
                }
                response = await _ARMUsers.AddUser(user);
                importedCount++;
            }
            TempData["importedCount"] = importedCount;
            TempData["skippedCount"] = skippedCount;
            return RedirectToAction("ListByApp", "ARMUsers", new { Appname = appName });

        }




    }
}
