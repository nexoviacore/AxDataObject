using ARM_APIs.Interface;
using ARM_APIs.Interfaces;
using ARM_APIs.Model;
using ARMCommon.Helpers;
using System.Data;

namespace ARM_APIs.Service
{
    public class SqlMetadataService : ISqlMetadataService
    {
        private readonly ISqlMetadataRepository _sqlmetarepository;
        private readonly IOracleMetadataMapper _oracleMapper;
        private readonly IPostgresMetadataMapper _postgresMapper;
        private readonly Utils _common;
        public SqlMetadataService(
            ISqlMetadataRepository sqlmetarepository, 
            IOracleMetadataMapper oracleMapper,
            IPostgresMetadataMapper postgresMapper,
            Utils common)
        {
            _sqlmetarepository = sqlmetarepository;
            _oracleMapper = oracleMapper;
            _postgresMapper = postgresMapper;
            _common = common;
        }
        public async Task<List<MetadataColumn>> GetMetadataAsync(DbRequest request)
        {
            string appName = request.AppName;
            string sqlQuery = request.SqlQuery;

            _common._appName = appName;

            DataTable schemaTable;
            try
            {
                schemaTable = await _sqlmetarepository.GetSchemaAsync(sqlQuery);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching schema from DB: {ex.Message}", ex);
            }

            if (schemaTable == null || schemaTable.Rows.Count == 0)
                throw new InvalidOperationException("Schema information could not be retrieved. Please verify your SQL query");

            var dbType = _common._dbHelper.GetDBType().ToLower();

            IMetadataMapper mapper = dbType switch
            {
                "oracle" => _oracleMapper,
                "postgre" => _postgresMapper,
                _ => throw new NotSupportedException($"Database '{dbType}' not supported")
            };

            return mapper.Map(schemaTable);
        }
    }
}
