using ARMCommon.Model;

namespace AxList
{
    public interface IAxListService
    {
        Task<APIResult> AxListAsync(AxList listData);
        Task<APIResult> GetListViewPermission(AxList listData);
    }
}
