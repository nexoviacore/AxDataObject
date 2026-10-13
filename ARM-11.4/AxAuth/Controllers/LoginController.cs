using ARMCommon.ActionFilter;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ARMCommon.Model;
using ARMCommon.Filter;
using AxAuth;

namespace AxAuth.Controllers
{
    [Route("api/v{version:apiVersion}")]
    [ApiVersion("1")]
    [ServiceFilter(typeof(ApiResponseFilter))]
    [ApiController]
    public class LoginController : ControllerBase
    {
        private readonly ILoginService _loginService;
        public LoginController(ILoginService loginService)
        {
            _loginService = loginService;
        }


        [AllowAnonymous]
        [HttpPost("ARMSigninDetails")]
        public async Task<IActionResult> ARMSigninDetails(MobileLoginSettings model)
        {
            try
            {
                var result = _loginService.GetMobileLoginSettings(model);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [AllowAnonymous]
        [RequiredFieldsFilter("AppName", "UserName")]
        [HttpPost("GetLoginUserDetails")]
        public async Task<IActionResult> GetLoginUserDetails([FromBody] LoginUserInput input)
        {
            var loginResult = await _loginService.GetLoginUserDetailsAsync(input);

            loginResult.result.Add("messagetype", "Custom");
            if (loginResult.result["Success"] != null && Convert.ToBoolean(loginResult.result["Success"]) != true)
            {
                return Unauthorized(loginResult);
            }
            else
            {
                return Ok(loginResult);
            }
        }


        [AllowAnonymous]
        [RequiredFieldsFilter("OTPLoginKey", "OTP")]
        [HttpPost("ValidateOTP")]
        public async Task<IActionResult> ValidateOTP([FromBody] LoginInput input)
        {
            var loginResult = await _loginService.ValidateOTPAsync(input);

            loginResult.result.Add("messagetype", "Custom");
            if (loginResult.result["Success"] != null && Convert.ToBoolean(loginResult.result["Success"]) != true)
            {
                return Unauthorized(loginResult);
            }
            else
            {
                return Ok(loginResult);
            }
        }

        [AllowAnonymous]
        [RequiredFieldsFilter("OTPLoginKey")]
        [HttpPost("ResendOTP")]
        public async Task<IActionResult> ResendOTP([FromBody] LoginInput input)
        {
            var loginResult = await _loginService.ResendOTPAsync(input);

            loginResult.result.Add("messagetype", "Custom");
            if (loginResult.result["Success"] != null && Convert.ToBoolean(loginResult.result["Success"]) != true)
            {
                return Unauthorized(loginResult);
            }
            else
            {
                return Ok(loginResult);
            }
        }

        [AllowAnonymous]
        [RequiredFieldsFilter("AppName", "UserName", "SessionId", "Seed")]
        [HttpPost("Signin")]
        public async Task<IActionResult> Signin([FromBody] LoginInput input)
        {
            var loginResult = await _loginService.SigninAsync(input);

            loginResult.result.Add("messagetype", "Custom");
            if (loginResult.result["Success"] != null && Convert.ToBoolean(loginResult.result["Success"]) != true)
            {
                if (loginResult.result.ContainsKey("duplicate_session") && loginResult.result["duplicate_session"] != null && Convert.ToBoolean(loginResult.result["duplicate_session"]) == true)
                    return Ok(loginResult);
                else if (loginResult.result.ContainsKey("change_password") && loginResult.result["change_password"] != null && Convert.ToBoolean(loginResult.result["change_password"]) == true)
                    return Ok(loginResult);
                else
                    return Unauthorized(loginResult);
            }
            else
            {
                return Ok(loginResult);
            }
        }

        #region ARMSignOut
        [ServiceFilter(typeof(ValidateSessionFilter))]
        [HttpPost("ARMSignOut")]
        public async Task<IActionResult> ARMSignOut(ARMSession session)
        {
            var signOutResult = await _loginService.SignoutAsync(session);
            if (signOutResult.success)
                return Ok("USERSIGNOUT");
            else
                return BadRequest(signOutResult);
        }

        #endregion

        #region AxpertConnection APIs
        [Authorize]
        [RequiredFieldsFilter("ARMSessionId")]
        [HttpPost("ARMConnectToAxpert")]
        public async Task<ActionResult> ARMConnectToAxpert(ARMAxpertConnect axpert)
        {
            ARMResult result = await _loginService.ConnectToAxpert(axpert);
            if (result != null && result.result != null && result.result.ContainsKey("success") && result.result["success"]?.ToString().ToLower() == "true")
                return Ok(result);
            else
                return BadRequest(result);

        }

        [AllowAnonymous]
        [RequiredFieldsFilter("Key", "AxSessionId", "User", "AppName")]
        [HttpPost("ARMConnectFromAxpert")]
        public async Task<IActionResult> ARMConnectFromAxpert(ARMAxpertConnect axpert)
        {
            try
            {
                ARMResult result = await _loginService.ConnectFromAxpert(axpert);
                if (result != null && result.result != null && result.result.ContainsKey("success") && result.result["success"]?.ToString().ToLower() == "true")
                    return Ok(result);
                else
                {
                    return BadRequest("ARMAUTHFAILED");
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Authorize]
        [RequiredFieldsFilter("ARMSessionId")]
        [HttpPost("AxValidateSession")]
        public async Task<ActionResult> AxValidateSession(ARMAxpertConnect axpert)
        {
            APIResult result = await _loginService.AxValidateSession(axpert);
            if (result != null && result.success)
                return Ok(result.data["message"]);
            else
                return Unauthorized(result.data["error"]);

        }
        #endregion
    }
}