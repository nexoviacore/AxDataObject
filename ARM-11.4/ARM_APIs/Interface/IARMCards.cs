using ARM_APIs.Model;
using ARMCommon.Model;
using System.Data;

namespace ARM_APIs.Interface
{
    public interface ICardsService
    {
        abstract Task<SQLResult> GetCardsData(Cards cards);
        abstract Task<DataTable> GetProcessCards(ARMProcessFlowTask processFlow);

        abstract Task<DataTable> GetProcessCardsData(ARMProcessFlowTask processFlow, DataTable dtCards);

        abstract Task<NonQueryResult> AddorEditHTMLPlugin(HTMLPlugin plugin);
        abstract Task<SQLResult> GetHTMLPlugins(HTMLPlugin plugin);

        abstract Task<SQLResult> GetCardsWithData(Cards cards);
    }
}
