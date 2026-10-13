using ARMCommon.ActionFilter;
using ARMCommon.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AxList
{
    [Route("api/v{version:apiVersion}")]
    [ApiVersion("1")]
    [ApiController]
    [Authorize]
    [ServiceFilter(typeof(ApiResponseFilter))]
    public class AxListController : ControllerBase
    {
        private readonly IAxListService _axListService;

        public AxListController(IAxListService axListService)
        {
            _axListService = axListService;
        }
       
        [RequiredFieldsFilter("ARMSessionId", "Action")]
        [HttpPost("AxList")]
        public async Task<IActionResult> AxList(AxList listData)
        {
            ARMResult result = new ARMResult();
            APIResult apiResult = new APIResult();
            if (string.IsNullOrEmpty(listData.TransId) && (listData.ADSNames == null || listData.ADSNames?.Count == 0))
            {
                result.result.Add("messagetype", "Custom");
                result.result.Add("message", "TransId or ADSNames should be mentioned");
                return BadRequest(result);
            }

            apiResult = await _axListService.AxListAsync(listData);

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

                if (apiResult.data != null && apiResult.data.ContainsKey("partialsuccess") && Convert.ToBoolean(apiResult.data["partialsuccess"]) == true)
                    return StatusCode(StatusCodes.Status206PartialContent, result);
                else
                    return Ok(result);
            }
        }
    }
}
