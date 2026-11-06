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
    public class AxPutController : ControllerBase
    {

        private readonly IAxPutService _putData;
        private readonly IARMLogTraceService _traceService;
        public AxPutController(IAxPutService putData, IARMLogTraceService traceService)
        {
            _putData = putData;
            _traceService = traceService;
        }       

        [HttpPost("AxPut")]
        public async Task<IActionResult> AxPut(AxPutInput putData)
        {
            ARMResult result = new ARMResult();
            if (string.IsNullOrEmpty(putData.ARMSessionId))
            {
                result.result.Add("status", false);
                result.result.Add("messagetype", "Custom");
                result.result.Add("message", "Required field 'ARMSessionId' cannot be empty.");
                if (!string.IsNullOrWhiteSpace(putData.ObjectId))
                    result.result.Add("objectid", putData.ObjectId);
                return BadRequest(result);
            }

            putData.IfMatch = Request.Headers.IfMatch.ToString();

            APIResult getDataResult = await _putData.AxPut(putData);

            if (!string.IsNullOrEmpty(getDataResult.error))
            {
                result.result.Add("messagetype", "Custom");
                result.result.Add("message", getDataResult.error);

                if (getDataResult.data != null && getDataResult.data.ContainsKey("permissions"))
                    result.result.Add("permissions", getDataResult.data?["permissions"]?.ToString());

                if (getDataResult.data != null && getDataResult.data.ContainsKey("trace"))
                    result.result.Add("trace", getDataResult.data?["trace"]?.ToString());

                if (getDataResult.data != null && getDataResult.data.ContainsKey("index"))
                    result.result.Add("index", getDataResult.data?["index"]?.ToString());

                if (getDataResult.data != null && getDataResult.data.ContainsKey("transid"))
                    result.result.Add("transid", getDataResult.data?["transid"]?.ToString());

                if (getDataResult.data != null && getDataResult.data.ContainsKey("recordid"))
                    result.result.Add("recordid", getDataResult.data?["recordid"]?.ToString());

                if (getDataResult.data != null && getDataResult.data.ContainsKey("caption"))
                    result.result.Add("caption", getDataResult.data?["caption"]?.ToString());

                if (getDataResult.data != null && getDataResult.data.ContainsKey("objectid"))
                    result.result.Add("objectid", getDataResult.data?["objectid"]?.ToString());

                if (getDataResult.data != null && getDataResult.data.TryGetValue("version", out var failureVersion)
                    && failureVersion != null)
                    result.result.Add("version", failureVersion);

                if (getDataResult.data != null && getDataResult.data.TryGetValue("dataObjectRetained", out var failureRetained)
                    && failureRetained != null)
                    result.result.Add("dataObjectRetained", failureRetained);

                if (getDataResult.data != null && getDataResult.data.TryGetValue("errorcode", out var errorCode)
                    && errorCode != null)
                    result.result.Add("errorcode", errorCode);

                int status = getDataResult.data != null &&
                    getDataResult.data.TryGetValue("httpstatus", out var httpStatus) &&
                    int.TryParse(httpStatus?.ToString(), out int parsedStatus)
                    ? parsedStatus
                    : StatusCodes.Status400BadRequest;
                return StatusCode(status, result);
            }
            else if (getDataResult.data != null && getDataResult.data.ContainsKey("error") && !string.IsNullOrEmpty(getDataResult.data?["error"]?.ToString() ?? ""))
            {
                result.result.Add("messagetype", "Custom");
                result.result.Add("message", getDataResult.data?["error"]?.ToString());

                if (getDataResult.data != null && getDataResult.data.ContainsKey("permissions"))
                    result.result.Add("permissions", getDataResult.data?["permissions"]?.ToString());

                if (getDataResult.data != null && getDataResult.data.ContainsKey("trace"))
                    result.result.Add("trace", getDataResult.data?["trace"]?.ToString());

                if (getDataResult.data != null && getDataResult.data.ContainsKey("index"))
                    result.result.Add("index", getDataResult.data?["index"]?.ToString());

                if (getDataResult.data != null && getDataResult.data.ContainsKey("transid"))
                    result.result.Add("transid", getDataResult.data?["transid"]?.ToString());

                if (getDataResult.data != null && getDataResult.data.ContainsKey("recordid"))
                    result.result.Add("recordid", getDataResult.data?["recordid"]?.ToString());

                if (getDataResult.data != null && getDataResult.data.ContainsKey("caption"))
                    result.result.Add("caption", getDataResult.data?["caption"]?.ToString());

                if (getDataResult.data != null && getDataResult.data.ContainsKey("objectid"))
                    result.result.Add("objectid", getDataResult.data?["objectid"]?.ToString());

                if (getDataResult.data != null && getDataResult.data.TryGetValue("version", out var dataFailureVersion)
                    && dataFailureVersion != null)
                    result.result.Add("version", dataFailureVersion);

                if (getDataResult.data != null && getDataResult.data.TryGetValue("dataObjectRetained", out var dataFailureRetained)
                    && dataFailureRetained != null)
                    result.result.Add("dataObjectRetained", dataFailureRetained);

                if (getDataResult.data != null && getDataResult.data.TryGetValue("errorcode", out var dataErrorCode)
                    && dataErrorCode != null)
                    result.result.Add("errorcode", dataErrorCode);

                int status = getDataResult.data != null &&
                    getDataResult.data.TryGetValue("httpstatus", out var dataHttpStatus) &&
                    int.TryParse(dataHttpStatus?.ToString(), out int parsedStatus)
                    ? parsedStatus
                    : StatusCodes.Status400BadRequest;
                return StatusCode(status, result);
            }
            else
            {
                result.result["message"] = "SUCCESS";

                if (getDataResult.data != null && getDataResult.data.ContainsKey("message"))
                    result.result.Add("messagetype", "Custom");

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
