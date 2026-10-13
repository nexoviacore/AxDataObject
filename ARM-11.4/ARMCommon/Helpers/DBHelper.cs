using ARMCommon.Interface;
using ARMCommon.Model;
using Newtonsoft.Json;
using Npgsql;
using NpgsqlTypes;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.Text.RegularExpressions;
using System.Text;
using System.Data.SqlClient;
using System.Data.Common;
namespace ARMCommon.Helpers
{
    public class DBHelper
    {
        public class OracleHelper : IDbHelper
        {
            private readonly string _query;
            private string _connectionString;
            private readonly string[] _paramName;
            private readonly DbType[] _paramType;
            private readonly object[] _paramValue;
            private readonly IARMLogTraceService _traceService;
            private readonly bool _traceEnabled;
            public double _dbConnTime = 0;
            public double _dbTimeTaken = 0;
            private OracleConnection _connection;
            private OracleTransaction _transaction;

            public OracleHelper(string query, string connectionString, string[] paramName, DbType[] paramType, object[] paramValue, IARMLogTraceService traceService = null)
            {
                _query = query;
                _connectionString = connectionString;
                _paramName = paramName;
                _paramType = paramType;
                _paramValue = paramValue;
                _traceService = traceService;

                if (_traceService != null && _traceService.IsTracingEnabled())
                    _traceEnabled = true;
            }

            public OracleHelper(IARMLogTraceService traceService = null)
            {
                _traceService = traceService;

                if (_traceService != null && _traceService.IsTracingEnabled())
                    _traceEnabled = true;
            }

            public async Task OpenDBConnectionAsync()
            {
                DateTime startTime = DateTime.Now;
                _connection = new OracleConnection(_connectionString);
                await _connection.OpenAsync();
                if (_connection.State == ConnectionState.Open)
                    SetDBConnectionTime(startTime, DateTime.Now);
            }

            public async Task CloseDBConnectionAsync()
            {
                await _connection.CloseAsync();
            }

            public string ConnectionString
            {
                get { return _connectionString; }
                set { _connectionString = value; }
            }

            public DbTransaction Transaction
            {
                get { return (DbTransaction)_transaction; }
                set { _transaction = (OracleTransaction)value; }
            }

            public string GetDBType()
            {
                return Constants.DBTYPE.ORACLE.ToString();
            }

            public double SetDBConnectionTime(DateTime startTime, DateTime endTime)
            {
                TimeSpan duration = endTime - startTime;
                _dbConnTime += duration.TotalMilliseconds;

                return _dbConnTime;
            }
            public double SetDBTimeTaken(DateTime startTime, DateTime endTime)
            {
                TimeSpan duration = endTime - startTime;
                _dbTimeTaken += duration.TotalMilliseconds;

                return _dbTimeTaken;
            }

            public double GetDBConnectionTime()
            {
                return _dbConnTime;
            }
            public double GetDBTimeTaken()
            {
                return _dbTimeTaken;
            }



            public string ReplaceSqlParameters(string sql, Dictionary<string, string> parameters)
            {
                Regex regex = new Regex(@"\:\w+");
                StringBuilder sb = new StringBuilder(sql);
                MatchCollection matches = regex.Matches(sql);

                // Create a case-insensitive dictionary lookup
                var caseInsensitiveParams = new Dictionary<string, string>(parameters, StringComparer.OrdinalIgnoreCase);

                // Iterate in reverse to avoid messing up string indexes
                for (int i = matches.Count - 1; i >= 0; i--)
                {
                    Match match = matches[i];
                    string paramKey = match.Value.Substring(1); // Remove leading ':'

                    if (caseInsensitiveParams.TryGetValue(paramKey, out string value))
                    {
                        sb.Remove(match.Index, match.Length);
                        sb.Insert(match.Index, "'" + value.Replace("'", "''") + "'"); // Escape single quotes for SQL
                    }
                }

                return sb.ToString();
            }

            public string ReplaceSqlComments(string sql, Dictionary<string, string> comments)
            {
                foreach (var entry in comments)
                {
                    sql = Regex.Replace(sql, Regex.Escape(entry.Key), entry.Value, RegexOptions.IgnoreCase);
                }
                return sql;
            }

