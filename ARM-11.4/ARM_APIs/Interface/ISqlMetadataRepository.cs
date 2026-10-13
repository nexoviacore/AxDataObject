using System.Data;

namespace ARM_APIs.Interface
{
    public interface ISqlMetadataRepository
    {
        Task<DataTable> GetSchemaAsync(string sqlQuery);
    }
}
