using ARM_APIs.Interface;
using ARMCommon.ActionFilter;
using ARMCommon.Filter;
using ARMCommon.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ARM_APIs.Model;

namespace ARM_APIs.Controllers
{
    [Authorize]
    [Route("api/v{version:apiVersion}")]
    [ApiVersion("2")]
    [ServiceFilter(typeof(ValidateSessionFilter))]
    [ServiceFilter(typeof(ApiResponseFilter))]
    [ApiController]
    public class GetMenuControllerV2 : Controller
    {
        private readonly IARMenuV2 _menu;

        public GetMenuControllerV2(IARMenuV2 menu)
        {
            _menu = menu;
        }

        [RequiredFieldsFilter("ARMSessionId")]
        [HttpPost("ARMGetMenu_old")]
        public async Task<IActionResult> ARMGetMenu(ARMSession model)
        {
            var result = await _menu.GetMenu(model);
            if (result is not null)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest("Error in Feteching Data"); 
            }
        }


        [RequiredFieldsFilter("ARMSessionId")]
        [HttpPost("ARMGetHomePageCards")]
        public async Task<IActionResult> ARMGetHomePageCards(ARMProcessFlowTask process)
        {
            var result = await _menu.GetHomePageCards(process);
            if (result is not null)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest("Error in Fetching Data");
            }
        }

        [ApiVersion("1")]
        [RequiredFieldsFilter("ARMSessionId")]
        //[ServiceFilter(typeof(ApiLoggingFilter))]
        [HttpPost("GlobalSearch")]
        public async Task<IActionResult> GlobalSearch(GlobalSearch search)
        {
            ARMResult result = new ARMResult();
            var data = await _menu.GlobalSearch(search);
            if (!string.IsNullOrEmpty(data.error))
            {
                result.result.Add("message", data.error);
                result.result.Add("messagetype", "Custom");
                return BadRequest(result);
            }
            else
            {
                result.result.Add("message", "SUCCESS");
                result.result.Add("data", data.data);
                return Ok(result);
            }
        }

    }
}