            public async Task<DataTable> ExecuteQueryAsync(string query, string connectionString)
            {
                DataTable dt = new DataTable();
                if (query.Trim().EndsWith(";"))
                    query = query.Trim().Substring(0, query.Trim().Length - 1);
                try
                {
                    if (_traceEnabled) _traceService.LogDbConnection(connectionString);
                    DateTime startTime = DateTime.Now;

                    using (var connection = new OracleConnection(connectionString))
                    {
                        await connection.OpenAsync();
                        query = query.Replace("@", ":");
                        using (var cmd = new OracleCommand(query, connection))
                        {
                            if (_traceEnabled) _traceService.LogSqlExecution(query);
                            try
                            {
                                cmd.Prepare();
                                var dr = await cmd.ExecuteReaderAsync();
                                dt.Load(dr);
                                dt = ChangeColumnNamesToLowerCase(dt);

                                DateTime endTime = DateTime.Now;
                                if (_traceEnabled) _traceService.LogDataTable(dt, startTime, endTime); ;

                            }
                            catch (Exception ex)
                            {
                                dt.Rows.Add(ex.Message);
                                string error = ex.Message;
                                error += Environment.NewLine + ex.InnerException;
                                error += Environment.NewLine + ex.StackTrace;

                                if (_traceEnabled) _traceService.LogError(error);
                            }
                            finally
                            {
                                await connection.CloseAsync();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    dt.Rows.Add(ex.Message);
                    string error = ex.Message;
                    error += Environment.NewLine + ex.InnerException;
                    error += Environment.NewLine + ex.StackTrace;

                    if (_traceEnabled) _traceService.LogError(error);
                }

                return dt;
            }

            public async Task<NonQueryResult> ExecuteNonQueryAsync(string query, string connectionString, string[] paramName = null, DbType[] paramType = null, object[] paramValue = null)
            {
                var result = new NonQueryResult();
                if (query.Trim().EndsWith(";"))
                    query = query.Trim().Substring(0, query.Trim().Length - 1);
                try
                {
                    if (_traceEnabled) _traceService.LogDbConnection(connectionString);
                    DateTime startTime = DateTime.Now;

                    using (var connection = new OracleConnection(connectionString))
                    {
                        if (_traceEnabled) _traceService.LogSqlExecution(query, paramName, paramType, paramValue);

                        await connection.OpenAsync();
                        query = query.Replace("@", ":");
                        using (var cmd = new OracleCommand(query, connection))
                        {
                            if (paramName != null)
                            {
                                if (paramName.Length != paramValue.Length || paramName.Length != paramType.Length)
                                {
                                    result.error = "Invalid params in SQL";
                                    if (_traceEnabled) _traceService.LogError(result.error);
                                }

                                for (int i = 0; i < paramName.Length; i++)
                                {
                                    cmd.Parameters.Add(paramName[i], GetOracleDbType(paramType[i])).Value = paramValue[i];
                                }
                            }

                            try
                            {
                                cmd.Prepare();
                                result.count = await cmd.ExecuteNonQueryAsync();
                                DateTime endTime = DateTime.Now;
                                if (_traceEnabled) _traceService.LogResultInfo(result.count, startTime, endTime);

                            }
                            catch (Exception ex)
                            {
                                result.error = ex.Message;

                                string error = ex.Message;
                                error += Environment.NewLine + ex.InnerException;
                                error += Environment.NewLine + ex.StackTrace;

                                if (_traceEnabled) _traceService.LogError(error);

                            }
                            finally
                            {
                                await connection.CloseAsync();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    result.error = ex.Message;

                    string error = ex.Message;
                    error += Environment.NewLine + ex.InnerException;
                    error += Environment.NewLine + ex.StackTrace;

                    if (_traceEnabled) _traceService.LogError(error);

                }

                return result;
            }
            public async Task<NonQueryResult> ExecuteNonQueryAsync(string query, DbConnection connection, string[] paramName = null, DbType[] paramType = null, object[] paramValue = null)
            {
                var result = new NonQueryResult();
                if (query.Trim().EndsWith(";"))
                    query = query.Trim().Substring(0, query.Trim().Length - 1);
                try
                {
                    if (_traceEnabled) _traceService.LogDbConnection(connection.ConnectionString);
                    DateTime startTime = DateTime.Now;

                    //using (var connection = new OracleConnection(connectionString))
                    {
                        if (_traceEnabled) _traceService.LogSqlExecution(query, paramName, paramType, paramValue);

                        if (connection?.State != ConnectionState.Open)
                        {
                            await connection.OpenAsync();
                        }

                        query = query.Replace("@", ":");
                        using (var cmd = new OracleCommand(query, (OracleConnection)connection))
                        {
                            if (paramName != null)
                            {
                                if (paramName.Length != paramValue.Length || paramName.Length != paramType.Length)
                                {
                                    result.error = "Invalid params in SQL";
                                    if (_traceEnabled) _traceService.LogError(result.error);
                                }

                                for (int i = 0; i < paramName.Length; i++)
                                {
                                    cmd.Parameters.Add(paramName[i], GetOracleDbType(paramType[i])).Value = paramValue[i];
                                }
                            }

                            try
                            {
                                cmd.Prepare();
                                result.count = await cmd.ExecuteNonQueryAsync();
                                DateTime endTime = DateTime.Now;
                                if (_traceEnabled) _traceService.LogResultInfo(result.count, startTime, endTime);

                            }
                            catch (Exception ex)
                            {
                                result.error = ex.Message;

                                string error = ex.Message;
                                error += Environment.NewLine + ex.InnerException;
                                error += Environment.NewLine + ex.StackTrace;

                                if (_traceEnabled) _traceService.LogError(error);

                            }
                            //finally
                            //{
                            //    await connection.CloseAsync();
                            //}
                        }
                    }
                }
                catch (Exception ex)
                {
                    result.error = ex.Message;

                    string error = ex.Message;
                    error += Environment.NewLine + ex.InnerException;
                    error += Environment.NewLine + ex.StackTrace;

                    if (_traceEnabled) _traceService.LogError(error);

                }

                return result;
            }

            public async Task<NonQueryResult> ExecuteNonQueryAsync(string query, string[] paramName = null, DbType[] paramType = null, object[] paramValue = null)
            {
                var result = new NonQueryResult();
                if (query.Trim().EndsWith(";"))
                    query = query.Trim().Substring(0, query.Trim().Length - 1);
                try
                {
                    DateTime startTime = DateTime.Now;
                    if (_traceEnabled) _traceService.LogSqlExecution(query, paramName, paramType, paramValue);

                    if (_connection == null || _connection.State != ConnectionState.Open)
                        await OpenDBConnectionAsync();

                    query = query.Replace("@", ":");
                    using var cmd = new OracleCommand(query, _connection);
                    cmd.BindByName = true;

                    if (paramName != null)
                    {
                        if (paramName.Length != paramValue.Length || paramName.Length != paramType.Length)
                        {
                            result.error = "Invalid params in SQL";
                            if (_traceEnabled) _traceService.LogError(result.error);
                        }

                        for (int i = 0; i < paramName.Length; i++)
                        {
                            cmd.Parameters.Add(paramName[i].TrimStart('@', ':'), GetOracleDbType(paramType[i])).Value = paramValue[i];
                        }
                    }

                    try
                    {
                        cmd.Prepare();
                        result.count = await cmd.ExecuteNonQueryAsync();
                        DateTime endTime = DateTime.Now;
                        if (_traceEnabled) _traceService.LogResultInfo(result.count, startTime, endTime);

                    }
                    catch (Exception ex)
                    {
                        result.error = ex.Message;

                        string error = ex.Message;
                        error += Environment.NewLine + ex.InnerException;
                        error += Environment.NewLine + ex.StackTrace;

                        if (_traceEnabled) _traceService.LogError(error);

                    }
                }
                catch (Exception ex)
                {
                    result.error = ex.Message;

                    string error = ex.Message;
                    error += Environment.NewLine + ex.InnerException;
                    error += Environment.NewLine + ex.StackTrace;

                    if (_traceEnabled) _traceService.LogError(error);

                }

                return result;
            }

            public async Task<ScalarResult> ExecuteScalarAsync(string query, string connectionString, string[] paramName = null, DbType[] paramType = null, object[] paramValue = null)
            {
                var result = new ScalarResult();
                if (query.Trim().EndsWith(";"))
                    query = query.Trim().Substring(0, query.Trim().Length - 1);
                try
                {
                    if (_traceEnabled) _traceService.LogDbConnection(connectionString);
                    DateTime startTime = DateTime.Now;

                    using (var connection = new OracleConnection(connectionString))
                    {
                        if (_traceEnabled) _traceService.LogSqlExecution(query, paramName, paramType, paramValue);

                        await connection.OpenAsync();
                        query = query.Replace("@", ":");
                        using (var cmd = new OracleCommand(query, connection))
                        {
                            if (paramName != null)
                            {
                                if (paramName.Length != paramValue.Length || paramName.Length != paramType.Length)
                                {
                                    result.error = "Invalid params in SQL";
                                    if (_traceEnabled) _traceService.LogError(result.error);
                                }

                                for (int i = 0; i < paramName.Length; i++)
                                {
                                    cmd.Parameters.Add(paramName[i], GetOracleDbType(paramType[i])).Value = paramValue[i];
                                }
                            }

                            try
                            {
                                cmd.Prepare();
                                result.result = await cmd.ExecuteScalarAsync();
                                DateTime endTime = DateTime.Now;
                                if (_traceEnabled) _traceService.LogScalarResultInfo(result.result?.ToString() ?? "", startTime, endTime);

                            }
                            catch (Exception ex)
                            {
                                result.error = ex.Message;

                                string error = ex.Message;
                                error += Environment.NewLine + ex.InnerException;
                                error += Environment.NewLine + ex.StackTrace;

                                if (_traceEnabled) _traceService.LogError(error);

                            }
                            finally
                            {
                                await connection.CloseAsync();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    result.error = ex.Message;

                    string error = ex.Message;
                    error += Environment.NewLine + ex.InnerException;
                    error += Environment.NewLine + ex.StackTrace;

                    if (_traceEnabled) _traceService.LogError(error);

                }

                return result;
            }

            public async Task<ScalarResult> ExecuteScalarAsync(string query, string[] paramName = null, DbType[] paramType = null, object[] paramValue = null)
            {
                var result = new ScalarResult();
                if (query.Trim().EndsWith(";"))
                    query = query.Trim().Substring(0, query.Trim().Length - 1);
                try
                {
                    DateTime startTime = DateTime.Now;

                    if (_traceEnabled) _traceService.LogSqlExecution(query, paramName, paramType, paramValue);

                    if (_connection == null || _connection.State != ConnectionState.Open)
                        await OpenDBConnectionAsync();

                    query = query.Replace("@", ":");
                    using var cmd = new OracleCommand(query, _connection);
                    cmd.BindByName = true;

                    if (paramName != null)
                    {
                        if (paramName.Length != paramValue.Length || paramName.Length != paramType.Length)
                        {
                            result.error = "Invalid params in SQL";
                            if (_traceEnabled) _traceService.LogError(result.error);
                        }

                        for (int i = 0; i < paramName.Length; i++)
                        {
                            if (paramType[i] == DbType.DateTime || paramType[i] == DbType.Date)
                            {
                                if (string.IsNullOrEmpty(paramValue[i]?.ToString()))
                                {
                                    paramValue[i] = DBNull.Value;
                                }
                                else
                                    paramType[i] = DbType.String;
                            }

                            cmd.Parameters.Add(paramName[i].TrimStart('@', ':'), GetOracleDbType(paramType[i])).Value = paramValue[i];
                        }
                    }

                    try
                    {
                        cmd.Prepare();
                        result.result = await cmd.ExecuteScalarAsync();
                        DateTime endTime = DateTime.Now;
                        if (_traceEnabled) _traceService.LogScalarResultInfo(result.result?.ToString() ?? "", startTime, endTime);

                    }
                    catch (Exception ex)
                    {
                        result.error = ex.Message;

                        string error = ex.Message;
                        error += Environment.NewLine + ex.InnerException;
                        error += Environment.NewLine + ex.StackTrace;

                        if (_traceEnabled) _traceService.LogError(error);

                    }


                }
                catch (Exception ex)
                {
                    result.error = ex.Message;

                    string error = ex.Message;
                    error += Environment.NewLine + ex.InnerException;
                    error += Environment.NewLine + ex.StackTrace;

                    if (_traceEnabled) _traceService.LogError(error);

                }

                return result;
            }


            public NonQueryResult ExecuteNonQuery(string query, string connectionString, string[] paramName = null, DbType[] paramType = null, object[] paramValue = null)
            {
                var result = new NonQueryResult();
                if (query.Trim().EndsWith(";"))
                    query = query.Trim().Substring(0, query.Trim().Length - 1);
                try
                {
                    if (_traceEnabled) _traceService.LogDbConnection(connectionString);
                    DateTime startTime = DateTime.Now;

                    using (var connection = new OracleConnection(connectionString))
                    {
                        if (_traceEnabled) _traceService.LogSqlExecution(query, paramName, paramType, paramValue);

                        connection.Open();
                        query = query.Replace("@", ":");
                        using (var cmd = new OracleCommand(query, connection))
                        {
                            if (paramName != null)
                            {
                                if (paramName.Length != paramValue.Length || paramName.Length != paramType.Length)
                                {
                                    result.error = "Invalid params in SQL";
                                    if (_traceEnabled) _traceService.LogError(result.error);
                                }

                                for (int i = 0; i < paramName.Length; i++)
                                {
                                    cmd.Parameters.Add(paramName[i], GetOracleDbType(paramType[i])).Value = paramValue[i];
                                }
                            }

                            try
                            {
                                cmd.Prepare();
                                result.count = cmd.ExecuteNonQuery();
                                DateTime endTime = DateTime.Now;
                                if (_traceEnabled) _traceService.LogResultInfo(result.count, startTime, endTime);

                            }
                            catch (Exception ex)
                            {
                                result.error = ex.Message;

                                string error = ex.Message;
                                error += Environment.NewLine + ex.InnerException;
                                error += Environment.NewLine + ex.StackTrace;

                                if (_traceEnabled) _traceService.LogError(error);

                            }
                            finally
                            {
                                connection.Close();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    result.error = ex.Message;

                    string error = ex.Message;
                    error += Environment.NewLine + ex.InnerException;
                    error += Environment.NewLine + ex.StackTrace;

                    if (_traceEnabled) _traceService.LogError(error);

                }

                return result;
            }

            public ScalarResult ExecuteScalar(string query, string connectionString, string[] paramName = null, DbType[] paramType = null, object[] paramValue = null)
            {
                var result = new ScalarResult();
                if (query.Trim().EndsWith(";"))
                    query = query.Trim().Substring(0, query.Trim().Length - 1);
                try
                {
                    if (_traceEnabled) _traceService.LogDbConnection(connectionString);
                    DateTime startTime = DateTime.Now;

                    using (var connection = new OracleConnection(connectionString))
                    {
                        if (_traceEnabled) _traceService.LogSqlExecution(query, paramName, paramType, paramValue);

                        connection.Open();
                        query = query.Replace("@", ":");
                        using (var cmd = new OracleCommand(query, connection))
                        {
                            if (paramName != null)
                            {
                                if (paramName.Length != paramValue.Length || paramName.Length != paramType.Length)
                                {
                                    result.error = "Invalid params in SQL";
                                    if (_traceEnabled) _traceService.LogError(result.error);
                                }

                                for (int i = 0; i < paramName.Length; i++)
                                {
                                    cmd.Parameters.Add(paramName[i], GetOracleDbType(paramType[i])).Value = paramValue[i];
                                }
                            }

                            try
                            {
                                cmd.Prepare();
                                result.result = cmd.ExecuteScalar();
                                DateTime endTime = DateTime.Now;
                                if (_traceEnabled) _traceService.LogScalarResultInfo(result.result?.ToString() ?? "", startTime, endTime);

                            }
                            catch (Exception ex)
                            {
                                result.error = ex.Message;

                                string error = ex.Message;
                                error += Environment.NewLine + ex.InnerException;
                                error += Environment.NewLine + ex.StackTrace;

                                if (_traceEnabled) _traceService.LogError(error);

                            }
                            finally
                            {
                                connection.Close();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    result.error = ex.Message;

                    string error = ex.Message;
                    error += Environment.NewLine + ex.InnerException;
                    error += Environment.NewLine + ex.StackTrace;

                    if (_traceEnabled) _traceService.LogError(error);

                }

                return result;
            }

            public async Task<ScalarResult> ExecuteScalarAsync(string query, DbConnection connection, string[] paramName = null, DbType[] paramType = null, object[] paramValue = null)
            {
                var result = new ScalarResult();
                if (query.Trim().EndsWith(";"))
                    query = query.Trim().Substring(0, query.Trim().Length - 1);
                try
                {
                    //                    if (_traceEnabled) _traceService.LogDbConnection(connectionString);                    

                    //using (var connection = new OracleConnection(connectionString))
                    {
                        if (_traceEnabled) _traceService.LogSqlExecution(query, paramName, paramType, paramValue);

                        DateTime startTime = DateTime.Now;

                        if (connection?.State != ConnectionState.Open)
                        {
                            await connection.OpenAsync();
                        }

                        query = query.Replace("@", ":");
                        using (var cmd = new OracleCommand(query, (OracleConnection)connection))
                        {
                            if (paramName != null)
                            {
                                if (paramName.Length != paramValue.Length || paramName.Length != paramType.Length)
                                {
                                    result.error = "Invalid params in SQL";
                                    if (_traceEnabled) _traceService.LogError(result.error);
                                }

                                for (int i = 0; i < paramName.Length; i++)
                                {
                                    cmd.Parameters.Add(paramName[i], GetOracleDbType(paramType[i])).Value = paramValue[i];
                                }
                            }

                            try
                            {
                                cmd.Prepare();
                                result.result = await cmd.ExecuteScalarAsync();
                                DateTime endTime = DateTime.Now;
                                SetDBTimeTaken(startTime, endTime);

                                if (_traceEnabled) _traceService.LogScalarResultInfo(result.result?.ToString() ?? "", startTime, endTime);
                            }
                            catch (Exception ex)
                            {
                                result.error = ex.Message;

                                string error = ex.Message;
                                error += Environment.NewLine + ex.InnerException;
                                error += Environment.NewLine + ex.StackTrace;

                                if (_traceEnabled) _traceService.LogError(error);

                            }
                            //finally
                            //{
                            //    await connection.CloseAsync();
                            //}
                        }
                    }
                }
                catch (Exception ex)
                {
                    result.error = ex.Message;

                    string error = ex.Message;
                    error += Environment.NewLine + ex.InnerException;
                    error += Environment.NewLine + ex.StackTrace;

                    if (_traceEnabled) _traceService.LogError(error);

                }

                return result;
            }

            public async Task<DataTable> ExecuteQueryAsync(string query, string connectionString, string[] paramName, DbType[] paramType, object[] paramValue)
            {
                DataTable dt = new DataTable();
                if (query.Trim().EndsWith(";"))
                    query = query.Trim().Substring(0, query.Trim().Length - 1);
                try
                {
                    if (_traceEnabled) _traceService.LogDbConnection(connectionString);
                    DateTime startTime = DateTime.Now;

                    using (var connection = new OracleConnection(connectionString))
                    {
                        if (_traceEnabled) _traceService.LogSqlExecution(query, paramName, paramType, paramValue);

                        await connection.OpenAsync();
                        query = query.Replace("@", ":");
                        using (var cmd = new OracleCommand(query, connection))
                        {
                            if (paramName != null)
                            {
                                if (paramName.Length != paramValue.Length || paramName.Length != paramType.Length)
                                {
                                    dt.Rows.Add("Invalid params in SQL");
                                    if (_traceEnabled) _traceService.LogError("Invalid params in SQL");
                                }
                                for (int i = 0; i < paramName.Length; i++)
                                {
                                    cmd.Parameters.Add(paramName[i], GetOracleDbType(paramType[i])).Value = paramValue[i];
                                }
                            }

                            try
                            {
                                cmd.Prepare();
                                var dr = await cmd.ExecuteReaderAsync();
                                dt.Load(dr);
                                dt = ChangeColumnNamesToLowerCase(dt);
                                DateTime endTime = DateTime.Now;
                                if (_traceEnabled) _traceService.LogDataTable(dt, startTime, endTime); ;


                            }
                            catch (Exception ex)
                            {
                                dt.Rows.Add(ex.Message);
                                string error = ex.Message;
                                error += Environment.NewLine + ex.InnerException;
                                error += Environment.NewLine + ex.StackTrace;

                                if (_traceEnabled) _traceService.LogError(error);

                            }
                            finally
                            {
                                await connection.CloseAsync();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    dt.Rows.Add(ex.Message);
                    string error = ex.Message;
                    error += Environment.NewLine + ex.InnerException;
                    error += Environment.NewLine + ex.StackTrace;

                    if (_traceEnabled) _traceService.LogError(error);
                }

                return dt;
            }

            public async Task<SQLResult> ExecuteSQLAsync(string query, string connectionString, string[] paramName = null, DbType[] paramType = null, object[] paramValue = null, string getCountsSql = "")
            {
                SQLResult sqlResult = new SQLResult();
                if (query.Trim().EndsWith(";"))
                    query = query.Trim().Substring(0, query.Trim().Length - 1);
                try
                {
                    if (_traceEnabled) _traceService.LogDbConnection(connectionString);
                    DateTime startTime = DateTime.Now;

                    using (var connection = new OracleConnection(connectionString))
                    {
                        if (_traceEnabled) _traceService.LogSqlExecution(query, paramName, paramType, paramValue);

                        await connection.OpenAsync();
                        query = query.Replace("@", ":");
                        using (var cmd = new OracleCommand(query, connection))
                        {
                            if (paramName != null)
                            {
                                if (paramName.Length != paramValue.Length || paramName.Length != paramType.Length)
                                {
                                    sqlResult.error = "Invalid params in SQL";
                                    if (_traceEnabled) _traceService.LogError(sqlResult.error);
                                }
                                for (int i = 0; i < paramName.Length; i++)
                                {
                                    cmd.Parameters.Add(paramName[i], GetOracleDbType(paramType[i])).Value = paramValue[i];
                                }

                            }
                            try
                            {
                                cmd.Prepare();
                                var dr = await cmd.ExecuteReaderAsync();
                                sqlResult.data.Load(dr);
                                sqlResult.data = ChangeColumnNamesToLowerCase(sqlResult.data);

                                DateTime endTime = DateTime.Now;
                                if (_traceEnabled) _traceService.LogDataTable(sqlResult.data, startTime, endTime); ;

                            }
                            catch (Exception ex)
                            {
                                sqlResult.error += ex.ToString();
                                string error = ex.Message;
                                error += Environment.NewLine + ex.InnerException;
                                error += Environment.NewLine + ex.StackTrace;

                                if (_traceEnabled) _traceService.LogError(error);
                            }
                            finally
                            {
                                await connection.CloseAsync();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    sqlResult.error += ex.ToString();
                    string error = ex.Message;
                    error += Environment.NewLine + ex.InnerException;
                    error += Environment.NewLine + ex.StackTrace;

                    if (_traceEnabled) _traceService.LogError(error);
                }

                return sqlResult;
            }

            public async Task<SQLResult> ExecuteSQLAsync(string query, DbConnection connection, string[] paramName = null, DbType[] paramType = null, object[] paramValue = null, string getCountsSql = "")
            {
                SQLResult sqlResult = new SQLResult();
                if (query.Trim().EndsWith(";"))
                    query = query.Trim().Substring(0, query.Trim().Length - 1);
                try
                {
                    //if (_traceEnabled) _traceService.LogDbConnection(connectionString);


                    //using (var connection = new OracleConnection(connectionString))
                    {
                        if (_traceEnabled) _traceService.LogSqlExecution(query, paramName, paramType, paramValue);

                        DateTime startTime = DateTime.Now;

                        if (connection?.State != ConnectionState.Open)
                        {
                            await connection.OpenAsync();
                        }

                        query = query.Replace("@", ":");
                        using (var cmd = new OracleCommand(query, (OracleConnection)connection))
                        {
                            if (paramName != null)
                            {
                                if (paramName.Length != paramValue.Length || paramName.Length != paramType.Length)
                                {
                                    sqlResult.error = "Invalid params in SQL";
                                    if (_traceEnabled) _traceService.LogError(sqlResult.error);
                                }
                                for (int i = 0; i < paramName.Length; i++)
                                {
                                    cmd.Parameters.Add(paramName[i], GetOracleDbType(paramType[i])).Value = paramValue[i];
                                }

                            }
                            try
                            {
                                cmd.Prepare();
                                var dr = await cmd.ExecuteReaderAsync();
                                sqlResult.data.Load(dr);
                                sqlResult.data = ChangeColumnNamesToLowerCase(sqlResult.data);

                                DateTime endTime = DateTime.Now;
                                SetDBTimeTaken(startTime, endTime);
                                if (_traceEnabled) _traceService.LogDataTable(sqlResult.data, startTime, endTime); ;

                            }
                            catch (Exception ex)
                            {
                                sqlResult.error += ex.ToString();
                                string error = ex.Message;
                                error += Environment.NewLine + ex.InnerException;
                                error += Environment.NewLine + ex.StackTrace;

                                if (_traceEnabled) _traceService.LogError(error);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    sqlResult.error += ex.ToString();
                    string error = ex.Message;
                    error += Environment.NewLine + ex.InnerException;
                    error += Environment.NewLine + ex.StackTrace;

                    if (_traceEnabled) _traceService.LogError(error);
                }

                return sqlResult;
            }

            public async Task<SQLResult> ExecuteSQLAsync(string query, string[] paramName = null, DbType[] paramType = null, object[] paramValue = null)
            {
                SQLResult sqlResult = new SQLResult();
                if (query.Trim().EndsWith(";"))
                    query = query.Trim().Substring(0, query.Trim().Length - 1);
                try
                {
                    {
                        if (_traceEnabled) _traceService.LogSqlExecution(query, paramName, paramType, paramValue);

                        DateTime startTime = DateTime.Now;

                        if (_connection == null || _connection.State != ConnectionState.Open)
                            await OpenDBConnectionAsync();

                        query = query.Replace("@", ":");
                        using var cmd = new OracleCommand(query, _connection);
                        cmd.BindByName = true;
                        if (paramName != null)
                        {
                            if (paramName.Length != paramValue.Length || paramName.Length != paramType.Length)
                            {
                                sqlResult.error = "Invalid params in SQL";
                                if (_traceEnabled) _traceService.LogError(sqlResult.error);
                            }
                            for (int i = 0; i < paramName.Length; i++)
                            {
                                cmd.Parameters.Add(paramName[i].TrimStart('@', ':'), GetOracleDbType(paramType[i])).Value = paramValue[i];
                            }

                        }
                        try
                        {
                            cmd.Prepare();
                            var dr = await cmd.ExecuteReaderAsync();
                            sqlResult.data.Load(dr);
                            sqlResult.data = ChangeColumnNamesToLowerCase(sqlResult.data);

                            DateTime endTime = DateTime.Now;
                            SetDBTimeTaken(startTime, endTime);
                            if (_traceEnabled) _traceService.LogDataTable(sqlResult.data, startTime, endTime); ;

                        }
                        catch (Exception ex)
                        {
                            sqlResult.error += ex.ToString();
                            string error = ex.Message;
                            error += Environment.NewLine + ex.InnerException;
                            error += Environment.NewLine + ex.StackTrace;

                            if (_traceEnabled) _traceService.LogError(error);
                        }
                    }
                }
                catch (Exception ex)
                {
                    sqlResult.error += ex.ToString();
                    string error = ex.Message;
                    error += Environment.NewLine + ex.InnerException;
                    error += Environment.NewLine + ex.StackTrace;

                    if (_traceEnabled) _traceService.LogError(error);
                }

                return sqlResult;
            }
            public SQLResult ExecuteSQL(string query, string connectionString, string[] paramName = null, DbType[] paramType = null, object[] paramValue = null, string getCountsSql = "")
            {
                SQLResult sqlResult = new SQLResult();
                if (query.Trim().EndsWith(";"))
                    query = query.Trim().Substring(0, query.Trim().Length - 1);
                try
                {
                    if (_traceEnabled) _traceService.LogDbConnection(connectionString);
                    DateTime startTime = DateTime.Now;

                    using (var connection = new OracleConnection(connectionString))
                    {
                        if (_traceEnabled) _traceService.LogSqlExecution(query, paramName, paramType, paramValue);

                        connection.Open();
                        query = query.Replace("@", ":");
                        using (var cmd = new OracleCommand(query, connection))
                        {
                            if (paramName != null)
                            {
                                if (paramName.Length != paramValue.Length || paramName.Length != paramType.Length)
                                {
                                    sqlResult.error = "Invalid params in SQL";
                                    if (_traceEnabled) _traceService.LogError(sqlResult.error);
                                }
                                for (int i = 0; i < paramName.Length; i++)
                                {
                                    cmd.Parameters.Add(paramName[i], GetOracleDbType(paramType[i])).Value = paramValue[i];
                                }

                            }
                            try
                            {
                                cmd.Prepare();
                                var dr = cmd.ExecuteReader();
                                sqlResult.data.Load(dr);
                                sqlResult.data = ChangeColumnNamesToLowerCase(sqlResult.data);

                                DateTime endTime = DateTime.Now;
                                if (_traceEnabled) _traceService.LogDataTable(sqlResult.data, startTime, endTime); ;

                            }
                            catch (Exception ex)
                            {
                                sqlResult.error += ex.ToString();
                                string error = ex.Message;
                                error += Environment.NewLine + ex.InnerException;
                                error += Environment.NewLine + ex.StackTrace;

                                if (_traceEnabled) _traceService.LogError(error);
                            }
                            finally
                            {
                                connection.Open();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    sqlResult.error += ex.ToString();
                    string error = ex.Message;
                    error += Environment.NewLine + ex.InnerException;
                    error += Environment.NewLine + ex.StackTrace;

                    if (_traceEnabled) _traceService.LogError(error);
                }

                return sqlResult;
            }

            //This function is used to get an SQL string from a SQL and then call that SQL string to get the result
            public async Task<SQLDataSetResult> ExecuteSQLsFromSQLAsync(string query, string connectionString, string[] paramName = null, DbType[] paramType = null, object[] paramValue = null, Dictionary<string, string>? viewFilters = null, Dictionary<string, string>? globalParams = null, Dictionary<string, string>? sqlCommentsReplace = null, bool getAllRecordsCount = false)
            {
                SQLDataSetResult sqlDataSetResult = new SQLDataSetResult();
                if (query.Trim().EndsWith(";"))
                    query = query.Trim().Substring(0, query.Trim().Length - 1);

                try
                {
                    if (_traceEnabled) _traceService.LogDbConnection(connectionString);
                    DateTime startTime = DateTime.Now;

                    using (var connection = new OracleConnection(connectionString))
                    {
                        if (_traceEnabled) _traceService.LogSqlExecution(query, paramName, paramType, paramValue, viewFilters, globalParams, sqlCommentsReplace);

                        await connection.OpenAsync();
                        query = query.Replace("@", ":");
                        using (var cmd = new OracleCommand(query, connection))
                        {
                            if (paramName != null)
                            {
                                if (paramName.Length != paramValue.Length || paramName.Length != paramType.Length)
                                {
                                    sqlDataSetResult.error = "Invalid params in SQL";
                                    if (_traceEnabled) _traceService.LogError(sqlDataSetResult.error);
                                }

                                for (int i = 0; i < paramName.Length; i++)
                                {
                                    cmd.Parameters.Add(paramName[i], GetOracleDbType(paramType[i])).Value = paramValue[i];
                                }
                            }

                            var dt = new DataTable();

                            try
                            {
                                cmd.Prepare();
                                var dr = await cmd.ExecuteReaderAsync();
                                dt.Load(dr);

                                DateTime endTime = DateTime.Now;
                                if (_traceEnabled) _traceService.LogDataTable(dt, startTime, endTime); ;

                            }
                            catch (Exception ex)
                            {
                                sqlDataSetResult.error += ex.Message;

                                string error = ex.Message;
                                error += Environment.NewLine + ex.InnerException;
                                error += Environment.NewLine + ex.StackTrace;

                                if (_traceEnabled) _traceService.LogError(error);
                            }
                            finally
                            {
                                await connection.CloseAsync();
                            }

                            if (dt != null && dt.Rows.Count > 0)
                            {

                                List<Task<SQLResult>> sqlTasks = new List<Task<SQLResult>>();

                                foreach (DataRow dataRow in dt.Rows)
                                {
                                    var sql = dataRow[0]?.ToString() ?? "";
                                    if (viewFilters != null && viewFilters.Count > 0)
                                    {
                                        sql = sql.Replace("--axp_filter", $" {viewFilters.Values.First()} ");
                                    }
                                    else
                                    {
                                        sql = sql.Replace("--axp_filter", "");
                                    }

                                    if (globalParams != null && globalParams.Count > 0)
                                    {
                                        sql = ReplaceSqlParameters(sql, globalParams);
                                    }

                                    if (sqlCommentsReplace != null && sqlCommentsReplace.Count > 0)
                                    {
                                        sql = ReplaceSqlComments(sql, sqlCommentsReplace);
                                    }

                                    if (!string.IsNullOrEmpty(sql))
                                    {
                                        sqlTasks.Add(ExecuteSQLAsync(sql, connectionString));
                                    }
                                }

                                var sqlResults = await Task.WhenAll(sqlTasks);

                                for (int i = 0; i < sqlResults.Length; i++)
                                {
                                    if (string.IsNullOrEmpty(sqlResults[i].error))
                                    {
                                        sqlResults[i].data.TableName = $"Table{i}";
                                        sqlDataSetResult.DataSet.Tables.Add(sqlResults[i].data);
                                    }
                                    else
                                    {
                                        sqlDataSetResult.error += sqlResults[i].error;
                                    }
                                }
                            }
                        }

                    }
                }
                catch (Exception ex)
                {
                    sqlDataSetResult.error = ex.ToString();

                    string error = ex.Message;
                    error += Environment.NewLine + ex.InnerException;
                    error += Environment.NewLine + ex.StackTrace;

                    if (_traceEnabled) _traceService.LogError(error);
                }
                return sqlDataSetResult;
            }

            public async Task<DataTable> ExecuteQueryAsync(string query, string connectionString, DBParamsDetails paramsDetails)
            {
                DataTable dt = new DataTable();

                if (query.Trim().EndsWith(";"))
                    query = query.Trim().Substring(0, query.Trim().Length - 1);

                try
                {
                    if (_traceEnabled) _traceService.LogDbConnection(connectionString);
                    DateTime startTime = DateTime.Now;

                    using (var connection = new OracleConnection(connectionString))
                    {
                        if (_traceEnabled)
                        {
                            _traceService.LogSqlExecution(query);
                            _traceService.LogMessage("Params: " + JsonConvert.SerializeObject(paramsDetails));
                        }

                        await connection.OpenAsync();
                        using (var command = new OracleCommand(query, connection))
                        {
                            try
                            {
                                if (paramsDetails.ParamsNames.Count > 0)
                                {
                                    foreach (var item in paramsDetails.ParamsNames)
                                    {
                                        command.Parameters.Add(item.Name, GetOracleDbType((DbType)item.Type), item.Value, ParameterDirection.Input);
                                    }
                                }
                                command.Prepare();
                                var dr = await command.ExecuteReaderAsync();
                                dt.Load(dr);


                                DateTime endTime = DateTime.Now;
                                if (_traceEnabled) _traceService.LogDataTable(dt, startTime, endTime); ;
                            }
                            catch (Exception ex)
                            {
                                string error = ex.Message;
                                error += Environment.NewLine + ex.InnerException;
                                error += Environment.NewLine + ex.StackTrace;

                                if (_traceEnabled) _traceService.LogError(error);
                            }
                            finally
                            {
                                await connection.CloseAsync();
                            }
                        }

                    }
                }
                catch (Exception ex)
                {
                    dt.Rows.Add(ex.ToString());

                    string error = ex.Message;
                    error += Environment.NewLine + ex.InnerException;
                    error += Environment.NewLine + ex.StackTrace;

                    if (_traceEnabled) _traceService.LogError(error);
                }
                return dt;
            }

            public async Task<SQLResult> ExecuteStoredProcedureAsync(string storedProcedure, string connectionString, string[] paramName = null, DbType[] paramType = null, object[] paramValue = null)
            {
                return new SQLResult(); //To-Do: StoredProcedure implementation
            }

            public async Task<SQLResult> ExecuteStoredProcedureAsync(string storedProcedure, string[] paramName = null, DbType[] paramType = null, object[] paramValue = null)
            {
                return new SQLResult(); //To-Do: StoredProcedure implementation
            }


            public async Task<TransactionResult> ExecuteSQLsInTransactionAsync(List<SQLCommandBuilder> sqlCommands, bool commitTransaction = true)
            {
                var result = new TransactionResult();

                if (sqlCommands == null || sqlCommands.Count == 0)
                {
                    result.success = false;
                    result.error = "No SQL commands to execute.";
                    return result;
                }

                try
                {
                    if (_connection == null || _connection.State != ConnectionState.Open)
                        await OpenDBConnectionAsync();

                    if (_transaction == null)
                    {
                        _transaction = (OracleTransaction)await _connection.BeginTransactionAsync();
                        if (_traceEnabled) _traceService.LogMessage($"Transaction started.", "");
                    }
                    
                    try
                    {
                        int totalRowsAffected = 0;
                        foreach (var sqlCommand in sqlCommands)
                        {
                            using var cmd = _connection.CreateCommand();
                            var query = sqlCommand.Query;

                            if (query.Trim().EndsWith(";"))
                                query = query.Trim().Substring(0, query.Trim().Length - 1);

                            query = query.Replace("@", ":");

                            cmd.Transaction = _transaction;
                            cmd.CommandText = query;
                            cmd.CommandType = CommandType.Text;

                            if (sqlCommand.ParameterNames != null)
                            {
                                if (sqlCommand.ParameterNames.Length != sqlCommand.ParameterValues.Length || sqlCommand.ParameterNames.Length != sqlCommand.ParameterTypes.Length)
                                {
                                    result.success = false;
                                    result.error = "Invalid params in SQL";
                                    if (_traceEnabled) _traceService.LogError(result.error);
                                }

                                for (int i = 0; i < sqlCommand.ParameterNames.Length; i++)
                                {
                                    cmd.Parameters.Add(sqlCommand.ParameterNames[i], GetOracleDbType(sqlCommand.ParameterTypes[i])).Value = sqlCommand.ParameterValues[i];
                                }

                                if (_traceEnabled) _traceService.LogSqlExecution(query, sqlCommand.ParameterNames, sqlCommand.ParameterTypes, sqlCommand.ParameterValues);
                            }
                            else
                                if (_traceEnabled) _traceService.LogSqlExecution(query);


                            var rowsAffected = await cmd.ExecuteNonQueryAsync();
                            if (_traceEnabled) _traceService.LogSqlExecution($"Rows Affected : {rowsAffected}");

                            totalRowsAffected += rowsAffected;
                        }

                        if (commitTransaction)
                        {
                            CommitTransaction();
                        }

                        result.success = true;
                        result.TotalStatements = sqlCommands.Count;
                        result.TotalRowsAffected = totalRowsAffected;

                        if (_traceEnabled)
                        {
                            _traceService.LogMessage($"Transaction : Success");
                            _traceService.LogMessage($"Total Statements : {sqlCommands.Count}");
                            _traceService.LogMessage($"Total Rows Affected : {totalRowsAffected}");
                        }
                    }
                    catch (Exception ex)
                    {
                        RollBackTransaction();
                        result.success = false;
                        result.error = ex.Message;
                        result.ExceptionDetail = ex;

                        if (_traceEnabled) _traceService.LogError(JsonConvert.SerializeObject(result));
                    }
                }
                catch (Exception ex)
                {
                    result.success = false;
                    result.error = ex.Message;
                    result.ExceptionDetail = ex;
                    if (_traceEnabled) _traceService.LogError(JsonConvert.SerializeObject(result));
                }

                return result;
            }
            public NonQueryResult CommitTransaction()
            {
                var result = new NonQueryResult();
                try
                {
                    if (_transaction == null)
                    {
                        result.error = "Transaction is null";
                        if (_traceEnabled) _traceService.LogError($"Transaction is null");
                        return result;
                    }

                    try
                    {
                        _transaction.Commit();
                        _transaction.Dispose();
                        if (_traceEnabled)
                        {
                            _traceService.LogMessage($"Transaction Committed.", "");
                            _traceService.LogMessage($"Transaction Disposed.");
                            _traceService.LogMessage($"Transaction : Success", "");
                        }
                    }
                    catch (Exception ex)
                    {
                        RollBackTransaction();
                        result.error = ex.Message;

                        if (_traceEnabled) _traceService.LogError(JsonConvert.SerializeObject(result));
                    }
                }
                catch (Exception ex)
                {
                    _traceService.LogError($"Transaction : failed");
                    result.error = ex.Message;
                    if (_traceEnabled) _traceService.LogError(JsonConvert.SerializeObject(result));
                }
                return result;
            }

            public NonQueryResult RollBackTransaction()
            {
                var result = new NonQueryResult();
                try
                {
                    if (_transaction == null)
                    {
                        result.error = "Transaction is null";
                        if (_traceEnabled) _traceService.LogError($"Transaction is null");
                        return result;
                    }

                    try
                    {
                        _transaction.Rollback();
                        _transaction.Dispose();
                        if (_traceEnabled)
                        {
                            _traceService.LogMessage($"Transaction Rolledback.", "");
                            _traceService.LogMessage($"Transaction Disposed.");
                            _traceService.LogMessage($"Transaction : failed", "");
                        }
                    }
                    catch (Exception ex)
                    {                        
                        _transaction.Dispose();
                        if (_traceEnabled)
                        {
                            _traceService.LogMessage($"Transaction Disposed.");
                            _traceService.LogError($"Transaction : failed");
                        }
                        result.error = ex.Message;

                        if (_traceEnabled) _traceService.LogError(JsonConvert.SerializeObject(result));
                    }
                }
                catch (Exception ex)
                {
                    _traceService.LogError($"Transaction : failed");
                    result.error = ex.Message;
                    if (_traceEnabled) _traceService.LogError(JsonConvert.SerializeObject(result));
                }
                return result;
            }
            public async Task<DataTable> ExecuteSqlSchemaAsync(string sqlQuery)
            {
                DataTable schemaTable = new DataTable();
                try
                {
                    if (_connection == null || _connection.State != ConnectionState.Open)
                    {
                        await OpenDBConnectionAsync();
                    }
                    using var cmd = new OracleCommand(sqlQuery, _connection);
                    using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SchemaOnly);
                    schemaTable = reader.GetSchemaTable();
                    schemaTable = ChangeColumnNamesToLowerCase(schemaTable);
                }
                catch (Exception ex)
                {
                    string error = $"Error executing Oracle schema query: {ex.Message}";
                    if (_traceEnabled)
                        _traceService.LogError(error);

                    throw new Exception(ex.Message);
                }
                return schemaTable;
            }

            public DataTable ChangeColumnNamesToLowerCase(DataTable dt)
            {
                if (dt == null)
                {
                    throw new ArgumentNullException(nameof(dt), "DataTable cannot be null.");
                }

                foreach (DataColumn column in dt.Columns)
                {
                    column.ColumnName = column.ColumnName.ToLower();
                }

                return dt;
            }

            private static OracleDbType GetOracleDbType(DbType type)
            {
                switch (type)
                {
                    case DbType.Int32:
                        return OracleDbType.Int32;
                    case DbType.Int64:
                        return OracleDbType.Int64;
                    case DbType.Decimal:
                        return OracleDbType.Decimal;
                    case DbType.String:
                        return OracleDbType.Varchar2;
                    case DbType.DateTime:
                        return OracleDbType.TimeStamp;
                    case DbType.Date:
                        return OracleDbType.Date;
                    case DbType.Binary:
                        return OracleDbType.Blob;
                    case DbType.Guid:
                        return OracleDbType.Raw; //GUID                    
                    case DbType.Object:
                        return OracleDbType.Clob; // JSON support → CLOB
                    default:
                        return OracleDbType.Varchar2;
                }
            }

            
        }

        public class PostgresHelper : IDbHelper
        {
            private readonly string _query;
            private string _connectionString;
            private readonly string[] _paramName;
            private readonly DbType[] _paramType;
            private readonly object[] _paramValue;
            private readonly IARMLogTraceService _traceService;
            private readonly bool _traceEnabled;
            public double _dbConnTime = 0;
            public double _dbTimeTaken = 0;
            private NpgsqlConnection _connection;
            private NpgsqlTransaction _transaction;

            public PostgresHelper(IARMLogTraceService traceService = null)
            {
                _traceService = traceService;

                if (_traceService != null && _traceService.IsTracingEnabled())
                    _traceEnabled = true;
            }

            public PostgresHelper(string query, string connectionString, string[] paramName, DbType[] paramType, object[] paramValue, IARMLogTraceService traceService = null)
            {
                _query = query;
                _connectionString = connectionString;
                _paramName = paramName;
                _paramType = paramType;
                _paramValue = paramValue;
                _traceService = traceService;

                if (_traceService != null && _traceService.IsTracingEnabled())
                    _traceEnabled = true;
            }

            public async Task OpenDBConnectionAsync()
            {
                DateTime startTime = DateTime.Now;
                _connection = new NpgsqlConnection(_connectionString);
                await _connection.OpenAsync();
                if (_connection.State == ConnectionState.Open)
                    SetDBConnectionTime(startTime, DateTime.Now);
            }

            public async Task CloseDBConnectionAsync()
            {
                await _connection.CloseAsync();
            }
            public string ConnectionString
            {
                get { return _connectionString; }
                set { _connectionString = value; }
            }

            public DbTransaction Transaction
            {
                get { return (DbTransaction)_transaction; }
                set { _transaction = (NpgsqlTransaction)value;}
            }
            public string GetDBType()
            {
                return Constants.DBTYPE.POSTGRE.ToString();
            }

            public double SetDBConnectionTime(DateTime startTime, DateTime endTime)
            {
                TimeSpan duration = endTime - startTime;
                _dbConnTime += duration.TotalMilliseconds;

                return _dbConnTime;
            }
            public double SetDBTimeTaken(DateTime startTime, DateTime endTime)
            {
                TimeSpan duration = endTime - startTime;
                _dbTimeTaken += duration.TotalMilliseconds;

                return _dbTimeTaken;
            }

            public double GetDBConnectionTime()
            {
                return _dbConnTime;
            }
            public double GetDBTimeTaken()
            {
                return _dbTimeTaken;
            }

            public string ReplaceSqlParameters(string sql, Dictionary<string, string> parameters)
            {
                Regex regex = new Regex(@"\:\w+");
                StringBuilder sb = new StringBuilder(sql);
                MatchCollection matches = regex.Matches(sql);

                // Create a case-insensitive dictionary lookup
                var caseInsensitiveParams = new Dictionary<string, string>(parameters, StringComparer.OrdinalIgnoreCase);

                // Iterate in reverse to avoid messing up string indexes
                for (int i = matches.Count - 1; i >= 0; i--)
                {
                    Match match = matches[i];
                    string paramKey = match.Value.Substring(1); // Remove leading ':'

                    if (caseInsensitiveParams.TryGetValue(paramKey, out string value))
                    {
                        sb.Remove(match.Index, match.Length);
                        sb.Insert(match.Index, "'" + value.Replace("'", "''") + "'"); // Escape single quotes for SQL
                    }
                }

                return sb.ToString();
            }

            public string ReplaceSqlComments(string sql, Dictionary<string, string> comments)
            {
                foreach (var entry in comments)
                {
                    sql = Regex.Replace(sql, Regex.Escape(entry.Key), entry.Value, RegexOptions.IgnoreCase);
                }
                return sql;
            }
            public async Task<DataTable> ExecuteQueryAsync(string query, string connectionString)
            {
                DataTable dt = new DataTable();
                if (query.Trim().EndsWith(";"))
                    query = query.Trim().Substring(0, query.Trim().Length - 1);

                try
                {
                    if (_traceEnabled) _traceService.LogDbConnection(connectionString);
                    DateTime startTime = DateTime.Now;

                    using (var connection = new NpgsqlConnection(connectionString))
                    {
                        if (_traceEnabled) _traceService.LogSqlExecution(query);

                        await connection.OpenAsync();
                        using (var cmd = new NpgsqlCommand(query, connection))
                        {
                            try
                            {
                                cmd.Prepare();
                                var dr = await cmd.ExecuteReaderAsync();
                                dt.Load(dr);


                                DateTime endTime = DateTime.Now;
                                if (_traceEnabled) _traceService.LogDataTable(dt, startTime, endTime); ;

                            }
                            catch (Exception ex)
                            {
                                dt.Rows.Add(ex.Message);

                                string error = ex.Message;
                                error += Environment.NewLine + ex.InnerException;
                                error += Environment.NewLine + ex.StackTrace;

                                if (_traceEnabled) _traceService.LogError(error);
                            }
                            finally
                            {
                                await connection.CloseAsync();
                            }
                        }

                    }
                }
                catch (Exception ex)
                {
                    dt.Rows.Add(ex.Message);

                    string error = ex.Message;
                    error += Environment.NewLine + ex.InnerException;
                    error += Environment.NewLine + ex.StackTrace;

                    if (_traceEnabled) _traceService.LogError(error);
                }
                return dt;
            }

            public async Task<NonQueryResult> ExecuteNonQueryAsync(string query, string connectionString, string[] paramName = null, DbType[] paramType = null, object[] paramValue = null)
            {
                var result = new NonQueryResult();

                if (query.Trim().EndsWith(";"))
                    query = query.Trim().Substring(0, query.Trim().Length - 1);

                try
                {
                    if (_traceEnabled) _traceService.LogDbConnection(connectionString);
                    DateTime startTime = DateTime.Now;

                    using (var connection = new NpgsqlConnection(connectionString))
                    {
                        if (_traceEnabled) _traceService.LogSqlExecution(query, paramName, paramType, paramValue);

                        await connection.OpenAsync();
                        using (var cmd = new NpgsqlCommand(query, connection))
                        {
                            if (paramName != null)
                            {
                                if (paramName.Length != paramValue.Length || paramName.Length != paramType.Length)
                                {
                                    result.error = "Invalid params in SQL";
                                    if (_traceEnabled) _traceService.LogError(result.error);
                                }

                                for (int i = 0; i < paramName.Length; i++)
                                {
                                    cmd.Parameters.Add(paramName[i], GetNpgsqlDbType(paramType[i])).Value = paramValue[i];
                                }
                            }
                            try
                            {
                                cmd.Prepare();
                                result.count = await cmd.ExecuteNonQueryAsync();

                                DateTime endTime = DateTime.Now;
                                if (_traceEnabled) _traceService.LogResultInfo(result.count, startTime, endTime);

                            }
                            catch (Exception ex)
                            {
                                result.error = ex.Message;

                                string error = ex.Message;
                                error += Environment.NewLine + ex.InnerException;
                                error += Environment.NewLine + ex.StackTrace;

                                if (_traceEnabled) _traceService.LogError(error);

                            }
                            finally
                            {
                                await connection.CloseAsync();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    result.error = ex.Message;

                    string error = ex.Message;
                    error += Environment.NewLine + ex.InnerException;
                    error += Environment.NewLine + ex.StackTrace;

                    if (_traceEnabled) _traceService.LogError(error);
                }

                return result;
            }

            public async Task<NonQueryResult> ExecuteNonQueryAsync(string query, DbConnection connection, string[] paramName = null, DbType[] paramType = null, object[] paramValue = null)
            {
                var result = new NonQueryResult();

                if (query.Trim().EndsWith(";"))
                    query = query.Trim().Substring(0, query.Trim().Length - 1);

                try
                {
                    if (_traceEnabled) _traceService.LogDbConnection(connection.ConnectionString);
                    DateTime startTime = DateTime.Now;

                    //using (var connection = new NpgsqlConnection(connectionString))
                    {
                        if (_traceEnabled) _traceService.LogSqlExecution(query, paramName, paramType, paramValue);

                        if (connection?.State != ConnectionState.Open)
                        {
                            await connection.OpenAsync();
                        }

                        using (var cmd = new NpgsqlCommand(query, (NpgsqlConnection)connection))
                        {
                            if (paramName != null)
                            {
                                if (paramName.Length != paramValue.Length || paramName.Length != paramType.Length)
                                {
                                    result.error = "Invalid params in SQL";
                                    if (_traceEnabled) _traceService.LogError(result.error);
                                }

                                for (int i = 0; i < paramName.Length; i++)
                                {
                                    cmd.Parameters.Add(paramName[i], GetNpgsqlDbType(paramType[i])).Value = paramValue[i];
                                }
                            }
                            try
                            {
                                cmd.Prepare();
                                result.count = await cmd.ExecuteNonQueryAsync();

                                DateTime endTime = DateTime.Now;
                                if (_traceEnabled) _traceService.LogResultInfo(result.count, startTime, endTime);

                            }
                            catch (Exception ex)
                            {
                                result.error = ex.Message;

                                string error = ex.Message;
                                error += Environment.NewLine + ex.InnerException;
                                error += Environment.NewLine + ex.StackTrace;

                                if (_traceEnabled) _traceService.LogError(error);

                            }
                            //finally
                            //{
                            //    await connection.CloseAsync();
                            //}
                        }
                    }
                }
                catch (Exception ex)
                {
                    result.error = ex.Message;

                    string error = ex.Message;
                    error += Environment.NewLine + ex.InnerException;
                    error += Environment.NewLine + ex.StackTrace;

                    if (_traceEnabled) _traceService.LogError(error);
                }

                return result;
            }
            public async Task<NonQueryResult> ExecuteNonQueryAsync(string query, string[] paramName = null, DbType[] paramType = null, object[] paramValue = null)
            {
                var result = new NonQueryResult();
                if (query.Trim().EndsWith(";"))
                    query = query.Trim().Substring(0, query.Trim().Length - 1);
                try
                {
                    DateTime startTime = DateTime.Now;
                    if (_traceEnabled) _traceService.LogSqlExecution(query, paramName, paramType, paramValue);

                    if (_connection == null || _connection.State != ConnectionState.Open)
                        await OpenDBConnectionAsync();

                    query = query.Replace("@", ":");
                    using var cmd = new NpgsqlCommand(query, _connection);

                    if (paramName != null)
                    {
                        if (paramName.Length != paramValue.Length || paramName.Length != paramType.Length)
                        {
                            result.error = "Invalid params in SQL";
                            if (_traceEnabled) _traceService.LogError(result.error);
                        }

                        for (int i = 0; i < paramName.Length; i++)
                        {
                            cmd.Parameters.Add(paramName[i], GetNpgsqlDbType(paramType[i])).Value = paramValue[i];
                        }
                    }

                    try
                    {
                        cmd.Prepare();
                        result.count = await cmd.ExecuteNonQueryAsync();
                        DateTime endTime = DateTime.Now;
                        if (_traceEnabled) _traceService.LogResultInfo(result.count, startTime, endTime);

                    }
                    catch (Exception ex)
                    {
                        result.error = ex.Message;

                        string error = ex.Message;
                        error += Environment.NewLine + ex.InnerException;
                        error += Environment.NewLine + ex.StackTrace;

                        if (_traceEnabled) _traceService.LogError(error);

                    }
                }
                catch (Exception ex)
                {
                    result.error = ex.Message;

                    string error = ex.Message;
                    error += Environment.NewLine + ex.InnerException;
                    error += Environment.NewLine + ex.StackTrace;

                    if (_traceEnabled) _traceService.LogError(error);

                }

                return result;
            }

            public async Task<ScalarResult> ExecuteScalarAsync(string query, string connectionString, string[] paramName = null, DbType[] paramType = null, object[] paramValue = null)
            {
                var result = new ScalarResult();
                if (query.Trim().EndsWith(";"))
                    query = query.Trim().Substring(0, query.Trim().Length - 1);
                try
                {
                    if (_traceEnabled) _traceService.LogDbConnection(connectionString);
                    DateTime startTime = DateTime.Now;

                    using (var connection = new NpgsqlConnection(connectionString))
                    {
                        if (_traceEnabled) _traceService.LogSqlExecution(query, paramName, paramType, paramValue);

                        await connection.OpenAsync();
                        using (var cmd = new NpgsqlCommand(query, connection))
                        {
                            if (paramName != null)
                            {
                                if (paramName.Length != paramValue.Length || paramName.Length != paramType.Length)
                                {
                                    result.error = "Invalid params in SQL";
                                    if (_traceEnabled) _traceService.LogError(result.error);
                                }

                                for (int i = 0; i < paramName.Length; i++)
                                {
                                    cmd.Parameters.Add(paramName[i], GetNpgsqlDbType(paramType[i])).Value = paramValue[i];
                                }
                            }

                            try
                            {
                                cmd.Prepare();
                                result.result = await cmd.ExecuteScalarAsync();
                                DateTime endTime = DateTime.Now;
                                if (_traceEnabled) _traceService.LogScalarResultInfo(result.result?.ToString() ?? "", startTime, endTime);

                            }
                            catch (Exception ex)
                            {
                                result.error = ex.Message;

                                string error = ex.Message;
                                error += Environment.NewLine + ex.InnerException;
                                error += Environment.NewLine + ex.StackTrace;

                                if (_traceEnabled) _traceService.LogError(error);

                            }
                            finally
                            {
                                await connection.CloseAsync();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    result.error = ex.Message;

                    string error = ex.Message;
                    error += Environment.NewLine + ex.InnerException;
                    error += Environment.NewLine + ex.StackTrace;

                    if (_traceEnabled) _traceService.LogError(error);

                }

                return result;
            }

            public NonQueryResult ExecuteNonQuery(string query, string connectionString, string[] paramName = null, DbType[] paramType = null, object[] paramValue = null)
            {
                var result = new NonQueryResult();

                if (query.Trim().EndsWith(";"))
                    query = query.Trim().Substring(0, query.Trim().Length - 1);

                try
                {
                    if (_traceEnabled) _traceService.LogDbConnection(connectionString);
                    DateTime startTime = DateTime.Now;

                    using (var connection = new NpgsqlConnection(connectionString))
                    {
                        if (_traceEnabled) _traceService.LogSqlExecution(query, paramName, paramType, paramValue);

                        connection.Open();
                        using (var cmd = new NpgsqlCommand(query, connection))
                        {
                            if (paramName != null)
                            {
                                if (paramName.Length != paramValue.Length || paramName.Length != paramType.Length)
                                {
                                    result.error = "Invalid params in SQL";
                                    if (_traceEnabled) _traceService.LogError(result.error);
                                }

                                for (int i = 0; i < paramName.Length; i++)
                                {
                                    cmd.Parameters.Add(paramName[i], GetNpgsqlDbType(paramType[i])).Value = paramValue[i];
                                }
                            }
                            try
                            {
                                cmd.Prepare();
                                result.count = cmd.ExecuteNonQuery();

                                DateTime endTime = DateTime.Now;
                                if (_traceEnabled) _traceService.LogResultInfo(result.count, startTime, endTime);

                            }
                            catch (Exception ex)
                            {
                                result.error = ex.Message;

                                string error = ex.Message;
                                error += Environment.NewLine + ex.InnerException;
                                error += Environment.NewLine + ex.StackTrace;

                                if (_traceEnabled) _traceService.LogError(error);

                            }
                            finally
                            {
                                connection.Close();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    result.error = ex.Message;

                    string error = ex.Message;
                    error += Environment.NewLine + ex.InnerException;
                    error += Environment.NewLine + ex.StackTrace;

                    if (_traceEnabled) _traceService.LogError(error);
                }

                return result;
            }

            public ScalarResult ExecuteScalar(string query, string connectionString, string[] paramName = null, DbType[] paramType = null, object[] paramValue = null)
            {
                var result = new ScalarResult();
                if (query.Trim().EndsWith(";"))
                    query = query.Trim().Substring(0, query.Trim().Length - 1);
                try
                {
                    if (_traceEnabled) _traceService.LogDbConnection(connectionString);
                    DateTime startTime = DateTime.Now;

                    using (var connection = new NpgsqlConnection(connectionString))
                    {
                        if (_traceEnabled) _traceService.LogSqlExecution(query, paramName, paramType, paramValue);

                        connection.Open();
                        using (var cmd = new NpgsqlCommand(query, connection))
                        {
                            if (paramName != null)
                            {
                                if (paramName.Length != paramValue.Length || paramName.Length != paramType.Length)
                                {
                                    result.error = "Invalid params in SQL";
                                    if (_traceEnabled) _traceService.LogError(result.error);
                                }

                                for (int i = 0; i < paramName.Length; i++)
                                {
                                    cmd.Parameters.Add(paramName[i], GetNpgsqlDbType(paramType[i])).Value = paramValue[i];
                                }
                            }

                            try
                            {
                                cmd.Prepare();
                                result.result = cmd.ExecuteScalar();
                                DateTime endTime = DateTime.Now;
                                if (_traceEnabled) _traceService.LogScalarResultInfo(result.result?.ToString() ?? "", startTime, endTime);

                            }
                            catch (Exception ex)
                            {
                                result.error = ex.Message;

                                string error = ex.Message;
                                error += Environment.NewLine + ex.InnerException;
                                error += Environment.NewLine + ex.StackTrace;

                                if (_traceEnabled) _traceService.LogError(error);

                            }
                            finally
                            {
                                connection.Close();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    result.error = ex.Message;

                    string error = ex.Message;
                    error += Environment.NewLine + ex.InnerException;
                    error += Environment.NewLine + ex.StackTrace;

                    if (_traceEnabled) _traceService.LogError(error);

                }

                return result;
            }

            public async Task<ScalarResult> ExecuteScalarAsync(string query, string[] paramName = null, DbType[] paramType = null, object[] paramValue = null)
            {
                var result = new ScalarResult();
                if (query.Trim().EndsWith(";"))
                    query = query.Trim().Substring(0, query.Trim().Length - 1);
                try
                {
                    DateTime startTime = DateTime.Now;

                    if (_traceEnabled) _traceService.LogSqlExecution(query, paramName, paramType, paramValue);

                    if (_connection == null || _connection.State != ConnectionState.Open)
                        await OpenDBConnectionAsync();

                    using var cmd = new NpgsqlCommand(query, _connection);

                    if (paramName != null)
                    {
                        if (paramName.Length != paramValue.Length || paramName.Length != paramType.Length)
                        {
                            result.error = "Invalid params in SQL";
                            if (_traceEnabled) _traceService.LogError(result.error);
                        }

                        for (int i = 0; i < paramName.Length; i++)
                        {
                            if (paramType[i] == DbType.DateTime || paramType[i] == DbType.Date)
                            {
                                if (string.IsNullOrEmpty(paramValue[i]?.ToString()))
                                {
                                    paramValue[i] = DBNull.Value;
                                }
                                else
                                    paramType[i] = DbType.String;
                            }

                            cmd.Parameters.Add(paramName[i], GetNpgsqlDbType(paramType[i])).Value = paramValue[i];
                        }
                    }

                    try
                    {
                        cmd.Prepare();
                        result.result = await cmd.ExecuteScalarAsync();
                        DateTime endTime = DateTime.Now;
                        if (_traceEnabled) _traceService.LogScalarResultInfo(result.result?.ToString() ?? "", startTime, endTime);

                    }
                    catch (Exception ex)
                    {
                        result.error = ex.Message;

                        string error = ex.Message;
                        error += Environment.NewLine + ex.InnerException;
                        error += Environment.NewLine + ex.StackTrace;

                        if (_traceEnabled) _traceService.LogError(error);

                    }


                }
                catch (Exception ex)
                {
                    result.error = ex.Message;

                    string error = ex.Message;
                    error += Environment.NewLine + ex.InnerException;
                    error += Environment.NewLine + ex.StackTrace;

                    if (_traceEnabled) _traceService.LogError(error);

                }

                return result;
            }


            public async Task<ScalarResult> ExecuteScalarAsync(string query, DbConnection connection, string[] paramName = null, DbType[] paramType = null, object[] paramValue = null)
            {
                var result = new ScalarResult();
                if (query.Trim().EndsWith(";"))
                    query = query.Trim().Substring(0, query.Trim().Length - 1);
                try
                {
                    //if (_traceEnabled) _traceService.LogDbConnection(connectionString);


                    //using (var connection = new NpgsqlConnection(connectionString))
                    {
                        if (_traceEnabled) _traceService.LogSqlExecution(query, paramName, paramType, paramValue);

                        DateTime startTime = DateTime.Now;

                        if (connection?.State != ConnectionState.Open)
                        {
                            await connection.OpenAsync();
                        }

                        using (var cmd = new NpgsqlCommand(query, (NpgsqlConnection)connection))
                        {
                            if (paramName != null)
                            {
                                if (paramName.Length != paramValue.Length || paramName.Length != paramType.Length)
                                {
                                    result.error = "Invalid params in SQL";
                                    if (_traceEnabled) _traceService.LogError(result.error);
                                }

                                for (int i = 0; i < paramName.Length; i++)
                                {
                                    cmd.Parameters.Add(paramName[i], GetNpgsqlDbType(paramType[i])).Value = paramValue[i];
                                }
                            }

                            try
                            {
                                cmd.Prepare();
                                result.result = await cmd.ExecuteScalarAsync();
                                DateTime endTime = DateTime.Now;

                                SetDBTimeTaken(startTime, endTime);
                                if (_traceEnabled) _traceService.LogScalarResultInfo(result.result?.ToString() ?? "", startTime, endTime);

                            }
                            catch (Exception ex)
                            {
                                result.error = ex.Message;

                                string error = ex.Message;
                                error += Environment.NewLine + ex.InnerException;
                                error += Environment.NewLine + ex.StackTrace;

                                if (_traceEnabled) _traceService.LogError(error);

                            }
                            //finally
                            //{
                            //    await connection.CloseAsync();
                            //}
                        }
                    }
                }
                catch (Exception ex)
                {
                    result.error = ex.Message;

                    string error = ex.Message;
                    error += Environment.NewLine + ex.InnerException;
                    error += Environment.NewLine + ex.StackTrace;

                    if (_traceEnabled) _traceService.LogError(error);

                }

                return result;
            }
            public async Task<DataTable> ExecuteQueryAsync(string query, string connectionString, string[] paramName, DbType[] paramType, object[] paramValue)
            {
                DataTable dt = new DataTable();

                if (query.Trim().EndsWith(";"))
                    query = query.Trim().Substring(0, query.Trim().Length - 1);

                try
                {
                    if (_traceEnabled) _traceService.LogDbConnection(connectionString);
                    DateTime startTime = DateTime.Now;

                    using (var connection = new NpgsqlConnection(connectionString))
                    {
                        await connection.OpenAsync();
                        using (var cmd = new NpgsqlCommand(query, connection))
                        {
                            if (paramName != null)
                            {
                                if (paramName.Length != paramValue.Length || paramName.Length != paramType.Length)
                                {
                                    dt.Rows.Add("Invalid params in SQL");
                                    if (_traceEnabled) _traceService.LogError("Invalid params in SQL");
                                }

                                for (int i = 0; i < paramName.Length; i++)
                                {
                                    cmd.Parameters.Add(paramName[i], GetNpgsqlDbType(paramType[i])).Value = paramValue[i];
                                }
                            }
                            try
                            {
                                cmd.Prepare();
                                var dr = await cmd.ExecuteReaderAsync();
                                dt.Load(dr);

                                DateTime endTime = DateTime.Now;
                                if (_traceEnabled) _traceService.LogDataTable(dt, startTime, endTime); ;
                            }
                            catch (Exception ex)
                            {
                                dt.Rows.Add(ex.Message);

                                string error = ex.Message;
                                error += Environment.NewLine + ex.InnerException;
                                error += Environment.NewLine + ex.StackTrace;

                                if (_traceEnabled) _traceService.LogError(error);
                            }

                            finally
                            {
                                await connection.CloseAsync();
                            }
                        }

                    }
                }
                catch (Exception ex)
                {
                    dt.Rows.Add(ex.Message);

                    string error = ex.Message;
                    error += Environment.NewLine + ex.InnerException;
                    error += Environment.NewLine + ex.StackTrace;

                    if (_traceEnabled) _traceService.LogError(error);
                }
                return dt;
            }

            public async Task<SQLResult> ExecuteSQLAsync(string query, string connectionString, string[] paramName = null, DbType[] paramType = null, object[] paramValue = null, string getCountsSql = "")
            {
                SQLResult sqlResult = new SQLResult();

                if (query.Trim().EndsWith(";"))
                    query = query.Trim().Substring(0, query.Trim().Length - 1);

                try
                {
                    if (_traceEnabled) _traceService.LogDbConnection(connectionString);

                    DateTime startTime = DateTime.Now;
                    using var connection = new NpgsqlConnection(connectionString);
                    await connection.OpenAsync();
                    using var cmd = new NpgsqlCommand(query, connection);
                    if (_traceEnabled) _traceService.LogSqlExecution(query, paramName, paramType, paramValue);

                    if (paramName != null)
                    {
                        if (paramName.Length != paramValue.Length || paramName.Length != paramType.Length)
                        {
                            sqlResult.error = "Invalid params in SQL";
                            if (_traceEnabled) _traceService.LogError(sqlResult.error);
                        }

                        for (int i = 0; i < paramName.Length; i++)
                        {
                            cmd.Parameters.Add(paramName[i], GetNpgsqlDbType(paramType[i])).Value = paramValue[i];
                        }
                    }

                    try
                    {
                        cmd.Prepare();
                        var dr = await cmd.ExecuteReaderAsync();
                        sqlResult.data.Load(dr);

                        DateTime endTime = DateTime.Now;
                        if (_traceEnabled)
                        {
                            _traceService.LogDataTable(sqlResult.data, startTime, endTime);
                        }

                    }
                    catch (Exception ex)
                    {
                        sqlResult.error = ex.Message;

                        string error = ex.Message;
                        error += Environment.NewLine + ex.InnerException;
                        error += Environment.NewLine + ex.StackTrace;

                        if (_traceEnabled) _traceService.LogError(error);
                    }


                    if (!string.IsNullOrEmpty(getCountsSql))
                    {
                        getCountsSql = $"SELECT COUNT(*) FROM (\n {getCountsSql.Trim()} \n) AS count_subquery";
                        DateTime countSQLStartTime = DateTime.Now;
                        using var command = new NpgsqlCommand(getCountsSql, connection);

                        if (_traceEnabled) _traceService.LogSqlExecution(getCountsSql);

                        object result = await command.ExecuteScalarAsync();

                        sqlResult.count = Convert.ToInt32(result);

                        DateTime countSQLEndTime = DateTime.Now;
                        if (_traceEnabled)
                        {
                            TimeSpan duration = (DateTime)countSQLEndTime - (DateTime)countSQLStartTime;
                            _traceService.LogMessage($"[Total Records Count] : {sqlResult.count.ToString()},  Started: {countSQLStartTime:yyyy-MM-dd HH:mm:ss.fff}, Ended: {countSQLEndTime:yyyy-MM-dd HH:mm:ss.fff}, Duration: {duration.TotalMilliseconds:F2}ms");
                        }

                    }
                    else
                    {
                        sqlResult.count = sqlResult.data.Rows.Count;
                    }
                }
                catch (Exception ex)
                {
                    sqlResult.error = ex.Message;

                    string error = ex.Message;
                    error += Environment.NewLine + ex.InnerException;
                    error += Environment.NewLine + ex.StackTrace;

                    if (_traceEnabled) _traceService.LogError(error);
                }
                return sqlResult;
            }

            public async Task<SQLResult> ExecuteSQLAsync(string query, DbConnection connection, string[] paramName = null, DbType[] paramType = null, object[] paramValue = null, string getCountsSql = "")
            {
                SQLResult sqlResult = new SQLResult();

                if (query.Trim().EndsWith(";"))
                    query = query.Trim().Substring(0, query.Trim().Length - 1);

                try
                {
                    //if (_traceEnabled) _traceService.LogDbConnection(connectionString);

                    //using (var connection = new NpgsqlConnection(connectionString))
                    {
                        DateTime startTime = DateTime.Now;

                        if (connection?.State != ConnectionState.Open)
                        {
                            await connection.OpenAsync();
                        }


                        using (var cmd = new NpgsqlCommand(query, (NpgsqlConnection)connection))
                        {
                            if (_traceEnabled) _traceService.LogSqlExecution(query, paramName, paramType, paramValue);

                            if (paramName != null)
                            {
                                if (paramName.Length != paramValue.Length || paramName.Length != paramType.Length)
                                {
                                    sqlResult.error = "Invalid params in SQL";
                                    if (_traceEnabled) _traceService.LogError(sqlResult.error);
                                }

                                for (int i = 0; i < paramName.Length; i++)
                                {
                                    cmd.Parameters.Add(paramName[i], GetNpgsqlDbType(paramType[i])).Value = paramValue[i];
                                }
                            }

                            try
                            {
                                cmd.Prepare();
                                var dr = await cmd.ExecuteReaderAsync();
                                sqlResult.data.Load(dr);

                                DateTime endTime = DateTime.Now;
                                SetDBTimeTaken(startTime, endTime);
                                if (_traceEnabled) _traceService.LogDataTable(sqlResult.data, startTime, endTime);

                            }
                            catch (Exception ex)
                            {
                                sqlResult.error = ex.Message;

                                string error = ex.Message;
                                error += Environment.NewLine + ex.InnerException;
                                error += Environment.NewLine + ex.StackTrace;

                                if (_traceEnabled) _traceService.LogError(error);
                            }
                        }

                        if (!string.IsNullOrEmpty(getCountsSql))
                        {
                            getCountsSql = $"SELECT COUNT(*) FROM (\n {getCountsSql.Trim()} \n) AS count_subquery";
                            DateTime countSQLStartTime = DateTime.Now;
                            using var command = new NpgsqlCommand(getCountsSql, (NpgsqlConnection)connection);

                            if (_traceEnabled) _traceService.LogSqlExecution(getCountsSql);

                            object result = await command.ExecuteScalarAsync();

                            sqlResult.count = Convert.ToInt32(result);

                            DateTime countSQLEndTime = DateTime.Now;
                            if (_traceEnabled)
                            {
                                TimeSpan duration = (DateTime)countSQLEndTime - (DateTime)countSQLStartTime;
                                _traceService.LogMessage($"[Total Records Count] : {sqlResult.count.ToString()},  Started: {countSQLStartTime:yyyy-MM-dd HH:mm:ss.fff}, Ended: {countSQLEndTime:yyyy-MM-dd HH:mm:ss.fff}, Duration: {duration.TotalMilliseconds:F2}ms");
                            }

                        }
                        else
                        {
                            sqlResult.count = sqlResult.data.Rows.Count;
                        }
                    }
                }
                catch (Exception ex)
                {
                    sqlResult.error = ex.Message;

                    string error = ex.Message;
                    error += Environment.NewLine + ex.InnerException;
                    error += Environment.NewLine + ex.StackTrace;

                    if (_traceEnabled) _traceService.LogError(error);
                }
                return sqlResult;
            }

            public async Task<SQLResult> ExecuteSQLAsync(string query, string[] paramName = null, DbType[] paramType = null, object[] paramValue = null)
            {
                SQLResult sqlResult = new SQLResult();

                if (query.Trim().EndsWith(";"))
                    query = query.Trim().Substring(0, query.Trim().Length - 1);

                try
                {
                    DateTime startTime = DateTime.Now;
                    if (_connection == null || _connection.State != ConnectionState.Open)
                        await OpenDBConnectionAsync();

                    using var cmd = new NpgsqlCommand(query, _connection);
                    if (_traceEnabled) _traceService.LogSqlExecution(query, paramName, paramType, paramValue);

                    if (paramName != null)
                    {
                        if (paramName.Length != paramValue.Length || paramName.Length != paramType.Length)
                        {
                            sqlResult.error = "Invalid params in SQL";
                            if (_traceEnabled) _traceService.LogError(sqlResult.error);
                        }

                        for (int i = 0; i < paramName.Length; i++)
                        {
                            cmd.Parameters.Add(paramName[i], GetNpgsqlDbType(paramType[i])).Value = paramValue[i];
                        }
                    }

                    try
                    {
                        cmd.Prepare();
                        var dr = await cmd.ExecuteReaderAsync();
                        sqlResult.data.Load(dr);

                        DateTime endTime = DateTime.Now;
                        if (_traceEnabled)
                        {
                            _traceService.LogDataTable(sqlResult.data, startTime, endTime);
                        }

                    }
                    catch (Exception ex)
                    {
                        sqlResult.error = ex.Message;

                        string error = ex.Message;
                        error += Environment.NewLine + ex.InnerException;
                        error += Environment.NewLine + ex.StackTrace;

                        if (_traceEnabled) _traceService.LogError(error);
                    }

                    //sqlResult.count = sqlResult.data.Rows.Count;
                }
                catch (Exception ex)
                {
                    sqlResult.error = ex.Message;

                    string error = ex.Message;
                    error += Environment.NewLine + ex.InnerException;
                    error += Environment.NewLine + ex.StackTrace;

                    if (_traceEnabled) _traceService.LogError(error);
                }
                return sqlResult;
            }

            public SQLResult ExecuteSQL(string query, string connectionString, string[] paramName = null, DbType[] paramType = null, object[] paramValue = null, string getCountsSql = "")
            {
                SQLResult sqlResult = new SQLResult();

                if (query.Trim().EndsWith(";"))
                    query = query.Trim().Substring(0, query.Trim().Length - 1);

                try
                {
                    if (_traceEnabled) _traceService.LogDbConnection(connectionString);

                    DateTime startTime = DateTime.Now;
                    using (var connection = new NpgsqlConnection(connectionString))
                    {
                        connection.Open();
                        using (var cmd = new NpgsqlCommand(query, connection))
                        {
                            if (_traceEnabled) _traceService.LogSqlExecution(query, paramName, paramType, paramValue);

                            if (paramName != null)
                            {
                                if (paramName.Length != paramValue.Length || paramName.Length != paramType.Length)
                                {
                                    sqlResult.error = "Invalid params in SQL";
                                    if (_traceEnabled) _traceService.LogError(sqlResult.error);
                                }

                                for (int i = 0; i < paramName.Length; i++)
                                {
                                    cmd.Parameters.Add(paramName[i], GetNpgsqlDbType(paramType[i])).Value = paramValue[i];
                                }
                            }

                            try
                            {
                                cmd.Prepare();
                                var dr = cmd.ExecuteReader();
                                sqlResult.data.Load(dr);

                                DateTime endTime = DateTime.Now;
                                if (_traceEnabled)
                                {
                                    _traceService.LogDataTable(sqlResult.data, startTime, endTime);
                                }

                            }
                            catch (Exception ex)
                            {
                                sqlResult.error = ex.Message;

                                string error = ex.Message;
                                error += Environment.NewLine + ex.InnerException;
                                error += Environment.NewLine + ex.StackTrace;

                                if (_traceEnabled) _traceService.LogError(error);
                            }
                        }

                        if (!string.IsNullOrEmpty(getCountsSql))
                        {
                            getCountsSql = $"SELECT COUNT(*) FROM (\n {getCountsSql.Trim()} \n) AS count_subquery";
                            DateTime countSQLStartTime = DateTime.Now;
                            using var command = new NpgsqlCommand(getCountsSql, connection);

                            if (_traceEnabled) _traceService.LogSqlExecution(getCountsSql);

                            object result = command.ExecuteScalar();

                            sqlResult.count = Convert.ToInt32(result);

                            DateTime countSQLEndTime = DateTime.Now;
                            if (_traceEnabled)
                            {
                                TimeSpan duration = (DateTime)countSQLEndTime - (DateTime)countSQLStartTime;
                                _traceService.LogMessage($"[Total Records Count] : {sqlResult.count.ToString()},  Started: {countSQLStartTime:yyyy-MM-dd HH:mm:ss.fff}, Ended: {countSQLEndTime:yyyy-MM-dd HH:mm:ss.fff}, Duration: {duration.TotalMilliseconds:F2}ms");
                            }

                        }
                        else
                        {
                            sqlResult.count = sqlResult.data.Rows.Count;
                        }
                    }
                }
                catch (Exception ex)
                {
                    sqlResult.error = ex.Message;

                    string error = ex.Message;
                    error += Environment.NewLine + ex.InnerException;
                    error += Environment.NewLine + ex.StackTrace;

                    if (_traceEnabled) _traceService.LogError(error);
                }
                return sqlResult;
            }

            //This function is used to get an SQL string from a SQL and then call that SQL string to get the result
            public async Task<SQLDataSetResult> ExecuteSQLsFromSQLAsync(string query, string connectionString, string[] paramName = null, DbType[] paramType = null, object[] paramValue = null, Dictionary<string, string>? viewFilters = null, Dictionary<string, string>? globalParams = null, Dictionary<string, string>? sqlCommentsReplace = null, bool getAllRecordsCount = false)
            {
                SQLDataSetResult sqlDataSetResult = new SQLDataSetResult();

                if (query.Trim().EndsWith(";"))
                    query = query.Trim().Substring(0, query.Trim().Length - 1);

                try
                {
                    if (_traceEnabled) _traceService.LogDbConnection(connectionString);
                    DateTime startTime = DateTime.Now;

                    using (var connection = new NpgsqlConnection(connectionString))
                    {
                        if (_traceEnabled) _traceService.LogSqlExecution(query, paramName, paramType, paramValue, viewFilters, globalParams, sqlCommentsReplace);

                        await connection.OpenAsync();
                        using (var cmd = new NpgsqlCommand(query, connection))
                        {
                            if (paramName != null)
                            {
                                if (paramName.Length != paramValue.Length || paramName.Length != paramType.Length)
                                {
                                    sqlDataSetResult.error = "Invalid params in SQL";
                                    if (_traceEnabled) _traceService.LogError(sqlDataSetResult.error);
                                }

                                for (int i = 0; i < paramName.Length; i++)
                                {
                                    cmd.Parameters.Add(paramName[i], GetNpgsqlDbType(paramType[i])).Value = paramValue[i];
                                }
                            }

                            var dt = new DataTable();
                            try
                            {
                                cmd.Prepare();
                                var dr = await cmd.ExecuteReaderAsync();
                                dt.Load(dr);

                                DateTime endTime = DateTime.Now;
                                if (_traceEnabled) _traceService.LogDataTable(dt, startTime, endTime); ;
                            }
                            catch (Exception ex)
                            {
                                sqlDataSetResult.error += ex.Message;

                                string error = ex.Message;
                                error += Environment.NewLine + ex.InnerException;
                                error += Environment.NewLine + ex.StackTrace;

                                if (_traceEnabled) _traceService.LogError(error);
                            }

                            finally
                            {
                                await connection.CloseAsync();
                            }

                            if (dt != null && dt.Rows.Count > 0)
                            {

                                List<Task<SQLResult>> sqlTasks = new List<Task<SQLResult>>();

                                foreach (DataRow dataRow in dt.Rows)
                                {
                                    var sql = dataRow[0]?.ToString() ?? "";
                                    if (viewFilters != null && viewFilters.Count > 0)
                                    {
                                        sql = sql.Replace("--axp_filter", $" {viewFilters.Values.First()} ");
                                    }
                                    else
                                    {
                                        sql = sql.Replace("--axp_filter", "");
                                    }

                                    if (globalParams != null && globalParams.Count > 0)
                                    {
                                        sql = ReplaceSqlParameters(sql, globalParams);
                                    }

                                    string getCountsSql = "";

                                    if (getAllRecordsCount == true)
                                    {
                                        getCountsSql = sql;
                                        if (sqlCommentsReplace != null && sqlCommentsReplace.Count > 0)
                                        {
                                            getCountsSql = Regex.Replace(getCountsSql, @"--pagination", "", RegexOptions.IgnoreCase);
                                            getCountsSql = ReplaceSqlComments(getCountsSql, sqlCommentsReplace);
                                        }
                                    }

                                    if (sqlCommentsReplace != null && sqlCommentsReplace.Count > 0)
                                    {
                                        sql = ReplaceSqlComments(sql, sqlCommentsReplace);
                                    }

                                    if (!string.IsNullOrEmpty(sql))
                                    {
                                        sqlTasks.Add(ExecuteSQLAsync(sql, connectionString, getCountsSql: getCountsSql));
                                    }
                                }

                                var sqlResults = await Task.WhenAll(sqlTasks);

                                for (int i = 0; i < sqlResults.Length; i++)
                                {
                                    if (string.IsNullOrEmpty(sqlResults[i].error))
                                    {
                                        sqlResults[i].data.TableName = $"Table{i}";
                                        sqlDataSetResult.DataSet.Tables.Add(sqlResults[i].data);
                                        sqlDataSetResult.RecordsCount.Add(sqlResults[i].count);
                                    }
                                    else
                                    {
                                        sqlDataSetResult.error += sqlResults[i].error;
                                    }
                                }
                            }
                        }

                    }
                }
                catch (Exception ex)
                {
                    sqlDataSetResult.error += ex.Message;

                    string error = ex.Message;
                    error += Environment.NewLine + ex.InnerException;
                    error += Environment.NewLine + ex.StackTrace;

                    if (_traceEnabled) _traceService.LogError(error);
                }
                return sqlDataSetResult;
            }


            public async Task<DataTable> ExecuteQueryAsync(string query, string connectionString, DBParamsDetails paramsDetails)
            {
                DataTable dt = new DataTable();

                if (query.Trim().EndsWith(";"))
                    query = query.Trim().Substring(0, query.Trim().Length - 1);
                try
                {
                    using (var connection = new NpgsqlConnection(connectionString))
                    {
                        if (_traceEnabled) _traceService.LogDbConnection(connectionString);
                        DateTime startTime = DateTime.Now;

                        await connection.OpenAsync();
                        using (var command = new NpgsqlCommand(query, connection))
                        {

                            if (_traceEnabled)
                            {
                                _traceService.LogSqlExecution(query);
                                _traceService.LogMessage("Params: " + JsonConvert.SerializeObject(paramsDetails));
                            }

                            try
                            {
                                if (paramsDetails.ParamsNames.Count > 0)
                                {
                                    foreach (var item in paramsDetails.ParamsNames)
                                    {
                                        command.Parameters.Add(item.Name, GetNpgsqlDbType((DbType)item.Type)).Value = item.Value;
                                    }
                                }
                                command.Prepare();
                                var dr = await command.ExecuteReaderAsync();
                                dt.Load(dr);

                                DateTime endTime = DateTime.Now;
                                if (_traceEnabled) _traceService.LogDataTable(dt, startTime, endTime); ;

                            }
                            catch (Exception ex)
                            {
                                dt.Rows.Add(ex.Message);

                                string error = ex.Message;
                                error += Environment.NewLine + ex.InnerException;
                                error += Environment.NewLine + ex.StackTrace;

                                if (_traceEnabled) _traceService.LogError(error);

                            }
                            finally
                            {
                                await connection.CloseAsync();
                            }
                        }

                    }
                }
                catch (Exception ex)
                {
                    dt.Rows.Add(ex.Message);

                    string error = ex.Message;
                    error += Environment.NewLine + ex.InnerException;
                    error += Environment.NewLine + ex.StackTrace;

                    if (_traceEnabled) _traceService.LogError(error);
                }
                return dt;
            }

            public async Task<SQLResult> ExecuteStoredProcedureAsync(string storedProcedure, string connectionString, string[] paramName = null, DbType[] paramType = null, object[] paramValue = null)
            {
                return new SQLResult(); //To-Do: StoredProcedure implementation
            }

            public async Task<SQLResult> ExecuteStoredProcedureAsync(string storedProcedure, string[] paramName = null, DbType[] paramType = null, object[] paramValue = null)
            {
                return new SQLResult(); //To-Do: StoredProcedure implementation
            }

            public async Task<TransactionResult> ExecuteSQLsInTransactionAsync(List<SQLCommandBuilder> sqlCommands, bool commitTransaction = true)
            {
                var result = new TransactionResult();

                if (sqlCommands == null || sqlCommands.Count == 0)
                {
                    result.success = false;
                    result.error = "No SQL commands to execute.";
                    return result;
                }

                try
                {
                    if (_connection == null || _connection.State != ConnectionState.Open)
                        await OpenDBConnectionAsync();

                    if (_transaction == null)
                    {                        
                        _transaction = (NpgsqlTransaction)await _connection.BeginTransactionAsync();
                        if (_traceEnabled) _traceService.LogMessage($"Transaction started.", "");
                    }

                    try
                    {
                        int totalRowsAffected = 0;
                        foreach (var sqlCommand in sqlCommands)
                        {
                            using var cmd = _connection.CreateCommand();
                            var query = sqlCommand.Query;

                            if (query.Trim().EndsWith(";"))
                                query = query.Trim().Substring(0, query.Trim().Length - 1);

                            cmd.Transaction = _transaction;
                            cmd.CommandText = query;
                            cmd.CommandType = CommandType.Text;

                            if (sqlCommand.ParameterNames != null)
                            {
                                if (sqlCommand.ParameterNames.Length != sqlCommand.ParameterValues.Length || sqlCommand.ParameterNames.Length != sqlCommand.ParameterTypes.Length)
                                {
                                    result.success = false;
                                    result.error = "Invalid params in SQL";
                                    if (_traceEnabled) _traceService.LogError(result.error);
                                }

                                for (int i = 0; i < sqlCommand.ParameterNames.Length; i++)
                                {
                                    cmd.Parameters.Add(sqlCommand.ParameterNames[i], GetNpgsqlDbType(sqlCommand.ParameterTypes[i])).Value = sqlCommand.ParameterValues[i];
                                }

                                if (_traceEnabled) _traceService.LogSqlExecution(query, sqlCommand.ParameterNames, sqlCommand.ParameterTypes, sqlCommand.ParameterValues);
                            }
                            else
                                if (_traceEnabled) _traceService.LogSqlExecution(query);


                            var rowsAffected = await cmd.ExecuteNonQueryAsync();
                            if (_traceEnabled) _traceService.LogSqlExecution($"Rows Affected : {rowsAffected}");

                            totalRowsAffected += rowsAffected;
                        }

                        if (commitTransaction)
                        {
                            CommitTransaction();
                        }
                        result.success = true;
                        result.TotalStatements = sqlCommands.Count;
                        result.TotalRowsAffected = totalRowsAffected;

                        if (_traceEnabled)
                        {
                            _traceService.LogMessage($"Transaction : Success");
                            _traceService.LogMessage($"Total Statements : {sqlCommands.Count}");
                            _traceService.LogMessage($"Total Rows Affected : {totalRowsAffected}");
                        }
                    }
                    catch (Exception ex)
                    {
                        RollBackTransaction();

                        result.success = false;
                        result.error = ex.Message;
                        result.ExceptionDetail = ex;

                        if (_traceEnabled) _traceService.LogError(JsonConvert.SerializeObject(result));
                    }
                }
                catch (Exception ex)
                {
                    result.success = false;
                    result.error = ex.Message;
                    result.ExceptionDetail = ex;
                    if (_traceEnabled) _traceService.LogError(JsonConvert.SerializeObject(result));
                }

                return result;
            }
            public NonQueryResult CommitTransaction()
            {
                var result = new NonQueryResult();
                try
                {
                    if (_transaction == null)
                    {
                        result.error = "Transaction is null";
                        if (_traceEnabled) _traceService.LogError($"Transaction is null");
                        return result;
                    }

                    try
                    {
                        _transaction.Commit();
                        _transaction.Dispose();
                        if (_traceEnabled)
                        {
                            _traceService.LogMessage($"Transaction Committed.", "");
                            _traceService.LogMessage($"Transaction Disposed.");
                            _traceService.LogMessage($"Transaction : Success", "");
                        }
                    }
                    catch (Exception ex)
                    {
                        _transaction.Rollback();
                        _transaction.Dispose();
                        if (_traceEnabled)
                        {
                            _traceService.LogMessage($"Transaction Rolledback.", "");
                            _traceService.LogMessage($"Transaction Disposed.");
                            _traceService.LogError($"Transaction : failed");
                        }
                        result.error = ex.Message;

                        if (_traceEnabled) _traceService.LogError(JsonConvert.SerializeObject(result));
                    }
                }
                catch (Exception ex)
                {
                    _traceService.LogError($"Transaction : failed");
                    result.error = ex.Message;
                    if (_traceEnabled) _traceService.LogError(JsonConvert.SerializeObject(result));
                }
                return result;
            }

            public NonQueryResult RollBackTransaction()
            {
                var result = new NonQueryResult();
                try
                {
                    if (_transaction == null)
                    {
                        result.error = "Transaction is null";
                        if (_traceEnabled) _traceService.LogError($"Transaction is null");
                        return result;
                    }

                    try
                    {
                        _transaction.Rollback();
                        _transaction.Dispose();
                        if (_traceEnabled)
                        {
                            _traceService.LogMessage($"Transaction Rolledback.", "");
                            _traceService.LogMessage($"Transaction Disposed.");
                            _traceService.LogMessage($"Transaction : failed", "");
                        }
                    }
                    catch (Exception ex)
                    {
                        _transaction.Rollback();
                        _transaction.Dispose();
                        if (_traceEnabled)
                        {
                            _traceService.LogMessage($"Transaction Rolledback.", "");
                            _traceService.LogMessage($"Transaction Disposed.");
                            _traceService.LogError($"Transaction : failed");
                        }
                        result.error = ex.Message;

                        if (_traceEnabled) _traceService.LogError(JsonConvert.SerializeObject(result));
                    }
                }
                catch (Exception ex)
                {
                    _traceService.LogError($"Transaction : failed");
                    result.error = ex.Message;
                    if (_traceEnabled) _traceService.LogError(JsonConvert.SerializeObject(result));
                }
                return result;
            }
            public async Task<DataTable> ExecuteSqlSchemaAsync(string sqlQuery)
            {
                DataTable schemaTable = new DataTable();
                try
                {
                    if (_connection == null || _connection.State != ConnectionState.Open)
                    {
                        await OpenDBConnectionAsync();
                    }
                    sqlQuery = Regex.Replace(sqlQuery, @":\w+", "'dummy'");
                    using var cmd = new NpgsqlCommand(sqlQuery, _connection);
                    using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SchemaOnly);
                    schemaTable = reader.GetSchemaTable();
                }
                catch (Exception ex)
                {
                    string error = $"Error executing PostgreSQL schema query: {ex.Message}";
                    if (_traceEnabled)
                        _traceService.LogError(error);

                    throw new Exception(ex.Message);
                }
                return schemaTable;
            }

            private static NpgsqlDbType GetNpgsqlDbType(DbType type)
            {
                switch (type)
                {
                    case DbType.Int32:
                        return NpgsqlDbType.Integer;
                    case DbType.Int64:
                        return NpgsqlDbType.Numeric;
                    case DbType.Decimal:
                        return NpgsqlDbType.Numeric;
                    case DbType.String:
                        return NpgsqlDbType.Varchar;
                    case DbType.DateTime:
                        return NpgsqlDbType.Timestamp;
                    case DbType.Date:
                        return NpgsqlDbType.Date;
                    case DbType.Binary:
                        return NpgsqlDbType.Bytea;
                    case DbType.Guid:
                        return NpgsqlDbType.Uuid;
                    case DbType.Object:// JSON support
                        return NpgsqlDbType.Json;
                    default:
                        return NpgsqlDbType.Varchar;

                }
            }

            
        }

        public class MSSQLHelper : IDbHelper
        {
            private readonly string _query;
            private string _connectionString;
            private readonly string[] _paramName;
            private readonly DbType[] _paramType;
            private readonly object[] _paramValue;
            private readonly IARMLogTraceService _traceService;
            private readonly bool _traceEnabled;
            public double _dbConnTime = 0;
            public double _dbTimeTaken = 0;
            private SqlConnection _connection;
            private SqlTransaction _transaction;

            public MSSQLHelper(string query, string connectionString, string[] paramName, DbType[] paramType, object[] paramValue, IARMLogTraceService traceService = null)
            {
                _query = query;
                _connectionString = connectionString;
                _paramName = paramName;
                _paramType = paramType;
                _paramValue = paramValue;
                _traceService = traceService;

                if (_traceService != null && _traceService.IsTracingEnabled())
                    _traceEnabled = true;
            }

            public MSSQLHelper(IARMLogTraceService traceService = null)
            {
                _traceService = traceService;

                if (_traceService != null && _traceService.IsTracingEnabled())
                    _traceEnabled = true;
            }

            public async Task OpenDBConnectionAsync()
            {
                DateTime startTime = DateTime.Now;
                _connection = new SqlConnection(_connectionString);
                await _connection.OpenAsync();
                if (_connection.State == ConnectionState.Open)
                    SetDBConnectionTime(startTime, DateTime.Now);
            }

            public async Task CloseDBConnectionAsync()
            {
                await _connection.CloseAsync();
            }

            public string ConnectionString
            {
                get { return _connectionString; }
                set { _connectionString = value; }
            }
            
            public DbTransaction Transaction
            {
                get { return (DbTransaction)_transaction; }
                set { _transaction = (SqlTransaction)value; }
            }

            public string GetDBType()
            {
                return Constants.DBTYPE.MSSQL.ToString();
            }

            public double SetDBConnectionTime(DateTime startTime, DateTime endTime)
            {
                TimeSpan duration = endTime - startTime;
                _dbConnTime += duration.TotalMilliseconds;

                return _dbConnTime;
            }
            public double SetDBTimeTaken(DateTime startTime, DateTime endTime)
            {
                TimeSpan duration = endTime - startTime;
                _dbTimeTaken += duration.TotalMilliseconds;

                return _dbTimeTaken;
            }

            public double GetDBConnectionTime()
            {
                return _dbConnTime;
            }
            public double GetDBTimeTaken()
            {
                return _dbTimeTaken;
            }

            public string ReplaceSqlParameters(string sql, Dictionary<string, string> parameters)
            {
                Regex regex = new Regex(@"\:\w+");
                StringBuilder sb = new StringBuilder(sql);
                MatchCollection matches = regex.Matches(sql);

                // Create a case-insensitive dictionary lookup
                var caseInsensitiveParams = new Dictionary<string, string>(parameters, StringComparer.OrdinalIgnoreCase);

                // Iterate in reverse to avoid messing up string indexes
                for (int i = matches.Count - 1; i >= 0; i--)
                {
                    Match match = matches[i];
                    string paramKey = match.Value.Substring(1); // Remove leading ':'

                    if (caseInsensitiveParams.TryGetValue(paramKey, out string value))
                    {
                        sb.Remove(match.Index, match.Length);
                        sb.Insert(match.Index, "'" + value.Replace("'", "''") + "'"); // Escape single quotes for SQL
                    }
                }

                return sb.ToString();
            }

            public string ReplaceSqlComments(string sql, Dictionary<string, string> comments)
            {
                foreach (var entry in comments)
                {
                    sql = Regex.Replace(sql, Regex.Escape(entry.Key), entry.Value, RegexOptions.IgnoreCase);
                }
                return sql;
            }

            public MSSQLHelper(string query, string connectionString, string[] paramName, DbType[] paramType, object[] paramValue)
            {
                _query = query;
                _connectionString = connectionString;
                _paramName = paramName;
                _paramType = paramType;
                _paramValue = paramValue;
            }

            public async Task<DataTable> ExecuteQueryAsync(string query, string connectionString)
            {
                DataTable dt = new DataTable();

                if (query.Trim().EndsWith(";"))
                    query = query.Trim().Substring(0, query.Trim().Length - 1);

                try
                {
                    if (_traceEnabled) _traceService.LogDbConnection(connectionString);
                    DateTime startTime = DateTime.Now;

                    using (var connection = new SqlConnection(connectionString))
                    {
                        await connection.OpenAsync();

                        if (query.Contains("$SCHEMA$"))
                        {
                            var builder = new SqlConnectionStringBuilder(connectionString);
                            string schemaName = builder.UserID;
                            query = query.Replace("$SCHEMA$", schemaName);
                        }

                        if (_traceEnabled) _traceService.LogSqlExecution(query);

                        using (var cmd = new SqlCommand(query, connection))
                        {
                            try
                            {
                                var dr = await cmd.ExecuteReaderAsync();
                                dt.Load(dr);
                                dt = ChangeColumnNamesToLowerCase(dt);

                                DateTime endTime = DateTime.Now;
                                if (_traceEnabled) _traceService.LogDataTable(dt, startTime, endTime); ;

                            }
                            catch (Exception ex)
                            {
                                dt.Rows.Add(ex.Message);

                                string error = ex.Message;
                                error += Environment.NewLine + ex.InnerException;
                                error += Environment.NewLine + ex.StackTrace;

                                if (_traceEnabled) _traceService.LogError(error);
                            }
                            finally
                            {
                                await connection.CloseAsync();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    dt.Rows.Add(ex.Message);

                    string error = ex.Message;
                    error += Environment.NewLine + ex.InnerException;
                    error += Environment.NewLine + ex.StackTrace;

                    if (_traceEnabled) _traceService.LogError(error);
                }

                return dt;
            }

            public async Task<NonQueryResult> ExecuteNonQueryAsync(string query, string connectionString, string[] paramName = null, DbType[] paramType = null, object[] paramValue = null)
            {
                var result = new NonQueryResult();

                if (query.Trim().EndsWith(";"))
                    query = query.Trim().Substring(0, query.Trim().Length - 1);

                try
                {
                    if (_traceEnabled) _traceService.LogDbConnection(connectionString);
                    DateTime startTime = DateTime.Now;

                    using (var connection = new SqlConnection(connectionString))
                    {
                        await connection.OpenAsync();

                        if (query.Contains("$SCHEMA$"))
                        {
                            var builder = new SqlConnectionStringBuilder(connectionString);
                            string schemaName = builder.UserID;
                            query = query.Replace("$SCHEMA$", schemaName);
                        }

                        if (_traceEnabled) _traceService.LogSqlExecution(query, paramName, paramType, paramValue);

                        using (var cmd = new SqlCommand(query, connection))
                        {
                            if (paramName != null)
                            {
                                if (paramName.Length != paramValue.Length || paramName.Length != paramType.Length)
                                {
                                    result.error = "Invalid params in SQL";
                                    if (_traceEnabled) _traceService.LogError(result.error);
                                }

                                for (int i = 0; i < paramName.Length; i++)
                                {
                                    cmd.Parameters.Add(new SqlParameter(paramName[i], GetSqlDbType(paramType[i])) { Value = paramValue[i] });
                                }
                            }

                            try
                            {
                                result.count = await cmd.ExecuteNonQueryAsync();

                                DateTime endTime = DateTime.Now;
                                if (_traceEnabled) _traceService.LogResultInfo(result.count, startTime, endTime);
                            }
                            catch (Exception ex)
                            {
                                result.error += ex.Message;

                                string error = ex.Message;
                                error += Environment.NewLine + ex.InnerException;
                                error += Environment.NewLine + ex.StackTrace;

                                if (_traceEnabled) _traceService.LogError(error);
                            }
                            finally
                            {
                                await connection.CloseAsync();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    result.error += ex.Message;

                    string error = ex.Message;
                    error += Environment.NewLine + ex.InnerException;
                    error += Environment.NewLine + ex.StackTrace;

                    if (_traceEnabled) _traceService.LogError(error);
                }

                return result;
            }

            public async Task<NonQueryResult> ExecuteNonQueryAsync(string query, DbConnection connection, string[] paramName = null, DbType[] paramType = null, object[] paramValue = null)
            {
                var result = new NonQueryResult();

                if (query.Trim().EndsWith(";"))
                    query = query.Trim().Substring(0, query.Trim().Length - 1);

                try
                {
                    if (_traceEnabled) _traceService.LogDbConnection(connection.ConnectionString);
                    DateTime startTime = DateTime.Now;

                    //using (var connection = new SqlConnection(connectionString))
                    {
                        if (connection?.State != ConnectionState.Open)
                        {
                            await connection.OpenAsync();
                        }

                        if (query.Contains("$SCHEMA$"))
                        {
                            var builder = new SqlConnectionStringBuilder(connection.ConnectionString);
                            string schemaName = builder.UserID;
                            query = query.Replace("$SCHEMA$", schemaName);
                        }

                        if (_traceEnabled) _traceService.LogSqlExecution(query, paramName, paramType, paramValue);

                        using (var cmd = new SqlCommand(query, (SqlConnection)connection))
                        {
                            if (paramName != null)
                            {
                                if (paramName.Length != paramValue.Length || paramName.Length != paramType.Length)
                                {
                                    result.error = "Invalid params in SQL";
                                    if (_traceEnabled) _traceService.LogError(result.error);
                                }

                                for (int i = 0; i < paramName.Length; i++)
                                {
                                    cmd.Parameters.Add(new SqlParameter(paramName[i], GetSqlDbType(paramType[i])) { Value = paramValue[i] });
                                }
                            }

                            try
                            {
                                result.count = await cmd.ExecuteNonQueryAsync();

                                DateTime endTime = DateTime.Now;
                                if (_traceEnabled) _traceService.LogResultInfo(result.count, startTime, endTime);
                            }
                            catch (Exception ex)
                            {
                                result.error += ex.Message;

                                string error = ex.Message;
                                error += Environment.NewLine + ex.InnerException;
                                error += Environment.NewLine + ex.StackTrace;

                                if (_traceEnabled) _traceService.LogError(error);
                            }
                            //finally
                            //{
                            //    await connection.CloseAsync();
                            //}
                        }
                    }
                }
                catch (Exception ex)
                {
                    result.error += ex.Message;

                    string error = ex.Message;
                    error += Environment.NewLine + ex.InnerException;
                    error += Environment.NewLine + ex.StackTrace;

                    if (_traceEnabled) _traceService.LogError(error);
                }

                return result;
            }

            public async Task<ScalarResult> ExecuteScalarAsync(string query, string connectionString, string[] paramName = null, DbType[] paramType = null, object[] paramValue = null)
            {
                var result = new ScalarResult();
                if (query.Trim().EndsWith(";"))
                    query = query.Trim().Substring(0, query.Trim().Length - 1);
                try
                {
                    if (_traceEnabled) _traceService.LogDbConnection(connectionString);
                    DateTime startTime = DateTime.Now;

                    using (var connection = new SqlConnection(connectionString))
                    {
                        await connection.OpenAsync();

                        if (query.Contains("$SCHEMA$"))
                        {
                            var builder = new SqlConnectionStringBuilder(connectionString);
                            string schemaName = builder.UserID;
                            query = query.Replace("$SCHEMA$", schemaName);
                        }

                        if (_traceEnabled) _traceService.LogSqlExecution(query, paramName, paramType, paramValue);

                        using (var cmd = new SqlCommand(query, connection))
                        {
                            if (paramName != null)
                            {
                                if (paramName.Length != paramValue.Length || paramName.Length != paramType.Length)
                                {
                                    result.error = "Invalid params in SQL";
                                    if (_traceEnabled) _traceService.LogError(result.error);
                                }

                                for (int i = 0; i < paramName.Length; i++)
                                {
                                    cmd.Parameters.Add(new SqlParameter(paramName[i], GetSqlDbType(paramType[i])) { Value = paramValue[i] });
                                }
                            }

                            try
                            {
                                //cmd.Prepare();
                                result.result = await cmd.ExecuteScalarAsync();
                                DateTime endTime = DateTime.Now;
                                if (_traceEnabled) _traceService.LogScalarResultInfo(result.result?.ToString() ?? "", startTime, endTime);

                            }
                            catch (Exception ex)
                            {
                                result.error = ex.Message;

                                string error = ex.Message;
                                error += Environment.NewLine + ex.InnerException;
                                error += Environment.NewLine + ex.StackTrace;

                                if (_traceEnabled) _traceService.LogError(error);

                            }
                            finally
                            {
                                await connection.CloseAsync();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    result.error = ex.Message;

                    string error = ex.Message;
                    error += Environment.NewLine + ex.InnerException;
                    error += Environment.NewLine + ex.StackTrace;

                    if (_traceEnabled) _traceService.LogError(error);

                }

                return result;
            }

            public async Task<ScalarResult> ExecuteScalarAsync(string query, string[] paramName = null, DbType[] paramType = null, object[] paramValue = null)
            {
                var result = new ScalarResult();
                if (query.Trim().EndsWith(";"))
                    query = query.Trim().Substring(0, query.Trim().Length - 1);
                try
                {
                    DateTime startTime = DateTime.Now;

                    if (_traceEnabled) _traceService.LogSqlExecution(query, paramName, paramType, paramValue);
                    if (_connection == null || _connection.State != ConnectionState.Open)
                        await OpenDBConnectionAsync();

                    using var cmd = new SqlCommand(query, _connection);

                    if (paramName != null)
                    {
                        if (paramName.Length != paramValue.Length || paramName.Length != paramType.Length)
                        {
                            result.error = "Invalid params in SQL";
                            if (_traceEnabled) _traceService.LogError(result.error);
                        }

                        for (int i = 0; i < paramName.Length; i++)
                        {
                            if (paramType[i] == DbType.DateTime || paramType[i] == DbType.Date)
                            {
                                if (string.IsNullOrEmpty(paramValue[i]?.ToString()))
                                {
                                    paramValue[i] = DBNull.Value;
                                }
                                else
                                    paramType[i] = DbType.String;
                            }

                            cmd.Parameters.Add(paramName[i], GetSqlDbType(paramType[i])).Value = paramValue[i];
                        }
                    }

                    try
                    {
                        //cmd.Prepare();
                        result.result = await cmd.ExecuteScalarAsync();
                        DateTime endTime = DateTime.Now;
                        if (_traceEnabled) _traceService.LogScalarResultInfo(result.result?.ToString() ?? "", startTime, endTime);

                    }
                    catch (Exception ex)
                    {
                        result.error = ex.Message;

                        string error = ex.Message;
                        error += Environment.NewLine + ex.InnerException;
                        error += Environment.NewLine + ex.StackTrace;

                        if (_traceEnabled) _traceService.LogError(error);

                    }


                }
                catch (Exception ex)
                {
                    result.error = ex.Message;

                    string error = ex.Message;
                    error += Environment.NewLine + ex.InnerException;
                    error += Environment.NewLine + ex.StackTrace;

                    if (_traceEnabled) _traceService.LogError(error);

                }

                return result;
            }

            public NonQueryResult ExecuteNonQuery(string query, string connectionString, string[] paramName = null, DbType[] paramType = null, object[] paramValue = null)
            {
                var result = new NonQueryResult();

                if (query.Trim().EndsWith(";"))
                    query = query.Trim().Substring(0, query.Trim().Length - 1);

                try
                {
                    if (_traceEnabled) _traceService.LogDbConnection(connectionString);
                    DateTime startTime = DateTime.Now;

                    using (var connection = new SqlConnection(connectionString))
                    {
                        connection.Open();

                        if (query.Contains("$SCHEMA$"))
                        {
                            var builder = new SqlConnectionStringBuilder(connectionString);
                            string schemaName = builder.UserID;
                            query = query.Replace("$SCHEMA$", schemaName);
                        }

                        if (_traceEnabled) _traceService.LogSqlExecution(query, paramName, paramType, paramValue);

                        using (var cmd = new SqlCommand(query, connection))
                        {
                            if (paramName != null)
                            {
                                if (paramName.Length != paramValue.Length || paramName.Length != paramType.Length)
                                {
                                    result.error = "Invalid params in SQL";
                                    if (_traceEnabled) _traceService.LogError(result.error);
                                }

                                for (int i = 0; i < paramName.Length; i++)
                                {
                                    cmd.Parameters.Add(new SqlParameter(paramName[i], GetSqlDbType(paramType[i])) { Value = paramValue[i] });
                                }
                            }

                            try
                            {
                                result.count = cmd.ExecuteNonQuery();

                                DateTime endTime = DateTime.Now;
                                if (_traceEnabled) _traceService.LogResultInfo(result.count, startTime, endTime);
                            }
                            catch (Exception ex)
                            {
                                result.error += ex.Message;

                                string error = ex.Message;
                                error += Environment.NewLine + ex.InnerException;
                                error += Environment.NewLine + ex.StackTrace;

                                if (_traceEnabled) _traceService.LogError(error);
                            }
                            finally
                            {
                                connection.Close();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    result.error += ex.Message;

                    string error = ex.Message;
                    error += Environment.NewLine + ex.InnerException;
                    error += Environment.NewLine + ex.StackTrace;

                    if (_traceEnabled) _traceService.LogError(error);
                }

                return result;
            }

            public ScalarResult ExecuteScalar(string query, string connectionString, string[] paramName = null, DbType[] paramType = null, object[] paramValue = null)
            {
                var result = new ScalarResult();
                if (query.Trim().EndsWith(";"))
                    query = query.Trim().Substring(0, query.Trim().Length - 1);
                try
                {
                    if (_traceEnabled) _traceService.LogDbConnection(connectionString);
                    DateTime startTime = DateTime.Now;

                    using (var connection = new SqlConnection(connectionString))
                    {
                        connection.Open();

                        if (query.Contains("$SCHEMA$"))
                        {
                            var builder = new SqlConnectionStringBuilder(connectionString);
                            string schemaName = builder.UserID;
                            query = query.Replace("$SCHEMA$", schemaName);
                        }

                        if (_traceEnabled) _traceService.LogSqlExecution(query, paramName, paramType, paramValue);

                        using (var cmd = new SqlCommand(query, connection))
                        {
                            if (paramName != null)
                            {
                                if (paramName.Length != paramValue.Length || paramName.Length != paramType.Length)
                                {
                                    result.error = "Invalid params in SQL";
                                    if (_traceEnabled) _traceService.LogError(result.error);
                                }

                                for (int i = 0; i < paramName.Length; i++)
                                {
                                    cmd.Parameters.Add(new SqlParameter(paramName[i], GetSqlDbType(paramType[i])) { Value = paramValue[i] });
                                }
                            }

                            try
                            {
                                //cmd.Prepare();
                                result.result = cmd.ExecuteScalar();
                                DateTime endTime = DateTime.Now;
                                if (_traceEnabled) _traceService.LogScalarResultInfo(result.result?.ToString() ?? "", startTime, endTime);

                            }
                            catch (Exception ex)
                            {
                                result.error = ex.Message;

                                string error = ex.Message;
                                error += Environment.NewLine + ex.InnerException;
                                error += Environment.NewLine + ex.StackTrace;

                                if (_traceEnabled) _traceService.LogError(error);

                            }
                            finally
                            {
                                connection.Close();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    result.error = ex.Message;

                    string error = ex.Message;
                    error += Environment.NewLine + ex.InnerException;
                    error += Environment.NewLine + ex.StackTrace;

                    if (_traceEnabled) _traceService.LogError(error);

                }

                return result;
            }

            public async Task<ScalarResult> ExecuteScalarAsync(string query, DbConnection connection, string[] paramName = null, DbType[] paramType = null, object[] paramValue = null)
            {
                var result = new ScalarResult();
                if (query.Trim().EndsWith(";"))
                    query = query.Trim().Substring(0, query.Trim().Length - 1);
                try
                {
                    //if (_traceEnabled) _traceService.LogDbConnection(connectionString);


                    //using (var connection = new SqlConnection(connectionString))
                    {

                        DateTime startTime = DateTime.Now;

                        if (connection?.State != ConnectionState.Open)
                        {
                            await connection.OpenAsync();
                        }

                        if (query.Contains("$SCHEMA$"))
                        {
                            var builder = new SqlConnectionStringBuilder(connection.ConnectionString);
                            string schemaName = builder.UserID;
                            query = query.Replace("$SCHEMA$", schemaName);
                        }

                        if (_traceEnabled) _traceService.LogSqlExecution(query, paramName, paramType, paramValue);

                        using (var cmd = new SqlCommand(query, (SqlConnection)connection))
                        {
                            if (paramName != null)
                            {
                                if (paramName.Length != paramValue.Length || paramName.Length != paramType.Length)
                                {
                                    result.error = "Invalid params in SQL";
                                    if (_traceEnabled) _traceService.LogError(result.error);
                                }

                                for (int i = 0; i < paramName.Length; i++)
                                {
                                    cmd.Parameters.Add(new SqlParameter(paramName[i], GetSqlDbType(paramType[i])) { Value = paramValue[i] });
                                }
                            }

                            try
                            {
                                //cmd.Prepare();
                                result.result = await cmd.ExecuteScalarAsync();
                                DateTime endTime = DateTime.Now;

                                SetDBTimeTaken(startTime, endTime);
                                if (_traceEnabled) _traceService.LogScalarResultInfo(result.result?.ToString() ?? "", startTime, endTime);

                            }
                            catch (Exception ex)
                            {
                                result.error = ex.Message;

                                string error = ex.Message;
                                error += Environment.NewLine + ex.InnerException;
                                error += Environment.NewLine + ex.StackTrace;

                                if (_traceEnabled) _traceService.LogError(error);

                            }
                            //finally
                            //{
                            //    await connection.CloseAsync();
                            //}
                        }
                    }
                }
                catch (Exception ex)
                {
                    result.error = ex.Message;

                    string error = ex.Message;
                    error += Environment.NewLine + ex.InnerException;
                    error += Environment.NewLine + ex.StackTrace;

                    if (_traceEnabled) _traceService.LogError(error);

                }

                return result;
            }


            public async Task<DataTable> ExecuteQueryAsync(string query, string connectionString, string[] paramName, DbType[] paramType, object[] paramValue)
            {
                DataTable dt = new DataTable();

                if (query.Trim().EndsWith(";"))
                    query = query.Trim().Substring(0, query.Trim().Length - 1);

                try
                {
                    if (_traceEnabled) _traceService.LogDbConnection(connectionString);
                    DateTime startTime = DateTime.Now;

                    using (var connection = new SqlConnection(connectionString))
                    {
                        await connection.OpenAsync();

                        if (query.Contains("$SCHEMA$"))
                        {
                            var builder = new SqlConnectionStringBuilder(connectionString);
                            string schemaName = builder.UserID;
                            query = query.Replace("$SCHEMA$", schemaName);
                        }

                        if (_traceEnabled) _traceService.LogSqlExecution(query, paramName, paramType, paramValue);

                        using (var cmd = new SqlCommand(query, connection))
                        {
                            if (paramName != null)
                            {
                                if (paramName.Length != paramValue.Length || paramName.Length != paramType.Length)
                                {
                                    dt.Rows.Add("Invalid params in SQL");
                                    if (_traceEnabled) _traceService.LogError("Invalid params in SQL");
                                }
                                for (int i = 0; i < paramName.Length; i++)
                                {
                                    cmd.Parameters.Add(new SqlParameter(paramName[i], GetSqlDbType(paramType[i])) { Value = paramValue[i] });
                                }

                            }
                            try
                            {
                                var dr = await cmd.ExecuteReaderAsync();
                                dt.Load(dr);
                                dt = ChangeColumnNamesToLowerCase(dt);

                                DateTime endTime = DateTime.Now;
                                if (_traceEnabled) _traceService.LogDataTable(dt, startTime, endTime); ;
                            }
                            catch (Exception ex)
                            {
                                dt.Rows.Add(ex.Message);

                                string error = ex.Message;
                                error += Environment.NewLine + ex.InnerException;
                                error += Environment.NewLine + ex.StackTrace;

                                if (_traceEnabled) _traceService.LogError(error);
                            }
                            finally
                            {
                                await connection.CloseAsync();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    dt.Rows.Add(ex.Message);

                    string error = ex.Message;
                    error += Environment.NewLine + ex.InnerException;
                    error += Environment.NewLine + ex.StackTrace;

                    if (_traceEnabled) _traceService.LogError(error);
                }

                return dt;
            }

            public async Task<SQLResult> ExecuteSQLAsync(string query, string connectionString, string[] paramName = null, DbType[] paramType = null, object[] paramValue = null, string getCountsSql = "")
            {
                SQLResult sqlResult = new SQLResult();

                if (query.Trim().EndsWith(";"))
                    query = query.Trim().Substring(0, query.Trim().Length - 1);

                try
                {
                    if (_traceEnabled) _traceService.LogDbConnection(connectionString);
                    DateTime startTime = DateTime.Now;

                    using (var connection = new SqlConnection(connectionString))
                    {
                        await connection.OpenAsync();

                        if (query.Contains("$SCHEMA$"))
                        {
                            var builder = new SqlConnectionStringBuilder(connectionString);
                            string schemaName = builder.UserID;
                            query = query.Replace("$SCHEMA$", schemaName);
                        }

                        if (_traceEnabled) _traceService.LogSqlExecution(query, paramName, paramType, paramValue);

                        using (var cmd = new SqlCommand(query, connection))
                        {
                            if (paramName != null)
                            {
                                if (paramName.Length != paramValue.Length || paramName.Length != paramType.Length)
                                {
                                    sqlResult.error = "Invalid params in SQL";
                                    if (_traceEnabled) _traceService.LogError(sqlResult.error);
                                }
                                for (int i = 0; i < paramName.Length; i++)
                                {
                                    cmd.Parameters.Add(new SqlParameter(paramName[i], GetSqlDbType(paramType[i])) { Value = paramValue[i] });
                                }
                            }
                            try
                            {
                                var dr = await cmd.ExecuteReaderAsync();
                                sqlResult.data.Load(dr);
                                sqlResult.data = ChangeColumnNamesToLowerCase(sqlResult.data);


                                DateTime endTime = DateTime.Now;
                                if (_traceEnabled) _traceService.LogDataTable(sqlResult.data, startTime, endTime); ;
                            }
                            catch (Exception ex)
                            {
                                sqlResult.error += ex.Message;

                                string error = ex.Message;
                                error += Environment.NewLine + ex.InnerException;
                                error += Environment.NewLine + ex.StackTrace;

                                if (_traceEnabled) _traceService.LogError(error);
                            }
                            finally
                            {
                                await connection.CloseAsync();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    sqlResult.error += ex.Message;

                    string error = ex.Message;
                    error += Environment.NewLine + ex.InnerException;
                    error += Environment.NewLine + ex.StackTrace;

                    if (_traceEnabled) _traceService.LogError(error);
                }

                return sqlResult;
            }

            public async Task<SQLResult> ExecuteSQLAsync(string query, DbConnection connection, string[] paramName = null, DbType[] paramType = null, object[] paramValue = null, string getCountsSql = "")
            {
                SQLResult sqlResult = new SQLResult();

                if (query.Trim().EndsWith(";"))
                    query = query.Trim().Substring(0, query.Trim().Length - 1);

                try
                {
                    //if (_traceEnabled) _traceService.LogDbConnection(connectionString);
                    DateTime startTime = DateTime.Now;

                    //using (var connection = new SqlConnection(connectionString))
                    {
                        if (connection?.State != ConnectionState.Open)
                        {
                            await connection.OpenAsync();
                        }

                        if (query.Contains("$SCHEMA$"))
                        {
                            var builder = new SqlConnectionStringBuilder(connection.ConnectionString);
                            string schemaName = builder.UserID;
                            query = query.Replace("$SCHEMA$", schemaName);
                        }

                        if (_traceEnabled) _traceService.LogSqlExecution(query, paramName, paramType, paramValue);

                        using (var cmd = new SqlCommand(query, (SqlConnection)connection))
                        {
                            if (paramName != null)
                            {
                                if (paramName.Length != paramValue.Length || paramName.Length != paramType.Length)
                                {
                                    sqlResult.error = "Invalid params in SQL";
                                    if (_traceEnabled) _traceService.LogError(sqlResult.error);
                                }
                                for (int i = 0; i < paramName.Length; i++)
                                {
                                    cmd.Parameters.Add(new SqlParameter(paramName[i], GetSqlDbType(paramType[i])) { Value = paramValue[i] });
                                }
                            }
                            try
                            {
                                var dr = await cmd.ExecuteReaderAsync();
                                sqlResult.data.Load(dr);
                                sqlResult.data = ChangeColumnNamesToLowerCase(sqlResult.data);


                                DateTime endTime = DateTime.Now;
                                if (_traceEnabled) _traceService.LogDataTable(sqlResult.data, startTime, endTime); ;
                            }
                            catch (Exception ex)
                            {
                                sqlResult.error += ex.Message;

                                string error = ex.Message;
                                error += Environment.NewLine + ex.InnerException;
                                error += Environment.NewLine + ex.StackTrace;

                                if (_traceEnabled) _traceService.LogError(error);
                            }
                            finally
                            {
                                //await connection.CloseAsync();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    sqlResult.error += ex.Message;

                    string error = ex.Message;
                    error += Environment.NewLine + ex.InnerException;
                    error += Environment.NewLine + ex.StackTrace;

                    if (_traceEnabled) _traceService.LogError(error);
                }

                return sqlResult;
            }

            public async Task<SQLResult> ExecuteSQLAsync(string query, string[] paramName = null, DbType[] paramType = null, object[] paramValue = null)
            {
                SQLResult sqlResult = new SQLResult();

                if (query.Trim().EndsWith(";"))
                    query = query.Trim().Substring(0, query.Trim().Length - 1);

                try
                {
                    DateTime startTime = DateTime.Now;

                    if (_connection == null || _connection.State != ConnectionState.Open)
                        await OpenDBConnectionAsync();

                    if (query.Contains("$SCHEMA$"))
                    {
                        var builder = new SqlConnectionStringBuilder(_connectionString);
                        string schemaName = builder.UserID;
                        query = query.Replace("$SCHEMA$", schemaName);
                    }

                    if (_traceEnabled) _traceService.LogSqlExecution(query, paramName, paramType, paramValue);

                    using var cmd = new SqlCommand(query, _connection);
                    if (paramName != null)
                    {
                        if (paramName.Length != paramValue.Length || paramName.Length != paramType.Length)
                        {
                            sqlResult.error = "Invalid params in SQL";
                            if (_traceEnabled) _traceService.LogError(sqlResult.error);
                        }
                        for (int i = 0; i < paramName.Length; i++)
                        {
                            if (!cmd.Parameters.Contains(paramName[i]))                            
                                cmd.Parameters.Add(new SqlParameter(paramName[i], GetSqlDbType(paramType[i])) { Value = paramValue[i] });
                        }
                    }
                    try
                    {
                        var dr = await cmd.ExecuteReaderAsync();
                        sqlResult.data.Load(dr);
                        sqlResult.data = ChangeColumnNamesToLowerCase(sqlResult.data);

                        DateTime endTime = DateTime.Now;
                        if (_traceEnabled) _traceService.LogDataTable(sqlResult.data, startTime, endTime); ;
                    }
                    catch (Exception ex)
                    {
                        sqlResult.error += ex.Message;

                        string error = ex.Message;
                        error += Environment.NewLine + ex.InnerException;
                        error += Environment.NewLine + ex.StackTrace;

                        if (_traceEnabled) _traceService.LogError(error);
                    }
                }
                catch (Exception ex)
                {
                    sqlResult.error += ex.Message;

                    string error = ex.Message;
                    error += Environment.NewLine + ex.InnerException;
                    error += Environment.NewLine + ex.StackTrace;

                    if (_traceEnabled) _traceService.LogError(error);
                }

                return sqlResult;
            }
            public SQLResult ExecuteSQL(string query, string connectionString, string[] paramName = null, DbType[] paramType = null, object[] paramValue = null, string getCountsSql = "")
            {
                SQLResult sqlResult = new SQLResult();

                if (query.Trim().EndsWith(";"))
                    query = query.Trim().Substring(0, query.Trim().Length - 1);

                try
                {
                    if (_traceEnabled) _traceService.LogDbConnection(connectionString);
                    DateTime startTime = DateTime.Now;

                    using (var connection = new SqlConnection(connectionString))
                    {
                        connection.Open();

                        if (query.Contains("$SCHEMA$"))
                        {
                            var builder = new SqlConnectionStringBuilder(connectionString);
                            string schemaName = builder.UserID;
                            query = query.Replace("$SCHEMA$", schemaName);
                        }

                        if (_traceEnabled) _traceService.LogSqlExecution(query, paramName, paramType, paramValue);

                        using (var cmd = new SqlCommand(query, connection))
                        {
                            if (paramName != null)
                            {
                                if (paramName.Length != paramValue.Length || paramName.Length != paramType.Length)
                                {
                                    sqlResult.error = "Invalid params in SQL";
                                    if (_traceEnabled) _traceService.LogError(sqlResult.error);
                                }
                                for (int i = 0; i < paramName.Length; i++)
                                {
                                    cmd.Parameters.Add(new SqlParameter(paramName[i], GetSqlDbType(paramType[i])) { Value = paramValue[i] });
                                }
                            }
                            try
                            {
                                var dr = cmd.ExecuteReader();
                                sqlResult.data.Load(dr);
                                sqlResult.data = ChangeColumnNamesToLowerCase(sqlResult.data);


                                DateTime endTime = DateTime.Now;
                                if (_traceEnabled) _traceService.LogDataTable(sqlResult.data, startTime, endTime); ;
                            }
                            catch (Exception ex)
                            {
                                sqlResult.error += ex.Message;

                                string error = ex.Message;
                                error += Environment.NewLine + ex.InnerException;
                                error += Environment.NewLine + ex.StackTrace;

                                if (_traceEnabled) _traceService.LogError(error);
                            }
                            finally
                            {
                                connection.Close();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    sqlResult.error += ex.Message;

                    string error = ex.Message;
                    error += Environment.NewLine + ex.InnerException;
                    error += Environment.NewLine + ex.StackTrace;

                    if (_traceEnabled) _traceService.LogError(error);
                }

                return sqlResult;
            }
            public async Task<SQLDataSetResult> ExecuteSQLsFromSQLAsync(string query, string connectionString, string[] paramName = null, DbType[] paramType = null, object[] paramValue = null, Dictionary<string, string>? viewFilters = null, Dictionary<string, string>? globalParams = null, Dictionary<string, string>? sqlCommentsReplace = null, bool getAllRecordsCount = false)
            {
                SQLDataSetResult sqlDataSetResult = new SQLDataSetResult();

                if (query.Trim().EndsWith(";"))
                    query = query.Trim().Substring(0, query.Trim().Length - 1);

                try
                {
                    if (_traceEnabled) _traceService.LogDbConnection(connectionString);
                    DateTime startTime = DateTime.Now;

                    using (var connection = new SqlConnection(connectionString))
                    {
                        await connection.OpenAsync();
                        if (query.Contains("$SCHEMA$"))
                        {
                            var builder = new SqlConnectionStringBuilder(connectionString);
                            string schemaName = builder.UserID;
                            query = query.Replace("$SCHEMA$", schemaName);
                        }

                        if (_traceEnabled) _traceService.LogSqlExecution(query, paramName, paramType, paramValue, viewFilters, globalParams, sqlCommentsReplace);

                        using (var cmd = new SqlCommand(query, connection))
                        {
                            if (paramName != null)
                            {
                                if (paramName.Length != paramValue.Length || paramName.Length != paramType.Length)
                                {
                                    sqlDataSetResult.error = "Invalid params in SQL";
                                    if (_traceEnabled) _traceService.LogError(sqlDataSetResult.error);
                                }

                                for (int i = 0; i < paramName.Length; i++)
                                {
                                    cmd.Parameters.Add(new SqlParameter(paramName[i], GetSqlDbType(paramType[i])) { Value = paramValue[i] });
                                }
                            }

                            var dt = new DataTable();

                            try
                            {
                                var dr = await cmd.ExecuteReaderAsync();
                                dt.Load(dr);

                                DateTime endTime = DateTime.Now;
                                if (_traceEnabled) _traceService.LogDataTable(dt, startTime, endTime); ;
                            }
                            catch (Exception ex)
                            {
                                sqlDataSetResult.error += ex.Message;

                                string error = ex.Message;
                                error += Environment.NewLine + ex.InnerException;
                                error += Environment.NewLine + ex.StackTrace;

                                if (_traceEnabled) _traceService.LogError(error);
                            }
                            finally
                            {
                                await connection.CloseAsync();
                            }

                            if (dt != null && dt.Rows.Count > 0)
                            {
                                List<Task<SQLResult>> sqlTasks = new List<Task<SQLResult>>();

                                foreach (DataRow dataRow in dt.Rows)
                                {
                                    var sql = dataRow[0]?.ToString() ?? "";
                                    if (viewFilters != null && viewFilters.Count > 0)
                                    {
                                        sql = sql.Replace("--axp_filter", $" {viewFilters.Values.First()} ");
                                    }
                                    else
                                    {
                                        sql = sql.Replace("--axp_filter", "");
                                    }

                                    if (globalParams != null && globalParams.Count > 0)
                                    {
                                        sql = ReplaceSqlParameters(sql, globalParams);
                                    }

                                    if (sqlCommentsReplace != null && sqlCommentsReplace.Count > 0)
                                    {
                                        sql = ReplaceSqlComments(sql, sqlCommentsReplace);
                                    }

                                    if (!string.IsNullOrEmpty(sql))
                                    {
                                        sqlTasks.Add(ExecuteSQLAsync(sql, connectionString));
                                    }
                                }

                                var sqlResults = await Task.WhenAll(sqlTasks);

                                for (int i = 0; i < sqlResults.Length; i++)
                                {
                                    if (string.IsNullOrEmpty(sqlResults[i].error))
                                    {
                                        sqlResults[i].data.TableName = $"Table{i}";
                                        sqlDataSetResult.DataSet.Tables.Add(sqlResults[i].data);
                                    }
                                    else
                                    {
                                        sqlDataSetResult.error += sqlResults[i].error;
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    sqlDataSetResult.error += ex.Message;

                    string error = ex.Message;
                    error += Environment.NewLine + ex.InnerException;
                    error += Environment.NewLine + ex.StackTrace;

                    if (_traceEnabled) _traceService.LogError(error);
                }
                return sqlDataSetResult;
            }

            public async Task<DataTable> ExecuteQueryAsync(string query, string connectionString, DBParamsDetails paramsDetails)
            {
                DataTable dt = new DataTable();
                if (query.Trim().EndsWith(";"))
                    query = query.Trim().Substring(0, query.Trim().Length - 1);

                try
                {
                    if (_traceEnabled) _traceService.LogDbConnection(connectionString);
                    DateTime startTime = DateTime.Now;

                    using (var connection = new SqlConnection(connectionString))
                    {
                        await connection.OpenAsync();

                        if (query.Contains("$SCHEMA$"))
                        {
                            var builder = new SqlConnectionStringBuilder(connectionString);
                            string schemaName = builder.UserID;
                            query = query.Replace("$SCHEMA$", schemaName);
                        }

                        if (_traceEnabled)
                        {
                            _traceService.LogSqlExecution(query);
                            _traceService.LogMessage("Params: " + JsonConvert.SerializeObject(paramsDetails));
                        }

                        using (var command = new SqlCommand(query, connection))
                        {
                            try
                            {
                                if (paramsDetails.ParamsNames.Count > 0)
                                {
                                    foreach (var item in paramsDetails.ParamsNames)
                                    {
                                        command.Parameters.Add(new SqlParameter(item.Name, GetSqlDbType((DbType)item.Type)) { Value = item.Value });
                                    }
                                }
                                var dr = await command.ExecuteReaderAsync();
                                dt.Load(dr);

                                DateTime endTime = DateTime.Now;
                                if (_traceEnabled) _traceService.LogDataTable(dt, startTime, endTime); ;
                            }
                            catch (Exception ex)
                            {
                                dt.Rows.Add(ex.Message);

                                string error = ex.Message;
                                error += Environment.NewLine + ex.InnerException;
                                error += Environment.NewLine + ex.StackTrace;

                                if (_traceEnabled) _traceService.LogError(error);
                            }
                            finally
                            {
                                await connection.CloseAsync();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    dt.Rows.Add(ex.Message);

                    string error = ex.Message;
                    error += Environment.NewLine + ex.InnerException;
                    error += Environment.NewLine + ex.StackTrace;

                    if (_traceEnabled) _traceService.LogError(error);
                }
                return dt;
            }

            public async Task<SQLResult> ExecuteStoredProcedureAsync(string storedProcedure, string connectionString, string[] paramName = null, DbType[] paramType = null, object[] paramValue = null)
            {
                SQLResult sqlResult = new SQLResult();

                try
                {
                    if (_traceEnabled) _traceService.LogDbConnection(connectionString);
                    DateTime startTime = DateTime.Now;

                    using (var connection = new SqlConnection(connectionString))
                    {

                        if (_traceEnabled) _traceService.LogSqlExecution(storedProcedure, paramName, paramType, paramValue);

                        await connection.OpenAsync();
                        if (storedProcedure.Contains("$SCHEMA$"))
                        {
                            var builder = new SqlConnectionStringBuilder(connectionString);
                            string schemaName = builder.UserID;
                            storedProcedure = storedProcedure.Replace("$SCHEMA$", schemaName);

                        }
                        using (var cmd = new SqlCommand(storedProcedure, connection))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            if (paramName != null)
                            {
                                if (paramName.Length != paramValue.Length || paramName.Length != paramType.Length)
                                {
                                    sqlResult.error = "Invalid params in SQL";
                                    if (_traceEnabled) _traceService.LogError(sqlResult.error);
                                }
                                for (int i = 0; i < paramName.Length; i++)
                                {
                                    cmd.Parameters.Add(new SqlParameter(paramName[i], GetSqlDbType(paramType[i])) { Value = paramValue[i] });
                                }
                            }
                            try
                            {
                                using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                                {
                                    adapter.Fill(sqlResult.data);
                                }

                                DateTime endTime = DateTime.Now;
                                if (_traceEnabled) _traceService.LogDataTable(sqlResult.data, startTime, endTime); ;

                            }
                            catch (Exception ex)
                            {
                                sqlResult.error = ex.Message;

                                string error = ex.Message;
                                error += Environment.NewLine + ex.InnerException;
                                error += Environment.NewLine + ex.StackTrace;

                                if (_traceEnabled) _traceService.LogError(error);
                            }
                            finally
                            {
                                await connection.CloseAsync();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    sqlResult.error = ex.Message;

                    string error = ex.Message;
                    error += Environment.NewLine + ex.InnerException;
                    error += Environment.NewLine + ex.StackTrace;

                    if (_traceEnabled) _traceService.LogError(error);
                }

                return sqlResult;
            }

            public async Task<SQLResult> ExecuteStoredProcedureAsync(string storedProcedure, string[] paramName = null, DbType[] paramType = null, object[] paramValue = null)
            {
                SQLResult sqlResult = new SQLResult();

                try
                {
                    DateTime startTime = DateTime.Now;

                    if (_traceEnabled) _traceService.LogSqlExecution(storedProcedure, paramName, paramType, paramValue);

                    if (_connection == null || _connection.State != ConnectionState.Open)
                        await OpenDBConnectionAsync();

                    if (storedProcedure.Contains("$SCHEMA$"))
                    {
                        var builder = new SqlConnectionStringBuilder(_connectionString);
                        string schemaName = builder.UserID;
                        storedProcedure = storedProcedure.Replace("$SCHEMA$", schemaName);

                    }
                    using var cmd = new SqlCommand(storedProcedure, _connection);
                    cmd.CommandType = CommandType.StoredProcedure;
                    if (paramName != null)
                    {
                        if (paramName.Length != paramValue.Length || paramName.Length != paramType.Length)
                        {
                            sqlResult.error = "Invalid params in SQL";
                            if (_traceEnabled) _traceService.LogError(sqlResult.error);
                        }
                        for (int i = 0; i < paramName.Length; i++)
                        {
                            cmd.Parameters.Add(new SqlParameter(paramName[i], GetSqlDbType(paramType[i])) { Value = paramValue[i] });
                        }
                    }
                    try
                    {
                        using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                        {
                            adapter.Fill(sqlResult.data);
                        }

                        DateTime endTime = DateTime.Now;
                        if (_traceEnabled) _traceService.LogDataTable(sqlResult.data, startTime, endTime); ;

                    }
                    catch (Exception ex)
                    {
                        sqlResult.error = ex.Message;

                        string error = ex.Message;
                        error += Environment.NewLine + ex.InnerException;
                        error += Environment.NewLine + ex.StackTrace;

                        if (_traceEnabled) _traceService.LogError(error);
                    }


                }
                catch (Exception ex)
                {
                    sqlResult.error = ex.Message;

                    string error = ex.Message;
                    error += Environment.NewLine + ex.InnerException;
                    error += Environment.NewLine + ex.StackTrace;

                    if (_traceEnabled) _traceService.LogError(error);
                }

                return sqlResult;
            }

            public async Task<NonQueryResult> ExecuteNonQueryAsync(string query, string[] paramName = null, DbType[] paramType = null, object[] paramValue = null)
            {
                var result = new NonQueryResult();
                if (query.Trim().EndsWith(";"))
                    query = query.Trim().Substring(0, query.Trim().Length - 1);
                try
                {
                    DateTime startTime = DateTime.Now;
                    if (_traceEnabled) _traceService.LogSqlExecution(query, paramName, paramType, paramValue);

                    if (_connection == null || _connection.State != ConnectionState.Open)
                        await OpenDBConnectionAsync();

                    using var cmd = new SqlCommand(query, _connection);

                    if (paramName != null)
                    {
                        if (paramName.Length != paramValue.Length || paramName.Length != paramType.Length)
                        {
                            result.error = "Invalid params in SQL";
                            if (_traceEnabled) _traceService.LogError(result.error);
                        }

                        for (int i = 0; i < paramName.Length; i++)
                        {
                            cmd.Parameters.Add(paramName[i], GetSqlDbType(paramType[i])).Value = paramValue[i];
                        }
                    }

                    try
                    {
                        //cmd.Prepare();
                        result.count = await cmd.ExecuteNonQueryAsync();
                        DateTime endTime = DateTime.Now;
                        if (_traceEnabled) _traceService.LogResultInfo(result.count, startTime, endTime);

                    }
                    catch (Exception ex)
                    {
                        result.error = ex.Message;

                        string error = ex.Message;
                        error += Environment.NewLine + ex.InnerException;
                        error += Environment.NewLine + ex.StackTrace;

                        if (_traceEnabled) _traceService.LogError(error);

                    }
                }
                catch (Exception ex)
                {
                    result.error = ex.Message;

                    string error = ex.Message;
                    error += Environment.NewLine + ex.InnerException;
                    error += Environment.NewLine + ex.StackTrace;

                    if (_traceEnabled) _traceService.LogError(error);

                }

                return result;
            }

            public async Task<TransactionResult> ExecuteSQLsInTransactionAsync(List<SQLCommandBuilder> sqlCommands, bool commitTransaction = true)
            {
                var result = new TransactionResult();

                if (sqlCommands == null || sqlCommands.Count == 0)
                {
                    result.success = false;
                    result.error = "No SQL commands to execute.";
                    return result;
                }

                try
                {
                    if (_connection == null || _connection.State != ConnectionState.Open)
                        await OpenDBConnectionAsync();

                    if (_transaction == null)
                    {
                        _transaction = (SqlTransaction)await _connection.BeginTransactionAsync();
                        if (_traceEnabled) _traceService.LogMessage($"Transaction started.", "");
                    }

                        try
                    {
                        int totalRowsAffected = 0;
                        foreach (var sqlCommand in sqlCommands)
                        {
                            using var cmd = _connection.CreateCommand();
                            var query = sqlCommand.Query;

                            if (query.Trim().EndsWith(";"))
                                query = query.Trim().Substring(0, query.Trim().Length - 1);

                            cmd.Transaction = _transaction;
                            cmd.CommandText = query;
                            cmd.CommandType = CommandType.Text;

                            if (sqlCommand.ParameterNames != null)
                            {
                                if (sqlCommand.ParameterNames.Length != sqlCommand.ParameterValues.Length || sqlCommand.ParameterNames.Length != sqlCommand.ParameterTypes.Length)
                                {
                                    result.success = false;
                                    result.error = "Invalid params in SQL";
                                    if (_traceEnabled) _traceService.LogError(result.error);
                                }

                                for (int i = 0; i < sqlCommand.ParameterNames.Length; i++)
                                {
                                    cmd.Parameters.Add(sqlCommand.ParameterNames[i], GetSqlDbType(sqlCommand.ParameterTypes[i])).Value = sqlCommand.ParameterValues[i];
                                }

                                if (_traceEnabled) _traceService.LogSqlExecution(query, sqlCommand.ParameterNames, sqlCommand.ParameterTypes, sqlCommand.ParameterValues);
                            }
                            else
                                if (_traceEnabled) _traceService.LogSqlExecution(query);


                            var rowsAffected = await cmd.ExecuteNonQueryAsync();
                            if (_traceEnabled) _traceService.LogSqlExecution($"Rows Affected : {rowsAffected}");

                            totalRowsAffected += rowsAffected;
                        }

                        if (commitTransaction)
                        {
                            CommitTransaction();
                        }

                        result.success = true;
                        result.TotalStatements = sqlCommands.Count;
                        result.TotalRowsAffected = totalRowsAffected;

                        if (_traceEnabled)
                        {
                            _traceService.LogMessage($"Transaction : Success");
                            _traceService.LogMessage($"Total Statements : {sqlCommands.Count}");
                            _traceService.LogMessage($"Total Rows Affected : {totalRowsAffected}");
                        }
                    }
                    catch (Exception ex)
                    {
                        RollBackTransaction();

                        result.success = false;
                        result.error = ex.Message;
                        result.ExceptionDetail = ex;

                        if (_traceEnabled) _traceService.LogError(JsonConvert.SerializeObject(result));
                    }
                }
                catch (Exception ex)
                {
                    result.success = false;
                    result.error = ex.Message;
                    result.ExceptionDetail = ex;
                    if (_traceEnabled) _traceService.LogError(JsonConvert.SerializeObject(result));
                }

                return result;
            }


            public NonQueryResult CommitTransaction()
            {
                var result = new NonQueryResult();
                try
                {
                    if (_transaction == null)
                    {
                        result.error = "Transaction is null";
                        if (_traceEnabled) _traceService.LogError($"Transaction is null");
                        return result;
                    }

                    try
                    {
                        _transaction.Commit();
                        _transaction.Dispose();
                        if (_traceEnabled)
                        {
                            _traceService.LogMessage($"Transaction Committed.", "");
                            _traceService.LogMessage($"Transaction Disposed.");
                            _traceService.LogMessage($"Transaction : Success", "");
                        }
                    }
                    catch (Exception ex)
                    {
                        _transaction.Rollback();
                        _transaction.Dispose();
                        if (_traceEnabled)
                        {
                            _traceService.LogMessage($"Transaction Rolledback.", "");
                            _traceService.LogMessage($"Transaction Disposed.");
                            _traceService.LogError($"Transaction : failed");
                        }
                        result.error = ex.Message;

                        if (_traceEnabled) _traceService.LogError(JsonConvert.SerializeObject(result));
                    }
                }
                catch (Exception ex)
                {
                    _traceService.LogError($"Transaction : failed");
                    result.error = ex.Message;
                    if (_traceEnabled) _traceService.LogError(JsonConvert.SerializeObject(result));
                }
                return result;
            }

            public NonQueryResult RollBackTransaction()
            {
                var result = new NonQueryResult();
                try
                {
                    if (_transaction == null)
                    {
                        result.error = "Transaction is null";
                        if (_traceEnabled) _traceService.LogError($"Transaction is null");
                        return result;
                    }

                    try
                    {
                        _transaction.Rollback();
                        _transaction.Dispose();
                        if (_traceEnabled)
                        {
                            _traceService.LogMessage($"Transaction Rolledback.", "");
                            _traceService.LogMessage($"Transaction Disposed.");
                            _traceService.LogMessage($"Transaction : failed", "");
                        }
                    }
                    catch (Exception ex)
                    {
                        _transaction.Rollback();
                        _transaction.Dispose();
                        if (_traceEnabled)
                        {
                            _traceService.LogMessage($"Transaction Rolledback.", "");
                            _traceService.LogMessage($"Transaction Disposed.");
                            _traceService.LogError($"Transaction : failed");
                        }
                        result.error = ex.Message;

                        if (_traceEnabled) _traceService.LogError(JsonConvert.SerializeObject(result));
                    }
                }
                catch (Exception ex)
                {
                    _traceService.LogError($"Transaction : failed");
                    result.error = ex.Message;
                    if (_traceEnabled) _traceService.LogError(JsonConvert.SerializeObject(result));
                }
                return result;
            }

            public async Task<DataTable> ExecuteSqlSchemaAsync(string sqlQuery)
            {
                DataTable schemaTable = new DataTable();
                try
                {
                    if (_connection == null || _connection.State != ConnectionState.Open)
                    {
                        await OpenDBConnectionAsync();
                    }
                    using var cmd = new SqlCommand(sqlQuery, _connection);
                    using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SchemaOnly);
                    schemaTable = reader.GetSchemaTable();
                    schemaTable = ChangeColumnNamesToLowerCase(schemaTable);
                }
                catch (Exception ex)
                {
                    string error = $"Error executing MSSQL schema query: {ex.Message}";
                    if (_traceEnabled)
                        _traceService.LogError(error);

                    throw new Exception(ex.Message);
                }
                return schemaTable;
            }

            public DataTable ChangeColumnNamesToLowerCase(DataTable dt)
            {
                if (dt == null)
                {
                    throw new ArgumentNullException(nameof(dt), "DataTable cannot be null.");
                }

                foreach (DataColumn column in dt.Columns)
                {
                    column.ColumnName = column.ColumnName.ToLower();
                }

                return dt;
            }
            private static SqlDbType GetSqlDbType(DbType type)
            {
                switch (type)
                {
                    case DbType.Int32:
                        return SqlDbType.Int;
                    case DbType.Int64:
                        return SqlDbType.BigInt;
                    case DbType.Decimal:
                        return SqlDbType.Decimal;
                    case DbType.DateTime:
                        return SqlDbType.DateTime;
                    case DbType.Date:
                        return SqlDbType.Date;
                    case DbType.String:
                        return SqlDbType.NVarChar;
                    case DbType.Binary:
                        return SqlDbType.VarBinary;
                    case DbType.Guid:
                        return SqlDbType.UniqueIdentifier;  //GUID                  
                    case DbType.Object:// JSON support → NVARCHAR(MAX)
                        return SqlDbType.NVarChar;
                    default:
                        return SqlDbType.NVarChar;
                }
            }            
        }

        public static IDbHelper CreateDbHelper(string query, string dbType, string connectionString, string[] paramName, DbType[] paramType, object[] paramValue)
        {
            switch (dbType.ToLower())
            {
                case "oracle":
                    return new OracleHelper(query, connectionString, paramName, paramType, paramValue);
                case "postgre":
                    return new PostgresHelper(query, connectionString, paramName, paramType, paramValue);
                case "ms sql":
                case "mssql":
                    return new MSSQLHelper(query, connectionString, paramName, paramType, paramValue);
                default:
                    throw new ArgumentException("Invalid database type");
            }
        }

        public static IDbHelper CreateDbHelper(string dbType, IARMLogTraceService traceService = null)
        {
            switch (dbType.ToLower())
            {
                case "oracle":
                    return new OracleHelper(traceService);
                case "postgre":
                case "postgres":
                    return new PostgresHelper(traceService);
                case "ms sql":
                case "mssql":
                    return new MSSQLHelper(traceService);
                default:
                    throw new ArgumentException("Invalid database type");
            }
        }

        //public static DbConnection OpenDBConnection(string connString, string dbType)
        //{
        //    switch (dbType.ToLower())
        //    {
        //        case "oracle":
        //            return new OracleConnection(connString);
        //        case "postgre":
        //        case "postgres":
        //            return new NpgsqlConnection(connString);
        //        case "ms sql":
        //            return new SqlConnection(connString);
        //        default:
        //            throw new NotSupportedException("Invalid database type");
        //    }

        //}

        public static DbConnection OpenDBConnection(string connectionString, string dbType)
        {
            switch (dbType.ToLower())
            {
                case "oracle":
                    {
                        var connection = new OracleConnection(connectionString);
                        connection.Open();
                        return connection;
                    }
                case "postgre":
                case "postgres":
                    {
                        var connection = new NpgsqlConnection(connectionString);
                        connection.Open();
                        return connection;
                    }
                case "ms sql":
                case "mssql":
                    {
                        var connection = new SqlConnection(connectionString);
                        connection.Open();
                        return connection;
                    }
                default:
                    throw new NotSupportedException("Invalid database type");
            }

        }

        public static Task CloseDBConnection(DbConnection connection)
        {
            if (connection?.State == ConnectionState.Open)
                return connection.CloseAsync();
            return Task.CompletedTask;

        }

        public static Dictionary<string, string> GetDBVars(string dbType)
        {
            Dictionary<string, string> dbVars = new Dictionary<string, string>();
            switch (dbType.ToLower())
            {
                case "oracle":
                    dbVars.Add("currencytable", "currency");
                    dbVars.Add("sqllower", "lower");
                    dbVars.Add("forupdate", " for update ");
                    dbVars.Add("sqlnull", "nvl");
                    dbVars.Add("nullcds", "select lpad(' ', 250, ' ') LVAL from dual");
                    dbVars.Add("dbdateformat", "dd-mmm-yyyy");
                    dbVars.Add("dbdatestring", "to_date( $DATE$,'dd/mm/yyyy hh24:mi:ss')"); //TODO
                    dbVars.Add("datesql", "Select sysdate as sdt from dual");
                    break;
                case "postgre":
                case "postgres":
                    dbVars.Add("currencytable", "currency");
                    dbVars.Add("sqllower", "lower");
                    dbVars.Add("forupdate", " for update ");
                    dbVars.Add("sqlnull", "coalesce");
                    dbVars.Add("nullcds", "SELECT LPAD('', 250, ' ') AS LVAL");
                    dbVars.Add("dbdateformat", "dd/mm/yyyy");
                    dbVars.Add("dbdatestring", "to_timestamp( $DATE$ ,'dd/mm/yyyy hh24:mi:ss')");
                    dbVars.Add("datesql", "Select now() as sdt");
                    break;
                case "ms sql":
                case "mssql":
                    dbVars.Add("currencytable", "currency");
                    dbVars.Add("sqllower", "lower");
                    dbVars.Add("forupdate", " with(holdlock,rowlock) ");
                    dbVars.Add("sqlnull", "isnull");
                    dbVars.Add("nullcds", "select lval from dual with(nolock)");
                    dbVars.Add("dbdateformat", "mm/dd/yyyy");
                    dbVars.Add("dbdatestring", "CONVERT(VARCHAR(23), $DATE$, 103)");
                    dbVars.Add("datesql", "SELECT GETDATE() as sdt");
                    break;
                default:
                    throw new NotSupportedException("Invalid database type");
            }

            return dbVars;
        }

        public IDbHelper GetDbHelper(string dbType, IARMLogTraceService traceService = null)
        {
            switch (dbType.ToLower())
            {
                case "oracle":
                    return new OracleHelper(traceService);
                case "postgre":
                case "postgres":
                    return new PostgresHelper(traceService);
                case "ms sql":
                case "mssql":
                    return new MSSQLHelper(traceService);
                default:
                    throw new ArgumentException("Invalid database type");
            }
        }

    }
}
