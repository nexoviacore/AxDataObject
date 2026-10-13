using ARM_APIs.Interface;
using ARMCommon.Helpers;
using System.Data;

namespace ARM_APIs.Repositories
{
    public class SqlMetadataRepository : ISqlMetadataRepository
    {
        private readonly Utils _common;
        public SqlMetadataRepository(Utils common)
        {
            _common = common;
        }
        public async Task<DataTable> GetSchemaAsync(string sqlQuery)
        {
            DataTable schemaTable = new DataTable();
            string result = string.Empty;
            try
            {
                if (_common._dbHelper == null)
                    result = await _common.OpenDBConnectionAsync();

                if (_common._dbHelper != null)
                {
                    schemaTable = await _common._dbHelper.ExecuteSqlSchemaAsync(sqlQuery);
                }
                else if (!string.IsNullOrEmpty(result) && result.StartsWith("Error:"))
                {
                    if (result.Contains("Redis settings for app",StringComparison.OrdinalIgnoreCase))
                    {
                        throw new Exception($"Application '{_common._appName}' does not exist");
                    }
                }

                return schemaTable;
            }
            finally
            {
                if (_common._dbHelper != null)
                    await _common.CloseDBConnectionAsync();
            }
        }

    }
}
