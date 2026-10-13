using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace ARMCommon.Interface
{
    public interface IOracleHelper
    {
        Task<DataTable> ExecuteSql(string query, string connectionString, string[] paramName, OracleDbType[] paramType, object[] paramValue);
    }
}
