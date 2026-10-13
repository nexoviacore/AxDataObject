using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMCommon.Model;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ARM.Controllers
{
    public class AXSubModuleController : Controller
    {


        private readonly DataContext _context;
        private readonly IRedisHelper _redis;
        private readonly Utils _common;



        public AXSubModuleController(DataContext context, IRedisHelper redis, Utils common)
        {
            _context = context;
            _redis = redis;
            _common = common;
        }

        public IActionResult Create(string Appname)
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AxSubModule userGroup)
        {

            if (ModelState.IsValid)
            {
                var id = new Guid();
                userGroup.Id = id;
                _context.Add(userGroup);

                await _context.SaveChangesAsync();
                return RedirectToAction("ListByApp", "AxSubModule", new { Appname = userGroup.appName });
            }

            return View();
        }

        public async Task<IActionResult> List()

        {

            return View(await _context.AxSubModules.OrderByDescending(a => a.SubModuleName).ToListAsync());

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

            var modulePages = await _context.AxSubModules.FindAsync(id);
            string str = _common.GetAppList();
            List<string> strList = str.Split(',').ToList();
            modulePages.app1 = new List<App>();


            foreach (string appItem in strList)
            {
                modulePages.app1.Add(new App() { ID = appItem, Name = appItem });

            };
            if (modulePages == null)
            {
                return NotFound();
            }
            return View(modulePages);
        }

        [HttpPost]

        public async Task<IActionResult> Edit(AxSubModule modulePages)
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
                    if (!SubModuleExists(modulePages.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                return RedirectToAction("ListByApp", "AxSubModule", new { Appname = modulePages.appName });
            }
            return View(modulePages);



        }

        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var modulePage = await _context.AxSubModules
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
            var modulePage = await _context.AxSubModules.FindAsync(id);
            _context.AxSubModules.Remove(modulePage);
            await _context.SaveChangesAsync();

            return RedirectToAction("List");
        }

        private bool SubModuleExists(Guid id)
        {
            return _context.AxSubModules.Any(e => e.Id == id);
        }

        private IEnumerable<AxSubModule> AxModuleList(string Appname)
        {
            // var users = _context.ARMUserGroups.ToList();
            var users = _context.AxSubModules.Where(p => p.appName == Appname).ToList();

            return users;
        }

    }


}

