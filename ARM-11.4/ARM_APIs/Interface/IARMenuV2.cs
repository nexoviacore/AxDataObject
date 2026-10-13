using ARMCommon.Model;
using System.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
using ARM_APIs.Model;

namespace ARM_APIs.Interface
{
    public interface IARMenuV2
    {

      //abstract Task<ARMResult> ARMConnectionTestDetails();
      abstract Task<ARMResult> GetMenu(ARMSession model);
      abstract  Task<ARMResult> GetHomePageCards(ARMProcessFlowTask process);
      abstract  Task<DataTable> GETMENUFORDEFAULTROLE_V2(string sessionId);
       abstract Task<DataTable> GETMENUFOROTHERROLE_V2(string sessionId, string allRole);
       abstract Task<List<string>> GetallRole(string sessionId);
       abstract Task<DataTable> GetHomePage(string sessionId, string userName);

        abstract Task<SQLResult> GlobalSearch(GlobalSearch search);
    }
}
