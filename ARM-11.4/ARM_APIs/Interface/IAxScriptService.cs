using ARM_APIs.Model;
using ARMCommon.Model;

namespace ARM_APIs.Interface
{
    public interface IAxScriptService
    {
        Task<APIResult> AxExecuteScriptViaRESTDLLAsync(AxScriptRequest request);
    }
}
