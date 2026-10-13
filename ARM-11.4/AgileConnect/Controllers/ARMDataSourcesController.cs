
using AgileConnect.Filter;
using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMCommon.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgileConnect.Controllers
{
    [ServiceFilter(typeof(SessionActionFilter))]
    [Authorize]

    public class ARMDataSourcesController : Controller
    {
        private readonly DataContext _context;
        private readonly IRedisHelper _redis;
        private readonly Utils _common;

        public ARMDataSourcesController(DataContext context, IRedisHelper redis, Utils common)
        {
            _context = context;
            _redis = redis;
            _common = common;
        }

        //public async Task<IActionResult> ListByApp(string Appname)
        // {
        //     ViewBag.result = Appname;
        //     return View(await _context.SQLDataSource.Where(p => p.AppName == Appname).OrderByDescending(a => a.UpdatedOn).ToListAsync());
        //  }
        public async Task<IActionResult> APIDefinitionsList(string Appname)
        {
            ViewBag.result = Appname;
            return View(await _context.APIDefinitions.Where(p => p.AppName == Appname).OrderByDescending(a => a.UpdatedOn).ToListAsync());

        }
        public async Task<IActionResult> SQLDataSourceList(string Appname)
        {
            ViewBag.result = Appname;
            return View(await _context.SQLDataSource.Where(p => p.AppName == Appname).OrderByDescending(a => a.UpdatedOn).ToListAsync());

        }
        //public async Task<IActionResult> List()

        //{

        //    return View(await _context.ARMDataSources.OrderByDescending(a => a.UpdatedOn).ToListAsync());

        //}
        //public IActionResult Create(string Appname)
        //{
        //    var dataSource = new ARMDataSource();
        //    dataSource.DataSyncDataSources = _context.ARMDataSources.Select(m => m).ToList();
        //    dataSource.UserGroups = _context.ARMUserGroups.GroupBy(x => x.Name)
        //                                     .Select(y => y.FirstOrDefault())
        //                                     .ToList();

        //    string str = Appname;
        //    List<string> strList = str.Split(',').ToList();
        //    dataSource.app1 = new List<App>();


        //    foreach (string appItem in strList)
        //    {
        //        dataSource.app1.Add(new App() { ID = appItem, Name = appItem });

        //    };
        //    return View(dataSource);
        //}
        public IActionResult CreateAPIDefinitions(string Appname)
        {
            var dataSource = new APIDefinitions();
            dataSource.DataSyncDataSources = _context.APIDefinitions.Select(m => m).ToList();
            dataSource.UserGroups = _context.ARMUserGroups.GroupBy(x => x.Name)
                                             .Select(y => y.FirstOrDefault())
                                             .ToList();

            string str = Appname;
            List<string> strList = str.Split(',').ToList();
            dataSource.app1 = new List<App>();


            foreach (string appItem in strList)
            {
                dataSource.app1.Add(new App() { ID = appItem, Name = appItem });

            };
            return View(dataSource);
        }


        [HttpPost]

        public async Task<IActionResult> CreateAPIDefinitions(string Appname, APIDefinitions dataSources)
        {
            //if (string.IsNullOrEmpty(dataSources.SQLScript))
            //{
            //    ModelState.AddModelError("SQLScript", "SQLScript Required");
            //}

            if (ModelState.IsValid)
            {

                dataSources.ID = Guid.NewGuid();

                dataSources.CreatedOn = DateTime.Now;
                dataSources.UpdatedOn = DateTime.Now;
                _context.Add(dataSources);
                await _context.SaveChangesAsync();
                await _redis.KeyDeleteAsync("DATA-" + dataSources.DataSourceID.ToLower());
                await _redis.KeyDeleteAsync("API-" + dataSources.DataSourceID.ToLower());
                return RedirectToAction("APIDefinitionsList", "ARMDataSources", new { Appname = dataSources.AppName });
            }
            return View(dataSources);
        }
        public IActionResult CreateSQLDataSource(string Appname)
        {
            var dataSource = new SQLDataSource();
            dataSource.DataSyncDataSources = _context.SQLDataSource.Select(m => m).ToList();
            dataSource.UserGroups = _context.ARMUserGroups.GroupBy(x => x.Name)
                                             .Select(y => y.FirstOrDefault())
                                             .ToList();

            string str = Appname;
            List<string> strList = str.Split(',').ToList();
            dataSource.app1 = new List<App>();


            foreach (string appItem in strList)
            {
                dataSource.app1.Add(new App() { ID = appItem, Name = appItem });

            };
            return View(dataSource);
        }


        [HttpPost]

        public async Task<IActionResult> CreateSQLDataSource(string Appname, SQLDataSource dataSources)
        {
            if (string.IsNullOrEmpty(dataSources.SQLScript))
            {
                ModelState.AddModelError("SQLScript", "SQLScript Required");
            }

            if (ModelState.IsValid)
            {

                dataSources.ID = Guid.NewGuid();

                dataSources.CreatedOn = DateTime.Now;
                dataSources.UpdatedOn = DateTime.Now;
                _context.Add(dataSources);
                await _context.SaveChangesAsync();
                await _redis.KeyDeleteAsync("DATA-" + dataSources.DataSourceID.ToLower());
                await _redis.KeyDeleteAsync("API-" + dataSources.DataSourceID.ToLower());
                return RedirectToAction("SQLDataSourceList", "ARMDataSources", new { Appname = dataSources.AppName });
            }
            return View(dataSources);
        }


        public async Task<IActionResult> EditAPIDefinitions(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var dataSource = await _context.APIDefinitions.FindAsync(id);
            //string DataSourceTypeValue = dataSource.Type;
            //ViewBag.DataSourceType = DataSourceTypeValue;
            if (dataSource == null)
            {
                return NotFound();
            }


            dataSource.UserGroups = _context.ARMUserGroups.GroupBy(x => x.Name)
                                             .Select(y => y.FirstOrDefault())
                                             .ToList();
            dataSource.DataSyncDataSources = _context.APIDefinitions.Select(m => m).ToList();

            string str = _common.GetAppList();
            List<string> strList = str.Split(',').ToList();
            dataSource.app1 = new List<App>();


            foreach (string appItem in strList)
            {
                dataSource.app1.Add(new App() { ID = appItem, Name = appItem });

            };

            return View(dataSource);
        }


        [HttpPost]

        public async Task<IActionResult> EditAPIDefinitions(Guid id, APIDefinitions dataSources)
        {

            if (string.IsNullOrEmpty(dataSources.DataSourceURL))
            {
                ModelState.AddModelError("DataSourceURL", "SQLScript Required");
                ModelState.AddModelError("DataSourceFormat", "SQLScript Required");
                ViewBag.result = "Data Source URL are required";
                ViewBag.color = "red";
                return RedirectToAction("Edit", "ARMDataSources", new { id = dataSources.ID });
            }

            if (id != dataSources.ID)
            {
                return NotFound();
            }


            if (ModelState.IsValid)
            {
                try
                {
                    dataSources.UpdatedOn = DateTime.Now;
                    _context.Update(dataSources);
                    await _context.SaveChangesAsync();
                    await _redis.KeyDeleteAsync("DATA-" + dataSources.DataSourceID.ToLower());
                    await _redis.KeyDeleteAsync("API-" + dataSources.DataSourceID.ToLower());
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!APIDefinitionsExists(dataSources.ID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                return RedirectToAction("APIDefinitionsList", "ARMDataSources", new { Appname = dataSources.AppName });
            }
            return View(dataSources);



        }

        public async Task<IActionResult> EditSQLDataSource(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var dataSource = await _context.SQLDataSource.FindAsync(id);
            if (dataSource == null)
            {
                return NotFound();
            }


            dataSource.UserGroups = _context.ARMUserGroups.GroupBy(x => x.Name)
                                             .Select(y => y.FirstOrDefault())
                                             .ToList();

            dataSource.DataSyncDataSources = _context.SQLDataSource.Select(m => m).ToList();

            string str = _common.GetAppList();
            List<string> strList = str.Split(',').ToList();
            dataSource.app1 = new List<App>();


            foreach (string appItem in strList)
            {
                dataSource.app1.Add(new App() { ID = appItem, Name = appItem });

            };

            return View(dataSource);
        }

        [HttpPost]
        public async Task<IActionResult> EditSQLDataSource(Guid id, SQLDataSource dataSources)
        {
            if (string.IsNullOrEmpty(dataSources.SQLScript))
            {
                ModelState.AddModelError("SQLScript", "SQLScript Required");
                ViewBag.result = "SQL Scripts are required";
                ViewBag.color = "red";
                return RedirectToAction("EditSQLDataSource", "ARMDataSources", new { id = dataSources.ID });
            }

            if (id != dataSources.ID)
            {
                return NotFound();
            }


            if (ModelState.IsValid)
            {
                try
                {
                    dataSources.UpdatedOn = DateTime.Now;
                    _context.Update(dataSources);
                    await _context.SaveChangesAsync();
                    await _redis.KeyDeleteAsync("DATA-" + dataSources.DataSourceID.ToLower());
                    await _redis.KeyDeleteAsync("API-" + dataSources.DataSourceID.ToLower());
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SQLDataSourceExists(dataSources.ID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                return RedirectToAction("SQLDataSourceList", "ARMDataSources", new { Appname = dataSources.AppName });
            }
            return View(dataSources);



        }



        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var dataSources = await _context.SQLDataSource.FindAsync(id);
            _context.SQLDataSource.Remove(dataSources);
            await _context.SaveChangesAsync();
            await _redis.KeyDeleteAsync("DATA-" + dataSources.DataSourceID.ToLower());
            return RedirectToAction("SQLDataSourceList", "ARMDataSources", new { Appname = dataSources.AppName });

        }


        //public async Task<IActionResult> Delete(Guid? id)
        //{
        //    if (id == null)
        //    {
        //        return NotFound();
        //    }

        //    var dataSources = await _context.SQLDataSource
        //        .FirstOrDefaultAsync(m => m.ID == id);
        //    if (dataSources == null)
        //    {
        //        return NotFound();
        //    }

        //    return View(dataSources);
        //}


        //[HttpPost, ActionName("Delete")]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> DeleteConfirmed(Guid id)
        //{
        //    var dataSources = await _context.SQLDataSource.FindAsync(id);
        //    _context.SQLDataSource.Remove(dataSources);
        //    await _context.SaveChangesAsync();
        //    await _redis.KeyDeleteAsync("DATA-" + dataSources.DataSourceID.ToLower());
        //    return RedirectToAction("List", "ARMAPPS", new { Appname = dataSources.AppName });
        //}

        //private bool DataSourcesExists(Guid id)
        //{
        //    return _context.ARMDataSources.Any(e => e.ID == id);
        //}
        private bool SQLDataSourceExists(Guid id)
        {
            return _context.SQLDataSource.Any(e => e.ID == id);
        }
        private bool APIDefinitionsExists(Guid id)
        {
            return _context.APIDefinitions.Any(e => e.ID == id);
        }


        public async Task<IActionResult> DeleteAPIDefinitions(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var dataSources = await _context.APIDefinitions
                .FirstOrDefaultAsync(m => m.ID == id);
            if (dataSources == null)
            {
                return NotFound();
            }

            return View(dataSources);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAPIDefinitions(Guid id)
        {
            var dataSources = await _context.APIDefinitions.FindAsync(id);
            _context.APIDefinitions.Remove(dataSources);
            await _context.SaveChangesAsync();
            await _redis.KeyDeleteAsync("DATA-" + dataSources.DataSourceID.ToLower());
            return RedirectToAction("List", "ARMAPPS", new { Appname = dataSources.AppName });
        }


    }
}
