using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMCommon.Model;
using Newtonsoft.Json;
using System.Data;

namespace AxTstructData
{
    public class AxDeleteRepository : IAxDeleteRepository
    {
        private readonly Utils _common;
        private readonly IARMLogTraceService _traceService;

        public AxDeleteRepository(Utils common, IARMLogTraceService traceService)
        {
            _common = common;
            _traceService = traceService;
        }

        public async Task<SQLResult> GetDeletePermissionFromDB(AxDelete putData)
        {

            if (_common._dbHelper == null)
                await _common.OpenDBConnectionAsync();
            string dbType = _common._dbHelper.GetDBType().ToUpper().Replace(" ", "");

            if (putData.Roles?.Count == 0)
                putData.Roles.Add("All");

            string sql = "";
            if (dbType.ToUpper().Replace(" ", "") == Constants.DBTYPE.MSSQL.ToString())
            {
                sql = Constants_SQL.GET_AXPUT_EDITPERMISSION_MSSQL;
                string[] paramName = { "@pmode", "@ptransid", "@pkeyfield", "@pkeyvalue", "@pusername", "@proles", "@pglobalvars" };
                DbType[] paramType = { DbType.String, DbType.String, DbType.String, DbType.String, DbType.String, DbType.String, DbType.String };
                object[] paramValue = { putData.Action, putData.TransId, putData.KeyField, $"'{putData.KeyValue}'", putData.UserName, string.Join(",", putData.Roles), putData.Dimensions };

                var dsResult = await _common._dbHelper.ExecuteStoredProcedureAsync(sql, paramName, paramType, paramValue);
                return dsResult;
            }
            else
            {
                sql = Constants_SQL.GET_AXPUT_EDITPERMISSION.ToString();
                if (dbType.ToUpper() == Constants.DBTYPE.ORACLE.ToString())
                    sql = Constants_SQL.GET_AXPUT_EDITPERMISSION_ORACLE.ToString();

                string[] paramName = { "@action", "@transid", "@keyfield", "@keyvalue", "@username", "@roles", "@dimensions" };
                DbType[] paramType = { DbType.String, DbType.String, DbType.String, DbType.String, DbType.String, DbType.String, DbType.String };
                object[] paramValue = { putData.Action, putData.TransId, putData.KeyField, $"'{putData.KeyValue}'", putData.UserName, string.Join(",", putData.Roles), putData.Dimensions };

                var dsResult = await _common._dbHelper.ExecuteSQLAsync(sql, paramName, paramType, paramValue);
                return dsResult;
            }
        }

        public async Task<TransactionResult> ExecuteDeleteSQLCommands(AxDelete putData, List<SQLCommandBuilder> sqlCommands)
        {
            if (_common._dbHelper == null)
                await _common.OpenDBConnectionAsync();

            return await _common._dbHelper.ExecuteSQLsInTransactionAsync(sqlCommands, false);
        }

    }
}
