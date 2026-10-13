using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMCommon.Model;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ARM.Controllers
{
    public class AxModuleController : Controller
    {

        private readonly DataContext _context;
        private readonly IRedisHelper _redis;
        private readonly Utils _common;

        public AxModuleController(DataContext context, IRedisHelper redis, Utils common)
        {
            _context = context;
            _redis = redis;
            _common = common;
        }

        public IActionResult Create(string Appname)
        {
            var modulePages = new AxModule();
            string str = Appname;
            List<string> strList = str.Split(',').ToList();
            modulePages.app1 = new List<App>();


            foreach (string appItem in strList)
            {
                modulePages.app1.Add(new App() { ID = appItem, Name = appItem });

            };
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AxModule userGroup)
        {
            if (ModelState.IsValid)
            {
                var id = new Guid();
                userGroup.Id = id;
                _context.Add(userGroup);
                await _context.SaveChangesAsync();

                return RedirectToAction("ListByApp", "AxModule", new { Appname = userGroup.appName });
            }

            return View();
        }

        public async Task<IActionResult> List()

        {

            return View(await _context.AxModules.OrderByDescending(a => a.ModuleName).ToListAsync());

        }
        public IActionResult ListByApp(string Appname)
        {
            ViewBag.result = Appname;
            var users = AxModuleList(Appname);
            return View(users);
        }

        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var modulePages = await _context.AxModules.FindAsync(id);
            if (modulePages == null)
            {
                return NotFound();
            }
            string str = _common.GetAppList();
            List<string> strList = str.Split(',').ToList();
            modulePages.app1 = new List<App>();


            foreach (string appItem in strList)
            {
                modulePages.app1.Add(new App() { ID = appItem, Name = appItem });

            };
            return View(modulePages);
        }

        [HttpPost]

        public async Task<IActionResult> Edit(AxModule modulePages)
        {


            if (ModelState.IsValid)
            {
                try
                {

                    _context.Update(modulePages);
                    await _context.SaveChangesAsync();

                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ModuleExists(modulePages.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                return RedirectToAction("ListByApp", "AxModule", new { Appname = modulePages.appName });
            }
            return View(modulePages);



        }
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var modulePage = await _context.AxModules
                .FirstOrDefaultAsync(m => m.Id == id);
            if (modulePage == null)
            {
                return NotFound();
            }

            return View(modulePage);
        }


        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var modulePage = await _context.AxModules.FindAsync(id);
            _context.AxModules.Remove(modulePage);
            await _context.SaveChangesAsync();

            return RedirectToAction("List");
        }
        private bool ModuleExists(Guid id)
        {
            return _context.AxModules.Any(e => e.Id == id);
        }
        private IEnumerable<AxModule> AxModuleList(string Appname)
        {
            // var users = _context.ARMUserGroups.ToList();
            var users = _context.AxModules.Where(p => p.appName == Appname).ToList();

            return users;
        }


    }
}
