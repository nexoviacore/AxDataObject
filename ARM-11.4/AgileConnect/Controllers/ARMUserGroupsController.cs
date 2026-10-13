
using AgileConnect.Filter;
using AgileConnect.Interfaces;
using ARMCommon.ActionFilter;
using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMCommon.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NpgsqlTypes;
using System.Data;

namespace AgileConnect.Controllers
{
    [ServiceFilter(typeof(SessionActionFilter))]
    [Authorize]

    public class ARMUserGroupsController : Controller
    {

        private readonly IARMUserGroups _userGroups;
        private readonly DataContext _context;
        private readonly IConfiguration _config;
        private readonly Utils _common;

        private readonly IPostgresHelper _postGres;



        public ARMUserGroupsController(DataContext context, IARMUserGroups userGroups, IConfiguration config, Utils common, IPostgresHelper postGres)
        {
            _config = config;
            _context = context;
            _userGroups = userGroups;
            _common = common;
            _postGres = postGres;
        }

        public IActionResult List()
        {
            var userGroups = _userGroups.UserGroupList();
            return View(userGroups);
        }


        public IActionResult ListByApp(string Appname)
        {
            ViewBag.result = Appname;
            var users = _userGroups.UserGroupList(Appname);
            return View(users);
        }


        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userGroup = await _context.ARMUserGroups
                .FirstOrDefaultAsync(m => m.ID == id);
            if (userGroup == null)
            {
                return NotFound();
            }

            return View(userGroup);
        }


        public async Task<IActionResult> Create(string appName)

        {
            ARMUserGroup userGroup = new ARMUserGroup();

            string str = appName;
            List<string> strList = str.Split(',').ToList();

            Dictionary<string, string> config = await _common.GetDBConfigurations(appName);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];
            string Template = Constants_SQL.DISNTICTROLES.ToString();
            DataTable table = new DataTable();
            // table = await _postGres.ExecuteSql(Template, connectionString, new string[] { }, new NpgsqlDbType[] { }, new object[] { });

            IDbHelper dbHelper = DBHelper.CreateDbHelper(Template, dbType, connectionString, new string[] { }, new DbType[] { }, new object[] { });
            table = await dbHelper.ExecuteQueryAsync(Template, connectionString, new string[] { }, new DbType[] { }, new object[] { });
            if (table.Rows.Count > 0)
            {

                string jsonString = string.Empty;
                jsonString = JsonConvert.SerializeObject(table, Formatting.Indented);
                JArray userGroupArray = JArray.Parse(jsonString);
                var userGroups = userGroupArray.Select(c => c["USERROLES"] ?? c["userroles"]).ToList();
                if (dbType.ToLower() == "oracle".ToLower())
                {
                    userGroups = userGroupArray.Select(c => c["USERROLES"] ?? c["userroles"]).ToList();

                }
                userGroup.Role = new List<Role>();
                foreach (var user in userGroups)
                {
                    userGroup.Role.Add(new Role() { ID = user.ToString(), Name = user.ToString() });
                }
            }
            return View(userGroup);

        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string appName, ARMUserGroup userGroup)
        {

            if (ModelState.IsValid)
            {
                userGroup.ID = Guid.NewGuid();
                //userGroup.ApprovalFrom = Guid.NewGuid();
                _context.Add(userGroup);
                await _context.SaveChangesAsync();
                return RedirectToAction("ListByApp", "ARMUserGroups", new { Appname = appName });
            }

            return View();
        }


        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var userGroup = _userGroups.GetUserGroupById(id);
            string GroupType = userGroup.GroupType;
            ViewBag.DataSourceType = GroupType;
            string strAppList = _common.GetAppList();
            List<string> strList = strAppList.Split(',').ToList();
            //userGroup.app1 = new List<App>();


            //foreach (string appItem in strList)
            //{
            //    userGroup.app1.Add(new App() { ID = appItem, Name = appItem });

            //};

            //ParamsDetails parameters = new ParamsDetails();
            //parameters.ParamsNames = new List<ConnectionParamsList>();


            Dictionary<string, string> config = await _common.GetDBConfigurations(userGroup.AppName);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];
            string Template = Constants_SQL.DISNTICTROLES.ToString();
            DataTable table = new DataTable();
            // table = await _postGres.ExecuteSql(Template, connectionString, new string[] { }, new NpgsqlDbType[] { }, new object[] { });

            IDbHelper dbHelper = DBHelper.CreateDbHelper(Template, dbType, connectionString, new string[] { }, new DbType[] { }, new object[] { });
            table = await dbHelper.ExecuteQueryAsync(Template, connectionString, new string[] { }, new DbType[] { }, new object[] { });
            if (table.Rows.Count > 0)
            {

                string jsonString = string.Empty;
                jsonString = JsonConvert.SerializeObject(table, Formatting.Indented);
                JArray userGroupArray = JArray.Parse(jsonString);
                var userGroups = userGroupArray.Select(c => c["userroles"]);
                if (dbType.ToLower() == "oracle".ToLower())
                {
                    userGroups = userGroupArray.Select(c => c["USERROLES"]).ToList();
                }
                userGroup.Role = new List<Role>();
                foreach (var user in userGroups)
                {
                    userGroup.Role.Add(new Role() { ID = user.ToString(), Name = user.ToString() });

                }
            }



            return View(userGroup);

        }

        [HttpPost]
        public async Task<IActionResult> Edit(string id, ARMUserGroup userGroup)
        {

            if (string.IsNullOrWhiteSpace(userGroup.Name))
            {
                ModelState.AddModelError("Name", "Name is required.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(userGroup);

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserGroupExists(userGroup.ID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction("ListByApp", "ARMUserGroups", new { Appname = userGroup.AppName });
            }
            return View(userGroup);
        }

        [HttpGet]
        public IActionResult Delete(string id)
        {
            var user = _userGroups.GetUserGroupById(id);
            var appName = user.AppName;
            var result = _userGroups.DeleteUserGroup(id);
            if (result)
                return RedirectToAction("ListByApp", "ARMUserGroups", new { Appname = appName });

            return View();

        }



        private bool UserGroupExists(Guid id)
        {
            return _context.ARMUserGroups.Any(e => e.ID == id);
        }


    }
}
