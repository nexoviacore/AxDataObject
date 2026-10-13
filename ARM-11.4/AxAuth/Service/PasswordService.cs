using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMCommon.Model;
using Newtonsoft.Json;
using System.Linq;
using System.Text;
using static ARMCommon.Helpers.Constants;

namespace AxAuth
{
    public class PasswordService : IPasswordService
    {
        private readonly IPasswordRepository _repo;
        private readonly Utils _common;
        private readonly IARMLogTraceService _traceService;
        private readonly AESPasswordHelper _aesPasswordHelper;
        public PasswordService(IPasswordRepository repo, IARMLogTraceService traceService, Utils common, AESPasswordHelper aesPasswordHelper)
        {
            _repo = repo;
            _traceService = traceService;
            _common = common;
            _aesPasswordHelper = aesPasswordHelper;
        }

        public async Task<ARMResult> ChangePasswordAsync(ChangePasswordInput input)
        {
            var result = new ARMResult();
            try
            {
                _common._appName = input.AppName;
                var userResult = await _repo.GetUserDetails(input.UserName);
                if (userResult == null || !string.IsNullOrEmpty(userResult.error) || userResult.data == null || userResult.data.Rows.Count == 0)
                    return new ARMResult(false, "NOUSERDETAILSFOUND");

                var userData = await _common.DataRowMapper<UserData>(userResult.data.Rows[0]);

                var pwdSettings = await _common.GetAxpertProperties(_common._appName, Constants.AXPERT_PROPERTIES.PASSWORD.ToString());
                var pwdProps = JsonConvert.DeserializeObject<Dictionary<string, object>>(pwdSettings);
                bool aesPwd = IsTrueFlag(pwdProps, "pwdaes");

                if (userData.Active?.ToUpper() != "T")
                    return new ARMResult(false, "INACTIVEUSER");

                var isVaildPassword = IsPasswordMatch(input.OldPassword, userData.Password);
                if (!isVaildPassword && userData.Password.Length > 32)
                {
                    string decryptedPwd = _aesPasswordHelper.DecryptText(userData.Password);
                    isVaildPassword = (input.OldPassword == _common.MD5Hash(decryptedPwd));
                }

                if (!isVaildPassword)
                    return new ARMResult(false, "INCORRECTPASSWORD");


                string newPasswordHash = _common.MD5Hash(input.NewPassword);
                string newPasswordAES = _aesPasswordHelper.EncryptText(input.NewPassword);

                if (userData.Password.Length > 32)
                {
                    if (IsPasswordMatch(newPasswordAES, userData.Password))
                        return new ARMResult(false, "PASSWORDSHOULDBEDIFFERENT");
                }
                else
                {
                    if (IsPasswordMatch(newPasswordHash, userData.Password))
                        return new ARMResult(false, "PASSWORDSHOULDBEDIFFERENT");
                }
                

                string complexityError = await ValidatePasswordComplexity(input.NewPassword, pwdProps);
                if (!string.IsNullOrEmpty(complexityError))
                    return new ARMResult(false, complexityError);

                bool pwdPolicyExists = await HasPwdPolicyRecord(input.UserName);

                var updateResult = await _repo.UpdatePassword(input.UserName, (aesPwd ? newPasswordAES : newPasswordHash), userData.Password, false, pwdPolicyExists, "N");
                if (updateResult == null || !updateResult.success)
                    return new ARMResult(false, "PASSWORDUPDATIONFAILED");

                return new ARMResult(true, "PASSWORDUPDATED");
            }
            catch (Exception ex)
            {
                if (input.Trace) _traceService.LogError("Error in ChangePasswordAsync : " + ex.Message + ex.StackTrace + ex.InnerException);
                return new ARMResult(false, "Error occurred. Please check with administrator.");
            }
            finally
            {
                await _common.CloseDBConnectionAsync();
            }
        }

