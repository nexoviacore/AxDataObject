using ARMCommon.Interface;
using ARMCommon.Model;
using Npgsql;
using NpgsqlTypes;
using System.Data;

namespace ARMCommon.Helpers
{
    public class PostgresHelper : IPostgresHelper
    {

        private readonly IConfiguration _config;
        public PostgresHelper(IConfiguration config)
        {
            _config = config;
        }

        public async Task<DataTable> ExecuteSelectSql(string sql, string connectionString, ParamsDetails paramsDetails)
        {

            DataTable dt = new DataTable();
            using (var connection = new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var command = new NpgsqlCommand(sql, connection))
                {
                    try
                    {
                        if (paramsDetails.ParamsNames.Count > 0)
                        {
                            foreach (var item in paramsDetails.ParamsNames)
                            {
                                command.Parameters.AddWithValue(item.Name, item.Value);
                            }
                        }
                        command.Prepare();
                        var dr = await command.ExecuteReaderAsync();
                        dt.Load(dr);
                    }
                    catch (Exception e)
                    {
                        dt.Rows.Add(e.Message);
                    }
                    finally
                    {
                        await connection.CloseAsync();
                    }
                }

            }
            return dt;
        }


        public async Task<DataTable> ExecuteSql(string query, string connectionString, string[] paramName, NpgsqlDbType[] paramType, object[] paramValue)
        {
            DataTable _dt = new DataTable();
            using (var connection = new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var cmd = new NpgsqlCommand(query, connection))
                {
                    if (paramName.Count() != paramValue.Count() || paramValue.Count() != paramType.Count())
                    {
                        return null;
                    }

                    for (int i = 0; i < paramName.Count(); i++)
                    {
                        cmd.Parameters.AddWithValue(paramName[i], paramType[i], paramValue[i]);
                    }
                      try
                    { 
                        cmd.Prepare();
                        var dr = await cmd.ExecuteReaderAsync();
                        _dt.Load(dr);
                    }
                    catch (Exception ex)
                    {
                        _dt.Rows.Add(ex.Message);
                    }

                    finally
                    {
                        await connection.CloseAsync();
                    }
                }

            }
            return _dt;
        }


    }
}
