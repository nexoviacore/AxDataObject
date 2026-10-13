using ARMCommon.Model;

namespace AxList
{
    public interface IAxListRepository
    {
        Task<SQLResult> GetListDataFromDB(AxList listData);
        Task<APIResult> GetListViewPermissionFromCache(AxList listData);
        Task<bool> StoreListViewPermissionToCache(AxList listData, APIResult data);
        Task<SQLResult> GetListViewPermissionFromDB(AxList listData);
    }
}
