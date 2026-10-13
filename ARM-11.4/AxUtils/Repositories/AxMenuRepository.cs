using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMCommon.Model;

namespace AxUtils
{
    public class AxMenuRepository : IAxMenuRepository
    {
        private readonly Utils _common;
        private readonly IARMLogTraceService _traceService;

        public AxMenuRepository(Utils common, IARMLogTraceService traceService)
        {
            _common = common;
            _traceService = traceService;
        }

        public async Task<SQLResult> GetMenuFromDB(bool defaultRole, string allResponsibilities)
        {
            if (_common._dbHelper == null)
                await _common.OpenDBConnectionAsync();            
            
            string sql = "";
            if (defaultRole)            
                sql = Constants_SQL.GETMENU_DEFAULTROLE.ToString();                           
            else
                sql = Constants_SQL.GETMENU_OTHERROLES.ToString().Replace("$allResponsibilities$", allResponsibilities);

            return await _common._dbHelper.ExecuteSQLAsync(sql);
        }

        public async Task<SQLResult> GetListViewAsDefaultStructs()
        {
            if (_common._dbHelper == null)
                await _common.OpenDBConnectionAsync();

            string sql = Constants_SQL.GETMENU_GET_LISTVIEWASDEFAULT;
            
            return await _common._dbHelper.ExecuteSQLAsync(sql);
        }

    }
}
