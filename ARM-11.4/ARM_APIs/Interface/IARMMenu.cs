using ARM_APIs.Model;
using ARMCommon.Model;
using System.Data;

namespace ARM_APIs.Interface
{
    public interface IARMMenu
    {
        //abstract Task<DataTable> GetMenuV2ForDefaultRole(string sessionId);

        //abstract Task<DataTable> GetMenuV2ForOtherRoles(string sessionId, string allRole);

        
        abstract Task<DataTable> GetMenuForDefaultRole(string sessionId);
        abstract Task<DataTable> GetnamesForOtherRoles(string sessionId, string allRole);
        abstract  Task<DataTable> GetMenuForOtherRole(string sessionId, string names);
        abstract Task<DataTable> GetCardsList(string ARMSessionId, bool? IsHomePage);
        abstract Task<DataTable> GetCardListById(string sessionId, string cardId);

        Task<DataTable> GetCardSQL(string sessionId, string cardsql);
        abstract Task<List<string>> GetallRole(string sessionId);
        abstract Task<string> GetUserName(string sessionId);
        abstract Task<DataTable> GetProcessCards(ARMProcessFlowTask processFlow);
        abstract Task<DataTable> GetCardsData(ARMProcessFlowTask processFlow, DataTable dtCards);
        abstract string GenerateCardSql(DataTable cardresult);
        abstract Task<DataTable> GetHomePage(string sessionId, string userName);

    }
}
