
using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMCommon.Model;
using System.Data;

namespace AxAuth
{
    public class LoginRepository : ILoginRepository
    {
        private readonly Utils _common;
        private readonly IARMLogTraceService _traceService;

        public LoginRepository(Utils common, IARMLogTraceService traceService)
        {
            //_redis = redis;
            _common = common;
            _traceService = traceService;
        }



        public async Task<string> GetAxPropsXMLFromDB()
        {
            string axPropsXML = "";
            SQLResult axPropsResult = await FetchAxPropsXMLFromDB();
            if (axPropsResult != null && string.IsNullOrEmpty(axPropsResult.error) && axPropsResult.data != null && axPropsResult.data.Rows.Count > 0)
            {
                foreach (DataRow dr in axPropsResult.data.Rows)
                {
                    axPropsXML += dr[0].ToString();
                }
            }
            return axPropsXML;

        }

        private async Task<SQLResult> FetchAxPropsXMLFromDB()
        {
            if (_common._dbHelper == null)
                await _common.OpenDBConnectionAsync();

            return await _common._dbHelper.ExecuteSQLAsync(Constants_SQL.GET_AXPROPSXML);
        }

        public async Task<LoginUserData> GetUserAuthDetails(string userName)
        {
            var keyParams = new string[] { _common.GetSchemaName(), userName, Constants.REDISPWDOTPAUTHLANG };
            var cacheKey = _common.GenerateRedisKeyString(keyParams);
            var redisConnected = await _common.OpenRedisConnectionAsync();
            if (redisConnected)
            {
                var userAuthData = await _common._redisCache.StringGetBinaryAsync(cacheKey);
                if (!string.IsNullOrEmpty(userAuthData))
                {
                    var userAuthArray = userAuthData.Split('♣');
                    var loginUserData = new LoginUserData();
                    loginUserData.AxLang = userAuthArray[0];
                    loginUserData.PwdAuth = userAuthArray[1];
                    loginUserData.OtpAuth = userAuthArray[2];
                    loginUserData.Active = userAuthArray[3];
                    loginUserData.Email = userAuthArray[4];
                    loginUserData.Mobile = userAuthArray[5];
                    loginUserData.StaySignedIn = userAuthArray[6];
                    loginUserData.SigninExpiry = userAuthArray[7];

                    return loginUserData;
                }
                else
                {
                    var userResult = await GetUserAuthDetailsFromDB(userName);
                    if (userResult != null && string.IsNullOrEmpty(userResult.error) && userResult.data != null && userResult.data.Rows.Count > 0)
                    {
                        DataRow dr = userResult.data.Rows[0];
                        string data = "";
                        var loginUserData = new LoginUserData();
                        loginUserData.AxLang = dr["axlang"]?.ToString() ?? "";
                        loginUserData.PwdAuth = dr["pwdauth"]?.ToString() ?? "T";
                        loginUserData.OtpAuth = dr["otpauth"]?.ToString() ?? "F";
                        loginUserData.Active = dr["active"]?.ToString() ?? "T";
                        loginUserData.Email = dr["email"]?.ToString() ?? "";
                        loginUserData.Mobile = dr["mobile"]?.ToString() ?? "";
                        loginUserData.StaySignedIn = dr["staysignedin"]?.ToString() ?? "";
                        loginUserData.SigninExpiry = dr["signinexpiry"]?.ToString() ?? "";

                        data = $"{loginUserData.AxLang}♣{loginUserData.PwdAuth}♣{loginUserData.OtpAuth}♣{loginUserData.Active}♣{loginUserData.Email}♣{loginUserData.Mobile}♣{loginUserData.StaySignedIn}♣{loginUserData.SigninExpiry}";

                        await _common._redisCache.StringSetBinaryAsync(cacheKey, data);

                        return loginUserData;
                    }
                }
            }
            return null;

        }

        public async Task<SQLResult> GetUserAuthDetailsFromDB(string userName)
        {
            if (_common._dbHelper == null)
                await _common.OpenDBConnectionAsync();

            string[] paramName = { "@username" };
            DbType[] paramType = { DbType.String };
            object[] paramValue = { userName };

            return await _common._dbHelper.ExecuteSQLAsync(Constants_SQL.GET_USER_AUTHDETAILS, paramName, paramType, paramValue);
        }

        public async Task<SQLResult> GetUserDetails(string userName)
        {
            if (_common._dbHelper == null)
                await _common.OpenDBConnectionAsync();

            string[] paramName = { "@username" };
            DbType[] paramType = { DbType.String };
            object[] paramValue = { userName };

            return await _common._dbHelper.ExecuteSQLAsync(Constants_SQL.GET_USER_DETAILS, paramName, paramType, paramValue);
        }

        public async Task<SQLResult> GetUserRoles(string userName)
        {
            if (_common._dbHelper == null)
                await _common.OpenDBConnectionAsync();

            DateTime today = DateTime.Today;
            //string currentDateStr = today.ToString("dd/MM/yyyy 00:00:00");
            string currentDateStr = _common._dbVars["date"].ToString();
            string sql = Constants_SQL.GET_USER_ROLES;

            string dateString = "@currentdate";
            if (_common._dbVars.ContainsKey("dbdatestring"))
            {
                dateString = _common._dbVars["dbdatestring"].ToString().Replace("$DATE$", "@currentdate");
            }

            sql = sql.Replace("$CURRENTDATE$", dateString);

            string[] paramName = { "@username", "@currentdate", "@currentdate" };
            DbType[] paramType = { DbType.String, DbType.String, DbType.String };
            object[] paramValue = { userName, currentDateStr, currentDateStr };

            return await _common._dbHelper.ExecuteSQLAsync(sql, paramName, paramType, paramValue);
        }

