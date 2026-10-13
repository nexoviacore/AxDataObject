using ARM_APIs.Interface;
using ARM_APIs.Model;
using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMCommon.Model;
using System.Data;
using Newtonsoft.Json;
using System.Globalization;
using StackExchange.Redis;
using System.Collections;
using ARM_APIs.Service;
using AutoMapper;

namespace ARM_APIs.Services
{
    public class EntityService : IEntityService
    {
        private readonly IRedisHelper _redis;
        private readonly IConfiguration _config;
        private readonly Utils _common;
        private readonly IARMLogTraceService _traceService;
        private bool isTracing;
        
        public EntityService(IRedisHelper redis, IConfiguration configuration, Utils common, IARMLogTraceService traceService)
        {    
            _redis = redis;
            _config = configuration;
            _common = common;
            _traceService = traceService;
        }

        //public async Task<SQLResult> GetEntityListData(Entity entity)
        //{
        //    Dictionary<string, string> config = await _common.GetDBConfigurations(entity.AppName);
        //    string connectionString = config["ConnectionString"];
        //    string dbType = config["DBType"];

        //    string sql = Constants_SQL.GET_ENTITYDATA_ANALYTICS.ToString();
        //    if (dbType.ToUpper() == Constants.DBTYPE.ORACLE.ToString())
        //        sql = Constants_SQL.GET_ENTITYDATA_ANALYTICS_ORACLE.ToString();
        //    string[] paramName = { "@transid", "@fields", "@pagesize", "@pageno" };
        //    DbType[] paramType = { DbType.String, DbType.String, DbType.Int32, DbType.Int32 };
        //    object[] paramValue = { entity.TransId, entity.Fields, entity.PageSize, entity.PageNo };
        //    return await GetDataCustom(dbType, sql, connectionString, paramName, paramType, paramValue);

        //}

        public async Task<SQLResult> GetEntityListData(Entity entity)
        {
            Dictionary<string, string> config = await _common.GetDBConfigurations(entity.AppName);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];

            string sql = Constants_SQL.GET_ENTITYDATA_ANALYTICS.ToString();
            if (dbType.ToUpper() == Constants.DBTYPE.ORACLE.ToString())
                sql = Constants_SQL.GET_ENTITYDATA_ANALYTICS_ORACLE.ToString();
            string[] paramName = { "@transid", "@fields", "@pagesize", "@pageno", "@username" };
            DbType[] paramType = { DbType.String, DbType.String, DbType.Int32, DbType.Int32, DbType.String };
            object[] paramValue = { entity.TransId, entity.Fields, entity.PageSize, entity.PageNo, entity.UserName };

            var viewFilters = await _common.GetViewFilterForTstruct(entity.ARMSessionId, entity.TransId);
            var globalParams = await _common.GetGlobalParams_Old(entity.ARMSessionId);

            var dsResult = await GetDataSet(dbType, sql, connectionString, paramName, paramType, paramValue, viewFilters, globalParams);
            return await GetJsonFromDataSet(dsResult);

        }
        //public async Task<SQLResult> GetFilteredEntityListData(Entity entity)
        //{

        //    Dictionary<string, string> config = await _common.GetDBConfigurations(entity.AppName);
        //    string connectionString = config["ConnectionString"];
        //    string dbType = config["DBType"];

        //    string sql = Constants_SQL.GET_FILTERED_ENTITYDATA_ANALYTICS.ToString();
        //    if (dbType.ToUpper() == Constants.DBTYPE.ORACLE.ToString())
        //        sql = Constants_SQL.GET_FILTERED_ENTITYDATA_ANALYTICS_ORACLE.ToString();
        //    string[] paramName = { "@transid", "@fields", "@pagesize", "@pageno", "@filter" };
        //    DbType[] paramType = { DbType.String, DbType.String, DbType.Int32, DbType.Int32, DbType.String, };
        //    object[] paramValue = { entity.TransId, entity.Fields, entity.PageSize, entity.PageNo, entity.Filter };
        //    return await GetDataCustom(dbType, sql, connectionString, paramName, paramType, paramValue);

        //}

        public async Task<SQLResult> GetFilteredEntityListData(Entity entity)
        {

            Dictionary<string, string> config = await _common.GetDBConfigurations(entity.AppName);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];

