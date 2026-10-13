using ARMCommon.Model;
using System.Data;
using System.Data.Common;
namespace ARMCommon.Interface
{
    public interface IDbHelper
    {
        string ConnectionString { get; set; }
        DbTransaction Transaction { get; set; }
        Task OpenDBConnectionAsync();
        Task CloseDBConnectionAsync();
        string GetDBType();
        double SetDBConnectionTime(DateTime startTime, DateTime endTime);
        double SetDBTimeTaken(DateTime startTime, DateTime endTime);
        double GetDBConnectionTime();
        double GetDBTimeTaken();


        Task<DataTable> ExecuteQueryAsync(string query, string connectionString);
        Task<DataTable> ExecuteQueryAsync(string query, string connectionString, string[] paramName, DbType[] paramType, object[] paramValue);
        Task<SQLResult> ExecuteSQLAsync(string query, string connectionString, string[] paramName = null, DbType[] paramType = null, object[] paramValue = null, string getCountsSql = "");
        SQLResult ExecuteSQL(string query, string connectionString, string[] paramName = null, DbType[] paramType = null, object[] paramValue = null, string getCountsSql = "");

        Task<DataTable> ExecuteQueryAsync(string query, string connectionString, DBParamsDetails paramsDetails);
        Task<SQLDataSetResult> ExecuteSQLsFromSQLAsync(string query, string connectionString, string[] paramName = null, DbType[] paramType = null, object[] paramValue = null, Dictionary<string, string>? viewFilters = null, Dictionary<string, string>? globalParams = null, Dictionary<string, string>? sqlCommentsReplace = null, bool getAllRecordsCount = false);

        Task<NonQueryResult> ExecuteNonQueryAsync(string query, string connectionString, string[] paramName = null, DbType[] paramType = null, object[] paramValue = null);
        Task<NonQueryResult> ExecuteNonQueryAsync(string query, DbConnection connection, string[] paramName = null, DbType[] paramType = null, object[] paramValue = null);
        NonQueryResult ExecuteNonQuery(string query, string connectionString, string[] paramName = null, DbType[] paramType = null, object[] paramValue = null);

        Task<ScalarResult> ExecuteScalarAsync(string query, string connectionString, string[] paramName = null, DbType[] paramType = null, object[] paramValue = null);        
        Task<SQLResult> ExecuteStoredProcedureAsync(string storedProcedure, string connectionString, string[] paramName = null, DbType[] paramType = null, object[] paramValue = null);

        Task<SQLResult> ExecuteSQLAsync(string query, DbConnection connection, string[] paramName = null, DbType[] paramType = null, object[] paramValue = null, string getCountsSql = "");
        Task<ScalarResult> ExecuteScalarAsync(string query, DbConnection connection, string[] paramName = null, DbType[] paramType = null, object[] paramValue = null);

        ScalarResult ExecuteScalar(string query, string connectionString, string[] paramName = null, DbType[] paramType = null, object[] paramValue = null);

        #region Single Connection DB Helper Methods
        Task<SQLResult> ExecuteSQLAsync(string query, string[] paramName = null, DbType[] paramType = null, object[] paramValue = null);
        Task<SQLResult> ExecuteStoredProcedureAsync(string storedProcedure, string[] paramName = null, DbType[] paramType = null, object[] paramValue = null);
        Task<ScalarResult> ExecuteScalarAsync(string query, string[] paramName = null, DbType[] paramType = null, object[] paramValue = null);
        Task<NonQueryResult> ExecuteNonQueryAsync(string query,  string[] paramName = null, DbType[] paramType = null, object[] paramValue = null);
        Task<TransactionResult> ExecuteSQLsInTransactionAsync(List<SQLCommandBuilder> sqlCommands, bool commitTransaction = true);
        NonQueryResult CommitTransaction();
        NonQueryResult RollBackTransaction();
        Task<DataTable> ExecuteSqlSchemaAsync(string sqlQuery);
        #endregion
    }
}