        public async Task<SQLResult> GetUserResponsibilities(string roles)
        {
            if (_common._dbHelper == null)
                await _common.OpenDBConnectionAsync();

            var sql = Constants_SQL.GET_USER_RESPONSIBILITIES;
            sql = sql.Replace("$ROLES$", roles);

            return await _common._dbHelper.ExecuteSQLAsync(sql);
        }

        public async Task<ScalarResult> UpdateLoginTry(string userName, int loginTry)
        {
            if (_common._dbHelper == null)
                await _common.OpenDBConnectionAsync();

            string[] paramName = { "@username", "@logintry" };
            DbType[] paramType = { DbType.String, DbType.Int32 };
            object[] paramValue = { userName, loginTry };

            return await _common._dbHelper.ExecuteScalarAsync(Constants_SQL.SET_USER_LOGINTRY);
        }

        public async Task<SQLResult> GetUserPwdPolicy(string userName)
        {
            if (_common._dbHelper == null)
                await _common.OpenDBConnectionAsync();

            string[] paramName = { "@username" };
            DbType[] paramType = { DbType.String };
            object[] paramValue = { userName };

            return await _common._dbHelper.ExecuteSQLAsync(Constants_SQL.GET_USER_PWDPOLICY, paramName, paramType, paramValue);
        }


        public async Task<SQLResult> GetAxGloTstruct()
        {
            if (_common._dbHelper == null)
                await _common.OpenDBConnectionAsync();

            return await _common._dbHelper.ExecuteSQLAsync(Constants_SQL.GET_AXGLO_TSTRUCT);
        }

        public async Task<SQLResult> GetAxGloFields()
        {
            if (_common._dbHelper == null)
                await _common.OpenDBConnectionAsync();

            return await _common._dbHelper.ExecuteSQLAsync(Constants_SQL.GET_AXGLO_FIELDS);
        }
        public async Task<SQLResult> GetAxGloUserData(string userName)
        {
            if (_common._dbHelper == null)
                await _common.OpenDBConnectionAsync();

            string[] paramName = { "@username" };
            DbType[] paramType = { DbType.String };
            object[] paramValue = { userName };

            return await _common._dbHelper.ExecuteSQLAsync(Constants_SQL.GET_USER_AXGLODATA, paramName, paramType, paramValue);
        }

        public async Task<NonQueryResult> AddSSOColumn(string columnName)
        {
            if (_common._dbHelper == null)
                await _common.OpenDBConnectionAsync();

            string sql = Constants_SQL.ADD_SSO_COLUMN;
            if (_common._dbHelper.GetDBType() == Constants.DBTYPE.ORACLE.ToString())
                sql = Constants_SQL.ADD_SSO_COLUMN_ORACLE;

            sql = sql.Replace("$COLUMN$", columnName);

            return await _common._dbHelper.ExecuteNonQueryAsync(sql);
        }
        public async Task<NonQueryResult> UpdateSSOKeyForUser(LoginInput input, string columnName)
        {
            if (_common._dbHelper == null)
                await _common.OpenDBConnectionAsync();

            string sql = Constants_SQL.SET_USER_SSOKEY;
            sql = sql.Replace("$COLUMN$", columnName);

            string[] paramName = { "@ssokey", "@username" };
            DbType[] paramType = { DbType.String, DbType.String };
            object[] paramValue = { input.SingleSigninKey, input.Username };

            return await _common._dbHelper.ExecuteNonQueryAsync(sql, paramName, paramType, paramValue);
        }

        public async Task<bool> StoreUserSessionInCache(string sessionId, string data, int expiry = 60)
        {
            var redisConnected = await _common.OpenRedisConnectionAsync(sessionId);
            if (redisConnected)
            {
                return await _common._redisCache.StringSetAsync(sessionId + "-UserData", data, expiry * 60);
            }
            else
                return false;

        }
        public async Task<string> GetUserSessionFromCache(string sessionId)
        {
            var redisConnected = await _common.OpenRedisConnectionAsync(sessionId);
            if (redisConnected)
            {
                return await _common._redisCache.StringGetAsync(sessionId + "-UserData");
            }
            else
                return string.Empty;
        }
        public async Task<bool> DeleteUserSessionFromCache(string sessionId)
        {
            var redisConnected = await _common.OpenRedisConnectionAsync(sessionId);
            if (redisConnected)
            {
                return await _common._redisCache.KeyDeleteAsync(sessionId + "-UserData");
            }
            else
                return false;
        }

        public async Task<SQLResult> GetDBVariablesFunctionsList()
        {
            if (_common._dbHelper == null)
                await _common.OpenDBConnectionAsync();

            return await _common._dbHelper.ExecuteSQLAsync(Constants_SQL.GET_DB_VARIABLES);
        }

        public async Task<SQLResult> GetDBVariablesFromFunction(string dbFunction, string dbFunctionParams = "")
        {
            if (_common._dbHelper == null)
                await _common.OpenDBConnectionAsync();

            var dbType = _common._dbHelper?.GetDBType();
            string sqlQuery = "";

     

            if (dbType.ToUpper() == Constants.DBTYPE.ORACLE.ToString())
            {
                sqlQuery = $"select {dbFunction}({dbFunctionParams}) as memvars from dual";
            }
            else if (dbType.ToUpper() == Constants.DBTYPE.MSSQL.ToString())
            {
                sqlQuery = $"select $SCHEMA$.$SCHEMA$.{dbFunction}({dbFunctionParams}) as memvars from dual";
            }
            else
            {
                sqlQuery = $"select {dbFunction}({dbFunctionParams}) as memvars";
            }

            return await _common._dbHelper.ExecuteSQLAsync(sqlQuery);
        }

    }

}
