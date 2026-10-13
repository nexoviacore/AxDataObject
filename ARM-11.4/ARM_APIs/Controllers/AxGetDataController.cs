using ARM_APIs.Model;
using ARM_APIs.Interface;
using ARMCommon.ActionFilter;
using ARMCommon.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ARMCommon.Interface;
using Newtonsoft.Json;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using ARMCommon.Filter;

namespace ARM_APIs.Controllers
{
    [Route("api/v{version:apiVersion}")]
    [ApiVersion("1")]
    [Authorize]
    [ServiceFilter(typeof(ValidateSessionFilter))]
    //[ServiceFilter(typeof(ApiLoggingFilter))]
    [ServiceFilter(typeof(ApiResponseFilter))]
    [ApiController]
    public class AxGetDataController : ControllerBase
    {

        private readonly IAxGetDataService _getData;
        private readonly IARMLogTraceService _traceService;
        public AxGetDataController(IAxGetDataService getData, IARMLogTraceService traceService)
        {
            _getData = getData;
            _traceService = traceService;
        }


        [RequiredFieldsFilter("ARMSessionId", "TransId", "Action")]
        [RequiredFieldsFilter("ARMSessionId")]
        [HttpPost("AxGet_Old")]
        public async Task<IActionResult> AxGet_Old(AxGetData getData)
        {
            ARMResult result = new ARMResult();

            APIResult apiResult = await _getData.AxGet(getData);

            if (!string.IsNullOrEmpty(apiResult.error))
            {
                result.result.Add("messagetype", "Custom");
                result.result.Add("message", apiResult.error);

                if (apiResult.data != null && apiResult.data.ContainsKey("permissions"))
                    result.result.Add("permissions", apiResult.data?["permissions"]?.ToString());

                if (apiResult.data != null && apiResult.data.ContainsKey("trace"))
                    result.result.Add("trace", apiResult.data?["trace"]?.ToString());

                return BadRequest(result);
            }
            else if (apiResult.data != null && apiResult.data.ContainsKey("error") && !string.IsNullOrEmpty(apiResult.data?["error"]?.ToString() ?? ""))
            {
                result.result.Add("messagetype", "Custom");
                result.result.Add("message", apiResult.data?["error"]?.ToString());

                if (apiResult.data != null && apiResult.data.ContainsKey("permissions"))
                    result.result.Add("permissions", apiResult.data?["permissions"]?.ToString());

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

        [RequiredFieldsFilter("ARMSessionId", "Action")]
        [HttpPost("AxList_Old")]
        public async Task<IActionResult> AxList_Old(AxListData getData)
        {
            ARMResult result = new ARMResult();

            if (string.IsNullOrEmpty(getData.TransId) && (getData.ADSNames == null || getData.ADSNames?.Count == 0))
            {
                result.result.Add("messagetype", "Custom");
                result.result.Add("message", "TransId or ADSNames should be mentioned");
                return BadRequest(result);
            }

            APIResult apiResult = await _getData.AxList(getData);

            if (!string.IsNullOrEmpty(apiResult.error))
            {
                result.result.Add("messagetype", "Custom");
                result.result.Add("message", apiResult.error);

                if (apiResult.data != null && apiResult.data.ContainsKey("permissions"))
                    result.result.Add("permissions", apiResult.data?["permissions"]?.ToString());

                if (apiResult.data != null && apiResult.data.ContainsKey("trace"))
                    result.result.Add("trace", apiResult.data?["trace"]?.ToString());

                return BadRequest(result);
            }
            else if (apiResult.data != null && apiResult.data.ContainsKey("error") && !string.IsNullOrEmpty(apiResult.data?["error"]?.ToString() ?? ""))
            {
                result.result.Add("messagetype", "Custom");
                result.result.Add("message", apiResult.data?["error"]?.ToString());

                if (apiResult.data != null && apiResult.data.ContainsKey("permissions"))
                    result.result.Add("permissions", apiResult.data?["permissions"]?.ToString());

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


        //[RequiredFieldsFilter("ARMSessionId", "Action", "TransId")]
        //[HttpPost("AxGetViewFlag")]
        //public async Task<IActionResult> AxGetViewFlag(AxListData data)
        //{
        //    var editable = await _getData.AxGetViewFlag(data);
        //    ARMResult result = new ARMResult();
        //    result.result.Add("message", "SUCCESS");
        //    result.result.Add("data", editable);
        //    return Ok(result);
        //}
    }
}
