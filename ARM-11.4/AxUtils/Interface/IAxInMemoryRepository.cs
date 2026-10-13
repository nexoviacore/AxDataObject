using ARMCommon.Model;

namespace AxUtils.Interface
{
    public interface IAxInMemoryRepository
    {
        Task<AxInMemDataResult> AxReadInMemory(AxInMemReadData menuData);
        Task<AxInMemDataResult> AxWriteInMemory(AxInMemWriteData menuData);
        Task<AxInMemDeleteDataResult> AxDeleteInMemory(AxInMemDeleteData menuData);
    }
}