        public async Task<ARMResult> ForgotPasswordAsync(ForgotPasswordInput input)
        {
            var result = new ARMResult();
            _common._appName = input.AppName;
            try
            {
                var userResult = await _repo.GetUserForPasswordReset(input.UserName, input.Email);
                if (userResult == null || !string.IsNullOrEmpty(userResult.error) || userResult.data == null || userResult.data.Rows.Count == 0)
                    return new ARMResult(false, "INVAILDDETAILS");

                var userData = await _common.DataRowMapper<PasswordUserData>(userResult.data.Rows[0]);

                if (userData.Active?.ToUpper() != "T")
                    return new ARMResult(false, "INACTIVEUSER");

                string newPassword = _common.GenerateOTP("10", false);
                string newPasswordHash = _common.MD5Hash(newPassword);
                string newPasswordAES = _aesPasswordHelper.EncryptText(newPassword);

                var pwdSettings = await _common.GetAxpertProperties(_common._appName, Constants.AXPERT_PROPERTIES.PASSWORD.ToString());
                var pwdProps = JsonConvert.DeserializeObject<Dictionary<string, object>>(pwdSettings);
                bool aesPwd = IsTrueFlag(pwdProps, "pwdaes");


                int regenAttempts = 0;
                while ((IsPasswordMatch(newPasswordHash, userData.Password) || IsPasswordMatch(newPasswordAES, userData.Password)) && regenAttempts < 3)
                {
                    newPassword = _common.GenerateOTP("10", false);
                    newPasswordHash = _common.MD5Hash(newPassword);
                    newPasswordAES = _aesPasswordHelper.EncryptText(newPassword);
                    regenAttempts++;
                }

                bool pwdPolicyExists = await HasPwdPolicyRecord(userData.Username);

                var updateResult = await _repo.UpdatePassword(userData.Username, (aesPwd ? newPasswordAES : newPasswordHash), userData.Password, true, pwdPolicyExists, "Y");
                if (updateResult == null || !updateResult.success)
                    return new ARMResult(false, "PASSWORDUPDATIONFAILED");

                await SendPasswordResetNotification(input.AppName, userData.Username, userData.Email, newPassword);

                return new ARMResult(true, "FORGOTPASSWORDUPDATED");
            }
            catch (Exception ex)
            {
                if (input.Trace) _traceService.LogError("Error in ForgotPasswordAsync : " + ex.Message + ex.StackTrace + ex.InnerException);
                return new ARMResult(false, ex.Message);
            }
            finally
            {
                await _common.CloseDBConnectionAsync();
            }
        }

        private bool IsPasswordMatch(string candidateHash, string storedPassword)
        {
            if (string.IsNullOrEmpty(storedPassword))
                return false;

            bool isMatch = candidateHash == storedPassword;
            if (!isMatch && storedPassword.Length > 32)
            {
                string decryptedPwd = AXPEncryptData.Decrypt(storedPassword);
                isMatch = candidateHash == _common.MD5Hash(decryptedPwd);
            }

            return isMatch;
        }

        private async Task<bool> HasPwdPolicyRecord(string userName)
        {
            var pwdPolicyResult = await _repo.GetUserPwdPolicy(userName);
            return pwdPolicyResult != null && string.IsNullOrEmpty(pwdPolicyResult.error) && pwdPolicyResult.data != null && pwdPolicyResult.data.Rows.Count > 0;
        }

        private async Task<string> ValidatePasswordComplexity(string password, Dictionary<string, object> pwdProps)
        {
            if (string.IsNullOrEmpty(password))
                return "New password is required.";
           
            int minChar = GetIntProperty(pwdProps, "pwdminchar");
            int maxChar = GetIntProperty(pwdProps, "pwdmaxchar");
            int capChar = GetIntProperty(pwdProps, "pwdcapchar");
            int smallChar = GetIntProperty(pwdProps, "pwdsmallchar");
            int numChar = GetIntProperty(pwdProps, "pwdnumchar");
            int splChar = GetIntProperty(pwdProps, "pwdsplchar");
            bool alphaNum = IsTrueFlag(pwdProps, "pwdalphanum");

            if (minChar > 0 && password.Length < minChar)
                return $"Password should be at least {minChar} characters long.";

            if (maxChar > 0 && password.Length > maxChar)
                return $"Password should not exceed {maxChar} characters.";

            if (capChar > 0 && password.Count(char.IsUpper) < capChar)
                return $"Password should contain at least {capChar} uppercase character(s).";

            if (smallChar > 0 && password.Count(char.IsLower) < smallChar)
                return $"Password should contain at least {smallChar} lowercase character(s).";

            if (numChar > 0 && password.Count(char.IsDigit) < numChar)
                return $"Password should contain at least {numChar} numeric character(s).";

            if (splChar > 0 && password.Count(c => !char.IsLetterOrDigit(c)) < splChar)
                return $"Password should contain at least {splChar} special character(s).";

            if (alphaNum && !(password.Any(char.IsLetter) && password.Any(char.IsDigit)))
                return "Password should be alphanumeric.";

            return string.Empty;
        }

