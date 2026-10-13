using ARMCommon.ActionFilter;
using ARMCommon.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ARMCommon.Interface;

namespace AxStructUtils
{
    [Route("api/v{version:apiVersion}")]
    [ApiVersion("1")]
    [Authorize]
    [ServiceFilter(typeof(ApiResponseFilter))]
    [ApiController]
    public class AxStructureController : ControllerBase
    {

        private readonly IAxStructureService _structure;
        public AxStructureController(IAxStructureService structure)
        {
            _structure = structure;
        }

        [RequiredFieldsFilter("ARMSessionId", "TransId")]
        [HttpPost("AxGetStructure")]
        public async Task<IActionResult> AxGetStructure(AxStructure structure)
        {
            ARMResult result = new ARMResult();

            APIResult structureResult = await _structure.AxGetStructure(structure);

            if (!string.IsNullOrEmpty(structureResult.error))
            {
                result.result.Add("messagetype", "Custom");
                result.result.Add("message", structureResult.error);

                if (structureResult.data != null && structureResult.data.ContainsKey("trace"))
                    result.result.Add("trace", structureResult.data?["trace"]?.ToString());

                return BadRequest(result);
            }
            else if (structureResult.data != null && structureResult.data.ContainsKey("error") && !string.IsNullOrEmpty(structureResult.data?["error"]?.ToString() ?? ""))
            {
                result.result.Add("messagetype", "Custom");
                result.result.Add("message", structureResult.data?["error"]?.ToString());

                if (structureResult.data != null && structureResult.data.ContainsKey("trace"))
                    result.result.Add("trace", structureResult.data?["trace"]?.ToString());


                return BadRequest(result);
            }
            else
            {
                result.result["message"] = "SUCCESS";

                if (structureResult.data != null && structureResult.data.ContainsKey("message"))
                    result.result.Add("messagetype", "Custom");

                foreach (KeyValuePair<string, object> kp in structureResult.data)
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
