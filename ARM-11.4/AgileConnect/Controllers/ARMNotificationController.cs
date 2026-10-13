
using AgileConnect.Filter;
using AgileConnect.Interfaces;
using ARMCommon.Helpers;
using ARMCommon.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgileConnect.Controllers
{
    [ServiceFilter(typeof(SessionActionFilter))]
    [Authorize]

    public class ARMNotificationController : Controller
    {
        private readonly IARMNotification _login;
        private readonly DataContext _context;
        private readonly IConfiguration _config;
        private readonly Utils _common;

        public ARMNotificationController(DataContext context, IARMNotification login, IConfiguration config, Utils common)
        {
            _config = config;
            _context = context;
            _login = login;
            _common = common;
        }

        public IActionResult List()
        {
            var users = _login.NotificationTemplateList();
            return View(users);
        }
        public IActionResult ListByApp(string Appname)
        {
            ViewBag.result = Appname;
            var users = _login.NotificationTemplateList(Appname);
            return View(users);
        }

        public IActionResult Create(string Appname)
        {

            ARMNotificationTemplate userGroup = new ARMNotificationTemplate();

            string str = Appname;
            List<string> strList = str.Split(',').ToList();
            userGroup.app1 = new List<App>();


            foreach (string appItem in strList)
            {
                userGroup.app1.Add(new App() { ID = appItem, Name = appItem });

            };
            return View(userGroup);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string Appname, ARMNotificationTemplate userGroup)
        {

            if (ModelState.IsValid)
            {
                _context.Add(userGroup);

                await _context.SaveChangesAsync();
                return RedirectToAction("List");
            }

            return View();
        }


        [HttpGet]
        public IActionResult Edit(int id)
        {
            var userGroup = _login.GetTemplateById(id);
            string str = _common.GetAppList();
            List<string> strList = str.Split(',').ToList();
            userGroup.app1 = new List<App>();
            foreach (string appItem in strList)
            {
                userGroup.app1.Add(new App() { ID = appItem, Name = appItem });

            };

            return View(userGroup);

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ARMNotificationTemplate userGroup)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var user = _context.NotificationTemplate.Find(userGroup.Id);
                    if (user == null)
                        throw new KeyNotFoundException("User not found");
                    _context.Remove(user);
                    _context.Update(userGroup);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserGroupExists(userGroup.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction("List");
            }
            return View(userGroup);
        }


        [HttpGet]
        public IActionResult Delete(int id)
        {
            var result = _login.DeleteTemplate(id);
            if (result)
                return RedirectToAction("List");

            return View();

        }


        private bool UserGroupExists(int id)
        {
            return _context.NotificationTemplate.Any(e => e.Id == id);
        }
    }
}
