using ARMCommon.ActionFilter;
using ARMCommon.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ARMCommon.Interface;

namespace AxUtils
{
    [Route("api/v{version:apiVersion}")]
    [ServiceFilter(typeof(ApiResponseFilter))]
    [ApiController]
    public class AxMenuController : ControllerBase
    {

        private readonly IAxMenuService _menu;
        public AxMenuController(IAxMenuService menu)
        {
            _menu = menu;
        }      

        [ApiVersion("1")]
        [Authorize]
        [RequiredFieldsFilter("ARMSessionId")]
        [HttpPost("ARMGetMenu")]
        public async Task<IActionResult> ARMGetMenu(ARMSession menuInput)
        {
            var result = await _menu.AxGetMenuAsync(menuInput);
            if (result is not null)
            {
                if (result.result.ContainsKey("success") && Convert.ToBoolean(result.result["success"]) == true)
                    return Ok(result);
                else
                    return BadRequest(result);
            }
            else
            {
                return BadRequest(result);
            }
        }    
    }
}
