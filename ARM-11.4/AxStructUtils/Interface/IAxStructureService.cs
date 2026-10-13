using ARMCommon.Model;

namespace AxStructUtils
{
    public interface IAxStructureService
    {
        Task<APIResult> AxGetStructure(AxStructure getData);
    }
}
