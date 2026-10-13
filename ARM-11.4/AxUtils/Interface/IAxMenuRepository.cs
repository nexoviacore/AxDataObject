using ARMCommon.Model;

namespace AxUtils
{
    public interface IAxMenuRepository
    {
        Task<SQLResult> GetMenuFromDB(bool defaultRole, string allResponsibilities);
        Task<SQLResult> GetListViewAsDefaultStructs();

    }
}
