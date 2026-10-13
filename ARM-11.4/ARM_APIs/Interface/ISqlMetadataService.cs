using ARM_APIs.Model;

namespace ARM_APIs.Interface
{
    public interface ISqlMetadataService
    {
        Task<List<MetadataColumn>> GetMetadataAsync(DbRequest request);
    }
}
