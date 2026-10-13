using ARM_APIs.Interface;
using ARMCommon.ActionFilter;
using ARMCommon.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace ARM_APIs.Controllers
{
    [Route("api/v{version:apiVersion}")]
    [ApiVersion("1")]
    [ServiceFilter(typeof(ApiResponseFilter))]
    [ApiController]
    public class ARMSigninDetailsController : Controller
    {

        private readonly IARMSigninDetails _signinDetails;

        public ARMSigninDetailsController(IARMSigninDetails signinDetails)
        {
            _signinDetails = signinDetails;
        }


        [AllowAnonymous]
        [HttpPost("ARMSigninDetails_Old")]
        public async Task<IActionResult> ARMSigninDetails_Old(ARMGetUserGroup model)
        {
            ARMResult resultData;
            try
            {
                var result = await _signinDetails.GetAppConfiguration(model.appname);

                resultData = new ARMResult();
                resultData.result.Add("message", "SUCCESS");
                resultData.result.Add("data", result);
                return Ok(resultData);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }



        [AllowAnonymous]
        [HttpPost("ARMGetAppModificationTime")]
        public async Task<IActionResult> ARMGetAppModificationTime(ARMGetUserGroup model)
        {
            ARMResult resultData;
            try
            {
                var result = await _signinDetails.GetAppModificationTime(model.appname);
                resultData = new ARMResult();
                resultData.result.Add("message", "SUCCESS");
                resultData.result.Add("data", result);
                return Ok(resultData);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}