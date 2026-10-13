using ARMCommon.Model;

namespace AxTstructData
{
    public interface IAxDeleteRepository
    {
        Task<SQLResult> GetDeletePermissionFromDB(AxDelete deleteData);
        Task<TransactionResult> ExecuteDeleteSQLCommands(AxDelete deleteData, List<SQLCommandBuilder> sqlCommands);
    }
}
