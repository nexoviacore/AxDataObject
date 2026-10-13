using ARMCommon.Model;

namespace AxList
{
    public interface IADSDataRepository
    {
        Task<ADSDataSource> GetADSDefinitionAsync(string schemaName, string dataSourceName);
        Task<bool> ValidateUserAccess(string userRoles, string requiredRoles);
        Task<string> GetADSDataFromCacheAsync(string cacheKey);
        Task<bool> SaveADSDataToCacheAsync(string cacheKey, object data, int expiryMinutes);
        string GenerateDataCacheKey(string schemaName, string dataSourceName, Dictionary<string, string> parameters);
        Task<SQLResult> GetADSDataFromDB(string sqlText, Dictionary<string, string> parameters);
        Task<SQLResult> GetADSDataViewPermissionFromDB(string transId, string adsName, string userName, List<string> roles, string keyField, string keyValue);
        Task<string> GetADSSQLForAxList(AxList listData, string adsName, string constraint = "NA");
        Task<Int32> GetADSDataTotalCountFromDB(string sqlText, Dictionary<string, string> parameters);

    }
}
