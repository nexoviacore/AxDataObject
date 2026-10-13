using ARMCommon.Interface;
using ARMCommon.Helpers;
using ARMCommon.Model;
using Newtonsoft.Json;
using System.Text;
using System.Data;
using Newtonsoft.Json.Linq;
using static ARMCommon.Helpers.Constants;
using AxParserApp;
using System.Globalization;
using StackExchange.Redis;

namespace AxAuth
{
    public class LoginService : ILoginService
    {
        private readonly ILoginRepository _repo;
        private readonly Utils _common;
        private readonly IARMLogTraceService _traceService;
        private readonly AESPasswordHelper _aesPasswordHelper;
        private GlobalVariablesManager _gloVars = new GlobalVariablesManager();
        private UserData _userData;
        private AxPropsManager _axProps;
        private readonly ITokenService _tokenService;
        private AxpertLog _axpertLog = new AxpertLog();
        private AxAudit _axAudit = new AxAudit();
        private AxParser _axParser;
        List<string> _roles = new List<string>();
        List<string> _responsibilities = new List<string>();
        List<string> _homePages = new List<string>();
        private string _userName = string.Empty;
        public LoginService(ILoginRepository repo, IARMLogTraceService traceService, Utils common, AESPasswordHelper aesPasswordHelper, ITokenService tokenService, AxParser axParser)
        {
            _repo = repo;
            _traceService = traceService;
            _common = common;
            _aesPasswordHelper = aesPasswordHelper;
            _tokenService = tokenService;
            _axParser = axParser;
        }

        public ARMResult GetMobileLoginSettings(MobileLoginSettings input)
        {
            var mobileSettings = _common._appSettingsHelper.GetMobileSettings(input.AppName);
            var ssoSettings = _common._appSettingsHelper.GetSSOSettings(input.AppName);
            
                var result = new ARMResult(true, "SUCCESS");
            var dataNode = new
            {
                applogo = "",
                appcolor = "#000000",
                modifiedontime = "",
                enablecitizenusers = (mobileSettings.Mob_CitizenUser == "T" ? true : false),
                enablegeofencing = (mobileSettings.Mob_Geofencing == "T" ? true : false),
                enablegeotagging = (mobileSettings.Mob_Geotag == "T" ? true : false),
                enablefingerprint = (mobileSettings.Mob_FingerAuth == "T" ? true : false),
                enablefacialrecognition = (mobileSettings.Mob_FaceAuth == "T" ? true : false),
                enableforceLogin = (mobileSettings.Mob_ForceLogin == "T" ? true : false),
                days = mobileSettings.Mob_ForceLoginUsers,
                sso = ssoSettings

            };
            result.result["data"] = dataNode;
            return result;
        }

        public async Task<ARMResult> GetLoginUserDetailsAsync(LoginUserInput input)
        {
            //_axpertLog.CalledOn = DateTime.Now;

            if (input.Trace)
            {
                _traceService.EnableTracing(true);
                _traceService.LogMessage("GetLoginUserDetailsAsync API call started.");
                _traceService.LogMessage($"Start Time: {DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")}");
                _traceService.LogMessage($"Input JSON: {JsonConvert.SerializeObject(input)}");
            }

            var result = new ARMResult();
            _common._appName = input.AppName;
            try
            {
                if (input.Trace) _traceService.LogMessage($"Initiating DB connection for application '{input.AppName}'");
                result = await GetLoginUserDetails(input);
            }
            catch (Exception ex)
            {
                result.result["success"] = false;
                result.result["message"] = "Error occurred. Please check with administrator.";
                if (input.Trace) _traceService.LogError("Error in User check : " + ex.Message + ex.StackTrace + ex.InnerException);
            }
            finally
            {
                await _common.CloseDBConnectionAsync();
            }

            if (input.Trace) result.result["Trace"] = await _traceService.GetTraceFile($"GetUserDetails-{input.Username}");

            return result;
        }

        private async Task<ARMResult> GetLoginUserDetails(LoginUserInput input)
        {
            var result = new ARMResult();
            try
            {
                if (input.Trace) _traceService.LogMessage("Get User Details");
                var userResult = await _repo.GetUserAuthDetails(input.Username);
                if (userResult != null)
                {
                    if (userResult.Active?.ToUpper() != "T" || (userResult.PwdAuth?.ToUpper() != "T" && userResult.OtpAuth?.ToUpper() != "T"))
                    {
                        result.result["success"] = false;
                        result.result["message"] = "Inactive User.";
                    }
                    else if (userResult.OtpAuth?.ToUpper() == "T" && string.IsNullOrEmpty(userResult.Mobile) && string.IsNullOrEmpty(userResult.Email))
                    {
                        result.result["success"] = false;
                        result.result["message"] = $"{input.Username} does not have Email id or mobile number to send OTP. Please check with administrator.";
                    }
                    else
                    {
                        result.result["success"] = true;
                        result.result["message"] = "Success";
                        result.result["pwdauth"] = userResult.PwdAuth;
                        result.result["language"] = userResult.AxLang;
                        if (userResult.PwdAuth == "T")
                        {
                            var strPwdSettings = await _common.GetAxpertProperties(input.AppName, Constants.AXPERT_PROPERTIES.PASSWORD.ToString());
                            if (!string.IsNullOrEmpty(strPwdSettings))
                            {
                                var pwdObj = JsonConvert.DeserializeObject<Dictionary<string, object>>(strPwdSettings);
                                result.result["pwdsettings"] = pwdObj;
                            }

                        }

                        if (userResult.OtpAuth == "T")
                        {
                            var strOtpSettings = await _common.GetAxpertProperties(input.AppName, Constants.AXPERT_PROPERTIES.EMAIL.ToString());
                            if (!string.IsNullOrEmpty(strOtpSettings))
                            {
                                var otpObj = JsonConvert.DeserializeObject<Dictionary<string, object>>(strOtpSettings);

                                if (otpObj.ContainsKey("AxOTPAuth") && (otpObj["AxOTPAuth"]?.ToString().ToLower() == "true" || otpObj["AxOTPAuth"]?.ToString().ToUpper() == "T"))
                                {
                                    result.result["otpauth"] = userResult.OtpAuth;

                                    var otpProps = new Dictionary<string, object>
                                    {
                                        { "otpchars", otpObj["AxOTPAuthCahrs"]?.ToString() ?? "4" },
                                        { "otpexpiry", otpObj["AxOTPAuthExpiry"]?.ToString().Split(" ")[0] ?? "1" },

                                    };

                                    result.result["otpsettings"] = otpProps;
                                }
                                else
                                    result.result["otpauth"] = "F";
                            }
                        }
                        else
                        {
                            result.result["otpauth"] = "F";
                        }
                    }
                }
                else
                {
                    result.result["success"] = false;
                    result.result["message"] = "Invalid User.";

                }
            }
            catch (Exception ex)
            {
                result.result["success"] = false;
                result.result["message"] = "Error occurred. Please check with administrator.";
                if (input.Trace) _traceService.LogError("Error in User check : " + ex.Message + ex.StackTrace + ex.InnerException);
            }

            return result;
        }        

