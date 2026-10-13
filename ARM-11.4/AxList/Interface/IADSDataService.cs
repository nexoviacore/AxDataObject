using ARMCommon.Model;

namespace AxList
{
    public interface IADSDataService
    {
        Task<ARMResult> GetDataFromDatasource(ADSDataRequest request);
        Task<ARMResult> GetADSDataAsync(ADSDataRequest request);
        Task<APIResult> GetADSPermission(string transId, string adsName, string userName, List<string> roles, string keyField, string keyValue);
        
        Task<APIResult> GetADSDataForAxList(AxList listData, string adsName, ADSDataRequest adsDataRequest = null);
    }
}
