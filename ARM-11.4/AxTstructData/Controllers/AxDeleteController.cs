using ARMCommon.ActionFilter;
using ARMCommon.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ARMCommon.Interface;
using ARMCommon.Helpers;

namespace AxTstructData
{
    [Route("api/v{version:apiVersion}")]
    [ApiVersion("1")]
    [Authorize]    
    [ServiceFilter(typeof(ApiResponseFilter))]
    [ApiController]
    public class AxDeleteController : ControllerBase
    {

        private readonly IAxDeleteService _deleteData;
        private readonly IARMLogTraceService _traceService;
        public AxDeleteController(IAxDeleteService deleteData, IARMLogTraceService traceService)
        {
            _deleteData = deleteData;
            _traceService = traceService;
        }       

        [HttpPost("AxDelete")]
        public async Task<IActionResult> AxDelete(AxDelete deleteData)
        {
            deleteData.Action = "Delete";
            return await DeleteDataAsync(deleteData);
        }

        [HttpPost("AxCancel")]
        public async Task<IActionResult> AxCancel(AxDelete deleteData)
        {
            deleteData.Action = "Cancel";
            return await DeleteDataAsync(deleteData);
        }

        public async Task<IActionResult> DeleteDataAsync(AxDelete deleteData)
        {
            ARMResult result = new ARMResult();
            APIResult getDataResult = await _deleteData.AxDelete(deleteData);

            if (!string.IsNullOrEmpty(getDataResult.error))
            {
                result.result.Add("messagetype", "Custom");
                result.result.Add("message", getDataResult.error);

                if (getDataResult.data != null && getDataResult.data.ContainsKey("permissions"))
                    result.result.Add("permissions", getDataResult.data?["permissions"]?.ToString());

                if (getDataResult.data != null && getDataResult.data.ContainsKey("trace"))
                    result.result.Add("trace", getDataResult.data?["trace"]?.ToString());

                if (getDataResult.data != null && getDataResult.data.ContainsKey("transid"))
                    result.result.Add("transid", getDataResult.data?["transid"]?.ToString());

                if (getDataResult.data != null && getDataResult.data.ContainsKey("recordid"))
                    result.result.Add("recordid", getDataResult.data?["recordid"]?.ToString());

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


                if (getDataResult.data != null && getDataResult.data.ContainsKey("transid"))
                    result.result.Add("transid", getDataResult.data?["transid"]?.ToString());

                if (getDataResult.data != null && getDataResult.data.ContainsKey("recordid"))
                    result.result.Add("recordid", getDataResult.data?["recordid"]?.ToString());

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