        private async Task<ARMResult> GenerateOTP(ARMResult armResult, LoginUserData userResult, LoginInput input, string armSessionId)
        {
            var otpResult = new ARMResult();
            var emailOTPSettings = await _common.GetAxpertProperties(input.AppName, Constants.AXPERT_PROPERTIES.EMAIL.ToString());
            if (emailOTPSettings != "" && emailOTPSettings != "noemailsettings")
            {
                var otpObj = JsonConvert.DeserializeObject<Dictionary<string, object>>(emailOTPSettings);

                if (otpObj.ContainsKey("AxOTPAuth") && (otpObj["AxOTPAuth"]?.ToString() == "true" || otpObj["AxOTPAuth"]?.ToString().ToUpper() == "T"))
                {
                    var otp = _common.GenerateOTP(otpObj["AxOTPAuthCahrs"]?.ToString() ?? "4", true);
                    var otpExpiryMins = int.Parse(otpObj["AxOTPAuthExpiry"]?.ToString().Split(" ")[0] ?? "1");
                    var otpExpiry = int.Parse(otpObj["AxOTPAuthExpiry"]?.ToString().Split(" ")[0] ?? "1") * 60;
                    var armUrl = otpObj["arm_url"]?.ToString();

                    string emailSubject = otpObj["emailsubject"].ToString();
                    string emailBody = otpObj["emailbody"].ToString();
                    string smsMesg = otpObj["smscontent"].ToString();

                    if (emailSubject == string.Empty)
                        emailSubject = "Axpert OTP Authentication";
                    if (emailBody == string.Empty)
                        emailBody = "Dear " + input.Username + ",<br>One Time Password(OTP) for Axpert login is " + otp + " <br>Regards,<br>Support Team.";
                    else
                    {
                        emailBody = emailBody.Replace("{username}", input.Username);
                        emailBody = emailBody.Replace("{otp}", otp);
                        emailBody = emailBody.Replace("{otpexpirytime}", otpExpiryMins.ToString());
                    }

                    if (smsMesg == string.Empty)
                        smsMesg = "Dear " + input.Username + ",\\nOne Time Password(OTP) for Axpert login is " + otp + "";
                    else
                    {
                        smsMesg = smsMesg.Replace("{username}", input.Username);
                        smsMesg = smsMesg.Replace("{otp}", otp);
                        smsMesg = smsMesg.Replace("{otpexpirytime}", otpExpiryMins.ToString());
                    }

                    emailBody = emailBody.Replace("\\r\\n", "<br>");
                    emailBody = emailBody.Replace("\r\n", "<br>");
                    emailBody = emailBody.Replace("\\n", "<br>");
                    emailBody = emailBody.Replace("\n", "<br>");

                    smsMesg = smsMesg.Replace("\\r\\n", "\\n");
                    smsMesg = smsMesg.Replace("\r\n", "\\n");


                    var keyParams = new string[] { input.AppName, Constants.AXEMAILSMTP_CONN_KEY };
                    var cacheKey = _common.GenerateRedisKeyString(keyParams);

                    var redisSettings = _common._appSettingsHelper.GetRedisSettings(input.AppName);

                    string data = "{\"email\":{\"to\":[\"" + userResult.Email + "\"],\"subject\": \"" + emailSubject + "\",\"body\":[\"" + emailBody + "\"]},\"sms\":{\"mobileno\":[\"" + userResult.Mobile + "\"],\"message\": [\"" + smsMesg + "\"],\"otp\": \"" + otp + "\"},\"redis\":{\"redisserver\":\"" + redisSettings.Host + "\",\"redisport\":\"" + redisSettings.Port + "\",\"redispwd\":\"" + redisSettings.DecryptedPwd + "\",\"emailconfigkey\":\"" + cacheKey + "\"}}";

                    var queueJson = "{\"queuename\":\"LoginOTPQueue\",\"queuedata\":" + JsonConvert.SerializeObject(data) + ",\"msgexpirysecs\":\"" + otpExpiry + "\"}";

                    using (HttpClient qclient = new HttpClient())
                    {
                        var qcontent = new StringContent(queueJson, Encoding.UTF8, "application/json");
                        var qresponse = await qclient.PostAsync(armUrl, qcontent);
                        var output = await qresponse.Content.ReadAsStringAsync();
                        if (qresponse.IsSuccessStatusCode && output.IndexOf("queue Inserted Successfully") > -1)
                        {
                            string otpMsg = "";
                            if (userResult.Email != string.Empty && userResult.Mobile != string.Empty)
                            {
                                userResult.Email = _common.MaskEmail(userResult.Email);
                                userResult.Mobile = _common.MaskMobileNumber(userResult.Mobile);
                                otpMsg = "An OTP has been sent to your mobile number " + userResult.Mobile + " and email id " + userResult.Email + "";
                            }
                            else if (userResult.Email != string.Empty && userResult.Mobile == string.Empty)
                            {
                                userResult.Email = _common.MaskEmail(userResult.Email);
                                otpMsg = "An OTP has been sent to your email id " + userResult.Email + "";
                            }
                            else if (userResult.Email == string.Empty && userResult.Mobile != string.Empty)
                            {
                                userResult.Mobile = _common.MaskMobileNumber(userResult.Mobile);
                                otpMsg = "An OTP has been sent to your mobile number " + userResult.Mobile + "";
                            }



                            var otpStored = await _common.StoreOTPInCache(armSessionId, otp, otpExpiry);
                            if (otpStored)
                            {
                                otpResult = new ARMResult(true, otpMsg);
                                otpResult.result["OTPLoginKey"] = armSessionId;

                            }
                            else
                                otpResult = new ARMResult(false, "Error in OTP generation.");
                        }
                        else
                        {
                            otpResult = new ARMResult(false, await qresponse.Content.ReadAsStringAsync());

                        }
                    }
                }
            }
            return otpResult;
        }

