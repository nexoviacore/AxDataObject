using ARMCommon.Model;

namespace AxTstructData
{
    public interface IAxPutService
    {
        Task<APIResult> AxPut(AxPutInput putData);

        Task<APIResult> CheckTstructCreateAccess(AxPut putData);
        Task<APIResult> CheckTstructEditAccess(AxPut putData);
    }
}
