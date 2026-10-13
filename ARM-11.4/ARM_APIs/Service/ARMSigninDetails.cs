using ARM_APIs.Interface;
using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMCommon.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;


namespace ARM_APIs.Service
{
    public class ARMSigninDetails : IARMSigninDetails
    {
        private readonly DataContext _context;
        private readonly IRedisHelper _redis;

        public ARMSigninDetails(DataContext context, IRedisHelper redis)
        {
            _context = context;
            _redis = redis;
        }

        public async Task<object> GetAppConfiguration(string appname)
        {
            ARMResult resultData;
            string rediskey = "App-" + appname;
            string appConfig = await _redis.StringGetAsync(rediskey);

            if (!string.IsNullOrEmpty(appConfig))
            {
                var appconfiguration = JObject.Parse(appConfig);
                var result = new
                {
                    applogo = appconfiguration["AppLogo"]?.ToString(),
                    appcolor = appconfiguration["AppColor"]?.ToString(),
                    modifiedontime = appconfiguration["modifiedontime"]?.ToString(),
                    enablecitizenusers = appconfiguration["IsCitizenUsers"]?.ToString(),
                    enablegeofencing = appconfiguration["IsGeoFencing"]?.ToString(),
                    enablegeotagging = appconfiguration["IsGeoTagging"]?.ToString(),
                    enablefingerprint = appconfiguration["EnableFingerPrint"]?.ToString(),
                    enablefacialrecognition = appconfiguration["EnablefacialRecognition"]?.ToString(),
                    enableforceLogin = appconfiguration["ForceLogin"]?.ToString(),
                    days = appconfiguration["Days"]?.ToString(),
                };

                resultData = new ARMResult();
                resultData.result.Add("message", "SUCCESS");
                resultData.result.Add("data", result);
                return new OkObjectResult(resultData);
            }
            else
            {
                var signInDetail = await _context.ARMApps.FirstOrDefaultAsync(f => f.AppName == appname);

                if (signInDetail == null)
                {
                    return new BadRequestObjectResult("APPNOTEXIST");
                }
                else
                {
                    var objResult = new
                    {
                        applogo = signInDetail.AppLogo,
                        appcolor = signInDetail.AppColor,
                        modifiedontime = signInDetail.modifiedon,
                        enablecitizenusers = signInDetail?.IsCitizenUsers,
                        enablegeofencing = signInDetail?.IsGeoFencing,
                        enablegeotagging = signInDetail?.IsGeoTagging,
                        enablefingerprint = signInDetail?.EnableFingerPrint,
                        enablefacialrecognition = signInDetail?.EnablefacialRecognition,
                        enableforceLogin = signInDetail?.ForceLogin,
                        days = signInDetail?.ForceLoginDays,
                    };

                    resultData = new ARMResult();
                    resultData.result.Add("message", "SUCCESS");
                    resultData.result.Add("data", objResult);
                    return new OkObjectResult(resultData);
                }
            }
        }

        public async Task<object> GetAppModificationTime(string appname)
        {
            ARMResult resultData;
            var signInDetail = await _context.ARMApps.FirstOrDefaultAsync(f => f.AppName == appname);

            if (signInDetail == null)
            {
                return new BadRequestObjectResult("APPNOTEXIST");
            }
            else
            {
                var objResult = new
                {
                    modifiedontime = signInDetail.modifiedon
                };

                resultData = new ARMResult();
                resultData.result.Add("message", "SUCCESS");
                resultData.result.Add("data", objResult);

                return new OkObjectResult(resultData);
            }
        }
    }
}

