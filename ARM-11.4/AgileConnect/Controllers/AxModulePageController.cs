using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMCommon.Model;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace ARM.Controllers
{
    public class AxModulePageController : Controller
    {
        private readonly DataContext _context;
        private readonly IRedisHelper _redis;
        private readonly Utils _common;

        public AxModulePageController(DataContext context, IRedisHelper redis, Utils common)
        {
            _context = context;
            _redis = redis;
            _common = common;
        }
        public IActionResult Create(string Appname)
        {
            var modulePages = new AxModulePages();
            string str = Appname;
            List<string> strList = str.Split(',').ToList();
            modulePages.app1 = new List<App>();


            foreach (string appItem in strList)
            {
                modulePages.app1.Add(new App() { ID = appItem, Name = appItem });

            };
            modulePages.UserGroups = _context.ARMUserGroups.GroupBy(x => x.Name)
                                               .Select(y => y.FirstOrDefault())
                                               .ToList();
            modulePages.AxInlineForm = _context.AxInLineForm.Select(m => m).ToList();
            modulePages.Modules = _context.AxModules.Select(m => m).ToList();
            modulePages.SubModules = _context.AxSubModules.Select(m => m).ToList();
            return View(modulePages);
        }

        public async Task<IActionResult> List()

        {

            return View(await _context.AxModulePages.OrderByDescending(a => a.PageName).ToListAsync());

        }

        public IActionResult ListByApp(string Appname)
        {
            ViewBag.result = Appname;
            var users = AxModuleList(Appname);
            return View(users);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AxModulePages userGroup)
        {

            if (ModelState.IsValid)
            {
                _context.Add(userGroup);

                await _context.SaveChangesAsync();
                var tableExists = _context.Database.GetDbConnection().GetSchema("Tables").AsEnumerable()
.Any(x => x.Field<string>("TABLE_NAME").ToLower() == userGroup.PageDataTable.ToLower());
                if (tableExists)
                {
                    Console.WriteLine("Exist");
                }
                else
                {
                    try
                    {
                        _context.Database.ExecuteSqlRaw($"CREATE TABLE {userGroup.PageDataTable} (Id UUID PRIMARY KEY,formname VARCHAR(255) NOT NULL, keyvalue VARCHAR(255) NOT NULL,paneldata TEXT NOT NULL,createddatetime timestamp NOT NULL , ActivateTill  timestamp  NULL, ActivateOn  timestamp  NULL, formmodule VARCHAR(255) NOT NULL, formsubmodule VARCHAR(255) NOT NULL, CreatedBy  VARCHAR(255),AssignToGroup VARCHAR(255), AssignToUser VARCHAR(255) , Assignif VARCHAR(255) , status text )");
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        throw;
                    }
                }
                return RedirectToAction("ListByApp", "AxModulePage", new { Appname = userGroup.appName });
            }

            return View();
        }

        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var modulePages = await _context.AxModulePages.FindAsync(id);
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
            modulePages.UserGroups = _context.ARMUserGroups.GroupBy(x => x.Name)
                                               .Select(y => y.FirstOrDefault())
                                               .ToList();
            modulePages.AxInlineForm = _context.AxInLineForm.Select(m => m).ToList();
            modulePages.Modules = _context.AxModules.Select(m => m).ToList();
            modulePages.SubModules = _context.AxSubModules.Select(m => m).ToList();
            return View(modulePages);
        }

        [HttpPost]

        public async Task<IActionResult> Edit(AxModulePages modulePages)
        {
            if (ModelState.IsValid)
            {
                try
                {

                    _context.Update(modulePages);
                    await _context.SaveChangesAsync();
                    //var tableExists = _context.Model.GetEntityTypes().Any(e => e.Name == modulePages.PageDataTable);
                    var tableExists = _context.Database.GetDbConnection().GetSchema("Tables").AsEnumerable()
.Any(x => x.Field<string>("TABLE_NAME").ToLower() == modulePages.PageDataTable.ToLower());
                    if (tableExists)
                    {
                        Console.WriteLine("Exist");
                    }
                    else
                    {
                        try
                        {
                            _context.Database.ExecuteSqlRaw($"CREATE TABLE {modulePages.PageDataTable} (Id UUID PRIMARY KEY,formname VARCHAR(255) NOT NULL, keyvalue VARCHAR(255) NOT NULL,paneldata TEXT NOT NULL,createddatetime timestamp NOT NULL , ActivateTill  timestamp  NULL, ActivateOn  timestamp  NULL, formmodule VARCHAR(255) NOT NULL, formsubmodule VARCHAR(255) NOT NULL, CreatedBy  VARCHAR(255),AssignToGroup VARCHAR(255), AssignToUser VARCHAR(255) , Assignif VARCHAR(255), status text )");
                        }
                        catch (DbUpdateConcurrencyException)
                        {
                            throw;
                        }


                    }

                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DataSourcesExists(modulePages.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                return RedirectToAction("ListByApp", "AxModulePage", new { Appname = modulePages.appName });
            }
            return View(modulePages);



        }

        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var modulePage = await _context.AxModulePages
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
            var modulePage = await _context.AxModulePages.FindAsync(id);
            _context.AxModulePages.Remove(modulePage);
            await _context.SaveChangesAsync();

            return RedirectToAction("ListByApp", "AxModulePage", new { Appname = modulePage.appName });
        }

        private bool DataSourcesExists(Guid id)
        {
            return _context.AxInLineForm.Any(e => e.Id == id);
        }

        private IEnumerable<AxModulePages> AxModuleList(string Appname)
        {
            // var users = _context.ARMUserGroups.ToList();
            var users = _context.AxModulePages.Where(p => p.appName == Appname).ToList();

            return users;
        }
    }
}
