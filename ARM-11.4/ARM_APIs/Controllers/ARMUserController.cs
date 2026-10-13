using ARM_APIs.Interface;
using ARMCommon.ActionFilter;
using ARMCommon.Filter;
using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMCommon.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ARM_APIs.Controllers
{
    [Route("api/v{version:apiVersion}")]
    [ApiVersion("1")]
    [Authorize]
    [ApiController]
    public class ARMUserController : Controller
    {
        private readonly IARMGetUserDetail _user;
       
        private readonly IRedisHelper _redis;
        private readonly INotificationHelper _notification;
        public readonly Utils _utils;
        public ARMUserController(IARMGetUserDetail user, IRedisHelper redis, INotificationHelper notification, Utils utils)
        {
            _user = user;
            _redis = redis;
            _notification = notification;
            _utils = utils;
        }
   
        #region ARMGetUserDetails
        [RequiredFieldsFilter("ARMSessionId")]
        [ServiceFilter(typeof(ValidateSessionFilter))]
        [ServiceFilter(typeof(ApiResponseFilter))]
        [HttpPost("ARMGetUserProfile")]
        public async Task<IActionResult> ARMGetUserProfile(ARMGetUserProfile user)
        {
            var loginuser = await _user.GetUserProfileDetails(user.ARMSessionId);
            if (loginuser == null)
            {
                return BadRequest("NOUSERDETAILSFOUND");
            }

            ARMResult result = new ARMResult();
            result.result.Add("message", "USERDETAILSFETCEHD");
            result.result.Add("email", loginuser.email);
            result.result.Add("mobileno", loginuser.mobileno);

            return Ok(result);

        }

        #endregion

        [RequiredFieldsFilter("email")]
        [ServiceFilter(typeof(ValidateSessionFilter))]
        [ServiceFilter(typeof(ApiResponseFilter))]
        [HttpPost("ARMUpdateUserProfile")]
        public async Task<IActionResult> ARMUpdateUserProfile(ARMUpdateUserProfile user)
        {
            if (await _user.ValidateUserByEmail(user.email))
            {
                return BadRequest("EMAILEXISTS");
            }
            if (!string.IsNullOrEmpty(user.mobileno))
            {
                if (await _user.MobileExist(user.mobileno))
                {
                    return BadRequest("DUPLICATEMOBILENO");
                }
            }

            string otp = _notification.GenerateOTP();
            string regId = Guid.NewGuid().ToString();
            await _user.SaveLogindetailsToRedis(otp, regId);
            var response = await _notification.SendEmailOTP(user.email, otp,user.username);
            if (response == null)
            {
                return BadRequest("ERRORINSENDINGOTP");
            }
            else
            {
                var result = _utils.GetOTPResult("OTPSENT", regId);
                return Ok(result);

            }

        }

        [RequiredFieldsFilter("regid", "otp", "email", "ARMSessionId")]
        [ServiceFilter(typeof(ValidateSessionFilter))]
        [ServiceFilter(typeof(ApiResponseFilter))]
        [HttpPost("ARMValidateUserProfile")]
        public async Task<IActionResult> ARMValidateUserProfile(ARMUpdateUserProfile user)
        {
           
            string key = $"{Constants.REDIS_PREFIX.ARMUPDATEUSERDETAIL.ToString()}_{user.regid}";
            string registrationData = await _redis.StringGetAsync(key);
            if (!string.IsNullOrEmpty(registrationData))
            {
                var registrationDetails = JsonConvert.DeserializeObject(registrationData).ToString();

                var userDetails = JObject.Parse(registrationDetails);
                string otp = userDetails["otp"].ToString();
                int totalAttempt = ((int)userDetails["otpattemptsleft"]);

                if (totalAttempt > 0)
                {
                    if (user.otp == otp)
                    {
                        if (string.IsNullOrEmpty(user.mobileno))
                        {
                            user.mobileno = "";
                        }
                        var updateprofile = await _user.UpdateUserProfileDetails(user.ARMSessionId, user.email, user.mobileno);
                        if (updateprofile)
                        {
                            return Ok("EMAIL/MOBILERESETSUCCESSFULLY");
                        }
                        else
                        {
                            return BadRequest("EMAIL/MOBILERESETFAILED");
                        }

                    }
                    else
                    {
                        userDetails["otpattemptsleft"] = totalAttempt - 1;
                        await _redis.StringSetAsync(key, JsonConvert.SerializeObject(userDetails));
                        var result = _utils.OTPFailureResult("OTPUNMATCHED", totalAttempt);
                        return BadRequest(result);
                    }
                }

                else
                {
                    return BadRequest("NOATTEMPTSLEFT");
                }
            }
            else
            {
                return BadRequest("NOREGISTRATIONDATAFOUND");
            }
        }

    }
}
