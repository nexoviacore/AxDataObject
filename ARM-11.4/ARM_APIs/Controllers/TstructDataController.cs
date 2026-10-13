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
    public class TstructDataController : ControllerBase
    {

        private readonly ITstructDataService _tstructData;
        private readonly IARMLogTraceService _traceService;
        public TstructDataController(ITstructDataService tstructData, IARMLogTraceService traceService)
        {
            _tstructData = tstructData;
            _traceService = traceService;
        }

        [RequiredFieldsFilter("ARMSessionId", "TransId", "Action")]
        [HttpPost("AxDataPut")]
        public async Task<IActionResult> AxDataPut(TstructData data)
        {
            ARMResult result = new ARMResult();

            if (data.SubmitData?["dataarray"] == null)
            {
                result.result.Add("success", false);
                result.result.Add("messagetype", "Custom");
                result.result.Add("message", $"Error. 'dataarray' node is not having valid data.");
                return BadRequest(result);
            }

            data.DataArrayJson = System.Text.Json.JsonSerializer.Serialize(data.SubmitData["dataarray"]);
            var dataArray = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(data.DataArrayJson);
            if (dataArray?["data"] == null)
            {
                result.result.Add("success", false);
                result.result.Add("messagetype", "Custom");
                result.result.Add("message", $"Error. 'dataarray.data' node is not having valid data.");
                return BadRequest(result);
            }

            APIResult apiResult = await _tstructData.AxDataPut(data);

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
                        if (result.result.ContainsKey(kp.Key))
                            result.result[kp.Key] = kp.Value;
                        else
                            result.result.Add(kp.Key, kp.Value);
                    }
                    catch { }
                }
                return Ok(result);
            }
        }

        [ApiVersion("2")]
        [RequiredFieldsFilter("ARMSessionId", "TransId", "Action")]
        [HttpPost("AxDataPut")]
        public async Task<IActionResult> AxDataPutV2(TstructData data)
        {
            ARMResult result = new ARMResult();
            APIResult apiResult = await _tstructData.AxDataPutV2(data);

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
                        if (result.result.ContainsKey(kp.Key))
                            result.result[kp.Key] = kp.Value;
                        else
                            result.result.Add(kp.Key, kp.Value);
                    }
                    catch { }
                }
                return Ok(result);
            }
        }

        [RequiredFieldsFilter("ARMSessionId")]
        [HttpPost("AxDataGet")]
        public async Task<IActionResult> AxDataGet(TstructGetData getData)
        {
            ARMResult result = new ARMResult();

            if (string.IsNullOrEmpty(getData.ADSGroupName) && string.IsNullOrEmpty(getData.ADSName))
            {
                result.result.Add("messagetype", "Custom");
                result.result.Add("message", "ADSGroupName or ADSName should be mentioned");
                return BadRequest(result);
            }

            if (string.IsNullOrEmpty(getData.TransId)) {
                getData.DoUserAttributesCheck = false;
            }

            APIResult apiResult = await _tstructData.AxDataGet(getData);

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

        [RequiredFieldsFilter("ARMSessionId")]
        [HttpPost("AxGetOld")]
        public async Task<IActionResult> AxGetOld(TstructDataGet getData)
        {
            ARMResult result = new ARMResult();

            APIResult apiResult = await _tstructData.AxGet(getData);

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

        [RequiredFieldsFilter("ARMSessionId", "TransId", "Action")]
        [HttpPost("AxPutOld")]
        public async Task<IActionResult> AxPutOld(TstructData data)
        {
            ARMResult result = new ARMResult();
            APIResult apiResult = await _tstructData.AxPut(data);

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
                        if (result.result.ContainsKey(kp.Key))
                            result.result[kp.Key] = kp.Value;
                        else
                            result.result.Add(kp.Key, kp.Value);
                    }
                    catch { }
                }
                return Ok(result);
            }
        }


        //[RequiredFieldsFilter("ARMSessionId")]
        //[HttpPost("AxADSDataGet")]
        //public async Task<IActionResult> AxADSDataGet(ADSGetData adsData)
        //{
        //    ARMResult result = new ARMResult();

        //    if (string.IsNullOrEmpty(adsData.ADSGroupName) && string.IsNullOrEmpty(adsData.ADSName))
        //    {
        //        result.result.Add("messagetype", "Custom");
        //        result.result.Add("message", "ADSGroupName or ADSName should be mentioned");
        //        return BadRequest(result);
        //    }


        //    APIResult apiResult = await _tstructData.AxADSDataGet(adsData);

        //    if (!string.IsNullOrEmpty(apiResult.error))
        //    {
        //        result.result.Add("messagetype", "Custom");
        //        result.result.Add("message", apiResult.error);

        //        if (apiResult.data != null && apiResult.data.ContainsKey("permissions"))
        //            result.result.Add("permissions", apiResult.data?["permissions"]?.ToString());

        //        if (apiResult.data != null && apiResult.data.ContainsKey("trace"))
        //            result.result.Add("trace", apiResult.data?["trace"]?.ToString());

        //        return BadRequest(result);
        //    }
        //    else if (apiResult.data != null && apiResult.data.ContainsKey("error") && !string.IsNullOrEmpty(apiResult.data?["error"]?.ToString() ?? ""))
        //    {
        //        result.result.Add("messagetype", "Custom");
        //        result.result.Add("message", apiResult.data?["error"]?.ToString());

        //        if (apiResult.data != null && apiResult.data.ContainsKey("permissions"))
        //            result.result.Add("permissions", apiResult.data?["permissions"]?.ToString());

        //        if (apiResult.data != null && apiResult.data.ContainsKey("trace"))
        //            result.result.Add("trace", apiResult.data?["trace"]?.ToString());


        //        return BadRequest(result);
        //    }
        //    else
        //    {
        //        result.result["message"] = "SUCCESS";

        //        if (apiResult.data != null && apiResult.data.ContainsKey("message"))
        //            result.result.Add("messagetype", "Custom");

        //        foreach (KeyValuePair<string, object> kp in apiResult.data)
        //        {
        //            try
        //            {
        //                if (kp.Value != null)
        //                {
        //                    if (result.result.ContainsKey(kp.Key))
        //                        result.result[kp.Key] = kp.Value;
        //                    else
        //                        result.result.Add(kp.Key, kp.Value);
        //                }
        //            }
        //            catch { }
        //        }
        //        return Ok(result);
        //    }
        //}
    }
}
