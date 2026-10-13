using ARMCommon.Model;

namespace AxTstructData
{
    public interface IAxPutRepository
    {
        Task<SQLResult> GetCreatePermissionFromDB(AxPut putData);
        Task<SQLResult> GetEditPermissionFromDB(AxPut putData);
        Task<SQLResult> ExecuteDCWiseSQLs(Int64 recordId, string sql);
        //Task<SQLResult> GetRecordIdsFromDB(Int32 siteNo, Int32 rowsCount);
        Task<SQLResult> GetChildTransRecordId(string parentRecid, string table, string mapName);
        Task<TransactionResult> ExecuteSaveDataSQLCommands(AxPut putData, List<SQLCommandBuilder> sqlCommands);
    }
}
