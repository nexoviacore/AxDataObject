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
            if (string.IsNullOrEmpty(putData.ARMSessionId) || putData.Data == null)
            {
                result.result.Add("status", false);
                result.result.Add("messagetype", "Custom");
                result.result.Add("message", "Required fields : 'ARMSessionId/Data' cannot be empty.");
                return BadRequest(result);
            }

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

                return BadRequest(result);
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

                return BadRequest(result);
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