        private async Task<ARMResult> StoreValuesInSession(ARMResult armResult, LoginInput input, string armSessionId)
        {
            var globalVars = new Dictionary<string, string>();
            if (armResult.result.ContainsKey("globalvars"))
                globalVars = (Dictionary<string, string>)armResult.result["globalvars"];

            var userVars = armResult.result.TryGetValue("uservars", out var userVar) ? userVar : null;
            var roles = globalVars.TryGetValue("usergroup", out var role) ? role : "";
            var responsibilities = globalVars.TryGetValue("userroles", out var resp) ? resp : "";
            var nickName = armResult.result.TryGetValue("nickname", out var nickname) ? nickname : "";

            var token = _tokenService.BuildToken(armSessionId, input.Username, input.AppName);

            if (token != null)
            {
                armResult.result["success"] = true;
                armResult.result["token"] = token;
                var expiryMins = _common._appSettingsHelper.GetARMTokenExpiryMinutes(input.AppName);
                armResult.result["expiry"] = expiryMins * 60 * 1000;
                armResult.result["username"] = input.Username;
                armResult.result["appname"] = input.AppName;
                armResult.result["ARMSessionId"] = armSessionId;

                var schemaName = _common._schemaName ?? input.AppName;
                Dictionary<string, string> sessionValues = new Dictionary<string, string>();
                sessionValues.Add(SESSION_DATA.USER_ROLES.ToString(), roles?.ToString() ?? "");
                sessionValues.Add(SESSION_DATA.USERNAME.ToString(), input.Username);
                sessionValues.Add(SESSION_DATA.ARMTOKEN.ToString(), token);
                sessionValues.Add(SESSION_DATA.APPNAME.ToString(), input.AppName);
                sessionValues.Add(SESSION_DATA.USERGROUP.ToString(), "Power");
                sessionValues.Add(SESSION_DATA.PASSWORD.ToString(), input.Password ?? "");
                sessionValues.Add(SESSION_DATA.GLOBALVARS.ToString(), JsonConvert.SerializeObject(globalVars));
                sessionValues.Add(SESSION_DATA.USERVARS.ToString(), JsonConvert.SerializeObject(userVars));
                sessionValues.Add(SESSION_DATA.USER_GROUP.ToString(), responsibilities?.ToString() ?? "");
                //sessionValues.Add(SESSION_DATA.AXPROPS.ToString(), axProps);
                //sessionValues.Add(SESSION_DATA.AXAPPS.ToString(), axApps);
                //sessionValues.Add(SESSION_DATA.SCRIPTSURL.ToString(), armWebScriptsUrl);
                sessionValues.Add(SESSION_DATA.SCHEMANAME.ToString(), schemaName);
                sessionValues.Add(SESSION_DATA.LANGUAGE.ToString(), input.Language ?? "ENGLISH");
                sessionValues.Add(SESSION_DATA.AXPERT_SESSIONID.ToString(), input.SessionId);
                sessionValues.Add(SESSION_DATA.SITENO.ToString(), _axProps.EnvironmentVars.SiteNo);

                if (input.ClearPreviousSession)
                    await _common.DeletePreviousLoggedUserInfo(schemaName, input.Username);

                await _common.SetARMLoggedUserInfo(schemaName, input.Username, armSessionId);
                await _common.SetAxpertLoggedUserInfo(schemaName, input.Username, input.Username + "♦" + armSessionId + "♣limited");
                await _common.StoreSessionValuesOnSign(armSessionId, sessionValues);
                _axParser._common = null;
                _axParser._structDef = null;
                await _common.StoreAxParserObjInCache(armSessionId, JsonConvert.SerializeObject(_axParser), expiryMins * 60);
            }
            return armResult;
        }

        public async Task<ARMResult> ValidateOTPAsync(LoginInput input)
        {
            var result = new ARMResult();
            try
            {
                var otpFromCache = await _common.GetOTPFromCache(input.OtpLoginKey);

                if (string.IsNullOrEmpty(otpFromCache))
                {
                    result = new ARMResult(false, "OTP expired. Please Regenerate OTP.");
                }
                else if (otpFromCache == input.Otp)
                {
                    var userData = await _repo.GetUserSessionFromCache(input.OtpLoginKey);
                    if (string.IsNullOrEmpty(userData))
                        result = new ARMResult(false, "User session is not available. Try relogin.");
                    else
                    {
                        await _repo.DeleteUserSessionFromCache(input.OtpLoginKey);
                        result = JsonConvert.DeserializeObject<ARMResult>(userData);
                        result.result["message"] = "Login Successful.";
                    }
                }
                else
                {
                    result = new ARMResult(false, "Entered OTP is invalid.");
                }
            }
            catch (Exception ex)
            {
                if (input.Trace) _traceService.LogError("Error in Validate OTP: " + ex.Message + ex.StackTrace + ex.InnerException);
                result = new ARMResult(false, "Error occurred. Please check with administrator.");
            }
            finally
            {
                await _common.CloseDBConnectionAsync();
            }
            return result;
        }

        public async Task<ARMResult> ResendOTPAsync(LoginInput input)
        {
            var result = new ARMResult();
            try
            {
                var userData = await _repo.GetUserSessionFromCache(input.OtpLoginKey);
                if (string.IsNullOrEmpty(userData))
                    return new ARMResult(false, "User session is not available. Try relogin.");
                else
                {
                    result = JsonConvert.DeserializeObject<ARMResult>(userData);
                    input.Username = result?.result?["username"]?.ToString() ?? "";
                    input.AppName = result?.result?["appname"]?.ToString() ?? "";
                    _common._appName = input.AppName;
                    if (!string.IsNullOrEmpty(input.Username))
                    {
                        var userResult = await _repo.GetUserAuthDetails(input.Username);

                        if (userResult != null)
                        {
                            if (userResult.OtpAuth?.ToUpper() == "T")
                            {
                                result = await GenerateOTP(result, userResult, input, input.OtpLoginKey);
                            }
                            else
                            {
                                result = new ARMResult(false, "OTP Auth is disabled for the user.");
                            }
                        }
                        else
                        {
                            result = new ARMResult(false, "User Auth details not found.");
                        }

                    }
                    else
                    {
                        result = new ARMResult(false, "User Auth details not found.");
                    }
                }
            }
            catch (Exception ex)
            {
                if (input.Trace) _traceService.LogError("Error in Resend OTP: " + ex.Message + ex.StackTrace + ex.InnerException);
                result = new ARMResult(false, "Error occurred. Please check with administrator.");
            }
            finally
            {
                await _common.CloseDBConnectionAsync();
            }
            return result;
        }

        public async Task<ARMResult> SigninAsync(LoginInput input)
        {
            _axpertLog.CalledOn = DateTime.Now;
            _axAudit.ServerTime = DateTime.Now;
            input.SessionId = input.SessionId.Replace("-", "");
            input.SessionId = (input.SessionId.Length > 30 ? input.SessionId.Substring(0, 30) : input.SessionId).Replace("-", "");

            var result = new ARMResult();
            if (input.Trace)
            {
                _traceService.EnableTracing(true);
                _traceService.LogMessage("SigninAsync API call started.");
                _traceService.LogMessage($"Start Time: {_axpertLog.CalledOn?.ToString("dd/MM/yyyy HH:mm:ss")}");
                _traceService.LogMessage($"Input JSON: {JsonConvert.SerializeObject(input)}");
            }

            _common._appName = input.AppName;
            _userName = input.Username.ToUpper();

            if (string.IsNullOrWhiteSpace(input.Seed) || input.Seed.Length < 4)
            {
                result = new ARMResult(false, $"Seed be at least 4 characters long.");
            }
            else
            {
                try
                {
                    result = await Login(input);
                    if (!result.result.ContainsKey("duplicate_session") && !result.result.ContainsKey("change_password"))
                        await LogSigninDetails(input, result);
                }
                catch (Exception ex)
                {
                    result = new ARMResult(false, $"Error: {ex.Message} ");
                }
                finally
                {
                    await _common.CloseDBConnectionAsync();
                }
            }

            if (input.Trace) result.result["Trace"] = await _traceService.GetTraceFile($"Login-{input.Username}");

            return result;
        }