        private int GetIntProperty(Dictionary<string, object> props, string key)
        {
            if (props != null && props.ContainsKey(key) && int.TryParse(props[key]?.ToString(), out int value))
                return value;

            return 0;
        }

        private bool IsTrueFlag(Dictionary<string, object> props, string key)
        {
            if (props != null && props.ContainsKey(key))
            {
                string value = props[key]?.ToString()?.ToUpper();
                return value == "T" || value == "TRUE";
            }

            return false;
        }

        private async Task SendPasswordResetNotification(string appName, string userName, string email, string newPassword)
        {
            try
            {
                var emailSettings = _common._appSettingsHelper.GetEmailSettings(appName);
                if (emailSettings == null)
                    throw new Exception("Email settings is missing for the app.");

                var armUrl = emailSettings?.Arm_Url;
                if (string.IsNullOrEmpty(armUrl))
                    throw new Exception("ARM URL settings is missing for the app.");

                var configProps = new Dictionary<string, object>
                {
                    { "host", emailSettings.Host ?? "" },
                    { "port", emailSettings.Port.ToString() ?? "" },
                    { "user", emailSettings.User ?? "" },
                    { "pwd", emailSettings.Pwd ?? "" },
                    { "arm_url", armUrl ?? "" },
                    { "emailsubject", "" },
                    { "emailbody",  "" },
                    { "smscontent", "" },
                    { "AxOTPAuth", (emailSettings.AxOTPAuth?.ToString() == "T" || emailSettings.AxOTPAuth?.ToString().ToLower() == "true") ? "true" : "false" },
                    { "AxOTPAuthCahrs", emailSettings.AxOTPAuthCahrs?.ToString() ?? "4" },
                    { "AxOTPAuthExpiry", emailSettings.AxOTPAuthExpiry?.Split(" ")?[0]?.ToString() ?? "1" },
                };

                string configJson = JsonConvert.SerializeObject(configProps);

                var keyParams = new string[] { "ARM", appName, "ForgotPassword", Guid.NewGuid().ToString() };
                string cacheKey = _common.GenerateRedisKeyString(keyParams);

                var redisConnected = await _common.OpenRedisConnectionAsync();
                if(redisConnected)
                    await _common._redisCache.StringSetBinaryAsync(cacheKey, configJson, 180);

                string emailSubject = "Reset Password";
                string emailBody = $"Dear {userName},<br>Your Axpert account password has been reset to {newPassword}<br>Regards,<br>Support Team.";

                emailBody = emailBody.Replace("\\r\\n", "<br>");
                emailBody = emailBody.Replace("\r\n", "<br>");
                emailBody = emailBody.Replace("\\n", "<br>");
                emailBody = emailBody.Replace("\n", "<br>");

                var redisSettings = _common._appSettingsHelper.GetRedisSettings(appName);

                string data = "{\"email\":{\"to\":[\"" + email + "\"],\"subject\": \"" + emailSubject + "\",\"body\":[\"" + emailBody + "\"]},\"sms\":{\"mobileno\":[],\"message\":[],\"otp\":\"\"},\"redis\":{\"redisserver\":\"" + redisSettings.Host + "\",\"redisport\":\"" + redisSettings.Port + "\",\"redispwd\":\"" + redisSettings.DecryptedPwd + "\",\"emailconfigkey\":\"" + cacheKey + "\"}}";

                var queueJson = "{\"queuename\":\"LoginOTPQueue\",\"queuedata\":" + JsonConvert.SerializeObject(data) + ",\"msgexpirysecs\":\"180\"}";

                using (HttpClient qclient = new HttpClient())
                {
                    var qcontent = new StringContent(queueJson, Encoding.UTF8, "application/json");
                    await qclient.PostAsync(armUrl, qcontent);
                }
            }
            catch (Exception ex)
            {
                _traceService.LogError("Error in SendPasswordResetNotification : " + ex.Message + ex.StackTrace + ex.InnerException);
            }
        }
    }
}
