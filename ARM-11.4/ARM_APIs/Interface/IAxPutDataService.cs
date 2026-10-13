using ARM_APIs.Model;
using ARMCommon.Model;

namespace ARM_APIs.Interface
{
    public interface IAxPutDataService
    {
        Task<APIResult> AxPut(AxPutData tstructData);

        Task<APIResult> AxGetEditableFlag(AxEditFlag tstructData);
    }
}
