using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMCommon.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace AgileConnect.Controllers
{
    public class LogsController : Controller
    {
        private readonly IConfiguration _config;
        private readonly ITokenService _tokenService;
        private readonly DataContext _context;
        private readonly IRedisHelper _redis;
        private string generatedToken = null;


        public LogsController(IConfiguration config, ITokenService tokenService, DataContext context, IRedisHelper redis)
        {
            _config = config;
            _tokenService = tokenService;
            _context = context;
            _redis = redis;
        }

        public async Task<IActionResult> logs(string appname)
        {
            Logs logs = new Logs();
            logs.appname = appname;

            var dictlogdetails = await _redis.HashGetAllDictAsync("logdetails");
            string instanceId = dictlogdetails.ContainsKey("InstanceId") ? dictlogdetails["InstanceId"] : "";

            // Retrieve current logs from your database or data source based on the instanceId
            var currentLogs = await _context.armlogs
                .Where(x => x.instanceid == instanceId) // Filter logs by instanceId
                .Select(x => new
                {
                    x.logtime,
                    x.instanceid,
                    x.logtype,
                    x.path,
                    x.module,
                    x.logdetails
                })
                .ToListAsync();
            string apinames = dictlogdetails.ContainsKey("api") ? dictlogdetails["api"] : "";
            string servicenames = dictlogdetails.ContainsKey("service") ? dictlogdetails["service"] : "";
            ViewBag.ApiNames = apinames;
            ViewBag.serviceNames = servicenames;
            return View(logs);
        }

        [HttpPost]
        public async Task<IActionResult> StartLogging(string appname, List<string> apiNames, List<string> serviceNames)
        {
            string key = "logdetails";
            string instanceId = Guid.NewGuid().ToString();
            await _redis.HashSetAsync(key, "InstanceId", instanceId);
            await _redis.HashSetAsync(key, "api", string.Join(",", apiNames));
            await _redis.HashSetAsync(key, "service", string.Join(",", serviceNames));

            // Optionally, you can return a JSON response indicating success
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> StopLogging(string appname, List<string> apiNames)
        {
            string key = "logdetails";
            await _redis.KeyDeleteAsync(key);
            // Optionally, you can return a JSON response indicating success
            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> GetAllLogs()
        {
            // Fetch specific columns (InstanceId, LogType, path, module, logdetails) from the "ARMLogs" table
            var allLogs = await _context.armlogs.Select(x => new
            {
                x.logtime,
                x.instanceid,
                x.logtype,
                x.path,
                x.module,
                x.logdetails
            }).ToListAsync();

            return Json(allLogs);
        }
        public async Task<IActionResult> GetCurrentLogs()
        {
            var dictlogdetails = await _redis.HashGetAllDictAsync("logdetails");
            string instanceId = dictlogdetails["InstanceId"];

            // Retrieve current logs from your database or data source based on the instanceId
            var currentLogs = await _context.armlogs
                .Where(x => x.instanceid == instanceId) // Filter logs by instanceId
                .Select(x => new
                {
                    x.logtime,
                    x.instanceid,
                    x.logtype,
                    x.path,
                    x.module,
                    x.logdetails
                })
                .ToListAsync();

            return Json(currentLogs);
        }

        [HttpGet]
        public async Task<IActionResult> KeyExist()
        {
            var dictlogdetails = await _redis.HashGetAllDictAsync("logdetails");
            string instanceId = dictlogdetails.ContainsKey("InstanceId") ? dictlogdetails["InstanceId"] : "";

            // Retrieve current logs from your database or data source based on the instanceId
            var currentLogs = await _context.armlogs
                .Where(x => x.instanceid == instanceId) // Filter logs by instanceId
                .Select(x => new
                {
                    x.logtime,
                    x.instanceid,
                    x.logtype,
                    x.path,
                    x.module,
                    x.logdetails
                })
                .ToListAsync();

            string apinames = dictlogdetails.ContainsKey("api") ? dictlogdetails["api"] : "";
            string[] apiNamesArray = apinames.Split(',');
            string apiNamesJson = Newtonsoft.Json.JsonConvert.SerializeObject(apiNamesArray);
            ViewBag.ApiNamesJson = apiNamesJson;
            string servicenames = dictlogdetails.ContainsKey("service") ? dictlogdetails["api"] : "";
            string[] serviceNamesArray = servicenames.Split(',');
            string serviceNamesJson = Newtonsoft.Json.JsonConvert.SerializeObject(serviceNamesArray);
            ViewBag.serviceNamesJson = serviceNamesJson;
            return Json(currentLogs);
        }

        public IActionResult GetDropdownValues()
        {
            // Read the JSON file from the wwwroot folder or any other suitable location
            var jsonPath = "D:\\Deepti\\AgileConnect\\ServiceList.Json";
            var jsonData = System.IO.File.ReadAllText(jsonPath);

            return Json(jsonData);
        }


    }
}