        private async Task<ARMResult> Login(LoginInput input)
        {
            var armResult = new ARMResult();
            var userResult = new LoginUserData();
            string armSessionId = $"ARM-{input.AppName}-{Guid.NewGuid().ToString()}";
            try
            {
                var axApps = _common.GetAppConnection();

                if (!input.ClearPreviousSession)
                {
                    var loggedInAxpertUser = await _common.GetPreviousAxpertLoggedUserInfo(_common._schemaName, input.Username);
                    var loggedInARMUser = await _common.GetPreviousARMLoggedUserInfo(_common._schemaName, input.Username);
                    if (!string.IsNullOrEmpty(loggedInAxpertUser) || !string.IsNullOrEmpty(loggedInARMUser))
                    {
                        armResult.result["success"] = false;
                        armResult.result["message"] = "Your previous session will get signed out automatically. Do you want to continue?";
                        armResult.result["duplicate_session"] = true;
                        return armResult;
                    }
                }

                var loginResponse = await ValidatePassword(input);

                if (loginResponse.Success)
                {
                    try
                    {
                        userResult = await _repo.GetUserAuthDetails(input.Username);

                        if (userResult != null)
                        {
                            armResult = new ARMResult(true, "Login Successful.");

                            var armSettings = _common._appSettingsHelper.GetARMSettings(input.AppName);

                            LoadInternalResourcesToGloVars(input.AppName);

                            _gloVars.AddOrUpdateGlobalVarToList("sesid", "c", input.SessionId);
                            _gloVars.AddOrUpdateGlobalVarToList("project", "c", input.AppName);
                            _gloVars.AddOrUpdateGlobalVarToList("axp_connectionname", "c", input.AppName);
                            _gloVars.AddOrUpdateGlobalVarToList("axp_appschema", "c", input.AppName);
                            _gloVars.AddOrUpdateGlobalVarToList("groupno", "c", _userData.GroupNo?.ToString() ?? "");
                            _gloVars.AddOrUpdateGlobalVarToList("pageaccess", "c", _userData.PageAccess ?? "");
                            _gloVars.AddOrUpdateGlobalVarToList("transidlist", "c", "");
                            _gloVars.AddOrUpdateGlobalVarToList("axp_apipath", "c", armSettings.scriptsUrlPath);
                            _gloVars.AddOrUpdateGlobalVarToList("auth_path", "c", armSettings.ScriptsPath);
                            _gloVars.AddOrUpdateGlobalVarToList("millions", "c", _axProps?.EnvironmentVars?.Millions ?? "F");
                            _gloVars.AddOrUpdateGlobalVarToList("rolename", "c", string.Join(",", _roles));
                            _gloVars.AddOrUpdateGlobalVarToList("usergroup", "c", string.Join(",", _roles));
                            _gloVars.AddOrUpdateGlobalVarToList("responsibilities", "c", string.Join(",", _responsibilities));
                            _gloVars.AddOrUpdateGlobalVarToList("responsibilies", "c", string.Join(",", _responsibilities));
                            _gloVars.AddOrUpdateGlobalVarToList("userroles", "c", string.Join(",", _responsibilities));
                            _gloVars.AddOrUpdateGlobalVarToList("axglo", "c", _userData.AxGlo ?? "no");
                            _gloVars.AddOrUpdateGlobalVarToList("axglo_recordid", "c", _userData.AxGloRecordId ?? "0");
                            _gloVars.AddOrUpdateGlobalVarToList("appvartypes", "c", _gloVars.GetGlobalVarsType() ?? "");

                            armResult.result["username"] = _userData.UserName ?? input.Username;
                            armResult.result["role"] = string.Join(",", _roles);
                            armResult.result["nickname"] = _userData.Nickname ?? input.Username;
                            armResult.result["email_id"] = _userData.Email ?? "";
                            armResult.result["mobileno"] = _userData.Mobile ?? "";
                            armResult.result["axglo"] = _userData.AxGlo ?? "no";
                            armResult.result["userlandingpage"] = _userData.Homepage ?? "";
                            armResult.result["roleslandingpage"] = string.Join(",", _homePages) ?? "";
                            armResult.result["firsttime"] = _userData.FirstTimeLogin ?? "0";
                            armResult.result["appsessionkey"] = armSessionId;
                            armResult.result["build"] = _userData.Build ?? "F";
                            armResult.result["forcepwdchange"] = "0"; //TODO
                            armResult.result["users"] = _userData.Manage ?? "F";
                            armResult.result["workflow"] = _userData.WorkFlow ?? "F";
                            armResult.result["import"] = _userData.ImportStruct ?? "F";
                            armResult.result["export"] = _userData.ExportStruct ?? "F"; ;
                            armResult.result["appmgr"] = _userData.AppManager ?? "F";
                            armResult.result["pwdexpiresin"] = _userData.PwdExpDays?.ToString() ?? "0";
                            armResult.result["isuniquehybrid"] = _userData.IsUniqueHybrid ?? "";
                            armResult.result["globalvars"] = _gloVars.GetGlobalVars();
                            armResult.result["uservars"] = new { };
                        }
                        else
                        {
                            armResult = new ARMResult(false, "User Auth details not found.");
                        }

                    }
                    catch (Exception ex)
                    {
                        if (input.Trace) _traceService.LogError("Error in User check : " + ex.Message + ex.StackTrace + ex.InnerException);
                        armResult = new ARMResult(false, "Error occurred. Please check with administrator.");
                    }
                }
                else
                {
                    if (input.Trace) _traceService.LogError("Error in Validate Password : " + loginResponse.Message);
                    armResult = new ARMResult(false, loginResponse.Message);
                }

                if (armResult.result["success"].ToString().ToLower() == "true")
                {
                    armResult = await StoreValuesInSession(armResult, input, armSessionId);

                    if (!input.Globalvars)
                    {
                        armResult.result.Remove("globalvars");
                        armResult.result.Remove("uservars");
                    }

                    if (userResult.OtpAuth?.ToUpper() == "T")
                    {
                        var otpResult = await GenerateOTP(armResult, userResult, input, armSessionId);
                        var expiryMins = _common._appSettingsHelper.GetARMTokenExpiryMinutes(input.AppName);
                        armResult.result.Remove("message");
                        await _repo.StoreUserSessionInCache(armSessionId, JsonConvert.SerializeObject(armResult), expiryMins);

                        return otpResult;

                    }

                    return armResult;
                }
                else
                {
                    if (loginResponse.Message == "Please change the password to continue.")
                        armResult.result["change_password"] = true;
                }
            }
            catch (Exception ex)
            {
                if (input.Trace) _traceService.LogError("Error in User check : " + ex.Message + ex.StackTrace + ex.InnerException);
                armResult = new ARMResult(false, ex.Message);
            }

            return armResult;
        }

