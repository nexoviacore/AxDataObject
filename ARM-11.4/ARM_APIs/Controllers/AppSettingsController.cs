using ARM_APIs.Model;
using ARMCommon.ActionFilter;
using ARMCommon.Helpers;
using ARMCommon.Model;
using Microsoft.AspNetCore.Mvc;

namespace ARM_APIs.Controllers
{
    [Route("api/v{version:apiVersion}")]
    [ApiVersion("1")]
    [ServiceFilter(typeof(ApiResponseFilter))]
    [ApiController]
    public class AppSettingsController : ControllerBase
    {
        private readonly AppSettingsValidaterService _appSettingsValidater;
        public AppSettingsController(AppSettingsValidaterService appSettingsValidater)
        {
            _appSettingsValidater = appSettingsValidater;
        }

        [HttpPost("ValidateAppSettings")]
        public async Task<IActionResult> ValidateAppSettings(AppRequest request)
        {
            try
            {
                ARMResult result = new ARMResult();
                if (request == null || string.IsNullOrEmpty(request.AppName))
                {
                    result.result.Add("messagetype", "Custom");
                    result.result.Add("errors", new List<string> { $"Error: Appname is missing" });
                    result.result.Add("success", false);
                    return Ok(result);
                }

                var validationResult = await _appSettingsValidater.Validate(request.AppName);

                result.result["success"] = true;
                result.result["messages"] = validationResult.Messages;
                result.result["messagetype"] = "Custom";
                if (!validationResult.Success)
                    result.result["errors"] = validationResult.Errors;
                return Ok(result);

            }
            catch (Exception ex)
            {
                ARMResult result = new ARMResult();
                result.result.Add("messagetype", "Custom");
                result.result.Add("errors", new List<string> { $"Error: {ex.Message}" });
                result.result.Add("success", false);
                return Ok(result);
            }
        }

        [HttpGet("ValidateAppSettings")]
        public async Task<IActionResult> ValidateAppSettings([FromQuery] string appName)
        {
            try
            {
                ARMResult result = new ARMResult();
                if ( string.IsNullOrEmpty(appName))
                {
                    result.result.Add("messagetype", "Custom");
                    result.result.Add("errors", new List<string> { $"Error: Appname is missing in URL" });
                    result.result.Add("success", false);
                    return Ok(result);
                }

                var validationResult = await _appSettingsValidater.Validate(appName);

                result.result["success"] = true;
                result.result["messages"] = validationResult.Messages;
                result.result["messagetype"] = "Custom";
                if (!validationResult.Success)
                    result.result["errors"] = validationResult.Errors;
                return Ok(result);

            }
            catch (Exception ex)
            {
                ARMResult result = new ARMResult();
                result.result.Add("messagetype", "Custom");
                result.result.Add("errors", new List<string> { $"Error: {ex.Message}" });
                result.result.Add("success", false);
                return Ok(result);
            }
        }


    }

    public class AppRequest
    {
        public string AppName { get; set; } = string.Empty;
    }
}
