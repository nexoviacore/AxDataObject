using ARMCommon.ActionFilter;
using ARMCommon.Filter;
using ARM_APIs.Interface;
using ARM_APIs.Model;
using ARMCommon.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ARM_APIs.Controllers
{
    [Route("api/v{version:apiVersion}")]
    [ApiVersion("1")]
    [ApiController]
    [Authorize]
    [ServiceFilter(typeof(ValidateSessionFilter))]
    //[ServiceFilter(typeof(ApiLoggingFilter))]
    [ServiceFilter(typeof(ApiResponseFilter))]
    public class AxScriptController : ControllerBase
    {
        private readonly IAxScriptService _axScriptService;

        public AxScriptController(IAxScriptService axScriptService)
        {
            _axScriptService = axScriptService;
        }


        [RequiredFieldsFilter("ARMSessionId", "Script", "Type", "Name", "Recordid")]
        [HttpPost("AxScript")]
        public async Task<IActionResult> AxScript(AxScriptRequest request)
        {
            ARMResult result = new ARMResult();
            APIResult apiResult = new APIResult();            

            apiResult = await _axScriptService.AxExecuteScriptViaRESTDLLAsync(request);

            if (!string.IsNullOrEmpty(apiResult.error))
            {
                result.result.Add("messagetype", "Custom");
                result.result.Add("message", apiResult.error);

                if (apiResult.data != null && apiResult.data.ContainsKey("trace"))
                    result.result.Add("trace", apiResult.data?["trace"]?.ToString());

                return BadRequest(result);
            }
            else if (apiResult.data != null && apiResult.data.ContainsKey("error") && !string.IsNullOrEmpty(apiResult.data?["error"]?.ToString() ?? ""))
            {
                result.result.Add("messagetype", "Custom");
                result.result.Add("message", apiResult.data?["error"]?.ToString());
                
                if (apiResult.data != null && apiResult.data.ContainsKey("trace"))
                    result.result.Add("trace", apiResult.data?["trace"]?.ToString());


                return BadRequest(result);
            }
            else
            {
                result.result["message"] = "SUCCESS";

                if (apiResult.data != null && apiResult.data.ContainsKey("message"))
                    result.result.Add("messagetype", "Custom");

                foreach (KeyValuePair<string, object> kp in apiResult.data)
                {
                    try
                    {
                        if (kp.Value != null)
                        {
                            if (result.result.ContainsKey(kp.Key))
                                result.result[kp.Key] = kp.Value;
                            else
                                result.result.Add(kp.Key, kp.Value);
                        }
                    }
                    catch { }
                }
                return Ok(result);
            }
        }
    }
}
