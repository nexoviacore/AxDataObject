using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using ARMCommon.Model;
using ARMCommon.ActionFilter;
using ARMCommon.Helpers;
using ARMCommon.Interface;

namespace ARM_APIs.Controllers
{
    [Route("api/v{version:apiVersion}")]
    [ApiVersion("1")]
    [ServiceFilter(typeof(ApiResponseFilter))]
    [ApiController]
    public class ARMNotificationsHubController : ControllerBase
    {
        public readonly IAppSettingsHelper _appSettingsHelper;
        public ARMNotificationsHubController(IAppSettingsHelper appSettingsHelper)
        {
            _appSettingsHelper = appSettingsHelper;
        }

        [HttpPost("SendSignalR")]
        public async Task<IActionResult> SendSignalR([FromBody] SendSignalRRequest request)
        {
            try
            {
                if(request.CalledFrom == "ARM")
                {
                    return BadRequest("Recursive ARM SignalR call. Please check ARMNotificationUrl in appsettings.ini file.");
                }

                API api = new API();
                var signalRUrl = _appSettingsHelper.GetARMNotificationUrl(request.Project);
                if(string.IsNullOrEmpty(signalRUrl))
                    return BadRequest("ARM notification url is not configured.");
                
                signalRUrl = signalRUrl.TrimEnd('/') + "/api/v1/SendSignalR";   

                request.CalledFrom = "ARM";
                var json = JsonSerializer.Serialize(request);
                var signalRResult = await api.POSTData(signalRUrl, json, "application/json");
                if (signalRResult != null && signalRResult.result.ContainsKey("success") && signalRResult.result["success"].ToString().ToLower() == "true")
                {
                    return Ok("SUCCESS");
                }
                else
                    return BadRequest("INAVALIDUSERID");
            }
            catch(Exception ex)
            {
                return BadRequest($"Error: {ex.Message}");
            }
        }
    }
}
