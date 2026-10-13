
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

    public class ARMHtmlController : Controller
    {
        private readonly DataContext _context;
        private readonly IRedisHelper _redis;
        private readonly Utils _common;



        public ARMHtmlController(DataContext context, IRedisHelper redis, Utils common)
        {
            _context = context;
            _redis = redis;
            _common = common;
        }

        public async Task<IActionResult> List()
        {
            return View(await _context.ARMDefinations.OrderByDescending(a => a.UpdatedOn).ToListAsync());
        }

        public async Task<IActionResult> ListByApp(string Appname)
        {
            ViewBag.result = Appname;
            return View(await _context.ARMDefinations.Where(p => p.AppName == Appname).OrderByDescending(a => a.UpdatedOn).ToListAsync());
        }

        public IActionResult Create(string Appname)
        {
            var ARMDefination = new ARMHtml();
            ARMDefination.UserGroups = _context.ARMUserGroups.GroupBy(x => x.Name)
                                              .Select(y => y.FirstOrDefault())
                                              .ToList();

            string str = Appname;
            List<string> strList = str.Split(',').ToList();
            ARMDefination.app1 = new List<App>();

            foreach (string appItem in strList)
            {
                ARMDefination.app1.Add(new App() { ID = appItem, Name = appItem });

            };
            return View(ARMDefination);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string Appname, ARMHtml definitions)
        {
            if (ModelState.IsValid)
            {
                definitions.ID = Guid.NewGuid();

                //if (definitions.selectedUserGroups != null && definitions.selectedUserGroups.Count > 0)
                //{
                //    foreach (var userGroup in definitions.selectedUserGroups)
                //    {
                //        await _context.Database.ExecuteSqlRawAsync("insert into public.\"UserGroup_Permissions\" values ('" + definitions.ID + "', '" + userGroup + "')");
                //    }
                //}

                definitions.CreatedOn = DateTime.Now;
                definitions.UpdatedOn = DateTime.Now;
                _context.Add(definitions);
                await _context.SaveChangesAsync();
                await _redis.KeyDeleteAsync("HTML-" + definitions.DefinitionID.ToLower());
                StreamWriter sw = new StreamWriter($"D:\\Deepti\\AgileConnect\\ARM_APIs\\Pages\\hcmdev\\HTMLPages\\{definitions.DefinitionID}.html");
                sw.WriteLine(definitions.DefinitionHTML);

                //Close the file
                sw.Close();
                return RedirectToAction("ListByApp", "ARMHtml", new { Appname = definitions.AppName });
            }
            return View(definitions);
        }

        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var definitions = await _context.ARMDefinations.FindAsync(id);

            //definitions.selectedUserGroups = (from ug in _context.ARMUserGroups
            //                                  join ugp in _context.UserGroup_Permissions
            //                                  on ug.ID equals ugp.UserGroupId
            //                                  where ugp.SourceId == definitions.ID
            //                                  select new { ID = ug.ID })
            //                                  .Select(m => m.ID).ToList();

            definitions.UserGroups = _context.ARMUserGroups.GroupBy(x => x.Name)
                                              .Select(y => y.FirstOrDefault())
                                              .ToList();
            string str = _common.GetAppList();
            List<string> strList = str.Split(',').ToList();
            definitions.app1 = new List<App>();


            foreach (string appItem in strList)
            {
                definitions.app1.Add(new App() { ID = appItem, Name = appItem });

            };
            if (definitions == null)
            {
                return NotFound();
            }



            return View(definitions);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, ARMHtml definitions)
        {
            if (id != definitions.ID)
            {
                return NotFound();
            }

            //await _context.Database.ExecuteSqlRawAsync("delete from public.\"UserGroup_Permissions\" where \"SourceId\" = '" + definitions.ID + "'");

            //if (definitions.selectedUserGroups != null && definitions.selectedUserGroups.Count > 0)
            //{
            //    foreach (var userGroup in definitions.selectedUserGroups)
            //    {
            //        await _context.Database.ExecuteSqlRawAsync("insert into public.\"UserGroup_Permissions\" values ('" + definitions.ID + "', '" + userGroup + "')");
            //    }
            //}

            if (ModelState.IsValid)
            {
                try
                {
                    definitions.UpdatedOn = DateTime.Now;
                    _context.Update(definitions);
                    await _context.SaveChangesAsync();
                    await _redis.KeyDeleteAsync("HTML-" + definitions.DefinitionID.ToLower());
                    StreamWriter sw = new StreamWriter($"D:\\Deepti\\AgileConnect\\ARM_APIs\\Pages\\hcmdev\\HTMLPages\\{definitions.DefinitionID}.html");
                    sw.WriteLine(definitions.DefinitionHTML);

                    //Close the file
                    sw.Close();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DefinitionsExists(definitions.ID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction("ListByApp", "ARMHtml", new { Appname = definitions.AppName });
            }
            return View(definitions);
        }
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var definitions = await _context.ARMDefinations
                .FirstOrDefaultAsync(m => m.ID == id);
            if (definitions == null)
            {
                return NotFound();
            }

            return View(definitions);
        }


        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var definitions = await _context.ARMDefinations.FindAsync(id);
            //await _context.Database.ExecuteSqlRawAsync("delete from public.\"UserGroup_Permissions\" where \"SourceId\" = '" + definitions.ID + "'");
            _context.ARMDefinations.Remove(definitions);
            await _context.SaveChangesAsync();
            await _redis.KeyDeleteAsync("HTML-" + definitions.DefinitionID.ToLower());
            return RedirectToAction("ListByApp", "ARMHtml", new { Appname = definitions.AppName });
        }

        private bool DefinitionsExists(Guid id)
        {
            return _context.ARMDefinations.Any(e => e.ID == id);
        }
    }
}
