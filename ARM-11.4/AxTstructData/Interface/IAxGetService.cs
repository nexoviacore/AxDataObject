using ARMCommon.Model;
using AxValidate;

namespace AxTstructData
{
    public interface IAxGetService
    {
        Task<APIResult> AxGet(AxGet getData);

        Task<APIResult> LoadData(AxGet getData, APIResult accessResult = null, Validate validate = null);
        Task<APIResult> CheckTstructViewAccess(AxGet getData);
    }
}