        private void LoadInternalResourcesToGloVars(string appName) {
            var internalResources = _common._appSettingsHelper.GetInternalResources(appName);
            if (internalResources != null)
            {
                foreach (var prop in typeof(InternalResources).GetProperties())
                {
                    if (prop.PropertyType != typeof(string))
                        continue;

                    string? value = prop.GetValue(internalResources) as string;

                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        _gloVars.AddOrUpdateGlobalVarToList(prop.Name, "c", value);
                        _axParser.RegisterVar(prop.Name, "c", value);
                    }
                }
            }
        }

        private async Task<LoginResponse> ValidatePassword(LoginInput input)
        {
            try
            {
                bool validUser = false;
                var userResult = await _repo.GetUserDetails(input.Username);
                if (userResult != null && string.IsNullOrEmpty(userResult.error) && userResult.data != null && userResult.data.Rows.Count > 0)
                {
                    _userData = await _common.DataRowMapper<UserData>(userResult.data.Rows[0]);
                    if (_userData.Active?.ToUpper() != "T" || _userData.ActFlag?.ToUpper() == "N" || _userData.ActFlag?.ToUpper() == "F")
                        return ConstructResult(false, "Inactive User.");

                    if (input.OtpAuth?.ToUpper() != "T" && _userData.PwdAuth?.ToUpper() != "T" && _userData.OtpAuth?.ToUpper() != "T")
                        return ConstructResult(false, "Invalid username or password.");

                    if (string.IsNullOrEmpty(_userData.Nickname))
                        _userData.Nickname = _userData.UserName;

                    if (_userName != "ADMIN")
                    {
                        if (_userData.IsFirstTime?.ToUpper() == "F")
                            _userData.FirstTimeLogin = "0";
                        else
                            _userData.FirstTimeLogin = "1";

                        if(_userData.FirstTimeLogin == "1") //Force change password
                        {
                            return ConstructResult(false, "Please change the password to continue.");
                        }
                    }
                    else
                        _userData.FirstTimeLogin = "0";

                    if (_userData.PwdAuth?.ToUpper() == "T")
                    {
                        if (string.IsNullOrEmpty(input.SingleSigninKey))
                        {

                            if (string.IsNullOrEmpty(input.Seed))
                            {
                                validUser = (input.Password == _userData.Password);
                                if (!validUser && _userData.Password.Length > 32)
                                {
                                    string decryptedPwd = _aesPasswordHelper.DecryptText(_userData.Password);
                                    string decryptedPwdHash = _common.MD5Hash(decryptedPwd);
                                    validUser = (input.Password == _common.MD5Hash(decryptedPwdHash));

                                }
                            }
                            else
                            {
                                validUser = (input.Password == _common.MD5Hash(input.Seed + _userData.Password));

                                if (!validUser && _userData.Password.Length > 32)
                                {
                                    string decryptedPwd = _aesPasswordHelper.DecryptText(_userData.Password);
                                    string decryptedPwdHash = _common.MD5Hash(decryptedPwd);
                                    validUser = (input.Password == _common.MD5Hash(input.Seed + decryptedPwdHash));

                                }
                            }
                        }
                        else
                        {
                            validUser = await CheckSSOKey(input);
                        }
                    }
                    else if (_userData.OtpAuth == "T" && _userData.PwdAuth?.ToUpper() != "T")
                    {
                        validUser = true;
                    }

                    string appUrl = _common.GetDomainName(input.AppName);
                    if (appUrl == string.Empty)
                    {
                        return ConstructResult(false, "Web URL is not configured for this app.");
                    }

                    if (_userName != "ADMIN")
                    {
                        string validLicenseMsg = ALCClient.CallALCFromLoginAPI(_userData.UserName, _userData.AuthKey, _userData.UserKey, appUrl, _common._schemaNameALC, input.AppName);
                        if (validLicenseMsg != "VALID")
                        {
                            return ConstructResult(false, validLicenseMsg);
                        }
                    }

                    string propsXml = await _repo.GetAxPropsXMLFromDB();

                    _axProps = AxPropsManager.ParseFromXml(propsXml);

                    if (validUser)
                    {
                        //if (_userData.IsIp?.ToUpper() == "T" && !string.IsNullOrEmpty(input.IPAddress) && _userData.IpAddr?.ToLower().IndexOf(input.IPAddress?.ToLower()) == -1)
                        //{
                        //    return ConstructResult(false, "You are trying to login from an unauthenticated ip address.");
                        //}

                        if (_userName != "ADMIN")
                        {
                            if (_axProps.Password.MaxLoginTry > 0)
                            {
                                int userLoginTries = 0;
                                if (!string.IsNullOrEmpty(_userData.LoginTry?.ToString()))
                                    userLoginTries = Convert.ToInt32(_userData.LoginTry);

                                if (userLoginTries > _axProps.Password.MaxLoginTry)
                                {
                                    return ConstructResult(false, "Your account is locked. Please contact administrator to unlock.");
                                }


                                SQLResult pwdPolicyResult = new SQLResult();

                                if (_axProps.Password.MaxLoginTry > 0 || _axProps.Password.PwdExpDays > 0)
                                {
                                    pwdPolicyResult = await _repo.GetUserPwdPolicy(input.Username);
                                }

                                if (_userData.PwdExpDays != null && _userData.PwdExpDays > 0)
                                    _axProps.Password.PwdExpDays = Convert.ToInt32(_userData.PwdExpDays);

                                if (_axProps.Password.PwdExpDays > 0)
                                {
                                    if (pwdPolicyResult != null && string.IsNullOrEmpty(pwdPolicyResult.error) && pwdPolicyResult.data != null && pwdPolicyResult.data.Rows.Count > 0 && pwdPolicyResult.data.Rows[0]["pwdchangedate"] != null && !string.IsNullOrEmpty(pwdPolicyResult.data.Rows[0]["pwdchangedate"]?.ToString()))
                                    {
                                        DateTime pwdChangeDate = (DateTime)pwdPolicyResult.data.Rows[0]["pwdchangedate"];
                                        TimeSpan changeDays = pwdChangeDate.Subtract(DateTime.Today);
                                        if (_axProps.Password.PwdExpDays > changeDays.Days)
                                            _userData.FirstTimeLogin = "2";

                                        if (_axProps.Password.PwdAlertDays > 0)
                                        {
                                            if (_axProps.Password.PwdExpDays > 0)
                                                _axProps.Password.PwdExpDays = changeDays.Days - (_axProps.Password.PwdExpDays - 1);

                                            if (_axProps.Password.PwdExpDays > _axProps.Password.PwdAlertDays)
                                                _axProps.Password.PwdExpDays = 0;
                                        }
                                        else
                                            _axProps.Password.PwdExpDays = 0;
                                    }
                                    else
                                    {
                                        _userData.FirstTimeLogin = "2";
                                    }
                                }

                                if (_axProps.Password.MaxLoginTry > 0)
                                {
                                    if (pwdPolicyResult != null && string.IsNullOrEmpty(pwdPolicyResult.error) && pwdPolicyResult.data != null && pwdPolicyResult.data.Rows.Count > 0)
                                    {
                                        string changePwd = pwdPolicyResult.data.Rows[0]["pwdchange"]?.ToString() ?? "N";
                                        if (changePwd.ToUpper() == "Y")
                                            _userData.FirstTimeLogin = "3";
                                    }
                                }

                                if (_userData.LoginTry > 0)
                                {
                                    await _repo.UpdateLoginTry(input.Username, 0);
                                }
                            }
                        }
                    }
                    else
                    {

                        if (_userName != "ADMIN" && _axProps.Password.MaxLoginTry > 0)
                        {
                            int userLoginTries = 0;
                            if (!string.IsNullOrEmpty(_userData.LoginTry?.ToString()))
                                userLoginTries = Convert.ToInt32(_userData.LoginTry);

                            await _repo.UpdateLoginTry(input.Username, userLoginTries + 1);

                            if (userLoginTries + 1 > _axProps.Password.MaxLoginTry)
                            {
                                return ConstructResult(false, "Your account is locked. Please contact administrator to unlock.");
                            }
                        }

                        return ConstructResult(false, "Invalid username or password.");
                    }

                    SQLResult rolesResult = await _repo.GetUserRoles(input.Username);
                    if (rolesResult != null && string.IsNullOrEmpty(rolesResult.error) && rolesResult.data != null && rolesResult.data.Rows.Count > 0)
                    {
                        var roles = rolesResult.data.AsEnumerable()
                                 .Select(row => $"'{row.Field<string>("usergroup")}'")
                                 .ToList();

                        _roles = rolesResult.data.AsEnumerable()
                                 .Select(row => row.Field<string>("usergroup"))
                                 .ToList();

                        var rolesList = string.Join(",", roles);



                        SQLResult respResult = await _repo.GetUserResponsibilities(rolesList);

                        if (respResult != null && string.IsNullOrEmpty(respResult.error) && respResult.data != null && respResult.data.Rows.Count > 0)
                        {
                            _responsibilities = respResult.data.AsEnumerable()
                                 .Select(row => row.Field<string>("userroles"))
                                 .ToList();

                            _homePages = respResult.data.AsEnumerable()
                                 .Select(row => row.Field<string>("homepage"))
                                 .ToList();


                        }
                    }
                    else
                    {
                        return ConstructResult(false, "Login failed. Userroles expired or not assigned.");
                    }

                    await LoadAppVariables(input);
                    await LoadAxGloVariables(input);
                    await AddDBVariables(input);
                    //TODO  GetConfigParamDetails;                    
                }
                else
                {
                    if(userResult != null && !string.IsNullOrEmpty(userResult.error))
                        return ConstructResult(false, userResult.error);
                    else
                        return ConstructResult(false, "Invalid username or password.");
                }
            }
            catch (Exception ex)
            {
                return ConstructResult(false, "Error in Password check : " + ex.Message);
            }

            return ConstructResult(true, "Login Successful.");

        }

        private async Task<bool> CheckSSOKey(LoginInput input)
        {
            bool validUser = false;
            string ssoKey = "";
            string ssoColName = $"{input.SSO?.ToLower() ?? ""}singleloginkey";
            
            if (!_userData.SSOKeys.ContainsKey(ssoColName))
            {
                var addSSOColumnResult = await _repo.AddSSOColumn(ssoColName);
                if (addSSOColumnResult == null || !string.IsNullOrEmpty(addSSOColumnResult.error))
                {
                    validUser = false;
                    return validUser;
                }
            }

            if (_userData.SSOKeys.ContainsKey(ssoColName) && !string.IsNullOrEmpty(_userData.SSOKeys[ssoColName]))
            {
                if (_userData.SSOKeys.TryGetValue(ssoColName, out ssoKey) && ssoKey == input.SingleSigninKey)
                    validUser = true;
                else
                    validUser = false;
            }
            else
            {               
                var updateSSOKeyResult = await _repo.UpdateSSOKeyForUser(input, ssoColName);
                if (updateSSOKeyResult != null && string.IsNullOrEmpty(updateSSOKeyResult.error))
                    validUser = true;
                else

                    validUser = false;
            }

            return validUser;
        }

        private async Task LoadAppVariables(LoginInput input)
        {
            _axParser.RegisterVar("username", "c", input.Username);
            _gloVars.AddOrUpdateGlobalVarToList("username", "c", input.Username);

            foreach (KeyValuePair<string, string> constant in _axProps.Constants)
            {
                _gloVars.AddOrUpdateGlobalVarToList(constant.Key, "c", constant.Value);
                _axParser.RegisterVar(constant.Key, "c", constant.Value);

            }

            foreach (KeyValuePair<string, List<string>> variable in _axProps.Variables)
            {
                _axParser.RegisterVar(variable.Key, "c", "");
                _axParser.EvalExps(variable.Value);
                var value = _axParser.GetVarValue(variable.Key);
                _gloVars.AddOrUpdateGlobalVarToList(variable.Key, "c", value);
            }
        }

        private async Task LoadAxGloVariables(LoginInput input)
        {
            var axGloTstruct = await _repo.GetAxGloTstruct();

            if (axGloTstruct != null && axGloTstruct.data != null && axGloTstruct.data.Rows.Count > 0)
            {
                List<string> skipColumns = new List<string>
                    {
                        "cancel",
                        "sourceid",
                        "mapname",
                        "username",
                        "modifiedon",
                        "createdby",
                        "createdon",
                        "wkid",
                        "app_level",
                        "app_desc",
                        "app_slevel",
                        "cancelremarks",
                        "wfroles"
                    };

                var axGloFields = await _repo.GetAxGloFields();
                _userData.AxGlo = "yes~axglo";
                Dictionary<string, string> axGloFieldTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                if (axGloFields != null && axGloFields.data != null && axGloFields.data.Rows.Count > 0)
                {
                    axGloFieldTypes = axGloFields.data.AsEnumerable()
                        .Where(row => row.Field<string>("fname") != null &&
                                      row.Field<string>("datatype") != null)
                        .GroupBy(row => row.Field<string>("fname"), StringComparer.OrdinalIgnoreCase)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Last().Field<string>("datatype"),
                            StringComparer.OrdinalIgnoreCase
                        );
                }

                var axGloUserData = await _repo.GetAxGloUserData(input.Username);
                if (axGloUserData != null && axGloUserData.data != null && axGloUserData.data.Rows.Count > 0)
                {
                    DataRow dr = axGloUserData.data.Rows[0];
                    _userData.AxGloRecordId = dr?["axglovarid"]?.ToString() ?? "0";

                    foreach (DataColumn col in axGloUserData.data.Columns)
                    {
                        if (!skipColumns.Contains(col.ColumnName.ToLower()))
                        {
                            var dataType = "c";
                            if (axGloFieldTypes != null && axGloFieldTypes.Count > 0 && axGloFieldTypes.ContainsKey(col.ColumnName.ToLower()))
                            {
                                dataType = axGloFieldTypes[col.ColumnName] ?? "c";
                            }

                            var value = "";
                            if (dr[col.ColumnName.ToLower()] != DBNull.Value)
                            {
                                value = dr[col.ColumnName.ToLower()]?.ToString() ?? "";
                                if (dataType == "d" && !string.IsNullOrEmpty(value))
                                {
                                    var dtVal = (DateTime)dr[col.ColumnName.ToLower()];
                                    value = dtVal.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
                                }
                            }

                            _gloVars.AddOrUpdateGlobalVarToList(col.ColumnName, dataType, value);
                            _axParser.RegisterVar(col.ColumnName, dataType, value);
                        }

                    }
                }
                else
                {
                    foreach (KeyValuePair<string, string> fields in axGloFieldTypes)
                    {
                        var dataType = "c";
                        if (axGloFieldTypes != null && axGloFieldTypes.Count > 0 && axGloFieldTypes.ContainsKey(fields.Key.ToLower()))
                        {
                            dataType = axGloFieldTypes[fields.Key] ?? "c";
                        }

                        _gloVars.AddOrUpdateGlobalVarToList(fields.Key, dataType, "");
                        _axParser.RegisterVar(fields.Key, dataType, "");
                    }
                }
            }
        }

        private async Task AddDBVariables(LoginInput input)
        {
            var dbFunctionsList = await _repo.GetDBVariablesFunctionsList();

            if (dbFunctionsList != null && dbFunctionsList.data != null && dbFunctionsList.data.Rows.Count > 0)
            {
               foreach(DataRow dr in dbFunctionsList.data.Rows)
               {                    
                    var isLoginEvent = dr["event_onlogin"]?.ToString() ?? "";
                    var isFormLoadEvent = dr["event_onformload"]?.ToString() ?? "";
                    var isReportLoadEvent = dr["event_onreportload"]?.ToString() ?? "";
                    
                    if (isLoginEvent == "T")
                    {
                        var dbFunction = dr["db_function"]?.ToString() ?? "";
                        var dbFunctionParams = dr["db_function_params"]?.ToString() ?? "";

                        if (!string.IsNullOrEmpty(dbFunction))
                        {
                            await AxMemLoad(dbFunction, dbFunctionParams);
                        }
                    }
                    else if(isFormLoadEvent == "T")
                    {
                        var formsTransid = dr["forms_transid"]?.ToString() ?? "";
                        if (!string.IsNullOrEmpty(formsTransid))
                        {
                            _gloVars.AddOrUpdateGlobalVarToList("forms_transids", "c", formsTransid);
                            _axParser.RegisterVar("forms_transids", "c", formsTransid);
                        }

                    }
                    else if (isReportLoadEvent == "T")
                    {
                        var reportsTransid = dr["reports_transid"]?.ToString() ?? "";
                        if (!string.IsNullOrEmpty(reportsTransid))
                        {
                            _gloVars.AddOrUpdateGlobalVarToList("reports_names", "c", reportsTransid);
                            _axParser.RegisterVar("reports_names", "c", reportsTransid);
                        }
                    }                
                }
            }
        }

        private async Task AxMemLoad(string dbFunction, string dbFunctionParams) 
        {
            var paramsList = new List<string>();
            if (!string.IsNullOrEmpty(dbFunctionParams))
            {
                foreach (string paramName in dbFunctionParams.Split(","))
                {
                    var value = _axParser.GetVarValue(paramName);
                    paramsList.Add(value);
                }
            }

            string strFunctionParams = "";
            if (dbFunctionParams != null && paramsList.Count > 0)
                strFunctionParams = string.Join(",", paramsList.Select(u => $"'{u}'"));

            var varList = await _repo.GetDBVariablesFromFunction(dbFunction, strFunctionParams);

            if (varList != null && varList.data != null && varList.data.Rows.Count > 0)
            {
                var varListJson = varList.data.Rows[0][0]?.ToString() ?? "";
                JArray varListArray = JArray.Parse(varListJson);
                foreach (JObject item in varListArray)
                {
                    foreach (JProperty property in item.Properties())
                    {
                        string varName = property.Name.Substring(0, property.Name.Length - 1);
                        string value = property.Value?.ToString() ?? "";
                        string dataType = property.Name.Substring(property.Name.Length - 1);
                        _gloVars.AddOrUpdateGlobalVarToList(varName, dataType, value);
                        _axParser.RegisterVar(varName, dataType, value);
                    }
                }
            }
        }


        private async Task LogSigninDetails(LoginInput input, ARMResult response)
        {
            _axpertLog.SessionId = input.SessionId;
            _axpertLog.Username = input.Username;
            _axpertLog.ServiceResult = response.result["message"]?.ToString();
            _axpertLog.RecordId = 0;
            _axpertLog.StructName = "";
            _axpertLog.ServiceName = "ARM - Signin API";
            if (response.result.ContainsKey("ARMSessionId"))
                _axpertLog.CallDetails = response.result["ARMSessionId"].ToString();
            else if (response.result.ContainsKey("OTPLoginKey"))
                _axpertLog.CallDetails = "OTP sent for user.";
            else
                _axpertLog.CallDetails = "";

            await _common.InsertIntoAxpertLog(_axpertLog);

            _axAudit.SessionId = input.SessionId;
            _axAudit.Username = input.Username;
            _axAudit.LoginTime = DateTime.Now;
            _axAudit.IP = input.IPAddress;
            _axAudit.Other = input.Other;

            if (response.result["Success"] != null && Convert.ToBoolean(response.result["Success"]) != true)
            {
                _axAudit.Status = "Failed";
                if (response.result.ContainsKey("duplicate_session") && response.result["duplicate_session"] != null && Convert.ToBoolean(response.result["duplicate_session"]) == true)
                    _axAudit.Reason = "Duplicate session exists.";
                else
                    _axAudit.Reason = response.result["message"]?.ToString() ?? "";
            }
            else
            {
                _axAudit.Status = "Success";
                _axAudit.Reason = _axpertLog.CallDetails;
            }

            _axAudit.Reason = (_axAudit.Reason.Length > 150 ? _axAudit.Reason.Substring(0, 150) : _axAudit.Reason);
            await _common.LogSigninIntoAxAudit(_axAudit);

        }
        private LoginResponse ConstructResult(bool success, string message, Dictionary<string, string> gloVars = null)
        {
            return new LoginResponse
            {
                Success = success,
                Message = message,
                GlobalVars = gloVars
            };
        }

        public async Task<APIResult> SignoutAsync(ARMSession session)
        {
            Dictionary<string, object> sessionVars = await _common.GetSessionData(session.ARMSessionId);
            var _axpertLog = new AxpertLog();
            _axpertLog.CalledOn = DateTime.Now;
            var apiResult = new APIResult();

            if (sessionVars == null || sessionVars.Count == 0)
            {
                
                apiResult.success = false;
                apiResult.data["error"] = $"Session data is not available.";
                return apiResult;
            }
            else
            {
                var userName = sessionVars[SESSION_DATA.USERNAME.ToString()].ToString();
                _common.DeletePreviousLoggedUserInfo(_common.GetSchemaName(), userName).GetAwaiter().GetResult();

                await _common._redisCache.KeyDeleteAsync("AXPERT-" + session.ARMSessionId);
                await _common._redisCache.KeyDeleteAsync(session.ARMSessionId);
                await _common._redisCache.DeleteKeysByPrefixAsync(session.ARMSessionId);
                try
                {
                    AxAudit axAudit = new AxAudit();
                    axAudit.SessionId = sessionVars[SESSION_DATA.AXPERT_SESSIONID.ToString()].ToString();
                    axAudit.LogoutTime = DateTime.Now;

                    _common.LogSignOutIntoAxAudit(axAudit).GetAwaiter().GetResult();

                    _axpertLog.SessionId = axAudit.SessionId;
                    _axpertLog.Username = userName;
                    _axpertLog.ServiceResult = "Logout successful";
                    _axpertLog.RecordId = 0;
                    _axpertLog.StructName = "";
                    _axpertLog.CallDetails = session.ARMSessionId;
                    _axpertLog.ServiceName = "ARM - SignOut API";
                    _common.InsertIntoAxpertLog(_axpertLog).GetAwaiter().GetResult();
                }
                catch { }
                finally
                {
                    _common.CloseDBConnectionAsync().GetAwaiter().GetResult();
                }

                apiResult.success = true;

                return apiResult;
            }
        }


        public async Task<ARMResult> ConnectToAxpert(ARMAxpertConnect axpert) 
        {
            ARMResult result = new ARMResult();
            try
            {
                var redisConnected = await _common.OpenRedisConnectionAsync(axpert.ARMSessionId);
                if (redisConnected)
                {
                    if (_common._redisCache.KeyExists(axpert.ARMSessionId))
                    {
                        Dictionary<string, object> sessionVars = await _common.GetSessionData(axpert.ARMSessionId);

                        axpert.AppName = sessionVars[SESSION_DATA.APPNAME.ToString()].ToString();

                        string secret = Guid.NewGuid().ToString();
                        //var privateKey = _config["ARM_PrivateKey"];
                        var privateKey = _common._appSettingsHelper.GetARMPrivateKey(axpert.AppName);
                        string hashedKey = _common.MD5Hash(privateKey + secret);
                        var axpertConnectObj = new
                        {
                            privatekey = hashedKey,//Encryptedkey
                            secret = secret,
                            hguid = secret,
                            hdeviceid = secret,
                            project = axpert.AppName,
                            username = sessionVars[SESSION_DATA.USERNAME.ToString()].ToString(),
                            roles = sessionVars[SESSION_DATA.USER_ROLES.ToString()].ToString(),
                            lang = "english"
                        };

                        var expiryMins = _common._appSettingsHelper.GetARMTokenExpiryMinutes(_common._appName);
                        await _common._redisCache.StringSetAsync($"AXPERT-{axpert.ARMSessionId}", JsonConvert.SerializeObject(axpertConnectObj), expiryMins * 60);

                        result.result["success"] =  true;
                        result.result.Add("message", "AXPERTCONNECTIONESTABLISHED");

                    }
                    else
                    {
                        result.result["success"] = false;
                        result.result.Add("message", "INVALIDSESSION");
                    }
                }
                else
                {
                    result.result["success"] = false;
                    result.result.Add("message", "INVALIDSESSION");

                }
            }
            catch (Exception ex)
            {
                result.result["success"] = false;
                result.result.Add("message", "INVALIDSESSION");
            }
            finally
            {
                await _common.CloseRedisConnectionAsync();
            }

            return result;
        }

        public async Task<ARMResult> ConnectFromAxpert(ARMAxpertConnect axpert)
        {
            ARMResult result = new ARMResult();
            try
            {
                var validationResult = await ValidateAxpertConnect(axpert);
                if (validationResult.ToString() != Constants.RESULTS.ERROR.ToString())
                {
                    var generatedToken = _tokenService.BuildToken(validationResult);
;
                    result.result.Add("message", "CONNECTIONESTABLISHED");
                    result.result.Add("connectionid", validationResult);
                    result.result.Add("token", generatedToken);
                    result.result.Add("expirymins", _common._appSettingsHelper.GetARMTokenExpiryMinutes(axpert.AppName));
                    result.result["success"] = true;
                }
                else
                {
                    result.result["success"] = false;
                    result.result["message"] = "ARMAUTHFAILED";
                }
            }            
            catch (Exception ex)
            {
                result.result["success"] = false;
                result.result["message"] = "ARMAUTHFAILED";
            }
            finally
            {
                await _common.CloseRedisConnectionAsync();
            }

            return result;
        }

        public async Task<string> ValidateAxpertConnect(ARMAxpertConnect axpert)
        {
            var privateKey = _common._appSettingsHelper.GetARMPrivateKey(axpert.AppName);
            if (string.IsNullOrEmpty(privateKey))
                throw new Exception($"ARM private key is missing for project {axpert.AppName}");

            string hashedKey = _common.MD5Hash(privateKey + axpert.AxSessionId);
            if (hashedKey == axpert.Key)
            {
                string sessionId = $"ARM-{axpert.AppName}-{Guid.NewGuid().ToString()}";
                _common._appName = axpert.AppName;
                
                await _common.DeletePreviousARMLoggedUserInfo(_common.GetSchemaName(), axpert.User);
                await _common.SetARMLoggedUserInfo(_common.GetSchemaName(), axpert.User, sessionId);

                return sessionId;

            }
            return RESULTS.ERROR.ToString();
        }

        public async Task<APIResult> AxValidateSession(ARMAxpertConnect axpert)
        {

            APIResult result = new APIResult();
            try
            {
                var isValidSession = await _common.ValidateSession(axpert.ARMSessionId);
                if (!isValidSession)
                {
                    result.success = false;
                    result.data["error"] = $"SessionId is not valid";                    
                }
                else
                {
                    result.success = true;
                    result.data["message"] = $"SessionId is valid";
                }
            }
            catch (Exception ex) 
            {
                result.success = false;
                result.data["error"] = ex.Message;
            }
            finally
            {
                await _common.CloseRedisConnectionAsync();
            }

            return result;
        }
    }

}
