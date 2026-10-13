using ARMCommon.ActionFilter;
using ARMCommon.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ARMCommon.Interface;

namespace AxTstructData
{
    [Route("api/v{version:apiVersion}")]
    [ApiVersion("1")]
    [Authorize]
    [ServiceFilter(typeof(ApiResponseFilter))]
    [ApiController]
    public class AxUserPermissionController : ControllerBase
    {

        private readonly IAxUserPermissionService _userAccess;
        public AxUserPermissionController(IAxUserPermissionService userAccess)
        {
            _userAccess = userAccess;
        }

        [RequiredFieldsFilter("ARMSessionId", "Action", "TransId")]
        [HttpPost("AxUserPermission")]
        public async Task<IActionResult> AxUserPermission(AxUserPermission tstructData)
        {
            var getDataResult = await _userAccess.GetUserPermission(tstructData);
            ARMResult result = new ARMResult();
            if (!string.IsNullOrEmpty(getDataResult.error))
            {
                result.result["success"] = false;
                result.result["messagetype"] =  "Custom";
                result.result["message"] = getDataResult.error;

                if (getDataResult.data != null && getDataResult.data.ContainsKey("trace"))
                    result.result["trace"] = getDataResult.data?["trace"]?.ToString();

                return Ok(result);
            }
            else if (getDataResult.data != null && getDataResult.data.ContainsKey("error") && !string.IsNullOrEmpty(getDataResult.data?["error"]?.ToString() ?? ""))
            {
                result.result["success"] = false;
                result.result["messagetype"] = "Custom";
                result.result["message"]= getDataResult.data?["error"]?.ToString();

                if (getDataResult.data != null && getDataResult.data.ContainsKey("trace"))
                    result.result["trace"] = getDataResult.data?["trace"]?.ToString();

                return Ok(result);
            }
            else
            {
                result.result["message"] = "SUCCESS";
                result.result["success"] =  true;
                if (getDataResult.data != null && getDataResult.data.ContainsKey("message"))
                    result.result["messagetype"] = "Custom";

                foreach (KeyValuePair<string, object> kp in getDataResult.data)
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
