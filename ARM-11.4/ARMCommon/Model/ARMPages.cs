using ARMCommon.Helpers;
using ARMCommon.Interface;
using System.Reflection;
using ARM_APIs.Interface;

namespace ARM_APIs.Model
{
    public class ARMPages : IARMPages
    {

        private readonly DataContext _context;
        private readonly IConfiguration _config;
        private readonly IRedisHelper _redis;
        private readonly ITokenService _tokenService;
        //public string key = "Session-" + Guid.NewGuid().ToString();
        private readonly IPostgresHelper _postGres;
        private readonly IAPI _api;

        public ARMPages(DataContext context, IConfiguration configuration, ITokenService tokenService, IRedisHelper redis, IPostgresHelper postGres, IAPI api)
        {
            _context = context;
            _config = configuration;
            _tokenService = tokenService;
            _redis = redis;
            _postGres = postGres;
            _api = api; 
        }

        public bool AppExists(string appName)
        {
            var app = _context.ARMApps.FirstOrDefault(p => p.AppName.ToLower() == appName.ToLower());
            if (app == null)
            {
                return false;
            }
            return true;
        }

        public bool SessionExists(string sessionId)
        {
            return _redis.KeyExists(sessionId);
        }

        public string GetPage(string appName, string pageName)
        {
            var folderPath = "";
            if (Assembly.GetEntryAssembly().Location.IndexOf("bin\\") > -1)
            {
                folderPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location.Substring(0, Assembly.GetEntryAssembly().Location.IndexOf("bin\\")));
            }
            else
            {
                folderPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            }
            var pagePath = $"{folderPath}\\Pages\\{appName}\\HTMLPages\\{pageName}";
            FileInfo oFileInfo = new FileInfo(pagePath);
            if (oFileInfo.Exists)
            {
                DateTime fileUpdatedTime = oFileInfo.LastWriteTimeUtc;
                var redisKey = $"{appName.ToUpper()}-{Constants.REDIS_PREFIX.ARMPAGE.ToString()}-{pageName.ToUpper()}-{fileUpdatedTime.ToString().Replace(" ", "").ToUpper()}";
                var html = _redis.StringGet(redisKey);
                if (string.IsNullOrEmpty(html))
                {
                    using (StreamReader sr = new StreamReader(System.IO.File.Open($"{oFileInfo.FullName}",
                           FileMode.Open,
                           FileAccess.Read,
                           FileShare.ReadWrite)))
                    {
                        html = sr.ReadToEnd();
                    }
                    if (string.IsNullOrEmpty(html))
                    {
                        return Constants.RESULTS.NO_RECORDS.ToString();
                    }
                    _redis.StringSet(redisKey, html);
                }
                return html;
            }
            else
            {
                return Constants.RESULTS.NO_RECORDS.ToString();
            }
        }
    }
}
