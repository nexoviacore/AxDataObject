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
    public class AxGetController : ControllerBase
    {

        private readonly IAxGetService _getData;
        private readonly IARMLogTraceService _traceService;
        public AxGetController(IAxGetService getData, IARMLogTraceService traceService)
        {
            _getData = getData;
            _traceService = traceService;
        }


        [RequiredFieldsFilter("ARMSessionId", "TransId", "Action")]
        [HttpPost("AxGet")]
        public async Task<IActionResult> AxGet(AxGet getData)
        {
            ARMResult result = new ARMResult();

            APIResult getDataResult = await _getData.AxGet(getData);

            if (!string.IsNullOrEmpty(getDataResult.error))
            {
                result.result.Add("messagetype", "Custom");
                result.result.Add("message", getDataResult.error);

                if (getDataResult.data != null && getDataResult.data.ContainsKey("permissions"))
                    result.result.Add("permissions", getDataResult.data?["permissions"]?.ToString());

                if (getDataResult.data != null && getDataResult.data.ContainsKey("trace"))
                    result.result.Add("trace", getDataResult.data?["trace"]?.ToString());

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
