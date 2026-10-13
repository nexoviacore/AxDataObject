using ARMCommon.Model;

namespace AxUtils
{
    public interface IAxInMemoryService
    {
        Task<AxInMemDataResult> AxReadInMemory(AxInMemReadData menuData);
        Task<AxInMemDataResult> AxWriteInMemory(AxInMemWriteData menuData);
        Task<AxInMemDeleteDataResult> AxDeleteInMemory(AxInMemDeleteData menuData);
    }
}
