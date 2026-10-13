using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMCommon.Model;
using Newtonsoft.Json;
using System.Data;

namespace AxList.Repositories
{
    public class AxListRepository : IAxListRepository
    {
        private readonly Utils _common;
        private readonly IARMLogTraceService _traceService;

        public AxListRepository(Utils common, IARMLogTraceService traceService)
        {
            //_redis = redis;
            _common = common;
            _traceService = traceService;
        }

        public async Task<APIResult> GetListViewPermissionFromCache(AxList listData)
        {
            var cacheKey = _common.GenerateRedisKeyString(new string[] { listData.SchemaName ?? listData.Project, listData.TransId, "ARM-AX-VIEW-PERMISSION", listData.UserName, string.Join(",", listData.Roles?.OrderBy(r => r)), listData.Dimensions });

            var redisConnected = await _common.OpenRedisConnectionAsync();
            if (redisConnected)
            {
                var cachedResult = await _common._redisCache.StringGetAsync(cacheKey);
                if (!string.IsNullOrEmpty(cachedResult))
                {
                    if (_traceService.IsTracingEnabled()) _traceService.LogMessage("Cache - Access Loaded from cache", cachedResult);
                    return JsonConvert.DeserializeObject<APIResult>(cachedResult);
                }
            }

            return null;
        }

        public async Task<bool> StoreListViewPermissionToCache(AxList listData, APIResult data)
        {
            var cacheKey = _common.GenerateRedisKeyString(new string[] { listData.SchemaName ?? listData.Project, listData.TransId, "ARM-AX-VIEW-PERMISSION", listData.UserName, string.Join(",", listData.Roles?.OrderBy(r => r)), listData.Dimensions });

            var redisConnected = await _common.OpenRedisConnectionAsync();
            if (redisConnected) 
                return await _common._redisCache.StringSetAsync(cacheKey, JsonConvert.SerializeObject(data));
            else
                return false;
        }

        public async Task<SQLResult> GetListViewPermissionFromDB(AxList listData)
        {
            if (_common._dbHelper == null)
                await _common.OpenDBConnectionAsync();

            string dbType = _common._dbHelper.GetDBType().ToUpper().Replace(" ", "");

            string sql = "";
            if (dbType.Replace(" ", "") == Constants.DBTYPE.MSSQL.ToString())
            {
                sql = Constants_SQL.GET_AXLIST_VIEWPERMISSION_MSSQL;
                string[] paramName = { "@pmode", "@ptransid", "@pusername", "@proles", "@pglobalvars" };
                DbType[] paramType = { DbType.String, DbType.String, DbType.String, DbType.String, DbType.String };
                object[] paramValue = { listData.Action, listData.TransId, listData.UserName, string.Join(",", listData.Roles), listData.Dimensions };

                return await _common._dbHelper.ExecuteStoredProcedureAsync(sql, paramName, paramType, paramValue);
            }
            else
            {

                sql = Constants_SQL.GET_AXLIST_VIEWPERMISSION.ToString(); //POSTGRES
                if (dbType == Constants.DBTYPE.ORACLE.ToString())
                    sql = Constants_SQL.GET_AXLIST_VIEWPERMISSION_ORACLE.ToString();

                string[] paramName = { "@action", "@transid", "@username", "@roles", "@dimensions" };
                DbType[] paramType = { DbType.String, DbType.String, DbType.String, DbType.String, DbType.String };
                object[] paramValue = { listData.Action, listData.TransId, listData.UserName, string.Join(",", listData.Roles), listData.Dimensions };
                return await _common._dbHelper.ExecuteSQLAsync(sql, paramName, paramType, paramValue);
            }
        }

        public async Task<SQLResult> GetListDataFromDB(AxList listData)
        {
            if (_common._dbHelper == null)
                await _common.OpenDBConnectionAsync();

            var result = new SQLResult();           
            var listSQL = await GetListDataSQL(listData);
            if(string.IsNullOrEmpty(listSQL))
            {
                result.success = false;
                result.error = "Error in Get ListData SQL";
                return result;
            }

            var viewFilter = await _common.GetViewFilterForTstruct(listData.ARMSessionId, listData.TransId);
            if(viewFilter != null && viewFilter.Count > 0 && viewFilter.ContainsKey(listData.TransId) && !string.IsNullOrEmpty(viewFilter[listData.TransId]))
                listSQL = listSQL.Replace("--axp_filter", $" {viewFilter[listData.TransId]} ");
            else
                listSQL = listSQL.Replace("--axp_filter", $" ");

            return await _common._dbHelper.ExecuteSQLAsync(listSQL);            
        }