            string sql = "";
            if (dbType.ToUpper().Replace(" ", "") == Constants.DBTYPE.MSSQL.ToString())
            {
                sql = Constants_SQL.GET_FILTERED_ENTITYDATA_ANALYTICS_MSSQL;
                string[] paramName = { "@ptransid", "@pflds", "@ppagesize", "@ppageno", "@pfilter", "@pusername", "@papplydac" };
                DbType[] paramType = { DbType.String, DbType.String, DbType.Int32, DbType.Int32, DbType.String, DbType.String, DbType.String };
                object[] paramValue = { entity.TransId, entity.Fields, entity.PageSize, entity.PageNo, entity.Filter, entity.UserName, "T" };

                var viewFilters = await _common.GetViewFilterForTstruct(entity.ARMSessionId, entity.TransId);
                var globalParams = await _common.GetGlobalParams_Old(entity.ARMSessionId);

                var dsResult = await GetDataSet(dbType, sql, connectionString, paramName, paramType, paramValue, viewFilters, globalParams);
                return await GetJsonFromDataSet(dsResult);
            }
            else
            {
                sql = Constants_SQL.GET_FILTERED_ENTITYDATA_ANALYTICS.ToString();
                if (dbType.ToUpper() == Constants.DBTYPE.ORACLE.ToString())
                    sql = Constants_SQL.GET_FILTERED_ENTITYDATA_ANALYTICS_ORACLE.ToString();
                string[] paramName = { "@transid", "@fields", "@pagesize", "@pageno", "@filter", "@username" };
                DbType[] paramType = { DbType.String, DbType.String, DbType.Int32, DbType.Int32, DbType.String, DbType.String };
                object[] paramValue = { entity.TransId, entity.Fields, entity.PageSize, entity.PageNo, entity.Filter, entity.UserName };

                var viewFilters = await _common.GetViewFilterForTstruct(entity.ARMSessionId, entity.TransId);
                var globalParams = await _common.GetGlobalParams_Old(entity.ARMSessionId);

                var dsResult = await GetDataSet(dbType, sql, connectionString, paramName, paramType, paramValue, viewFilters, globalParams);
                return await GetJsonFromDataSet(dsResult);
            }
        }

        public async Task<SQLResult> GetEntityMetaDataFromDB(Entity entity)
        {

            Dictionary<string, string> config = await _common.GetDBConfigurations(entity.AppName);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];

            string sql = "";
            if (dbType.ToUpper().Replace(" ", "") == Constants.DBTYPE.MSSQL.ToString())
            {
                sql = Constants_SQL.GET_ENTITYMETADATA_ANALYTICS_MSSQL;
                string[] paramName = { "@ptransid", "@psubentity" , "@planguage"};
                DbType[] paramType = { DbType.String, DbType.String, DbType.String };
                object[] paramValue = { entity.TransId, "F" , entity.Language ?? "English"};
                return await GetStoredProcedureData(dbType, sql, connectionString, paramName, paramType, paramValue);
            }
            else
            {
                sql = Constants_SQL.GET_ENTITYMETADATA_ANALYTICS.ToString();
                if (dbType.ToUpper() == Constants.DBTYPE.ORACLE.ToString())
                    sql = Constants_SQL.GET_ENTITYMETADATA_ANALYTICS_ORACLE.ToString();

                string[] paramName = { "@transid", "@language" };
                DbType[] paramType = { DbType.String, DbType.String };
                object[] paramValue = { entity.TransId, entity.Language ?? "English" };
                return await GetData(dbType, sql, connectionString, paramName, paramType, paramValue);
            }
        }

        public async Task<SQLResult> GetEntityMetaDataV2(Entity entity)
        {
            var result = new SQLResult();
            var keyParams = new string[] { entity.SchemaName, entity.TransId, "ARM-AX-DATALIST-MetaData", entity.UserName, entity.Roles ?? "All", entity.Language?.Substring(0, 3) ?? "ENG" };
            string rediskey = GenerateRedisKeyString(keyParams);
            var data = await _redis.StringGetAsync(rediskey);

            if (string.IsNullOrEmpty(data) || entity.MetaDataFromDB == true)
            {

                result = await GetEntityMetaDataFromDB(entity);
                if (string.IsNullOrEmpty(result.error) && result.data != null && result.data.Rows.Count > 0)
                {
                    data = JsonConvert.SerializeObject(result.data);
                    await _redis.StringSetAsync(rediskey, data);

                    var entityName = await GetEntityNameAsync(entity.TransId, result.data);
                    await SetPageTitle(entity.SchemaName, "Tstruct", entity.TransId, entityName, entity.Language?.Substring(0, 3) ?? "ENG");

                    return result;
                }
                else
                    return result;
            }

            result.data = JsonConvert.DeserializeObject<DataTable>(data);

            return result;
        }

        //public async Task<SQLResult> GetEntityMetaData(Entity entity)
        //{
        //    isTracing = entity.Trace == true;
        //    _traceService.EnableTracing(isTracing);

        //    var result = new SQLResult();
        //    var keyParams = new string[] { entity.SchemaName, entity.TransId, "ARM-AX-DATALIST-MetaData", entity.UserName, entity.Roles ?? "All", entity.Language?.Substring(0,3) ?? "ENG" };
        //    string rediskey = GenerateRedisKeyString(keyParams);
        //    var data = await _redis.StringGetAsync(rediskey);

        //    if (string.IsNullOrEmpty(data) || entity.MetaDataFromDB == true)
        //    {
        //        var accessResult = new APIResult();
        //        var cacheKey = _common.GenerateRedisKeyString(new string[] { entity.SchemaName ?? entity.AppName, entity.TransId, "ARM-AX-VIEW-PERMISSION", entity.UserName, entity.Roles ?? "All" });
        //        var cachedResult = await _redis.StringGetAsync(cacheKey);
        //        if (string.IsNullOrEmpty(cachedResult))
        //        {
        //            //AxGetDataService _getDataService = new AxGetDataService(_redis, _config, _common, _traceService);

        //            var config = new MapperConfiguration(cfg => cfg.CreateMap<Entity, AxList.AxList>().ForMember(dest => dest.Roles, opt => opt.Ignore()));
        //            var mapper = config.CreateMapper();
        //            AxList.AxList listobj = mapper.Map<AxList.AxList>(entity);
        //            listobj.Roles = entity.Roles?.Split(",").ToList<string>() ?? new List<string> { "All" };
        //            listobj.Project = entity.AppName;
        //            listobj.Action = Constants.TSTRUCT_ACTIONS.VIEW.ToString();

        //            accessResult = await _axListService.GetListViewPermission(listobj);
        //        }
        //        else {
        //            accessResult = JsonConvert.DeserializeObject<APIResult>(cachedResult);
        //        }

        //        if (!string.IsNullOrEmpty(accessResult.error))
        //        {
        //            if (isTracing) _traceService.LogMessage($"Result JSON - {JsonConvert.SerializeObject(result)}");

        //            result.error = accessResult.error;
        //            return result;
        //        }

        //        if (isTracing) _traceService.LogMessage("Fetching Entity MetaData from DB.");
        //        result = await GetEntityMetaDataFromDB(entity);
        //        if (string.IsNullOrEmpty(result.error) && result.data != null && result.data.Rows.Count > 0)
        //        {
        //            var excludedFields = accessResult.data["ExcludedFields"].ToString().Split(',').ToList<string>();
        //            var includeFields = accessResult.data["IncludedFields"].ToString().Split(',').ToList<string>();
        //            var maskedFields = accessResult.data["MaskedFields"].ToString().Split(',').ToList<string>();

        //            result.data = FilterMetaDataBasedOnUserAccess(result.data, excludedFields, includeFields, entity.TransId);
        //            data = JsonConvert.SerializeObject(result.data);
        //            await _redis.StringSetAsync(rediskey, data);

        //            var entityName = await GetEntityNameAsync(entity.TransId, result.data);
        //            await SetPageTitle(entity.SchemaName, "Tstruct", entity.TransId, entityName, entity.Language?.Substring(0, 3) ?? "ENG");

        //            return result;
        //        }
        //        else
        //            return result;
        //    }
        //    else
        //    {
        //        if (isTracing) _traceService.LogMessage("Fetching Entity MetaData from Redis Cache.");
        //    }

        //    result.data = JsonConvert.DeserializeObject<DataTable>(data);

        //    return result;
        //}

        private DataTable FilterMetaDataBasedOnUserAccess(DataTable dt, List<string> excludedFields, List<string> includedFields, string transId)
        {
            var none = Constants.TSTRUCT_DATA_VARS.NONE.ToString();
            var all = Constants.TSTRUCT_DATA_VARS.ALL.ToString();

            bool excludeNone = excludedFields.Contains(none, StringComparer.OrdinalIgnoreCase);
            bool includeAll = includedFields.Contains(all, StringComparer.OrdinalIgnoreCase);

            List<string> structFields = new List<string> { "transid", "sourceid" };
            var filteredRows = dt.AsEnumerable().Where(row => row.Field<string>("ftransid") == transId).Where(row =>
                {
                    string fld = row.Field<string>("fldname");
                    string dc = row.Field<string>("dcname");

                    // Exclusion logic
                    if (!excludeNone && (excludedFields.Contains(fld, StringComparer.OrdinalIgnoreCase) || excludedFields.Contains(dc, StringComparer.OrdinalIgnoreCase)))
                        return false;

                    // Inclusion logic
                    if (includeAll || structFields.Contains(fld, StringComparer.OrdinalIgnoreCase))
                        return true;

                    return includedFields.Contains(fld, StringComparer.OrdinalIgnoreCase) || includedFields.Contains(dc, StringComparer.OrdinalIgnoreCase);
                });

            return filteredRows.CopyToDataTable();
        }


        private async Task<bool> SetPageTitle(string schemaName, string pageType, string pageId, string pageName, string language)
        {
            var pageTitleKeyParams = new string[] { schemaName, "axpagetitle", language };
            string pageTitlekey = GenerateRedisKeyString(pageTitleKeyParams);
            var pageTitles = await _redis.ObjectGetAsync(pageTitlekey);
            var pageTitleArr = new ArrayList();
            var value = pageType + "♠" + pageName + "♠" + pageId;
            if (pageTitles != null)
            {
                pageTitleArr = (ArrayList)pageTitles;
                if (!pageTitleArr.Contains(value))
                {
                    pageTitleArr.Add(value);
                    await _redis.ObjectSetAsync(pageTitlekey, pageTitleArr);
                    return true;
                }
            }
            else
            {
                pageTitleArr.Add(value);
                await _redis.ObjectSetAsync(pageTitlekey, pageTitleArr);
                return true;
            }
            return false;
        }

        //public async Task<SQLResult> GetEntityChartsData(EntityCharts entity)
        //{

        //    Dictionary<string, string> config = await _common.GetDBConfigurations(entity.AppName);
        //    string connectionString = config["ConnectionString"];
        //    string dbType = config["DBType"];

        //    string sql = Constants_SQL.GET_ENTITYCHARTSDATA_ANALYTICS.ToString()
        //    if (dbType.ToUpper() == Constants.DBTYPE.ORACLE.ToString())
        //        sql = Constants_SQL.GET_ENTITYCHARTSDATA_CUSTOM_ANALYTICS_ORACLE.ToString();
        //    string[] paramName = { "@transid", "@condition", "@criteria" };
        //    DbType[] paramType = { DbType.String, DbType.String, DbType.String };
        //    object[] paramValue = { entity.TransId, entity.Condition, entity.Criteria };
        //    var dsResult = await GetDataSet(dbType, sql, connectionString, paramName, paramType, paramValue);
        //    return await GetJsonFromDataSet(dsResult);

        //}
        //
        public async Task<SQLResult> GetEntityChartsData(EntityCharts entity)
        {

            Dictionary<string, string> config = await _common.GetDBConfigurations(entity.AppName);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];

            string sql = "";
            if (entity.Condition.ToUpper() == "GENERAL")
                sql = Constants_SQL.GET_ENTITYCHARTSDATA_GENERAL_ANALYTICS.ToString();
            else
                sql = Constants_SQL.GET_ENTITYCHARTSDATA_CUSTOM_ANALYTICS.ToString();

            if (dbType.ToUpper() == Constants.DBTYPE.ORACLE.ToString())
            {
                sql = Constants_SQL.GET_ENTITYCHARTSDATA_CUSTOM_ANALYTICS_ORACLE.ToString();
            }

            string[] paramName = { "@transid", "@condition", "@criteria", "@username" };
            DbType[] paramType = { DbType.String, DbType.String, DbType.String, DbType.String };
            object[] paramValue = { entity.TransId, entity.Condition, entity.Criteria, entity.UserName };

            var viewFilters = await _common.GetViewFilterForTstruct(entity.ARMSessionId, entity.TransId);

            var dsResult = await GetDataSet(dbType, sql, connectionString, paramName, paramType, paramValue, viewFilters);
            return await GetJsonFromDataSet(dsResult);

        }

        //public async Task<SQLResult> GetAnalyticsChartsData(AnalyticsCharts charts)
        //{
        //    isTracing = charts.Trace == true;
        //    _traceService.EnableTracing(isTracing);

        //    Dictionary<string, string> config = await _common.GetDBConfigurations(charts.AppName);
        //    string connectionString = config["ConnectionString"];
        //    string dbType = config["DBType"];

        //    var accessResult = new APIResult();
        //    var cacheKey = _common.GenerateRedisKeyString(new string[] { charts.SchemaName ?? charts.AppName, charts.TransId, "ARM-AX-VIEW-PERMISSION", charts.UserName, string.Join(",", charts.Roles) ?? "All" });
        //    var cachedResult = await _redis.StringGetAsync(cacheKey);
        //    if (string.IsNullOrEmpty(cachedResult))
        //    {
        //        //AxGetDataService _getDataService = new AxGetDataService(_redis, _config, _common, _traceService);
        //        AxList.AxList listobj = new AxList.AxList();
        //        listobj.Project = charts.AppName;
        //        listobj.SchemaName = charts.SchemaName;
        //        listobj.TransId = charts.TransId;
        //        listobj.Action = Constants.TSTRUCT_ACTIONS.VIEW.ToString();
        //        listobj.Language = charts.Language;
        //        listobj.UserName = charts.UserName;
        //        listobj.Roles = charts.Roles?.Split(",").ToList<string>() ?? new List<string> { "All" };
        //        listobj.Trace = (bool)(charts.Trace ?? false);

        //        accessResult = await _axListService.GetListViewPermission(listobj);
        //    }
        //    else
        //    {
        //        accessResult = JsonConvert.DeserializeObject<APIResult>(cachedResult);
        //    }

        //    if (!string.IsNullOrEmpty(accessResult.error))
        //    {
        //        var result = new SQLResult();
        //        if (isTracing)
        //        {
        //            _traceService.LogMessage($"Result JSON - {JsonConvert.SerializeObject(accessResult)}");
        //            result.Trace = await _traceService.GetTraceFile($"GetDataList-{charts.TransId}");
        //        }
        //        result.error = accessResult.error;
        //        return result;
        //    }

        //    var filterCond = accessResult.data["FilterCond"].ToString();
        //    charts.Constraint = filterCond;

        //    string sql = "";
        //    var chartsMetaData = charts.ChartMetaData[0];
        //    string criteria = "";
        //    string condition = "";
        //    if (chartsMetaData.AggField.ToUpper() == "COUNT" && chartsMetaData.GroupField.ToUpper() == "ALL")
        //    {
        //        condition = "General";
        //        sql = Constants_SQL.GET_ANALYTICSPAGECHARTSDATA_GENERAL_ANALYTICS.ToString();

        //        if (dbType.ToUpper() == Constants.DBTYPE.ORACLE.ToString())
        //        {
        //            sql = Constants_SQL.GET_ANALYTICSPAGECHARTSDATA_GENERAL_ANALYTICS_ORACLE.ToString();
        //        }
        //    }
        //    else
        //    {
        //        condition = "Custom";
        //        sql = Constants_SQL.GET_ANALYTICSPAGECHARTSDATA_CUSTOM_ANALYTICS.ToString();

        //        if (dbType.ToUpper() == Constants.DBTYPE.ORACLE.ToString())
        //        {
        //            sql = Constants_SQL.GET_ANALYTICSPAGECHARTSDATA_CUSTOM_ANALYTICS_ORACLE.ToString();
        //        }

        //        var entity = new Entity();
        //        entity.AppName = charts.AppName;
        //        entity.SchemaName = charts.SchemaName;
        //        entity.TransId = charts.TransId;
        //        entity.Language = charts.Language;
        //        entity.UserName = charts.UserName;
        //        entity.Roles = charts.Roles;
        //        var metaData = await GetEntityMetaData(entity);

        //        var filteredRows = metaData.data.AsEnumerable()
        //                               .Where(row => ((row.Field<string>("fldname") == chartsMetaData.AggField && row.Field<string>("ftransid") == chartsMetaData.AggTransId) || (row.Field<string>("fldname") == chartsMetaData.GroupField && row.Field<string>("ftransid") == chartsMetaData.GroupTransId)));

        //        Dictionary<string, FieldMetadata> metadataFlds = filteredRows.ToDictionary(row => row.Field<string>("fldname"), row => MapDataRowToMetadata(row));

        //        FieldMetadata aggFld;
        //        metadataFlds.TryGetValue(chartsMetaData.AggField, out aggFld);

        //        FieldMetadata groupFld;
        //        metadataFlds.TryGetValue(chartsMetaData.GroupField, out groupFld);

        //        string tableName;
        //        if (aggFld?.Dcname.ToLower() == "dc1")
        //            tableName = aggFld.Tablename;
        //        else if (groupFld?.Dcname.ToLower() == "dc1")
        //            tableName = groupFld.Tablename;
        //        else
        //        {
        //            tableName = metaData.data.AsEnumerable()
        //                 .First(row => row.Field<string>("dcname") == "dc1")
        //                 .Field<string>("tablename");
        //        }

        //        if (aggFld is null && chartsMetaData.AggField.ToUpper() == "COUNT")
        //        {
        //            aggFld = new FieldMetadata
        //            {
        //                Fldname = chartsMetaData.AggField,
        //                Ftransid = chartsMetaData.AggTransId,
        //                Tablename = tableName,
        //                Dcname = "dc1",
        //            };
        //        }

        //        if (groupFld is null && chartsMetaData.GroupField.ToUpper() == "ALL")
        //        {
        //            groupFld = new FieldMetadata
        //            {
        //                Fldname = "",
        //                Ftransid = chartsMetaData.GroupTransId,
        //                Tablename = tableName,
        //                Dcname = "dc1",
        //            };
        //        }

        //        string aggGrpFldLink = "";
        //        if (chartsMetaData.AggTransId == chartsMetaData.GroupTransId && (aggFld.Dcname == groupFld.Dcname && groupFld.Dcname == "dc1"))
        //        {
        //            aggGrpFldLink = "";
        //        }
        //        else
        //        {
        //            aggGrpFldLink = $"{aggFld.Tablename}.{tableName}id={groupFld.Tablename}.{tableName}id";
        //        }

        //        /* aggfnc~grpfld_transid~groupfld~normalized~srctable~srcfld~allowempty~grpfld_tablename~aggfld_transid~aggfld~aggfld_tablename~grpfld_transid_AND_aggfld_transid_relation*/
        //        criteria = $"{chartsMetaData.AggFunc}~{groupFld.Ftransid}~{groupFld.Fldname}~{groupFld.Normalized}~{groupFld.Srctable}~{groupFld.Srcfield}~{groupFld.Allowempty}~{groupFld.Tablename}~{aggFld.Ftransid}~{aggFld.Fldname}~{aggFld.Tablename}~{aggGrpFldLink}";

        //    }

        //    string applyDac = "F";
        //    string constraints = "";

        //    if (!string.IsNullOrEmpty(charts.Constraint))
        //    {
        //        applyDac = "T";
        //        constraints = charts.Constraint;
        //    }


        //    string[] paramName = { "@transid", "@criteria", "@username", "@applydac", "@roles", "@constraints" };
        //    DbType[] paramType = { DbType.String, DbType.String, DbType.String, DbType.String, DbType.String, DbType.String };
        //    object[] paramValue = { charts.TransId, criteria, charts.UserName, applyDac, charts.Roles, charts.Constraint };

        //    var viewFilters = await _common.GetViewFilterForTstruct(charts.ARMSessionId, charts.TransId);
        //    var globalParams = await _common.GetGlobalParams_Old(charts.ARMSessionId);

        //    var dsResult = await GetDataSet(dbType, sql, connectionString, paramName, paramType, paramValue, viewFilters, globalParams);
        //    return await GetJsonFromDataSet(dsResult);

        //}

        //public async Task<SQLResult> GetAnalyticsMultipleChartsData(AnalyticsCharts charts)
        //{
        //    isTracing = charts.Trace == true;
        //    _traceService.EnableTracing(isTracing);


        //    Dictionary<string, string> config = await _common.GetDBConfigurations(charts.AppName);
        //    string connectionString = config["ConnectionString"];
        //    string dbType = config["DBType"];

        //    var accessResult = new APIResult();
        //    var cacheKey = _common.GenerateRedisKeyString(new string[] { charts.SchemaName ?? charts.AppName, charts.TransId, "ARM-AX-VIEW-PERMISSION", charts.UserName, string.Join(",", charts.Roles) ?? "All" });
        //    var cachedResult = await _redis.StringGetAsync(cacheKey);
        //    if (string.IsNullOrEmpty(cachedResult))
        //    {
        //        //AxGetDataService _getDataService = new AxGetDataService(_redis, _config, _common, _traceService);
        //        AxList.AxList listobj = new AxList.AxList();
        //        listobj.Project = charts.AppName;
        //        listobj.SchemaName = charts.SchemaName;
        //        listobj.TransId = charts.TransId;
        //        listobj.Action = Constants.TSTRUCT_ACTIONS.VIEW.ToString();
        //        listobj.Language = charts.Language;
        //        listobj.UserName = charts.UserName;
        //        listobj.Roles = charts.Roles?.Split(",").ToList<string>() ?? new List<string> { "All" };
        //        listobj.Trace = (bool)(charts.Trace ?? false);

        //        accessResult = await _axListService.GetListViewPermission(listobj);
        //    }
        //    else
        //    {
        //        accessResult = JsonConvert.DeserializeObject<APIResult>(cachedResult);
        //    }

        //    if (!string.IsNullOrEmpty(accessResult.error))
        //    {
        //        var result = new SQLResult();
        //        if (isTracing)
        //        {
        //            _traceService.LogMessage($"Result JSON - {JsonConvert.SerializeObject(accessResult)}");
        //            result.Trace = await _traceService.GetTraceFile($"GetDataList-{charts.TransId}");
        //        }
        //        result.error = accessResult.error;
        //        return result;
        //    }

        //    var filterCond = accessResult.data["FilterCond"].ToString();
        //    charts.Constraint = filterCond;

        //    string sql = "";
        //    string criteria = "";
        //    string condition = "";

        //    foreach (ChartMetaData chartsMetaData in charts.ChartMetaData)
        //    {
        //        if (chartsMetaData.AggField.ToUpper() == "COUNT" && chartsMetaData.GroupField.ToUpper() == "ALL")
        //        {
        //            condition = "General";
        //            sql = Constants_SQL.GET_ENTITYCHARTSDATA_GENERAL_ANALYTICS.ToString();

        //            if (dbType.ToUpper() == Constants.DBTYPE.ORACLE.ToString())
        //            {
        //                sql = Constants_SQL.GET_ENTITYCHARTSDATA_GENERAL_ANALYTICS_ORACLE.ToString();
        //            }
        //            else if (dbType.ToUpper().Replace(" ", "") == Constants.DBTYPE.MSSQL.ToString())
        //            {
        //                sql = Constants_SQL.GET_ENTITYCHARTSDATA_GENERAL_ANALYTICS_MSSQL.ToString();
        //            }
        //        }
        //        else
        //        {
        //            condition = "Custom";
        //            sql = Constants_SQL.GET_ENTITYCHARTSDATA_CUSTOM_ANALYTICS.ToString();

        //            if (dbType.ToUpper() == Constants.DBTYPE.ORACLE.ToString())
        //            {
        //                sql = Constants_SQL.GET_ENTITYCHARTSDATA_CUSTOM_ANALYTICS_ORACLE.ToString();
        //            }
        //            else if (dbType.ToUpper().Replace(" ", "") == Constants.DBTYPE.MSSQL.ToString())
        //            {
        //                sql = Constants_SQL.GET_ENTITYCHARTSDATA_CUSTOM_ANALYTICS_MSSQL.ToString();
        //            }
        //        }

        //        var entity = new Entity();
        //        entity.AppName = charts.AppName;
        //        entity.SchemaName = charts.SchemaName;
        //        entity.TransId = charts.TransId;
        //        entity.Language = charts.Language;
        //        entity.UserName = charts.UserName;
        //        entity.Roles = charts.Roles;
        //       var metaData = await GetEntityMetaData(entity);

        //        var filteredRows = metaData.data.AsEnumerable()
        //                                .Where(row => ((row.Field<string>("fldname") == chartsMetaData.AggField && row.Field<string>("ftransid") == chartsMetaData.AggTransId) || (row.Field<string>("fldname") == chartsMetaData.GroupField && row.Field<string>("ftransid") == chartsMetaData.GroupTransId)));

        //        Dictionary<string, FieldMetadata> metadataFlds = filteredRows.ToDictionary(row => row.Field<string>("fldname"), row => MapDataRowToMetadata(row));

        //        FieldMetadata aggFld;
        //        metadataFlds.TryGetValue(chartsMetaData.AggField, out aggFld);

        //        FieldMetadata groupFld;
        //        metadataFlds.TryGetValue(chartsMetaData.GroupField, out groupFld);

        //        string tableName;
        //        if (aggFld?.Dcname.ToLower() == "dc1")
        //            tableName = aggFld.Tablename;
        //        else if (groupFld?.Dcname.ToLower() == "dc1")
        //            tableName = groupFld.Tablename;
        //        else
        //        {
        //            tableName = metaData.data.AsEnumerable()
        //                    .First(row => row.Field<string>("dcname") == "dc1")
        //                    .Field<string>("tablename");
        //        }

        //        if (aggFld is null && chartsMetaData.AggField.ToUpper() == "COUNT")
        //        {
        //            aggFld = new FieldMetadata
        //            {
        //                Fldname = chartsMetaData.AggField,
        //                Ftransid = chartsMetaData.AggTransId,
        //                Tablename = tableName,
        //                Dcname = "dc1",
        //            };
        //        }

        //        if (groupFld is null && chartsMetaData.GroupField.ToUpper() == "ALL")
        //        {
        //            groupFld = new FieldMetadata
        //            {
        //                Fldname = "",
        //                Ftransid = chartsMetaData.GroupTransId,
        //                Tablename = tableName,
        //                Dcname = "dc1",
        //            };
        //        }

        //        string aggGrpFldLink = "";
        //        if (chartsMetaData.AggTransId == chartsMetaData.GroupTransId && (aggFld.Dcname == groupFld.Dcname && groupFld.Dcname == "dc1"))
        //        {
        //            aggGrpFldLink = "";
        //        }
        //        else
        //        {
        //            aggGrpFldLink = $"{aggFld.Tablename}.{tableName}id={groupFld.Tablename}.{tableName}id";
        //        }

        //        if (criteria == "")
        //            criteria = $"{charts.TransId}~{groupFld.Fldname}~{aggFld.Fldname}~{chartsMetaData.AggFunc}~{groupFld.Normalized}~{groupFld.Srctable}~{groupFld.Srcfield}~{groupFld.Allowempty}~{groupFld.Tablename}~~";
        //        else
        //            criteria += $"^{charts.TransId}~{groupFld.Fldname}~{aggFld.Fldname}~{chartsMetaData.AggFunc}~{groupFld.Normalized}~{groupFld.Srctable}~{groupFld.Srcfield}~{groupFld.Allowempty}~{groupFld.Tablename}~~";

        //    }

        //    string applyDac = "F";
        //    string constraints = "";

        //    if (!string.IsNullOrEmpty(charts.Constraint))
        //    {
        //        applyDac = "T";
        //        constraints = charts.Constraint;
        //    }


        //    string[] paramName = { "@transid", "@condition", "@criteria", "@username", "@applydac", "@roles", "@constraints" };
        //    DbType[] paramType = { DbType.String, DbType.String, DbType.String, DbType.String , DbType.String, DbType.String, DbType.String };
        //    object[] paramValue = { charts.TransId, condition, criteria, charts.UserName, applyDac, charts.Roles, charts.Constraint };

        //    var viewFilters = await _common.GetViewFilterForTstruct(charts.ARMSessionId, charts.TransId);
        //    var globalParams = await _common.GetGlobalParams_Old(charts.ARMSessionId);

        //    var dsResult = await GetDataSet(dbType, sql, connectionString, paramName, paramType, paramValue, viewFilters, globalParams);
        //    return await GetJsonFromDataSet(dsResult);

        //}

        public async Task<SQLResult> GetEntityFormMetaData(EntityForm entity)
        {
            isTracing = entity.Trace == true;
            _traceService.EnableTracing(isTracing);

            var result = new SQLResult();
            var keyParams = new string[] { entity.SchemaName, entity.TransId, "ARM-AX-DATAPAGE-MetaData", entity.Language?.Substring(0, 3) ?? "ENG" };
            string rediskey = GenerateRedisKeyString(keyParams);
            var data = await _redis.StringGetAsync(rediskey);

            if (string.IsNullOrEmpty(data))
            {
                if (isTracing) _traceService.LogMessage("Fetching DataPage Form MetaData from DB.");
                result = await GetEntityFormMetaDataFromDB(entity);
                if (string.IsNullOrEmpty(result.error) && result.data != null && result.data.Rows.Count > 0)
                {
                    data = JsonConvert.SerializeObject(result.data);
                    await _redis.StringSetAsync(rediskey, data);
                    return result;
                }
                else
                    return result;
            }
            else
            {
                if (isTracing) _traceService.LogMessage("Fetching DataPage Form MetaData from Redis Cache.");
            }

            result.data = JsonConvert.DeserializeObject<DataTable>(data);

            return result;
        }

        public async Task<SQLResult> GetEntityFormMetaDataFromDB(EntityForm entity)
        {
            Dictionary<string, string> config = await _common.GetDBConfigurations(entity.AppName);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];

            string sql = "";
            if (dbType.ToUpper().Replace(" ", "") == Constants.DBTYPE.MSSQL.ToString())
            {
                sql = Constants_SQL.GET_ENTITYFORMMETADATA_MSSQL.ToString();
                string[] paramName = { "@transid" };
                DbType[] paramType = { DbType.String };
                object[] paramValue = { entity.TransId };
                return await GetData(dbType, sql, connectionString, paramName, paramType, paramValue);
            }
            else
            {
                if(dbType.ToUpper() == Constants.DBTYPE.ORACLE.ToString())
                    sql = Constants_SQL.GET_ENTITYFORMMETADATA_ORACLE.ToString();
                else
                    sql = Constants_SQL.GET_ENTITYFORMMETADATA.ToString();

                string[] paramName = { "@language", "@transid", "@language", "@transid", "@language", "@transid" };
                DbType[] paramType = { DbType.String, DbType.String, DbType.String, DbType.String, DbType.String, DbType.String };
                object[] paramValue = {  entity.Language ?? "English", entity.TransId, entity.Language ?? "English", entity.TransId , entity.Language ?? "English", entity.TransId };
                return await GetData(dbType, sql, connectionString, paramName, paramType, paramValue);
            }
        }

        //public async Task<SQLResult> GetSubEntityListData(Entity entity)
        //{
        //    Dictionary<string, string> config = await _common.GetDBConfigurations(entity.AppName);
        //    string connectionString = config["ConnectionString"];
        //    string dbType = config["DBType"];

        //    string sql = Constants_SQL.GET_SUBENTITYDATA_ANALYTICS.ToString();
        //    if (dbType.ToUpper() == Constants.DBTYPE.ORACLE.ToString())
        //        sql = Constants_SQL.GET_SUBENTITYDATA_ANALYTICS_ORACLE.ToString();
        //    string[] paramName = { "@transid", "@fields", "@recordid", "@pagesize", "@pageno" };
        //    DbType[] paramType = { DbType.String, DbType.String, DbType.Int64, DbType.Int32, DbType.Int32 };
        //    object[] paramValue = { entity.TransId, entity.Fields, entity.RecordId, entity.PageSize, entity.PageNo };
        //    return await GetDataCustom(dbType, sql, connectionString, paramName, paramType, paramValue);
        //}

        //public async Task<SQLResult> GetSubEntityListData(EntityForm entity)
        //{
        //    isTracing = entity.Trace == true;
        //    _traceService.EnableTracing(isTracing);

        //    var accessResult = new APIResult();
        //    var cacheKey = _common.GenerateRedisKeyString(new string[] { entity.SchemaName ?? entity.AppName, entity.Fields.Split('=')[0], "ARM-AX-VIEW-PERMISSION", entity.UserName, entity.Roles ?? "All" });
        //    var cachedResult = await _redis.StringGetAsync(cacheKey);            
        //    if (string.IsNullOrEmpty(cachedResult))
        //    {
        //        //AxGetDataService _getDataService = new AxGetDataService(_redis, _config, _common, _traceService);

        //        AxList.AxList listobj = new AxList.AxList();
        //        listobj.Project = entity.AppName;
        //        listobj.SchemaName = entity.SchemaName;
        //        listobj.TransId = entity.Fields.Split('=')[0];
        //        listobj.Action = Constants.TSTRUCT_ACTIONS.VIEW.ToString();
        //        listobj.Language = entity.Language;
        //        listobj.UserName = entity.UserName;
        //        listobj.Roles = entity.Roles?.Split(",").ToList<string>() ?? new List<string> { "All" };
        //        listobj.Trace = (bool)(entity.Trace ?? false);

        //        accessResult = await _axListService.GetListViewPermission(listobj);
        //    }
        //    else
        //    {
        //        accessResult = JsonConvert.DeserializeObject<APIResult>(cachedResult);
        //    }

        //    if (!string.IsNullOrEmpty(accessResult.error))
        //    {
        //        var result = new SQLResult();
        //        if (isTracing) { 
        //            _traceService.LogMessage($"Result JSON - {JsonConvert.SerializeObject(accessResult)}");
        //            result.Trace = await _traceService.GetTraceFile($"GetConnectedDataList-{entity.TransId}");
        //        }
        //        result.error = accessResult.error;
        //        return result;
        //    }

        //    var apiResult = new APIResult();
        //    var listDataResult = await GetSubEntityListDataFromDB(entity);
        //    if (string.IsNullOrEmpty(listDataResult.error) && listDataResult.DataSet != null && listDataResult.DataSet.Tables.Count > 0)
        //    {
        //        var dt = listDataResult.DataSet.Tables[0];

        //        var excludedFields = accessResult.data["ExcludedFields"].ToString().Split(',').ToList<string>();
        //        var includeFields = accessResult.data["IncludedFields"].ToString().Split(',').ToList<string>();
        //        var maskedFields = accessResult.data["MaskedFields"].ToString().Split(',').ToList<string>();

        //        var newDt = await _common.HandleIncludeExcludeCols(dt, excludedFields, includeFields, maskedFields);

        //        apiResult.success = true;
        //        listDataResult.DataSet = new DataSet();
        //        listDataResult.DataSet.Tables.Add(newDt);
        //    }

        //    if (isTracing)
        //    {
        //        _traceService.LogMessage($"GetListDataFromDB Result JSON - {JsonConvert.SerializeObject(listDataResult)}");
        //    }

        //    return await GetJsonFromDataSetForSubEntityListing(listDataResult, Convert.ToInt32(entity.PageSize), Convert.ToInt32(entity.PageNo));
        //}

        public async Task<SQLDataSetResult> GetSubEntityListDataFromDB(EntityForm entity)
        {
            Dictionary<string, string> config = await _common.GetDBConfigurations(entity.AppName);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];

            string sql = Constants_SQL.GET_SUBENTITYDATA_ANALYTICS.ToString();

            if (dbType.ToUpper() == Constants.DBTYPE.ORACLE.ToString())
                sql = Constants_SQL.GET_SUBENTITYDATA_ANALYTICS_ORACLE.ToString();
            string[] paramName = { "@transid", "@fields", "@pagesize", "@pageno" };
            DbType[] paramType = { DbType.String, DbType.String, DbType.Int32, DbType.Int32 };
            object[] paramValue = { entity.TransId, entity.Fields, entity.PageSize, entity.PageNo };

            var viewFilters = new Dictionary<string, string>();
            //var viewFilters = await _common.GetViewFilterForTstruct(entity.ARMSessionId, entity.TransId);
            var globalParams = await _common.GetGlobalParams_Old(entity.ARMSessionId);

            return await GetDataSet(dbType, sql, connectionString, paramName, paramType, paramValue, viewFilters, globalParams);
            
        }

        public async Task<SQLResult> GetSubEntityMetaData(EntityForm entity)
        {
            isTracing = entity.Trace == true;
            _traceService.EnableTracing(isTracing);

            var result = new SQLResult();
            var keyParams = new string[] { entity.SchemaName, entity.TransId, "ARM-AX-DATAPAGE-MetaData", entity.UserName, entity.Roles ?? "All", entity.Language?.Substring(0, 3) ?? "ENG" };
            string rediskey = GenerateRedisKeyString(keyParams);
            var data = await _redis.StringGetAsync(rediskey);
            bool validCache = false;
            if (!string.IsNullOrEmpty(data))
            {
                _traceService.LogMessage("Fetching DataPage MetaData from Redis Cache.");
                result.data = JsonConvert.DeserializeObject<DataTable>(data);



                var ckeyParams = new string[] { entity.SchemaName, entity.TransId, "ARM-AX-DATAPAGE-ConnectedDataTransIds", entity.UserName, entity.Roles ?? "All", entity.Language?.Substring(0, 3) ?? "ENG" };
                string crediskey = GenerateRedisKeyString(ckeyParams);
                string connectedTstFromCache = await _redis.StringGetAsync(crediskey);
                if (!string.IsNullOrEmpty(connectedTstFromCache))
                {
                    ckeyParams = new string[] { entity.SchemaName, entity.TransId, connectedTstFromCache, "ARM-AX-DATAPAGE-ConnectedData", entity.UserName, entity.Roles ?? "All", entity.Language?.Substring(0, 3) ?? "ENG" };
                    crediskey = GenerateRedisKeyString(ckeyParams);
                    if(_redis.KeyExists(crediskey))
                        validCache = true;
                }
            }

            if (string.IsNullOrEmpty(data) || !validCache)
            {
                _traceService.LogMessage("Fetching DataPage MetaData from DB.");
                result = await GetSubEntityMetaDataFromDB(entity);
                if (string.IsNullOrEmpty(result.error))
                {
                    AxGetDataService _getDataService = new AxGetDataService(_redis, _config, _common, _traceService);

                    var config = new MapperConfiguration(cfg => cfg.CreateMap<EntityForm, AxListData>().ForMember(dest => dest.Roles, opt => opt.Ignore()));
                    var mapper = config.CreateMapper();
                    AxListData listobj = mapper.Map<AxListData>(entity);
                    listobj.Roles = entity.Roles?.Split(",").ToList<string>() ?? new List<string> { "All" };
                    listobj.Project = entity.AppName;
                    listobj.Action = Constants.TSTRUCT_ACTIONS.VIEW.ToString();
                    listobj.ConnectedData_TransIds = string.Join(",",
                        result.data.AsEnumerable()
                            .Select(row => row.Field<string>("ftransid"))
                            .Distinct()
                    );
                    var connectedStructures = await _getDataService.AxGetConnectedDataList(listobj);

                    var dtFilteredMetaData = result.data.Clone();
                    foreach (string tstid in listobj.ConnectedData_TransIds.Split(","))
                    {
                        if (connectedStructures != null && connectedStructures.data != null && connectedStructures.data.Count > 0 && connectedStructures.data.ContainsKey(tstid)) {
                            var accessResult = JsonConvert.DeserializeObject<APIResult>(JsonConvert.SerializeObject(connectedStructures.data[tstid]));
                            var excludedFields = accessResult.data["ExcludedFields"].ToString().Split(',').ToList<string>();
                            var includeFields = accessResult.data["IncludedFields"].ToString().Split(',').ToList<string>();
                            var maskedFields = accessResult.data["MaskedFields"].ToString().Split(',').ToList<string>();
                            var dt = FilterMetaDataBasedOnUserAccess(result.data, excludedFields, includeFields, tstid);
                            dtFilteredMetaData.Merge(dt);
                        }
                    }

                    string connectedTransids = string.Join("-",
                       result.data.AsEnumerable()
                           .Select(row => row.Field<string>("ftransid"))
                           .Distinct().OrderBy(x => x)
                   );

                    result.data = dtFilteredMetaData;

                    
                    var ckeyParams = new string[] { entity.SchemaName, entity.TransId, connectedTransids, "ARM-AX-DATAPAGE-ConnectedData", entity.UserName, entity.Roles ?? "All", entity.Language?.Substring(0, 3) ?? "ENG" };
                    string crediskey = GenerateRedisKeyString(ckeyParams);
                    await _redis.StringSetAsync(crediskey, "true");

                    ckeyParams = new string[] { entity.SchemaName, entity.TransId, "ARM-AX-DATAPAGE-ConnectedDataTransIds", entity.UserName, entity.Roles ?? "All", entity.Language?.Substring(0, 3) ?? "ENG" };
                    crediskey = GenerateRedisKeyString(ckeyParams);
                    await _redis.StringSetAsync(crediskey, connectedTransids);

                    data = JsonConvert.SerializeObject(result.data);                    
                    await _redis.StringSetAsync(rediskey, data);
                    return result;
                }
                else
                    return result;
            }
            else
            {
                _traceService.LogMessage("Fetching DataPage MetaData from Redis Cache.");
                result.data = JsonConvert.DeserializeObject<DataTable>(data);
            }
            return result;
        }
        private async Task<SQLResult> GetSubEntityMetaDataFromDB(EntityForm entity)
        {
            Dictionary<string, string> config = await _common.GetDBConfigurations(entity.AppName);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];
            string sql = "";
            if (dbType.ToUpper().Replace(" ", "") == Constants.DBTYPE.MSSQL.ToString())
            {
                sql = Constants_SQL.GET_SUBENTITYMETADATA_ANALYTICS_MSSQL;
                string[] paramName = { "@ptransid", "@psubentity" , "@planguage"};
                DbType[] paramType = { DbType.String, DbType.String, DbType.String };
                object[] paramValue = { entity.TransId, "T" , entity.Language ?? "English"};
                return await GetStoredProcedureData(dbType, sql, connectionString, paramName, paramType, paramValue);
            }
            else
            {
                sql = Constants_SQL.GET_SUBENTITYMETADATA_ANALYTICS.ToString();
                if (dbType.ToUpper() == Constants.DBTYPE.ORACLE.ToString())
                    sql = Constants_SQL.GET_SUBENTITYMETADATA_ANALYTICS_ORACLE.ToString();
                string[] paramName = { "@transid", "@language" };
                DbType[] paramType = { DbType.String, DbType.String };
                object[] paramValue = { entity.TransId, entity.Language ?? "English" };
                return await GetData(dbType, sql, connectionString, paramName, paramType, paramValue);
            }
        }


        //public async Task<SQLResult> GetSubEntityChartsData(EntityCharts entity)
        //{
        //    Dictionary<string, string> config = await _common.GetDBConfigurations(entity.AppName);
        //    string connectionString = config["ConnectionString"];
        //    string dbType = config["DBType"];

        //    //string sql = Constants_SQL.GET_SUBENTITYCHARTSDATA.ToString();
        //    string sql = Constants_SQL.GET_SUBENTITYCHARTSDATA_ANALYTICS.ToString();
        //    if (dbType.ToUpper() == Constants.DBTYPE.ORACLE.ToString())
        //        sql = Constants_SQL.GET_SUBENTITYCHARTSDATA_ANALYTICS_ORACLE.ToString();
        //    string[] paramName = { "@transid", "@recordid", "@keynamevalue", "@condition", "@criteria" };
        //    DbType[] paramType = { DbType.String, DbType.Int64, DbType.String, DbType.String, DbType.String };
        //    object[] paramValue = { entity.TransId, entity.RecordId, entity.KeyValue, entity.Condition, entity.Criteria };
        //    return await GetDataCustom(dbType, sql, connectionString, paramName, paramType, paramValue);
        //}

        public async Task<SQLResult> GetSubEntityChartsData(EntityCharts entity)
        {
            Dictionary<string, string> config = await _common.GetDBConfigurations(entity.AppName);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];

            string sql = Constants_SQL.GET_SUBENTITYCHARTSDATA_ANALYTICS.ToString();
            if (dbType.ToUpper() == Constants.DBTYPE.ORACLE.ToString())
                sql = Constants_SQL.GET_SUBENTITYCHARTSDATA_ANALYTICS_ORACLE.ToString();

            string[] paramName = { "@transid", "@condition", "@criteria", "@username" };
            DbType[] paramType = { DbType.String, DbType.String, DbType.String, DbType.String };
            object[] paramValue = { entity.TransId, entity.Condition, entity.Criteria, entity.UserName };

            var dsResult = await GetDataSet(dbType, sql, connectionString, paramName, paramType, paramValue);
            return await GetJsonFromDataSet(dsResult);
        }
        public async Task<SQLResult> GetEntityList(Entity entity)
        {
            Dictionary<string, string> config = await _common.GetDBConfigurations(entity.AppName);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];
            string sql = "";
            bool hasDefaultRole = entity.Roles.Split(',').Contains("default");
            string[] roles = entity.Roles.Split(',');
            string rolesList = string.Join(", ", roles.Where(word => !string.IsNullOrWhiteSpace(word)).Select(word => $"'{word}'"));

            if (string.IsNullOrEmpty(entity.EntityName))
                if (hasDefaultRole)
                {
                    sql = Constants_SQL.GET_ALLENTITYLIST_ANALYTICS_DEFAULTROLE.ToString();

                    if (dbType.ToUpper() == Constants.DBTYPE.ORACLE.ToString())
                        sql = Constants_SQL.GET_ALLENTITYLIST_ANALYTICS_DEFAULTROLE_ORACLE.ToString();
                }
                else
                {
                    sql = Constants_SQL.GET_ALLENTITYLIST_ANALYTICS_OTHERROLES.ToString();
                    
                    if (dbType.ToUpper() == Constants.DBTYPE.ORACLE.ToString())
                        sql = Constants_SQL.GET_ALLENTITYLIST_ANALYTICS_OTHERROLES_ORACLE.ToString();

                    sql = sql.Replace("$OTHERROLES$", rolesList);
                }
            else
            {

                string[] entities = entity.EntityName.Split(',');
                string entityList = string.Join(", ", entities.Where(word => !string.IsNullOrWhiteSpace(word)).Select(word => $"'{word}'"));
                if (hasDefaultRole)
                {
                    sql = Constants_SQL.GET_FILTERED_ENTITYLIST_ANALYTICS_DEFAULTROLE.ToString();
                    if (dbType.ToUpper() == Constants.DBTYPE.ORACLE.ToString())
                        sql = Constants_SQL.GET_FILTERED_ENTITYLIST_ANALYTICS_DEFAULTROLE_ORACLE.ToString();
                }
                else
                {
                    sql = Constants_SQL.GET_FILTERED_ENTITYLIST_ANALYTICS_OTHERROLES.ToString();
                    if (dbType.ToUpper() == Constants.DBTYPE.ORACLE.ToString())
                        sql = Constants_SQL.GET_FILTERED_ENTITYLIST_ANALYTICS_OTHERROLES_ORACLE.ToString();

                    sql = sql.Replace("$OTHERROLES$", rolesList);
                }

                sql = sql.Replace("$ENTITYLIST$", entityList);
            }

            string[] paramName = { "@language"};
            DbType[] paramType = { DbType.String};
            object[] paramValue = { entity.Language  ?? "English"};

            return await GetData(dbType, sql, connectionString, paramName, paramType, paramValue);
        }

        public async Task<SQLResult> GetEntityEditableFlag(Entity entity)
        {
            Dictionary<string, string> config = await _common.GetDBConfigurations(entity.AppName);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];

            string sql = Constants_SQL.GET_DAC_EDITABLE_FLAG_ANALYTICS.ToString();

            if (dbType.ToUpper() == Constants.DBTYPE.ORACLE.ToString())
                sql = Constants_SQL.GET_DAC_EDITABLE_FLAG_ANALYTICS_ORACLE;
            else if (dbType.ToUpper().Replace(" ", "") == Constants.DBTYPE.MSSQL.ToString())
                sql = Constants_SQL.GET_DAC_EDITABLE_FLAG_ANALYTICS_MSSQL;

            string[] paramName = { "@transid", "@recordid", "@username" };
            DbType[] paramType = { DbType.String, DbType.Int64, DbType.String };
            object[] paramValue = { entity.TransId, entity.RecordId, entity.UserName };

            var dsResult = await GetData(dbType, sql, connectionString, paramName, paramType, paramValue);
            return dsResult;
        }

        //public async Task<SQLResult> GetEntityDropdownValues(EntityFilter entityFilter)
        //{
        //    Dictionary<string, string> config = await _common.GetDBConfigurations(entityFilter.AppName);
        //    string connectionString = config["ConnectionString"];
        //    string dbType = config["DBType"];

        //    string sql = Constants_SQL.GET_FILTER_FIELDDATA_ANALYTICS.ToString();

        //    if (dbType.ToUpper() == Constants.DBTYPE.ORACLE.ToString())
        //        sql = Constants_SQL.GET_FILTER_FIELDDATA_ANALYTICS_ORACLE;

        //    sql = sql.Replace("$SEARCHTEXT$", entityFilter.SearchText?.ToLower() ?? "");

        //    entityFilter.Filter = await GetDropdownFilterString(entityFilter);

        //    string[] paramName = { "@transid", "@filter" };
        //    DbType[] paramType = { DbType.String, DbType.String };
        //    object[] paramValue = { entityFilter.TransId, entityFilter.Filter };

        //    var dsResult = await GetData(dbType, sql, connectionString, paramName, paramType, paramValue);
        //    return dsResult;
        //}

        public async Task<SQLResult> GetEntityFormConnectedDataMetrics(EntityForm entityForm)
        {
            Dictionary<string, string> config = await _common.GetDBConfigurations(entityForm.AppName);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];

            string sql = Constants_SQL.GET_ENTITYFORM_CONNECTEDDATAMETRICS.ToString();

            if (dbType.ToUpper() == Constants.DBTYPE.ORACLE.ToString())
                sql = Constants_SQL.GET_ENTITYFORM_CONNECTEDDATAMETRICS_ORACLE;


            string[] paramName = { "@criteria" };
            DbType[] paramType = { DbType.String };
            object[] paramValue = { entityForm.Criteria };

            var dsResult = await GetDataSet(dbType, sql, connectionString, paramName, paramType, paramValue);
            return await GetJsonFromDataSet(dsResult);
        }

        public async Task<bool> CheckEntityFormDatasource(EntityForm entityForm)
        {
            var cacheKey = _common.GenerateRedisKeyString(new string[] { entityForm.SchemaName ?? entityForm.AppName, entityForm.TransId, "ARM-AX-DATAPAGE-CDP-DATASOURCE"});
            var cachedResult = await _redis.StringGetAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedResult))
            {
                if (_traceService.IsTracingEnabled()) _traceService.LogMessage("Data Page - CDP Datasource checked from cache", cachedResult);
                return JsonConvert.DeserializeObject<bool>(cachedResult);
            }

            Dictionary<string, string> config = await _common.GetDBConfigurations(entityForm.AppName);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];

            string sql = Constants_SQL.GETDATASOURCESSQL.ToString();

            string[] paramName = { "@datasource" };
            DbType[] paramType = { DbType.String };
            object[] paramValue = { "cdp_" + entityForm.TransId.ToLower() };

            var dsResult = await GetData(dbType, sql, connectionString, paramName, paramType, paramValue);
            if (_traceService.IsTracingEnabled()) _traceService.LogMessage("Data Page - CDP Datasource checked from DB", cachedResult);
            if (dsResult != null && string.IsNullOrEmpty(dsResult.error) && dsResult.data != null && dsResult.data.Rows.Count > 0)
            {
                await _redis.StringSetAsync(cacheKey, "true");
                return true;
            }
            await _redis.StringSetAsync(cacheKey, "false");
            return false;
        }

        private DataTable ConvertDataJsonNodesToLowerCase(DataTable dt)
        {
            dt.Columns["data_json"].ReadOnly = false;
            foreach (DataRow row in dt.Rows)
            {
                DataTable newDt = new DataTable();
                var dataList = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(row["data_json"].ToString());
                foreach (var item in dataList)
                {
                    if (newDt.Columns.Count == 0)
                    {
                        // Create columns with lowercase names
                        foreach (var key in item.Keys)
                        {
                            newDt.Columns.Add(key.ToLower(), typeof(object));
                        }
                    }

                    // Add the data to the new DataTable
                    DataRow newRow = newDt.NewRow();
                    foreach (var key in item.Keys)
                    {
                        newRow[key.ToLower()] = item[key];
                    }
                    newDt.Rows.Add(newRow);
                }
                row["data_json"] = JsonConvert.SerializeObject(newDt);
            }

            return dt;

        }

        private async Task<SQLResult> GetData(string dbType, string sql, string connectionString, string[] paramName = null, DbType[] paramType = null, object[] paramValue = null)
        {
            IDbHelper dbHelper = DBHelper.CreateDbHelper(dbType, _traceService);
            return await dbHelper.ExecuteSQLAsync(sql, connectionString, paramName, paramType, paramValue);
        }

        private async Task<SQLResult> GetStoredProcedureData(string dbType, string procName, string connectionString, string[] paramName = null, DbType[] paramType = null, object[] paramValue = null)
        {
            IDbHelper dbHelper = DBHelper.CreateDbHelper(dbType, _traceService);
            return await dbHelper.ExecuteStoredProcedureAsync(procName, connectionString, paramName, paramType, paramValue);
        }

        private async Task<SQLResult> GetDataCustom(string dbType, string sql, string connectionString, string[] paramName, DbType[] paramType, object[] paramValue)
        {
            IDbHelper dbHelper = DBHelper.CreateDbHelper(dbType, _traceService);
            var dt = await dbHelper.ExecuteSQLAsync(sql, connectionString, paramName, paramType, paramValue);
            if (dbType.ToUpper() == Constants.DBTYPE.ORACLE.ToString() && string.IsNullOrEmpty(dt.error))
                dt.data = ConvertDataJsonNodesToLowerCase(dt.data);
            return dt;
        }

        private async Task<SQLDataSetResult> GetDataSet(string dbType, string sql, string connectionString, string[] paramName, DbType[] paramType, object[] paramValue, Dictionary<string, string>? viewFilters = null, Dictionary<string, string>? globalParams = null)
        {
            IDbHelper dbHelper = DBHelper.CreateDbHelper(dbType, _traceService);
            return await dbHelper.ExecuteSQLsFromSQLAsync(sql, connectionString, paramName, paramType, paramValue, viewFilters, globalParams);
        }

        private async Task<SQLResult> GetJsonFromDataSet(SQLDataSetResult dsResult)
        {
            var sqlResult = new SQLResult();
            if (!string.IsNullOrEmpty(dsResult.error) && dsResult.DataSet.Tables.Count == 0)
            {
                sqlResult.error = dsResult.error;
            }
            else
            {

                List<Task<string>> jsonTasks = new List<Task<string>>();
                foreach (DataTable dt in dsResult.DataSet.Tables)
                {
                    sqlResult.count += dt.Rows.Count;
                    jsonTasks.Add(Task.Run(() => JsonConvert.SerializeObject(dt)));
                }

                sqlResult.data.Columns.Add("data_json", typeof(string));

                string[] jsonResults = await Task.WhenAll(jsonTasks);
                foreach (string json in jsonResults)
                {
                    sqlResult.data.Rows.Add(json);
                }
            }

            return sqlResult;
        }

        private async Task<SQLResult> GetJsonFromDataTable(DataTable dt)
        {
            var sqlResult = new SQLResult();
            sqlResult.data.Columns.Add("data_json", typeof(string));
            sqlResult.data.Rows.Add(JsonConvert.SerializeObject(dt));

            return sqlResult;
        }

        private async Task<SQLResult> GetJsonFromDataJson(string json)
        {
            var sqlResult = new SQLResult();
            sqlResult.data.Columns.Add("data_json", typeof(string));
            sqlResult.data.Rows.Add(json);

            return sqlResult;
        }

        private async Task<SQLResult> GetJsonFromDataSetForSubEntityListing(SQLDataSetResult dsResult, int pageSize, int pageNo)
        {
            var sqlResult = new SQLResult();
            if (dsResult.error != null)
            {
                sqlResult.error = dsResult.error;
            }
            else
            {

                List<Task<DataTable>> dtTasks = new List<Task<DataTable>>();
                foreach (DataTable dt in dsResult.DataSet.Tables)
                {
                    dtTasks.Add(Task.Run(() => MergeSubEntityData(dt)));
                }

                sqlResult.data.Columns.Add("modifiedon", typeof(string));
                sqlResult.data.Columns.Add("data_json", typeof(object));

                DataTable[] dtResults = await Task.WhenAll(dtTasks);
                foreach (DataTable dt in dtResults)
                {
                    sqlResult.data.Merge(dt);
                }

                sqlResult.data = GetPaginatedData(sqlResult.data, "modifiedon", false, pageSize, pageNo);
            }

            return sqlResult;
        }

        private DataTable MergeSubEntityData(DataTable dt)
        {
            var newDt = new DataTable();
            newDt.Columns.Add("modifiedon", typeof(string));
            newDt.Columns.Add("data_json", typeof(object));

            foreach (DataRow dr in dt.Rows)
            {
                DataRow row = newDt.NewRow();
                if (DateTime.TryParse(dr["modifiedon"].ToString(), out DateTime modifiedOn))
                {
                    row["modifiedon"] = modifiedOn.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                }
                else
                {
                    row["modifiedon"] = dr["modifiedon"].ToString();
                }

                row["data_json"] = DataRowToJson(dr);
                newDt.Rows.Add(row);
            }

            return newDt;
        }

        private Dictionary<string, object> DataRowToJson(DataRow row)
        {
            var dict = new Dictionary<string, object>();
            foreach (DataColumn column in row.Table.Columns)
            {
                dict[column.ColumnName] = row[column];
            }
            return dict;
        }

        private static DataTable GetPaginatedData(DataTable dt, string sortBy, bool ascending, int pageSize, int pageNumber)
        {
            DataView dv = new DataView(dt);
            dv.Sort = sortBy + (ascending ? " ASC" : " DESC");
            DataTable sortedTable = dv.ToTable();

            var paginatedData = sortedTable.AsEnumerable()
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize);

            DataTable paginatedTable = new DataTable();
            paginatedTable.Columns.Add("data_json", typeof(object));

            foreach (var row in paginatedData)
            {
                DataRow newRow = paginatedTable.NewRow();
                newRow["data_json"] = row["data_json"];
                paginatedTable.Rows.Add(newRow);
            }

            return paginatedTable;
        }

        public async Task<APIResult> SetAnalyticsData(AnalyticsData analyticsData)
        {
            APIResult apiResult = new APIResult();
            string userName = Convert.ToBoolean(analyticsData.All) ? "All" : analyticsData.UserName;

            string prefix = GetPagePrefix(analyticsData.Page);
            if (string.IsNullOrEmpty(prefix))
            {
                apiResult.error = "Invalid page type.";
                return apiResult;
            }

            try
            {
                foreach (var prop in analyticsData.Properties)
                {
                    string redisKey = GenerateRedisKeyString(new string[] { analyticsData.SchemaName, analyticsData.TransId, prefix, prop.Key.ToUpper() });

                    if (userName == "All")
                    {
                        //TO-DO : Implement timestamp based change logic
                        await _redis.KeyDeleteAsync(redisKey);
                    }

                    await DeleteUserConfigDataFromDB(analyticsData, prop.Key.ToUpper(), userName);
                    await _redis.HashSetAsync(redisKey, userName, prop.Value.ToString());
                    await InsertUserConfigDataInDB(analyticsData, userName, prop.Key.ToUpper(), prop.Value.ToString());
                }
            }
            catch (Exception ex)
            {
                apiResult.error = $"Save failed. {ex.Message}.";
                return apiResult;
            }

            apiResult.data.Add("success", true);
            apiResult.data.Add("message", "Saved successfully");
            return apiResult;
        }


        public async Task<APIResult> GetAnalyticsData(AnalyticsData analyticsData)
        { 
            APIResult apiResult = new APIResult();
            string prefix = GetPagePrefix(analyticsData.Page);
            if (string.IsNullOrEmpty(prefix))
            {
                apiResult.error = "Invalid page type.";
                return apiResult;
            }

            var resultDict = new Dictionary<string, Object>();
            try
            {
                List<string> propertiesFromDB = new List<string>();
                List<string> propertiesFromCache = new List<string>();
                foreach (var prop in analyticsData.PropertiesList)
                {
                    string redisKey = GenerateRedisKeyString(new string[] { analyticsData.SchemaName, analyticsData.TransId, prefix, prop.ToUpper() });
                    var result = await _redis.HashGetAsync(redisKey, analyticsData.UserName);

                    if (result == null)
                    {
                        result = await _redis.HashGetAsync(redisKey, "All");
                    }

                    if (result != null)
                    {
                        propertiesFromCache.Add(prop.ToUpper());
                        resultDict.Add(prop.ToUpper(), result);
                    }
                    else
                    {
                        propertiesFromDB.Add(prop.ToUpper());
                    }

                    
                }

                _traceService.LogMessage($"Properties available in cache - {string.Join("','", propertiesFromCache)}");

                if (propertiesFromDB.Count > 0)
                {
                    _traceService.LogMessage($"Properties to be fetched from DB - {string.Join("','", propertiesFromDB)}");

                    var propertiesList = string.Join("','", propertiesFromDB.Select(s => s.ToLower()));
                    var result = await GetUserConfigDataFromDB(analyticsData, propertiesList, analyticsData.UserName);

                    if (result != null && string.IsNullOrEmpty(result.error) && result.data != null && result.data.Rows.Count > 0)
                    {
                        foreach (var property in propertiesFromDB)
                        {
                            string redisKey = GenerateRedisKeyString(new string[] { analyticsData.SchemaName, analyticsData.TransId, prefix, property.ToUpper() });
                            var resultDictionary = FetchUserSpecificData(result.data, analyticsData.UserName, property);
                            var value = "";
                            var userName = analyticsData.UserName;
                            if (resultDictionary != null && resultDictionary.Count > 0)
                            {
                                if (resultDictionary.ContainsKey(analyticsData.UserName))
                                {
                                    value = resultDictionary[analyticsData.UserName];
                                    resultDict.Add(property.ToUpper(), value);
                                }
                                else if (resultDictionary.ContainsKey("All"))
                                {
                                    value = resultDictionary["All"];
                                    userName = "All";
                                    resultDict.Add(property.ToUpper(), value);
                                }
                            }

                            await _redis.HashSetAsync(redisKey, userName, value);

                        }
                    }
                    else
                    {
                        foreach (var property in propertiesFromDB)
                        {
                            resultDict.Add(property.ToUpper(), "");
                            string redisKey = GenerateRedisKeyString(new string[] { analyticsData.SchemaName, analyticsData.TransId, prefix, property.ToUpper() });
                            await _redis.HashSetAsync(redisKey, "All", "");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                apiResult.error = $"Get failed. {ex.Message}.";
                return apiResult;
            }

            apiResult.data = resultDict;
            return apiResult;
        }

        public Dictionary<string, string> FetchUserSpecificData(DataTable dataTable, string username, string keyname)
        {

            var filteredRows = dataTable.AsEnumerable()
                .Where(row =>
                    (row.Field<string>("username").Equals(username, StringComparison.OrdinalIgnoreCase) || row.Field<string>("username").Equals("All", StringComparison.OrdinalIgnoreCase)) &&
                    row.Field<string>("keyname").Equals(keyname, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var resultDictionary = new Dictionary<string, string>();

            foreach (var row in filteredRows)
            {
                resultDictionary[row.Field<string>("username")] = row.Field<string>("value");
            }

            return resultDictionary;
        }

        public async Task<APIResult> GetAnalyticsProperties(string schemaName, string appName, string page, string transId, List<string> propertiesList, string userName)
        {
            var analyticsData = new AnalyticsData();
            analyticsData.TransId = transId;
            analyticsData.AppName = appName;
            analyticsData.SchemaName = schemaName;
            analyticsData.Page = page;
            analyticsData.UserName = userName;
            analyticsData.PropertiesList = propertiesList;
            return await GetAnalyticsData(analyticsData);
        }

        public string GetPagePrefix(string page)
        {
            string prefix = "";
            bool isValidAnalyticsPage = Enum.IsDefined(typeof(Constants.ANALYTICS_PAGES), page.ToUpper());
            if (isValidAnalyticsPage)
            {
                return $"ARM-USERDATA-{page.ToUpper()}";
            }

            return prefix;
        }

        private string GenerateRedisKeyString(string[] keyNodes)
        {
            string redisKey = "";
            if (keyNodes != null || keyNodes.Length > 0)
            {
                foreach (var keyNode in keyNodes)
                {
                    if (!string.IsNullOrEmpty(keyNode))
                        redisKey += $"-{keyNode}";
                }
            }
            if (redisKey.StartsWith("-"))
                redisKey = redisKey.Substring(1);
            return redisKey;
        }

        private DataTable ExcludeDBColumns(DataTable originalTable)
        {
            List<string> excludeCols = new List<string> { "props", "normalized", "srctable", "srcfield", "srctransid", "allowempty", "datacnd", "entityrelfld", "allowduplicate", "tablename" };
            DataTable filteredTable = new DataTable();

            foreach (DataColumn col in originalTable.Columns)
            {
                if (!excludeCols.Contains(col.ColumnName))
                {
                    filteredTable.Columns.Add(col.ColumnName, col.DataType);
                }
            }

            foreach (DataRow row in originalTable.Rows)
            {
                DataRow newRow = filteredTable.NewRow();
                foreach (DataColumn col in filteredTable.Columns)
                {
                    newRow[col.ColumnName] = row[col.ColumnName];
                }
                filteredTable.Rows.Add(newRow);
            }

            return filteredTable;
        }
        //Trace Done
        //public async Task<EntityListOutput> GetEntityListPageLoadData(Entity entity)
        //{
        //    var result = new EntityListOutput();
        //    try
        //    {
        //        isTracing = entity.Trace == true;
        //        _traceService.EnableTracing(isTracing);

        //        if (isTracing)
        //            _traceService.LogMessage($"Input JSON - {JsonConvert.SerializeObject(entity)}");

        //        var metaData = await GetEntityMetaData(entity);
        //        if (!string.IsNullOrEmpty(metaData.error))
        //        {
        //            if (_traceService.IsTracingEnabled())
        //            {
        //                _traceService.LogError($"{metaData.error}");
        //                result.Trace = await _traceService.GetTraceFile($"GetDataList-{entity.TransId}");
        //            }
        //            result.Error = metaData.error;
        //            return result;
        //        }

        //        var entityName = await GetEntityNameAsync(entity.TransId, metaData.data);
        //        var properties = await GetAnalyticsProperties(entity.SchemaName, entity.AppName, entity.Page, entity.TransId, entity.PropertiesList, entity.UserName);

        //        var fieldsStr = "All";
        //        if (properties != null && properties.data != null && properties.data.Count > 0 && properties.data.ContainsKey("FIELDS"))
        //        {
        //            fieldsStr = properties.data["FIELDS"]?.ToString() ?? "";
        //        }

        //        if (!string.IsNullOrEmpty(fieldsStr) && fieldsStr != "All")
        //        {
        //            entity.Fields = GetFieldString(entity.TransId, fieldsStr, metaData.data);
        //        }
        //        else
        //        {
        //            entity.Fields = "All";
        //        }

        //        SQLResult entityList = new SQLResult();
        //        if (entity.Filters != null && entity.Filters.Count > 0)
        //        {
        //            entity.Filter = await GetFilterString(entity, metaData);
        //        }
        //        else
        //            entity.Filter = "NA";

        //        //AxListService _getDataService = new AxListService(null, _traceService, _common, _redis);
        //        var config = new MapperConfiguration(cfg => cfg.CreateMap<Entity, AxList.AxList>().ForMember(dest => dest.Roles, opt => opt.Ignore()));
        //        var mapper = config.CreateMapper();
        //        AxList.AxList listobj = mapper.Map<AxList.AxList>(entity);
        //        listobj.Roles = entity.Roles?.Split(",").ToList<string>() ?? new List<string> { "All" };
        //        listobj.Project = entity.AppName;
        //        listobj.Action = Constants.TSTRUCT_ACTIONS.VIEW.ToString();

        //        var listDataResult = await _axListService.AxListAsync(listobj);

        //        if (!string.IsNullOrEmpty(listDataResult.error))
        //            result.Error = listDataResult.error;
        //        else if (listDataResult.data != null && listDataResult.data.ContainsKey("error") && !string.IsNullOrEmpty(listDataResult.data?["error"]?.ToString() ?? ""))
        //            result.Error = listDataResult.data?["error"]?.ToString();
        //        else if (listDataResult.data?["data"] != null)
        //            entityList = await GetJsonFromDataTable((DataTable)listDataResult.data["data"]);

        //        result.TransId = entity.TransId;
        //        result.EntityName = entityName;
        //        if (entity.OnlyData == false)
        //            result.MetaData = metaData.data;

        //        result.Properties = properties.data;
        //        result.ListData = entityList.data;
        //        result.Error = entityList.error;
        //        result.RecordsFetched = entityList.count;
        //        result.Fields = entity.Fields;
        //        result.Filter = entity.Filter;
        //        result.PageNo = entity.PageNo;
        //        result.PageSize = entity.PageSize;
        //        result.ActiveFilter = System.Text.Json.JsonSerializer.Serialize(entity.Filters);
        //    }
        //    catch (Exception ex)
        //    {
        //        _traceService.LogError($"{ex.Message + Environment.NewLine + ex.StackTrace + Environment.NewLine + ex.InnerException}");
        //    }

        //    if (isTracing)
        //    {
        //        _traceService.LogMessage($"Result JSON - {JsonConvert.SerializeObject(result)}");
        //        result.Trace = await _traceService.GetTraceFile($"GetDataList-{entity.TransId}");
        //    }

        //    return result;
        //}

        //public async Task<EntityFormOutput> GetEntityFormPageLoadData(EntityForm entityForm)
        //{
        //    var result = new EntityFormOutput();
        //    try
        //    {
        //        isTracing = entityForm.Trace == true;
        //        _traceService.EnableTracing(isTracing);

        //        if (isTracing)
        //            _traceService.LogMessage($"Input JSON - {JsonConvert.SerializeObject(entityForm)}");

        //        var subEntityMetaData = await GetSubEntityMetaData(entityForm);
        //        var formMetaData = await GetEntityFormMetaData(entityForm);
        //        var customDatasource = await CheckEntityFormDatasource(entityForm);
        //        var properties = await GetAnalyticsProperties(entityForm.SchemaName, entityForm.AppName, entityForm.Page, entityForm.TransId, entityForm.PropertiesList, entityForm.UserName);
        //        var keyField = await GetAnalyticsProperties(entityForm.SchemaName, entityForm.AppName, Constants.ANALYTICS_PAGES.ENTITY.ToString(), entityForm.TransId, new List<string> { "KEYFIELD" }, entityForm.UserName);
        //        var entityName = await GetEntityNameAsync(entityForm.TransId, subEntityMetaData.data);

        //        result.TransId = entityForm.TransId;
        //        result.EntityName = entityName;
        //        result.SubEntityMetaData = subEntityMetaData.data;
        //        result.Properties = properties.data;
        //        result.FormMetaData = formMetaData.data;
        //        result.KeyField = keyField.data["KEYFIELD"]?.ToString() ?? "";
        //        result.Error = subEntityMetaData.error + formMetaData.error;
        //        result.DataSource = customDatasource;
        //    }
        //    catch (Exception ex)
        //    {
        //        _traceService.LogError($"{ex.Message + Environment.NewLine + ex.StackTrace + Environment.NewLine + ex.InnerException}");
        //    }

        //    if (isTracing)
        //    {
        //        _traceService.LogMessage($"Result JSON - {JsonConvert.SerializeObject(result)}");
        //        result.Trace = await _traceService.GetTraceFile($"GetDataPage-{entityForm.TransId}");
        //    }
        //    return result;
        //}

        public async Task<string> GetEntityNameAsync(string ftransid, DataTable dataTable)
        {
            var result = await Task.Run(() => dataTable.AsEnumerable()
            .FirstOrDefault(row => row.Field<string>("ftransid") == ftransid)?.Field<string>("fcaption"));

            return result;
        }


        private async Task<string> GetFilterString(Entity entity, SQLResult metaData)
        {
            string[] formFields = { "modifiedon", "modifiedon", "createdon", "createdby", "username" };


            List<string> filters = new List<string>();
            foreach (var filter in entity.Filters)
            {
                var filterString = string.Empty;
                var transId = filter["ftransid"].ToString();
                var fieldName = filter["fldname"].ToString();

                if (Array.IndexOf(formFields, fieldName.ToLower()) > -1)
                {
                    switch (fieldName)
                    {
                        case "modifiedon":
                        case "createdon":
                            {
                                var fldData = metaData.data.AsEnumerable().FirstOrDefault(row => row.Field<string>("ftransid") == transId && row.Field<string>("dcname") == "dc1");
                                filterString += $"{fieldName}~F~~~d~F~{fldData["tablename"].ToString() ?? ""}|";

                                string fromdate = filter["from"]?.ToString() ?? "";
                                string todate = filter["to"]?.ToString() ?? "";
                                if (!string.IsNullOrEmpty(fromdate) && !string.IsNullOrEmpty(todate))
                                    filterString += $"between '{fromdate}' and '{todate}'";
                                else if (!string.IsNullOrEmpty(fromdate) && string.IsNullOrEmpty(todate))
                                    filterString += $">= '{fromdate}'";
                                else if (string.IsNullOrEmpty(fromdate) && !string.IsNullOrEmpty(todate))
                                    filterString += $"<= '{todate}'";
                            }
                            break;
                        case "username":
                        case "createdby":
                            {
                                var fldData = metaData.data.AsEnumerable().FirstOrDefault(row => row.Field<string>("ftransid") == transId && row.Field<string>("dcname") == "dc1");
                                filterString += $"{fieldName}~F~~~c~F~{fldData["tablename"].ToString() ?? ""}|";

                                string jsonString = System.Text.Json.JsonSerializer.Serialize(filter["value"]);
                                List<string> list = System.Text.Json.JsonSerializer.Deserialize<List<string>>(jsonString);
                                var values = list.Select(v => $"'{v}'").ToList();
                                filterString += $"in ({string.Join(",", values.ConvertAll(s => s.ToLower()))})";
                            }
                            break;

                        default:
                            break;
                    }
                }
                else
                {
                    var fldData = metaData.data.AsEnumerable().FirstOrDefault(row => row.Field<string>("ftransid") == transId && row.Field<string>("fldname") == fieldName);
                    if (fldData != null)
                    {
                        filterString += $"{fieldName}~{fldData["normalized"].ToString()}~{fldData["srctable"].ToString() ?? ""}~{fldData["srcfield"].ToString() ?? ""}~{fldData["fdatatype"].ToString() ?? ""}~{fldData["listingfld"].ToString() ?? ""}~{fldData["tablename"].ToString() ?? ""}|";
                        var dataType = filter["datatype"]?.ToString()?.ToUpper() ?? "TEXT";
                        switch (dataType.ToUpper())
                        {
                            case "TEXT":
                                string value = filter["value"]?.ToString()?.ToLower() ?? "";
                                var condition = filter["condition"]?.ToString()?.ToUpper() ?? "EQUALS";
                                if (condition == "EQUALS")
                                    filterString += $"= '{value}'";
                                else if (condition == "STARTSWITH")
                                    filterString += $"like '{value}%'";
                                else if (condition == "ENDSWITH")
                                    filterString += $"like '%{value}'";
                                else if (condition == "CONTAINS")
                                    filterString += $"like '%{value}%'";
                                else
                                    filterString += $"= '{filter["value"]}'";
                                break;
                            case "DROPDOWN":
                                string jsonString = System.Text.Json.JsonSerializer.Serialize(filter["value"]);
                                List<string> list = System.Text.Json.JsonSerializer.Deserialize<List<string>>(jsonString);
                                var values = list.Select(v => $"'{v}'").ToList();
                                filterString += $"in ({string.Join(",", values.ConvertAll(s => s.ToLower()))})";
                                break;
                            case "NUMERIC":
                                string from = filter["from"]?.ToString() ?? "";
                                string to = filter["to"]?.ToString() ?? "";
                                if (!string.IsNullOrEmpty(from) && !string.IsNullOrEmpty(to))
                                    filterString += $"between {from} and {to}";
                                else if (!string.IsNullOrEmpty(from) && string.IsNullOrEmpty(to))
                                    filterString += $">= {from}";
                                else if (string.IsNullOrEmpty(from) && !string.IsNullOrEmpty(to))
                                    filterString += $"<= {to}";
                                break;
                            case "DATE":
                                string fromdate = filter["from"]?.ToString() ?? "";
                                string todate = filter["to"]?.ToString() ?? "";
                                if (!string.IsNullOrEmpty(fromdate) && !string.IsNullOrEmpty(todate))
                                    filterString += $"between '{fromdate}' and '{todate}'";
                                else if (!string.IsNullOrEmpty(fromdate) && string.IsNullOrEmpty(todate))
                                    filterString += $">= '{fromdate}'";
                                else if (string.IsNullOrEmpty(fromdate) && !string.IsNullOrEmpty(todate))
                                    filterString += $"<= '{todate}'";
                                break;
                            default:
                                filterString += $"= '{filter["value"]?.ToString()?.ToLower() ?? ""}'";
                                break;
                        }
                    }
                }

                if (!string.IsNullOrEmpty(filterString))
                    filters.Add(filterString);
            }

            return string.Join("^", filters) ?? "NA";

        }


        private string GetFieldString(string transId, string fieldsList, DataTable metaData)
        {
            fieldsList = fieldsList + ",";

            var result = string.Join("^",
            metaData.AsEnumerable()
                .Where(row => fieldsList.IndexOf(row.Field<string>("fldname") + ",") > -1 && row.Field<string>("ftransid") == transId)
                .GroupBy(row => row.Field<string>("tablename"))
                .Select(group => $"{group.Key}=" +
                    string.Join("|", group.Select(row => $"{row.Field<string>("fldname")}~{row.Field<string>("normalized")}~{row.Field<string>("srctable")}~{row.Field<string>("srcfield")}~{row.Field<string>("srcfield")}~{row.Field<string>("allowempty")}"))));

            return result;
        }


        //private async Task<string> GetDropdownFilterString(EntityFilter entityFilter)
        //{
        //    var entity = new Entity();
        //    entity.AppName = entityFilter.AppName;
        //    entity.SchemaName = entityFilter.SchemaName;
        //    entity.UserName = entityFilter.UserName;
        //    entity.TransId = entityFilter.TransId;
        //    entity.Language = entityFilter.Language;
        //    string[] formFields = { "modifiedon", "modifiedon", "createdon", "createdby", "username" };

        //    var filterString = "";
        //    List<string> filters = new List<string>();
        //    string fieldName = entityFilter.FieldId;
        //    var metaData = await GetEntityMetaData(entity);

        //    if (Array.IndexOf(formFields, fieldName.ToLower()) > -1)
        //    {
        //        switch (fieldName)
        //        {
        //            case "modifiedon":
        //            case "createdon":
        //            case "modifiedby":
        //            case "username":
        //            case "createdby":
        //                {
        //                    var fldData = metaData.data.AsEnumerable().FirstOrDefault(row => row.Field<string>("ftransid") == entity.TransId && row.Field<string>("dcname") == "dc1");
        //                    filterString = $"{fieldName}~F~{fldData["tablename"].ToString() ?? ""}~";
        //                }
        //                break;

        //            default:
        //                break;
        //        }
        //    }
        //    else
        //    {

        //        var fieldData = metaData.data.AsEnumerable()
        //                    .FirstOrDefault(row => row.Field<string>("fldname") == entityFilter.FieldId && row.Field<string>("ftransid") == entityFilter.TransId);

        //        if (fieldData != null)
        //        {
        //            if (fieldData["normalized"]?.ToString() == "T")
        //                filterString = $"{entityFilter.FieldId}~T~{fieldData["srctable"]}~{fieldData["srcfield"]}";
        //            else
        //                filterString = $"{entityFilter.FieldId}~F~{fieldData["tablename"]}~";
        //        }

        //    }

        //    return filterString;
        //}

        //public async Task<AnalyticsEntityOutput> GetAnalyticsPageLoadData(AnalyticsEntityInput analyticsInput)
        //{
        //    var result = new AnalyticsEntityOutput();
        //    var analyticsData = new AnalyticsData();
        //    analyticsData.AppName = analyticsInput.AppName;
        //    analyticsData.SchemaName = analyticsInput.SchemaName;
        //    analyticsData.UserName = analyticsInput.UserName;
        //    analyticsData.Page = analyticsInput.Page;
        //    analyticsData.PropertiesList = new List<string>();
        //    analyticsData.PropertiesList.Add("ENTITIES");
        //    var selectedEntites = await GetAnalyticsData(analyticsData); //Get Selected Entities
        //    var entity = new Entity();
        //    entity.AppName = analyticsInput.AppName;
        //    entity.SchemaName = analyticsInput.SchemaName;
        //    entity.Roles = analyticsInput.Roles;
        //    entity.Language = analyticsInput.Language;
        //    if (selectedEntites == null || selectedEntites.data == null || selectedEntites.data.Count == 0 || string.IsNullOrEmpty(selectedEntites.data["ENTITIES"]?.ToString()))
        //    {
        //        //Get All Entities List                
        //        entity.EntityName = "";
        //        var allEntities = await GetEntityList(entity);
        //        result.AllEntitiesList = allEntities.data;
        //    }
        //    else
        //    {
        //        //Get Selected Entities List
        //        entity.EntityName = selectedEntites.data["ENTITIES"].ToString();
        //        var selectedEntityList = await GetEntityList(entity);
        //        if (selectedEntityList == null || selectedEntityList.data == null || selectedEntityList.data.Rows.Count == 0)
        //        {
        //            //Get All Entities List                
        //            entity.EntityName = "";
        //            var allEntities = await GetEntityList(entity);
        //        }
        //        else
        //        {
        //            //Reorder the selected entities
        //            var orderArray = entity.EntityName.Split(',');
        //            selectedEntityList.data = selectedEntityList.data.AsEnumerable().OrderBy(row => Array.IndexOf(orderArray, row.Field<string>("name"))).CopyToDataTable();

        //            result.SelectedEntities = string.Join(",", selectedEntityList.data.AsEnumerable().Select(item => item["name"].ToString()).ToList());
        //            result.SelectedEntitiesList = selectedEntityList.data;

        //            var transId = selectedEntityList.data.Rows[0][0].ToString();
        //            entity.TransId = transId;
        //            entity.UserName = analyticsInput.UserName;
        //            var metaData = await GetEntityMetaData(entity);
        //            result.MetaData = metaData.data;

        //            analyticsData.PropertiesList = analyticsInput.PropertiesList;
        //            analyticsData.TransId = transId;
        //            var properties = await GetAnalyticsData(analyticsData); //Get Selected Entities
        //            result.TransId = transId;
        //            result.Properties = properties.data;
        //        }
        //    }
        //    return result;
        //}

        //public async Task<AnalyticsEntityOutput> GetAnalyticsEntityData(AnalyticsEntityInput analyticsInput)
        //{
        //    var result = new AnalyticsEntityOutput();
        //    var analyticsData = new AnalyticsData();
        //    analyticsData.AppName = analyticsInput.AppName;
        //    analyticsData.SchemaName = analyticsInput.SchemaName;
        //    analyticsData.UserName = analyticsInput.UserName;
        //    analyticsData.Page = analyticsInput.Page;
        //    analyticsData.PropertiesList = analyticsInput.PropertiesList;
        //    analyticsData.TransId = analyticsInput.TransId;

        //    var properties = await GetAnalyticsData(analyticsData);
        //    result.TransId = analyticsInput.TransId;
        //    result.Properties = properties.data;

        //    var entity = new Entity();
        //    entity.AppName = analyticsInput.AppName;
        //    entity.SchemaName = analyticsInput.SchemaName;
        //    entity.Roles = analyticsInput.Roles;
        //    entity.TransId = analyticsInput.TransId;
        //    entity.Language = analyticsInput.Language;
        //    entity.UserName = analyticsInput.UserName;
        //    var metaData = await GetEntityMetaData(entity);
        //    result.MetaData = metaData.data;

        //    return result;
        //}

        public FieldMetadata MapDataRowToMetadata(DataRow row)
        {
            FieldMetadata metadata = new FieldMetadata();
            foreach (var prop in typeof(FieldMetadata).GetProperties())
            {
                if (row.Table.Columns.Contains(prop.Name))
                {
                    prop.SetValue(metadata, row[prop.Name]?.ToString());
                }
            }
            return metadata;
        }


        public async Task<SQLResult> GetUserConfigDataFromDB(AnalyticsData data, string propertyNames, string userName)
        {
            Dictionary<string, string> config = await _common.GetDBConfigurations(data.AppName);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];

            string sql = Constants_SQL.GET_USERCONFIGDATA.ToString();

            if (sql.Contains("$KEYNAMES$"))
                sql = sql.Replace("$KEYNAMES$", $"{propertyNames}");

            string[] paramName = { "@page", "@struct", "@username" };
            DbType[] paramType = { DbType.String, DbType.String, DbType.String };
            object[] paramValue = { data.Page.ToUpper(), data.TransId?.ToUpper() ?? "", userName };

            var result = await GetData(dbType, sql, connectionString, paramName, paramType, paramValue);
            return result;
        }

        public async Task<NonQueryResult> DeleteUserConfigDataFromDB(AnalyticsData data, string propertyName, string userName)
        {
            Dictionary<string, string> config = await _common.GetDBConfigurations(data.AppName);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];

            string sql = Constants_SQL.DELETE_USERCONFIGDATA.ToString();

            string[] paramName = { "@page", "@struct", "@username", "@username", "@keyname" };
            DbType[] paramType = { DbType.String, DbType.String, DbType.String, DbType.String, DbType.String };
            object[] paramValue = { data.Page.ToUpper(), data.TransId?.ToUpper() ?? "", userName, userName, propertyName.ToUpper() };

            IDbHelper dbHelper = DBHelper.CreateDbHelper(dbType, _traceService);
            return await dbHelper.ExecuteNonQueryAsync(sql, connectionString, paramName, paramType, paramValue);
        }

        public async Task<NonQueryResult> InsertUserConfigDataInDB(AnalyticsData data, string userName, string propertyName, string value)
        {
            Dictionary<string, string> config = await _common.GetDBConfigurations(data.AppName);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];

            string sql = Constants_SQL.INSERT_USERCONFIGDATA.ToString();

            string[] paramName = { "@page", "@struct", "@keyname", "@username", "@value" };
            DbType[] paramType = { DbType.String, DbType.String, DbType.String, DbType.String, DbType.String };
            object[] paramValue = { data.Page.ToUpper(), data.TransId?.ToUpper() ?? "", propertyName.ToUpper(), userName, value };

            IDbHelper dbHelper = DBHelper.CreateDbHelper(dbType, _traceService);
            return await dbHelper.ExecuteNonQueryAsync(sql, connectionString, paramName, paramType, paramValue);
        }
    }
}

