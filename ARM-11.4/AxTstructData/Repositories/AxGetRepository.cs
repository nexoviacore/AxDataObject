using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMCommon.Model;
using Newtonsoft.Json;
using System.Data;
using System.Text.RegularExpressions;

namespace AxTstructData
{
    public class AxGetRepository : IAxGetRepository
    {
        private readonly Utils _common;
        private readonly IARMLogTraceService _traceService;

        public AxGetRepository(Utils common, IARMLogTraceService traceService)
        {
            _common = common;
            _traceService = traceService;
        }

        public async Task<SQLResult> GetTstructViewPermissionFromDB(AxGet getData)
        {
            if (_common._dbHelper == null)
                await _common.OpenDBConnectionAsync();

            string dbType = _common._dbHelper.GetDBType().ToUpper().Replace(" ", "");

            if (getData.Roles?.Count == 0)
                getData.Roles.Add("All");

            string sql = "";
            if (dbType.ToUpper().Replace(" ", "") == Constants.DBTYPE.MSSQL.ToString())
            {
                sql = Constants_SQL.GET_AXGET_VIEWPERMISSION_MSSQL;
                string[] paramName = { "@pmode", "@ptransid", "@pkeyfield", "@pkeyvalue", "@pusername", "@proles", "@pglobalvars" };
                DbType[] paramType = { DbType.String, DbType.String, DbType.String, DbType.String, DbType.String, DbType.String, DbType.String };
                object[] paramValue = { getData.Action, getData.TransId, getData.KeyField, $"'{getData.KeyValue}'", getData.UserName, string.Join(",", getData.Roles) , getData.Dimensions};

                return await _common._dbHelper.ExecuteStoredProcedureAsync(sql, paramName, paramType, paramValue); ;
            }
            else
            {
                sql = Constants_SQL.GET_AXGET_VIEWPERMISSION.ToString();
                if (dbType.ToUpper() == Constants.DBTYPE.ORACLE.ToString())
                    sql = Constants_SQL.GET_AXGET_VIEWPERMISSION_ORACLE.ToString();

                string[] paramName = { "@action", "@transid", "@keyfield", "@keyvalue", "@username", "@roles", "@dimensions" };
                DbType[] paramType = { DbType.String, DbType.String, DbType.String, DbType.String, DbType.String, DbType.String, DbType.String };
                object[] paramValue = { getData.Action, getData.TransId, getData.KeyField, $"'{getData.KeyValue}'", getData.UserName, string.Join(",", getData.Roles), getData.Dimensions };

                return await _common._dbHelper.ExecuteSQLAsync(sql, paramName, paramType, paramValue);
            }
        }

        public async Task<SQLResult> GetDCWiseSQLs(AxGet getData)
        {
            SQLResult result = new SQLResult();

            if (getData.CachePermissions == true)
            {
                result = await GetDCWiseSQLsFromCache(getData);
            }

            if (result == null || result.data == null || result.data.Rows.Count == 0)
            {
                result = await GetDCWiseSQLsFromDB(getData);

                if (result != null && result.data != null && result.data.Rows.Count > 0 && getData.CachePermissions == true)
                {
                    await StoreDCWiseSQLsToCache(getData, result);
                }
            }

            return result;
        }

        private async Task<SQLResult> GetDCWiseSQLsFromDB(AxGet getData)
        {
            if (_common._dbHelper == null)
                await _common.OpenDBConnectionAsync();

            string dbType = _common._dbHelper.GetDBType().ToUpper().Replace(" ", "");
            if (getData.Roles?.Count == 0)
                getData.Roles.Add("All");

            string sql = Constants_SQL.AXGET_DBGET.ToString();
            if (dbType.ToUpper() == Constants.DBTYPE.ORACLE.ToString())
                sql = Constants_SQL.AXGET_DBGET_ORACLE.ToString();
            else if (dbType.ToUpper().Replace(" ", "") == Constants.DBTYPE.MSSQL.ToString())
                sql = Constants_SQL.AXGET_DBGET_MSSQL;

            string[] paramName = { "@transid", "@recordid" };
            DbType[] paramType = { DbType.String, DbType.Int64 };
            object[] paramValue = { getData.TransId, 0 };

            return await _common._dbHelper.ExecuteSQLAsync(sql, paramName, paramType, paramValue);
        }

        public async Task<SQLResult> ExecuteDCWiseSQLs(Int64 recordId, string sql)
        {
            if( _common._dbHelper.GetDBType() == Constants.DBTYPE.MSSQL.ToString())
            {
                sql = Regex.Replace(sql, @"(?<!\w):recordid(?!\w)", "@recordid");
            }
            
            string[] paramName = { "@recordid" };
            DbType[] paramType = { DbType.Int64 };
            object[] paramValue = { recordId };
            return await _common._dbHelper.ExecuteSQLAsync(sql, paramName, paramType, paramValue);
        }

        private string GenerateDCWiseSQLsCacheKey(AxGet getData)
        {
            if (getData.Roles?.Count == 0)
                getData.Roles.Add("All");

            var cacheKey = _common.GenerateRedisKeyString(new string[] {
                _common._schemaName, "ARM",
                getData.TransId,
                "AxGet-DcSQLs",
                string.Join(",", getData.Roles?.OrderBy(r => r)) ?? "All"
            });

            return cacheKey;
        }

        private async Task<SQLResult> GetDCWiseSQLsFromCache(AxGet getData)
        {
            var redisConnected = await _common.OpenRedisConnectionAsync();
            if (redisConnected)
            {
                var cacheKey = GenerateDCWiseSQLsCacheKey(getData);
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

        private async Task<bool> StoreDCWiseSQLsToCache(AxGet getData, SQLResult result)
        {
            var redisConnected = await _common.OpenRedisConnectionAsync();
            if (redisConnected)
            {
                var cacheKey = GenerateDCWiseSQLsCacheKey(getData);
                var serializedResult = JsonConvert.SerializeObject(result);
                return await _common._redisCache.StringSetAsync(cacheKey, serializedResult);
            }

            return false;
        }

    }
}
