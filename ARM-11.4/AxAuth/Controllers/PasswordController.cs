using ARMCommon.ActionFilter;
using ARMCommon.Filter;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AxAuth.Controllers
{
    [Route("api/v{version:apiVersion}")]
    [ApiVersion("1")]
    [ServiceFilter(typeof(ApiResponseFilter))]
    [ApiController]
    public class PasswordController : ControllerBase
    {
        private readonly IPasswordService _passwordService;
        public PasswordController(IPasswordService passwordService)
        {
            _passwordService = passwordService;
        }

        [RequiredFieldsFilter("AppName", "Username", "OldPassword", "NewPassword")]
        [HttpPost("AxResetPassword")]
        public async Task<IActionResult> AxResetPassword([FromBody] ChangePasswordInput input)
        {
            var result = await _passwordService.ChangePasswordAsync(input);

            if (result.result["success"] != null && Convert.ToBoolean(result.result["success"]) != true)
                return BadRequest(result);
            else
                return Ok(result);
        }

        [AllowAnonymous]
        [RequiredFieldsFilter("AppName", "Username", "Email")]
        [HttpPost("AxForgotPassword")]
        public async Task<IActionResult> AxForgotPassword([FromBody] ForgotPasswordInput input)
        {
            var result = await _passwordService.ForgotPasswordAsync(input);

            if (result.result["success"] != null && Convert.ToBoolean(result.result["success"]) != true)
                return BadRequest(result);
            else
                return Ok(result);
        }
    }
}
