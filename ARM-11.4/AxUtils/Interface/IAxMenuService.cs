using ARMCommon.Model;

namespace AxUtils
{
    public interface IAxMenuService
    {
        Task<ARMResult> AxGetMenuAsync(ARMSession menuData);
    }
}
