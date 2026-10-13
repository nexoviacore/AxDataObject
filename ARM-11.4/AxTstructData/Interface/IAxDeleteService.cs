using ARMCommon.Model;

namespace AxTstructData
{
    public interface IAxDeleteService
    {
        Task<APIResult> AxDelete(AxDelete deleteData);
    }
}
