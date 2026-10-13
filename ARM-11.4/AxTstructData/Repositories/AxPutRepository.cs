using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMCommon.Model;
using Newtonsoft.Json;
using System.Data;

namespace AxTstructData
{
    public class AxPutRepository : IAxPutRepository
    {
        private readonly Utils _common;
        private readonly IARMLogTraceService _traceService;

        public AxPutRepository(Utils common, IARMLogTraceService traceService)
        {
            _common = common;
            _traceService = traceService;
        }

        public async Task<SQLResult> GetEditPermissionFromDB(AxPut putData)
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

        public async Task<SQLResult> GetCreatePermissionFromDB(AxPut putData)
        {

            if (_common._dbHelper == null)
                await _common.OpenDBConnectionAsync();

            string dbType = _common._dbHelper.GetDBType().ToUpper().Replace(" ", "");

            if (putData.Roles?.Count == 0)
                putData.Roles.Add("All");

            string sql = "";
            if (dbType.ToUpper().Replace(" ", "") == Constants.DBTYPE.MSSQL.ToString())
            {
                sql = Constants_SQL.GET_AXPUT_CREATEPERMISSION_MSSQL;
                string[] paramName = { "@pmode", "@ptransid", "@pusername", "@proles", "@pglobalvars" };
                DbType[] paramType = { DbType.String, DbType.String, DbType.String, DbType.String, DbType.String };
                object[] paramValue = { putData.Action, putData.TransId, putData.UserName.ToString(), string.Join(",", putData.Roles), putData.Dimensions };


                var dsResult = await _common._dbHelper.ExecuteStoredProcedureAsync(sql, paramName, paramType, paramValue);
                return dsResult;
            }
            else
            {
                sql = Constants_SQL.GET_AXPUT_CREATEPERMISSION.ToString();
                if (dbType.ToUpper() == Constants.DBTYPE.ORACLE.ToString())
                    sql = Constants_SQL.GET_AXPUT_CREATEPERMISSION_ORACLE.ToString();
                string[] paramName = { "@action", "@transid", "@username", "@roles", "@dimensions" };
                DbType[] paramType = { DbType.String, DbType.String, DbType.String, DbType.String, DbType.String };
                object[] paramValue = { putData.Action, putData.TransId, putData.UserName.ToString(), string.Join(",", putData.Roles), putData.Dimensions };

                var dsResult = await _common._dbHelper.ExecuteSQLAsync(sql, paramName, paramType, paramValue);
                return dsResult;
            }


        }

        public async Task<SQLResult> ExecuteDCWiseSQLs(Int64 recordId, string sql)
        {
            string[] paramName = { "@recordid" };
            DbType[] paramType = { DbType.Int64 };
            object[] paramValue = { recordId };
            return await _common._dbHelper.ExecuteSQLAsync(sql, paramName, paramType, paramValue);
        }

        public async Task<SQLResult> GetChildTransRecordId(string parentRecId, string table, string mapName)
        {
            if (_common._dbHelper == null)
                await _common.OpenDBConnectionAsync();

            string sql = $"SELECT {table}id FROM {table} WHERE sourceid = {parentRecId} and mapname = '{mapName}'";

            var result = await _common._dbHelper.ExecuteSQLAsync(sql);
            return result;
        }

        private string GenerateDCWiseSQLsCacheKey(AxPut putData)
        {
            if (putData.Roles?.Count == 0)
                putData.Roles.Add("All");

            var cacheKey = _common.GenerateRedisKeyString(new string[] {
                _common._schemaName, "ARM",
                putData.TransId,
                "AxPut-DcSQLs",
                string.Join(",", putData.Roles?.OrderBy(r => r)) ?? "All"
            });

            return cacheKey;
        }

        private async Task<SQLResult> GetDCWiseSQLsFromCache(AxPut putData)
        {
            var redisConnected = await _common.OpenRedisConnectionAsync();
            if (redisConnected)
            {
                var cacheKey = GenerateDCWiseSQLsCacheKey(putData);
                var cachedResult = await _common._redisCache.StringGetAsync(cacheKey);

                if (!string.IsNullOrEmpty(cachedResult))
                {
                    if (_traceService.IsTracingEnabled())
                        _traceService.LogMessage("Cache - DCWiseSQLs Loaded from cache", cachedResult);

                    return JsonConvert.DeserializeObject<SQLResult>(cachedResult);
                }
            }

            return null;
        }

        private async Task<bool> StoreDCWiseSQLsToCache(AxPut putData, SQLResult result)
        {
            var redisConnected = await _common.OpenRedisConnectionAsync();
            if (redisConnected)
            {
                var cacheKey = GenerateDCWiseSQLsCacheKey(putData);
                var serializedResult = JsonConvert.SerializeObject(result);
                return await _common._redisCache.StringSetAsync(cacheKey, serializedResult);
            }

            return false;
        }        

        //public async Task<SQLResult> GetRecordIdsFromDB(Int32 siteNo, Int32 rowsCount)
        //{
        //    if (_common._dbHelper == null)
        //        await _common.OpenDBConnectionAsync();

        //    string dbType = _common._dbHelper.GetDBType().ToUpper().Replace(" ", "");

        //    string sql = Constants_SQL.AXPUT_GETRECORDIDS.ToString();
        //    if (dbType.ToUpper() == Constants.DBTYPE.ORACLE.ToString())
        //        sql = Constants_SQL.AXPUT_GETRECORDIDS_ORACLE.ToString();

        //    if (siteNo < 0)
        //        siteNo = 1;

        //    string[] paramName = { "@siteno", "@rowscount" };
        //    DbType[] paramType = { DbType.Int32, DbType.Int32 };
        //    object[] paramValue = { siteNo , rowsCount };

        //    return await _common._dbHelper.ExecuteSQLAsync(sql, paramName, paramType, paramValue);
        //}

        public async Task<TransactionResult> ExecuteSaveDataSQLCommands(AxPut putData, List<SQLCommandBuilder> sqlCommands)
        {
            if (_common._dbHelper == null)
                await _common.OpenDBConnectionAsync();

            return await _common._dbHelper.ExecuteSQLsInTransactionAsync(sqlCommands, false);
        }

    }
}
