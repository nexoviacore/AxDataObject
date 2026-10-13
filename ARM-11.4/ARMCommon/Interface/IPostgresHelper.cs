using ARMCommon.Model;
using NpgsqlTypes;
using System.Data;

namespace ARMCommon.Interface
{
    public interface IPostgresHelper
    {
        Task<DataTable> ExecuteSelectSql(string sql, string connectionString, ParamsDetails paramsDetails);
        Task<DataTable> ExecuteSql(string query, string connectionString, string[] paramName, NpgsqlDbType[] paramType, object[] paramValue);

    }
}
