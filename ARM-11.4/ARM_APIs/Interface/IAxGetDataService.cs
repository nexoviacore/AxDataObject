using ARM_APIs.Model;
using ARMCommon.Model;

namespace ARM_APIs.Interface
{
    public interface IAxGetDataService
    {
        Task<APIResult> AxGet(AxGetData tstructData);
        Task<APIResult> AxList(AxListData tstructData);

        Task<APIResult> AxGetConnectedDataList(AxListData tstructData);
    }
}
