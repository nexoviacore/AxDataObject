using Microsoft.AspNetCore.Mvc;
using ARMCommon.Helpers;
using ARMCommon.Model;
using ARM_APIs.Interface;
using Newtonsoft.Json;
using ARMCommon.ActionFilter;
using ARMCommon.Filter;
using Microsoft.AspNetCore.Authorization;



namespace ARM_APIs.Controllers
{
    [Route("api/v{version:apiVersion}")]
    [ApiVersion("1")]
    [ApiController]
    public class ARMExecuteAPIController : ControllerBase
    {

        private readonly IARMExecuteAPI _executeAPI;

        public ARMExecuteAPIController(IARMExecuteAPI executeAPI)
        {
            _executeAPI = executeAPI;
        }

        //[Authorize]
        [RequiredFieldsFilter("SecretKey")]
        [HttpPost("ARMGetEncryptedSecret")]
        public string ARMGetEncryptedSecret(ARMPublishedAPI inputAPI)
        {
            AES aes = new AES();
            string currentTime = DateTime.Now.ToString("yyyyMMddHHmmss");
            return aes.EncryptString(currentTime, inputAPI.SecretKey);
        }

        [RequiredFieldsFilterAxpertResult("SecretKey", "PublicKey", "Project")]
        [HttpPost("ARMExecuteAPI")]
        public async Task<IActionResult> ARMExecuteAPI(ARMPublishedAPI inputAPI)
        {
            ARMResult result = new ARMResult();
            SQLResult apiDetails = await _executeAPI.GetPublishedAPI(inputAPI.Project, inputAPI.PublicKey);
            if (apiDetails != null)
            {
                if (!string.IsNullOrEmpty(apiDetails.error))
                {
                    result.result.Add("success", false);
                    result.result.Add("message", $"Error. {apiDetails.error}");
                    return BadRequest(JsonConvert.SerializeObject(result.result));
                }
                else if (apiDetails.data != null && apiDetails.data.Rows.Count == 0)
                {
                    result.result.Add("success", false);
                    result.result.Add("message", $"Error. Invalid API details.");
                    return BadRequest(JsonConvert.SerializeObject(result.result));
                }
                else
                {
                    ARMAPIDetails apiObj = new ARMAPIDetails();
                    apiObj = await _executeAPI.GetAPIObject(apiDetails);

                    APIResult apiResult = await _executeAPI.ExecuteAPI(inputAPI, apiObj, true);
                    if (string.IsNullOrEmpty(apiResult.error))
                    {
                        result.result.Add("success", true);
                        foreach (KeyValuePair<string, object> kp in apiResult.data)
                        {
                            try
                            {
                                result.result.Add(kp.Key, kp.Value);
                            }
                            catch { }
                        }
                        return Ok(JsonConvert.SerializeObject(result.result));
                    }
                    else
                    {
                        result.result.Add("success", false);
                        result.result.Add("message", $"Error. {apiResult.error}");
                        return BadRequest(JsonConvert.SerializeObject(result.result));
                    }
                }
            }
            else
            {
                result.result.Add("success", false);
                result.result.Add("message", "Error. Invalid API details.");
                return BadRequest(JsonConvert.SerializeObject(result.result));
            }
        }

        [Authorize]
        [RequiredFieldsFilterAxpertResult("PublicKey", "Project", "ARMSessionId")]
        [ServiceFilter(typeof(ValidateSessionFilter))]
        [HttpPost("ARMExecutePublishedAPI")]
        public async Task<IActionResult> ARMExecutePublishedAPI(ARMPublishedAPI inputAPI)
        {
            ARMResult result = new ARMResult();
            SQLResult apiDetails = await _executeAPI.GetPublishedAPI(inputAPI.Project, inputAPI.PublicKey);
            if (apiDetails != null)
            {
                if (!string.IsNullOrEmpty(apiDetails.error))
                {
                    result.result.Add("success", false);
                    result.result.Add("message", $"Error. {apiDetails.error}");
                    return BadRequest(JsonConvert.SerializeObject(result.result));
                }
                else if (apiDetails.data != null && apiDetails.data.Rows.Count == 0)
                {
                    result.result.Add("success", false);
                    result.result.Add("message", $"Error. Invalid API details.");
                    return BadRequest(JsonConvert.SerializeObject(result.result));
                }
                else
                {
                    ARMAPIDetails apiObj = new ARMAPIDetails();
                    apiObj = await _executeAPI.GetAPIObject(apiDetails);

                    APIResult apiResult = await _executeAPI.ExecuteAPI(inputAPI, apiObj, false);
                    if (string.IsNullOrEmpty(apiResult.error))
                    {
                        result.result.Add("success", true);
                        foreach (KeyValuePair<string, object> kp in apiResult.data)
                        {
                            try
                            {
                                result.result.Add(kp.Key, kp.Value);
                            }
                            catch { }
                        }
                        return Ok(JsonConvert.SerializeObject(result.result));
                    }
                    else
                    {
                        result.result.Add("success", false);
                        result.result.Add("message", $"Error. {apiResult.error}");
                        return BadRequest(JsonConvert.SerializeObject(result.result));
                    }
                }
            }
            else
            {
                result.result.Add("success", false);
                result.result.Add("message", "Error. Invalid API details.");
                return BadRequest(JsonConvert.SerializeObject(result.result));
            }
        }

    }
}
