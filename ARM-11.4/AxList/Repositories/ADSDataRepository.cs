using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMCommon.Model;
using Newtonsoft.Json;
using System.Data;
using System.Text.RegularExpressions;

namespace AxList
{
    public class ADSDataRepository : IADSDataRepository
    {
        private readonly Utils _common;
        private readonly IARMLogTraceService _traceService;
        private Regex ParamRegex = new Regex(@"(?<!:):(?=[A-Za-z_])(\w+)", RegexOptions.Compiled);

        public ADSDataRepository(Utils common, IARMLogTraceService traceService)
        {
            _common = common;
            _traceService = traceService;
        }

        public async Task<ADSDataSource> GetADSDefinitionAsync(string schemaName, string dataSourceName)
        {
            try
            {
                var cachedADS = await GetADSDefinitionFromCache(schemaName, dataSourceName);

                if (!string.IsNullOrEmpty(cachedADS))
                {
                    return JsonConvert.DeserializeObject<ADSDataSource>(cachedADS);
                }
                else
                {
                    var adsDefinition = await GetADSDefinitionFromDB(schemaName, dataSourceName);
                    if (adsDefinition != null && string.IsNullOrEmpty(adsDefinition.error) && adsDefinition.data != null && adsDefinition.data.Rows.Count > 0)
                    {
                        var row = adsDefinition.data.Rows[0];
                        var dataSource = new ADSDataSource
                        {
                            AxDirectSQLID = row["axdirectsqlid"].ToString(),
                            SQLText = row["sqltext"].ToString(),
                            SQLParams = row["sqlparams"]?.ToString(),
                            Roles = row["roles"]?.ToString() ?? "F",
                            CacheData = row["cachedata"]?.ToString() ?? "F",
                            CacheInterval = row["cacheinterval"]?.ToString() ?? "",
                            SQLName = dataSourceName,
                            EncryptedFlds = row["encryptedflds"]?.ToString() ?? "",
                        };

                        await SetADSDefinitionToCache(schemaName, dataSourceName, JsonConvert.SerializeObject(dataSource));

                        return dataSource;
                    }                    
                    else
                    {
                        return null;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                // Log error but don't fail
                return null;
            }
        }
    
        private async Task<string> GetADSDefinitionFromCache(string schemaName, string dataSourceName)
        {
            var redisConnected = await _common.OpenRedisConnectionAsync();
            if (redisConnected)
            {
                string cacheKey = $"{schemaName}-ARM-AxADSSQL-{dataSourceName}";
                return await _common._redisCache.StringGetAsync(cacheKey);
            }
            return "";
        }

        public async Task<SQLResult> GetADSDefinitionFromDB(string schemaName, string dataSourceName)
        {
            if (_common._dbHelper == null)
                await _common.OpenDBConnectionAsync();

            string[] paramName = { "@datasource" };
            DbType[] paramType = { DbType.String };
            object[] paramValue = { dataSourceName };

            return await _common._dbHelper.ExecuteSQLAsync(Constants_SQL.GET_ADS_DEFINITION, paramName, paramType, paramValue);
        }

        private async Task<bool> SetADSDefinitionToCache(string schemaName, string dataSourceName, string value)
        {
            var redisConnected = await _common.OpenRedisConnectionAsync();
            if (redisConnected)
            {
                string cacheKey = $"{schemaName}-ARM-AxADSSQL-{dataSourceName}";
                return await _common._redisCache.StringSetAsync(cacheKey, value);
            }
            return false;
        }

        public async Task<bool> ValidateUserAccess(string userRoles, string requiredRoles)
        {
            try
            {
                if (string.IsNullOrEmpty(requiredRoles) || requiredRoles.ToUpper().Split(",").Contains("ALL"))
                    return true;

                var userRoleList = userRoles.Split(',').Select(r => r.Trim()).ToList();
                var requiredRoleList = requiredRoles.Split(',').Select(r => r.Trim()).ToList();

                // Check if user has any of the required roles
                return userRoleList.Any(role => requiredRoleList.Contains(role));
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> SaveADSDataToCacheAsync(string cacheKey, object data, int expiryMinutes)
        {
            try
            {
                var redisConnected = await _common.OpenRedisConnectionAsync();
                if (redisConnected)
                {
                    int expirySeconds = expiryMinutes * 60;
                    await _common._redisCache.StringSetAsync(cacheKey, JsonConvert.SerializeObject(data), expirySeconds);
                    return true;
                }
            }
            catch
            {
                return false;
            }
            return false;
        }

        public string GenerateDataCacheKey(string schemaName, string dataSourceName, Dictionary<string, string> parameters)
        {
            try
            {
                if (parameters == null || parameters.Count == 0)
                    return $"{schemaName}-ARM-AxADSData-{dataSourceName}-NoParams";

                // Sort parameters by key name in ascending order
                var sortedParams = parameters.OrderBy(kvp => kvp.Key).ToList();
                
                var paramStrings = sortedParams.Select(kvp => $"{kvp.Key}={kvp.Value}");
                string paramString = string.Join("~", paramStrings);
                
                return $"{schemaName}-ARM-AxADSData-{dataSourceName}-{paramString}";
            }
            catch
            {
                return $"{schemaName}-ARM-AxADSData-{dataSourceName}-NoParams";
            }
        }

        public async Task<string> GetADSDataFromCacheAsync(string cacheKey)
        {
            var redisConnected = await _common.OpenRedisConnectionAsync();
            if (redisConnected)
            {
                return await _common._redisCache.StringGetAsync(cacheKey);
            }
            return "";
        }

        public async Task<SQLResult> GetADSDataFromDB(string sqlText, Dictionary<string, string> parameters)
        {
            if (_common._dbHelper == null)
                await _common.OpenDBConnectionAsync();            
            
            var adsResult = new SQLResult();
            if (parameters != null && parameters.Count > 0)
            {
                var (modifiedSqlText, paramNames, paramTypes, paramValues) = await _common.GetSQLParams(sqlText, parameters);

                if(_common._dbType.ToUpper() == Constants.DBTYPE.MSSQL.ToString())
                    modifiedSqlText = ParamRegex.Replace(modifiedSqlText, "@$1");

                adsResult = await _common._dbHelper.ExecuteSQLAsync(modifiedSqlText, paramNames, paramTypes, paramValues);
            }
            else
            {
                if (_common._dbType.ToUpper() == Constants.DBTYPE.MSSQL.ToString())
                    sqlText = ParamRegex.Replace(sqlText, "@$1");

                adsResult = await _common._dbHelper.ExecuteSQLAsync(sqlText);
            }            
            
            return adsResult;
        }        

        public async Task<SQLResult> GetADSDataViewPermissionFromDB(string transId, string adsName, string userName, List<string> roles, string keyField, string keyValue)
        {
            if (_common._dbHelper == null)
                await _common.OpenDBConnectionAsync();

            string dbType = _common._dbHelper.GetDBType().ToUpper().Replace(" ", "");

            if (roles?.Count == 0)
                roles.Add("All");

            string sql = Constants_SQL.GET_AXLIST_ADSDATAVIEWPERMISSION.ToString();
            if (dbType.ToUpper() == Constants.DBTYPE.ORACLE.ToString())
                sql = Constants_SQL.GET_AXLIST_ADSDATAVIEWPERMISSION_ORACLE.ToString();

            string[] paramName = { "@transid", "@datasource", "@username", "@roles", "@keyfield", "@keyvalue" };
            DbType[] paramType = { DbType.String, DbType.String, DbType.String, DbType.String, DbType.String, DbType.String };
            object[] paramValue = { transId ?? "", adsName, userName, string.Join(",", roles), keyField ?? "", keyValue ?? "" };

            return await _common._dbHelper.ExecuteSQLAsync(sql, paramName, paramType, paramValue);
        }

        public async Task<string> GetADSSQLForAxList(AxList listData, string adsName, string constraint = "NA")
        {
            string sql = "";
            if(listData.CachePermissions)
                sql = await GetADSSQLForAxListFromCache(listData, adsName, constraint);

            if (string.IsNullOrEmpty(sql))
            {
                var sqlResult = await GetADSSQLForAxListFromDB(listData, adsName, constraint);
                if (!string.IsNullOrEmpty(sqlResult.error))
                {
                    sql = null;
                }

                if (sqlResult.data == null || sqlResult.data.Rows.Count == 0)
                {
                    sql = null;
                }

                sql = sqlResult.data.Rows[0][0].ToString();

                if (!string.IsNullOrEmpty(sql) && listData.CachePermissions)
                {
                    await StoreADSSQLForAxListToCache(listData, sql, adsName, constraint);
                }
            }

            return sql;
        }

        private string GenerateADSSQLForAxListCacheKey(AxList listData, string adsName, string constraint = "NA")
        {
            if (listData.Roles?.Count == 0)
                listData.Roles.Add("All");

            string applyDac = "F";
            string constraints = "";

            if (!string.IsNullOrEmpty(constraint))
            {
                applyDac = "T";
                constraints = constraint;
            }

            var cacheKey = _common.GenerateRedisKeyString(new string[] { listData.SchemaName, "ARM-AxADSSQL", adsName, "AxList", listData.TransId, string.Join(",", listData.Roles.OrderBy(r => r)), constraints });

            return cacheKey;
        }
        private async Task<string> GetADSSQLForAxListFromCache(AxList listData, string adsName, string constraint = "NA")
        {
            var cacheKey = GenerateADSSQLForAxListCacheKey(listData, adsName, constraint);
            var redisConnected = await _common.OpenRedisConnectionAsync();
            if (redisConnected)
            {
                return await _common._redisCache.StringGetAsync(cacheKey);
            }
            return "";
        }

        private async Task<bool> StoreADSSQLForAxListToCache(AxList listData, string sql, string adsName, string constraint = "NA")
        {
            var cacheKey = GenerateADSSQLForAxListCacheKey(listData, adsName, constraint);
            var redisConnected = await _common.OpenRedisConnectionAsync();
            if (redisConnected)
            {
                return await _common._redisCache.StringSetAsync(cacheKey, sql);
            }
            return false;
        }

        private async Task<SQLResult> GetADSSQLForAxListFromDB(AxList listData, string adsName, string constraints = "NA")
        {
            if (_common._dbHelper == null)
                await _common.OpenDBConnectionAsync();

            string dbType = _common._dbHelper.GetDBType().ToUpper().Replace(" ", "");

            if (string.IsNullOrEmpty(listData.TransId))
                listData.TransId = "";

            if (string.IsNullOrEmpty(constraints))
                constraints = "NA";

            string sql = Constants_SQL.GET_AXGET_GETADSSQL.ToString();
            if (dbType.ToUpper() == Constants.DBTYPE.ORACLE.ToString())
                sql = Constants_SQL.GET_AXGET_GETADSSQL_ORACLE.ToString();

            string[] paramName = { "@transid", "@datasource", "@constraints" };
            DbType[] paramType = { DbType.String, DbType.String, DbType.String };
            object[] paramValue = { listData.TransId ?? "", adsName, constraints ?? "NA" };

            return await _common._dbHelper.ExecuteSQLAsync(sql, paramName, paramType, paramValue);
        }

        public async Task<Int32> GetADSDataTotalCountFromDB(string sqlText, Dictionary<string, string> parameters)
        {
            if (_common._dbHelper == null)
                await _common.OpenDBConnectionAsync();
            
            if (parameters != null && parameters.Count > 0)
            {
                var (modifiedSqlText, paramNames, paramTypes, paramValues) = await _common.GetSQLParams(sqlText, parameters);
                
                if (_common._dbType.ToUpper() == Constants.DBTYPE.MSSQL.ToString())
                    modifiedSqlText = ParamRegex.Replace(modifiedSqlText, "@$1");

                ScalarResult result  = await _common._dbHelper.ExecuteScalarAsync(modifiedSqlText, paramNames, paramTypes, paramValues);
                if(string.IsNullOrEmpty(result.error))
                    return Convert.ToInt32(result.result);
                else
                    return -1;
            }
            else
            {
                if (_common._dbType.ToUpper() == Constants.DBTYPE.MSSQL.ToString())
                    sqlText = ParamRegex.Replace(sqlText, "@$1");

                ScalarResult result = await _common._dbHelper.ExecuteScalarAsync(sqlText);
                if (string.IsNullOrEmpty(result.error))
                    return Convert.ToInt32(result.result);
                else
                    return -1;
            }
        }
    }
}
