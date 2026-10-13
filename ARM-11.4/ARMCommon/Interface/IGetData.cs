using ARMCommon.Model;

namespace ARMCommon.Interface
{
    public interface IGetData 
    {
        //abstract Task<object> GetDataSourceData(string appName, string datasource, Dictionary<string, string> sqlParams);
        abstract Task<Dictionary<string, string>> GetLoginUser(string ARMSessionId);

        abstract Task<SQLResult> GetDataSourceData(string appName, string datasource, Dictionary<string, string> sqlParams = null);

        abstract Task<SQLResult> GetDataFromDatasource(ARMGetDataRequest data);
        abstract Task<SQLDataSetResult> GetDataFromADS(ARMGetDataRequest data);
    }
}
