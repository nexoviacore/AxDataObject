using ARM_APIs.Model;
using System.Collections.Generic;
using System.Data;

namespace ARM_APIs.Interfaces
{
    public interface IMetadataMapper
    {
        List<MetadataColumn> Map(DataTable schemaTable);
    }

    public interface IOracleMetadataMapper : IMetadataMapper { }
    public interface IPostgresMetadataMapper : IMetadataMapper { }
}
