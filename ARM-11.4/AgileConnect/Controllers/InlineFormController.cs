using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMCommon.Model;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgileConnect.Controllers
{
    public class InlineFormController : Controller
    {
        private DataContext _context;
        private readonly IRedisHelper _redis;
        private readonly Utils _common;

        public InlineFormController(DataContext context, IRedisHelper redis, Utils common)
        {
            _context = context;
            _redis = redis;
            _common = common;
        }
        public IActionResult Create(string Appname)
        {
            var inlineForm = new AxInlineForm();
            string str = Appname;
            List<string> strList = str.Split(',').ToList();
            inlineForm.app1 = new List<App>();


            foreach (string appItem in strList)
            {
                inlineForm.app1.Add(new App() { ID = appItem, Name = appItem });

            };
            inlineForm.UserGroups = _context.ARMUserGroups.GroupBy(x => x.Name)
                                               .Select(y => y.FirstOrDefault())
                                               .ToList();

            inlineForm.Modules = _context.AxModules.Select(m => m).ToList();
            inlineForm.SubModules = _context.AxSubModules.Select(m => m).ToList();
            return View(inlineForm);
        }

        public async Task<IActionResult> List()
        {

            return View(await _context.AxInLineForm.OrderByDescending(a => a.Name).ToListAsync());

        }

        public IActionResult ListByApp(string Appname)
        {
            ViewBag.result = Appname;
            var users = AxModuleList(Appname);
            return View(users);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AxInlineForm userGroup)
        {

            if (ModelState.IsValid)
            {
                string rediskey = $"{Constants.REDIS_PREFIX.AXINLINEFORM.ToString()}_{userGroup.Name}";
                userGroup.FormCreator = HttpContext.Session.GetString("Username");
                userGroup.FormCreatedOn = DateTime.Now.ToString();
                _context.Add(userGroup);
                await _context.SaveChangesAsync();
                await _redis.StringSetAsync(rediskey, userGroup.FormText);
                return RedirectToAction("ListByApp", "InlineForm", new { Appname = userGroup.appName });
            }

            return View();
        }

        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var inlineForm = await _context.AxInLineForm.FindAsync(id);
            if (inlineForm == null)
            {
                return NotFound();
            }
            string str = _common.GetAppList();
            List<string> strList = str.Split(',').ToList();
            inlineForm.app1 = new List<App>();


            foreach (string appItem in strList)
            {
                inlineForm.app1.Add(new App() { ID = appItem, Name = appItem });

            };
            inlineForm.UserGroups = _context.ARMUserGroups.GroupBy(x => x.Name)
                                                  .Select(y => y.FirstOrDefault())
                                                  .ToList();

            inlineForm.Modules = _context.AxModules.Select(m => m).ToList();
            inlineForm.SubModules = _context.AxSubModules.Select(m => m).ToList();
            return View(inlineForm);
        }

        [HttpPost]

        public async Task<IActionResult> Edit(AxInlineForm dataSources)
        {


            if (ModelState.IsValid)
            {
                string rediskey = $"{Constants.REDIS_PREFIX.AXINLINEFORM.ToString()}_{dataSources.Name}";
                try
                {
                    //dataSources.FormCreator = User.Identity.Name;
                    dataSources.FormUpdatedBy = HttpContext.Session.GetString("Username");
                    dataSources.FormUpdatedOn = DateTime.Now.ToString();
                    _context.Update(dataSources);
                    await _context.SaveChangesAsync();
                    await _redis.StringSetAsync(rediskey, dataSources.FormText);

                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DataSourcesExists(dataSources.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                return RedirectToAction("ListByApp", "InlineForm", new { Appname = dataSources.appName });
            }
            return View(dataSources);



        }

        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var dataSources = await _context.AxInLineForm
                .FirstOrDefaultAsync(m => m.Id == id);
            if (dataSources == null)
            {
                return NotFound();
            }

            return View(dataSources);
        }


        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var dataSources = await _context.AxInLineForm.FindAsync(id);
            _context.AxInLineForm.Remove(dataSources);
            await _context.SaveChangesAsync();

            return RedirectToAction("List");
        }

        private bool DataSourcesExists(Guid id)
        {
            return _context.AxInLineForm.Any(e => e.Id == id);
        }

        private IEnumerable<AxInlineForm> AxModuleList(string Appname)
        {
            // var users = _context.ARMUserGroups.ToList();
            var users = _context.AxInLineForm.Where(p => p.appName == Appname).ToList();

            return users;
        }
    }
}
