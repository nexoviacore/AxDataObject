using ARMCommon.Interface;
using ARMCommon.Model;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using static ARMCommon.Helpers.DBHelper;

namespace ARMCommon.Helpers
{
    public class MSSqlHelper : ImssqlHelper
    {
        private readonly IConfiguration _config;
        public MSSqlHelper(IConfiguration config)
        {
            _config = config;
        }

        public async Task<DataTable> ExecuteSelectSql(string sql, string connectionString, ParamsDetails paramsDetails)
        {
            DataTable dt = new DataTable();
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand(sql, connection))
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

        public async Task<DataTable> ExecuteSql(string query, string connectionString, string[] paramName, SqlDbType[] paramType, object[] paramValue)
        {
            DataTable dt = new DataTable();
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var cmd = new SqlCommand(query, connection))
                {
                    if (paramName.Length != paramValue.Length || paramValue.Length != paramType.Length)
                    {
                        return null;
                    }

                    for (int i = 0; i < paramName.Length; i++)
                    {
                        cmd.Parameters.Add(new SqlParameter(paramName[i], paramType[i]) { Value = paramValue[i] });
                    }

                    try
                    {
                        var dr = await cmd.ExecuteReaderAsync();
                        dt.Load(dr);
                    }
                    catch (Exception ex)
                    {
                        dt.Rows.Add(ex.Message);
                    }
                    finally
                    {
                        await connection.CloseAsync();
                    }
                }
            }
            return dt;
        }
    }
}
