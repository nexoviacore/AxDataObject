using ARMCommon.Model;

namespace AxTstructData
{
    public interface IAxGetRepository
    {
        Task<SQLResult> GetTstructViewPermissionFromDB(AxGet getData);

        Task<SQLResult> GetDCWiseSQLs(AxGet getData);
        Task<SQLResult> ExecuteDCWiseSQLs(Int64 recordId, string sql);
    }
}
