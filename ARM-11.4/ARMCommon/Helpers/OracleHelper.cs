using ARMCommon.Interface;
using Oracle.ManagedDataAccess.Client;
using System.Data;

public class OracleHelper : IOracleHelper
{
    public async Task<DataTable> ExecuteSql(string query, string connectionString, string[] paramName, OracleDbType[] paramType, object[] paramValue)
    {
        DataTable dataTable = new DataTable();
        using (var connection = new OracleConnection(connectionString))
        {
            await connection.OpenAsync();
            using (var cmd = new OracleCommand(query, connection))
            {
                if (paramName.Length != paramValue.Length || paramName.Length != paramType.Length)
                {
                    return null;
                }

                for (int i = 0; i < paramName.Length; i++)
                {
                    cmd.Parameters.Add(paramName[i], paramType[i], paramValue[i], ParameterDirection.Input);
                }

                try
                {
                    cmd.Prepare();
                    var dr = await cmd.ExecuteReaderAsync();
                    dataTable.Load(dr);
                }
                catch (Exception ex)
                {
                    dataTable.Rows.Add(ex.Message);
                }
                finally
                {
                    await connection.CloseAsync();
                }
            }
        }
        return dataTable;
    }
}
