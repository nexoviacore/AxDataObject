using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMCommon.Model;
using System.Data;

namespace AxAuth
{
    public class PasswordRepository : IPasswordRepository
    {
        private readonly Utils _common;
        private readonly IARMLogTraceService _traceService;

        public PasswordRepository(Utils common, IARMLogTraceService traceService)
        {
            _common = common;
            _traceService = traceService;
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

        public async Task<SQLResult> GetUserForPasswordReset(string userName, string email)
        {
            if (_common._dbHelper == null)
                await _common.OpenDBConnectionAsync();

            string[] paramName = { "@username", "@email" };
            DbType[] paramType = { DbType.String, DbType.String };
            object[] paramValue = { userName?.ToLower(), email?.ToLower() };

            return await _common._dbHelper.ExecuteSQLAsync(Constants_SQL.GET_USER_FORPASSWORDRESET, paramName, paramType, paramValue);
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

        public async Task<TransactionResult> UpdatePassword(string userName, string newPasswordHash, string oldPasswordHash, bool isFirstTime, bool pwdPolicyExists, string pwdChangeFlag)
        {
            if (_common._dbHelper == null)
                await _common.OpenDBConnectionAsync();

            var sqlCommands = new List<SQLCommandBuilder>();

            sqlCommands.Add(new SQLCommandBuilder(
                Constants_SQL.UPDATE_USER_PASSWORD,
                new string[] { "@newpassword", "@oldpassword", "@isfirsttime", "@username" },
                new DbType[] { DbType.String, DbType.String, DbType.String, DbType.String },
                new object[] { newPasswordHash, oldPasswordHash ?? "", isFirstTime ? "T" : "F", userName },
                Constants.DATA_ACTION.UPDATE));

            string pwdPolicySql = pwdPolicyExists ? Constants_SQL.UPDATE_USER_PWDPOLICY : Constants_SQL.INSERT_USER_PWDPOLICY;

            pwdPolicySql = pwdPolicySql.Replace("@pwdchangedate", _common._dbVars["dbdatestring"].Replace("$DATE$", "@pwdchangedate"));

            sqlCommands.Add(new SQLCommandBuilder(
                pwdPolicySql,
                new string[] { "@pwdchangedate", "@pwdchange", "@username" },
                new DbType[] { DbType.String, DbType.String, DbType.String },
                new object[] { DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"), pwdChangeFlag, userName },
                pwdPolicyExists ? Constants.DATA_ACTION.UPDATE : Constants.DATA_ACTION.INSERT));

            return await _common._dbHelper.ExecuteSQLsInTransactionAsync(sqlCommands);
        }
    }
}