        private async Task<string> GetListDataSQL(AxList listData)
        {
            string sql = "";
            if (listData.CachePermissions)
                sql = await GetListDataSQLFromCache(listData);

            if (string.IsNullOrEmpty(sql))
            {
                var sqlResult = await GetListDataSQLFromDB(listData);
                if (!string.IsNullOrEmpty(sqlResult.error))
                {
                    sql =  null;
                }

                if (sqlResult.data == null || sqlResult.data.Rows.Count == 0)
                {
                    sql = null;
                }

                sql = sqlResult.data.Rows[0][0].ToString();

                if (!string.IsNullOrEmpty(sql) && listData.CachePermissions)
                {
                    await StoreListDataSQLToCache(listData, sql);
                }
            }

            return sql;
        }


        private async Task<SQLResult> GetListDataSQLFromDB(AxList listData)
        {
            if (_common._dbHelper == null)
                await _common.OpenDBConnectionAsync();

            string dbType = _common._dbHelper.GetDBType().ToUpper().Replace(" ", "");

            string applyDac = "F";
            string constraints = "";

            if (!string.IsNullOrEmpty(listData.Constraint))
            {
                applyDac = "T";
                constraints = listData.Constraint;
            }

            string sql = "";
            
            if (dbType.ToUpper().Replace(" ", "") == Constants.DBTYPE.MSSQL.ToString())
            {
                sql = Constants_SQL.GET_AXLIST_LISTDATA_MSSQL;
                string[] paramName = { "@ptransid", "@pflds", "@ppagesize", "@ppageno", "@pfilter", "@pusername", "@papplydac", "@puserrole", "@pconstraints" };
                DbType[] paramType = { DbType.String, DbType.String, DbType.Int32, DbType.Int32, DbType.String, DbType.String, DbType.String, DbType.String, DbType.String };
                object[] paramValue = { listData.TransId, listData.Fields ?? "All", listData.PageSize, listData.PageNo, listData.Filter ?? "NA", listData.UserName, applyDac, string.Join(",", listData.Roles), constraints };

                return await _common._dbHelper.ExecuteSQLAsync(sql, paramName, paramType, paramValue);
            }
            else
            {
                sql = Constants_SQL.GET_AXLIST_LISTDATA.ToString();
                if (dbType.ToUpper() == Constants.DBTYPE.ORACLE.ToString())
                    sql = Constants_SQL.GET_AXLIST_LISTDATA_ORACLE.ToString();

                string[] paramName = { "@transid", "@fields", "@pagesize", "@pageno", "@filter", "@username", "@applydac", "@roles", "@constraints" };
                DbType[] paramType = { DbType.String, DbType.String, DbType.Int32, DbType.Int32, DbType.String, DbType.String, DbType.String, DbType.String, DbType.String };
                object[] paramValue = { listData.TransId, listData.Fields ?? "All", listData.PageSize, listData.PageNo, listData.Filter ?? "NA", listData.UserName, applyDac, string.Join(",", listData.Roles), constraints };

                return await _common._dbHelper.ExecuteSQLAsync(sql, paramName, paramType, paramValue);
            }
        }

        private string GenerateDataListSQLCacheKey(AxList listData)
        {
            string applyDac = "F";
            string constraints = "";

            if (!string.IsNullOrEmpty(listData.Constraint))
            {
                applyDac = "T";
                constraints = listData.Constraint;
            }

            var cacheKey = _common.GenerateRedisKeyString(new string[] { listData.SchemaName, listData.TransId, "ARM-AxDataListSQL",  listData.Fields ?? "All", listData.PageSize.ToString(), listData.PageNo.ToString(), listData.Filter ?? "NA", listData.UserName, applyDac, string.Join(",", listData.Roles.OrderBy(r => r)), constraints });

            return cacheKey;
        }
        private async Task<string> GetListDataSQLFromCache(AxList listData)
        {            
            var redisConnected = await _common.OpenRedisConnectionAsync();
            if (redisConnected)
            {
                var cacheKey = GenerateDataListSQLCacheKey(listData);
                return await _common._redisCache.StringGetAsync(cacheKey);
            }
            else
                return "";
        }

        private async Task<bool> StoreListDataSQLToCache(AxList listData, string sql)
        {           
            var redisConnected = await _common.OpenRedisConnectionAsync();
            if (redisConnected)
            {
                var cacheKey = GenerateDataListSQLCacheKey(listData);
                return await _common._redisCache.StringSetAsync(cacheKey, sql);
            }
            else
                return false;
        }

    }
}
