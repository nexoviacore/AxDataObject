using ARM_APIs.Interfaces;
using ARM_APIs.Model;
using System.Data;

namespace ARM_APIs.Service
{
    public class OracleMetadataMapper : IOracleMetadataMapper
    {
        public List<MetadataColumn> Map(DataTable schemaTable)
        {
            var metadataList = new List<MetadataColumn>();

            foreach (DataRow row in schemaTable.Rows)
            {
                object? length = null;
                string columnname = row["ColumnName"]?.ToString().ToLower() ?? string.Empty;

                string OrclTypeName = ((Type)row["DataType"]).Name;
                string datatype = OrclTypeName switch
                {
                    "Decimal" => "number",
                    "String" => (row["ColumnSize"] != DBNull.Value && Convert.ToInt32(row["ColumnSize"]) > 4000) ? "clob" : "varchar2",
                    "DateTime" => "date",
                    "Int32" or "Int64" or "Int16" or "Double" or "Single" => "number",
                    "Boolean" => "char",
                    "Byte[]" => "blob",
                    _ => OrclTypeName
                };

                if (datatype == "number")
                {
                    int precision = row.Table.Columns.Contains("NumericPrecision") && row["NumericPrecision"] != DBNull.Value
                        ? Convert.ToInt32(row["NumericPrecision"]) : 0;
                    int scale = row.Table.Columns.Contains("NumericScale") && row["NumericScale"] != DBNull.Value
                        ? Convert.ToInt32(row["NumericScale"]) : 0;

                    if(precision>0)
                        length = scale > 0 ? $"{precision},{scale}" : $"{precision}";   
                }
                else if (datatype == "clob" || datatype == "blob")
                {
                    length = "max";
                }
                else if (datatype == "varchar2" || datatype == "char")
                {
                    length = row["ColumnSize"] != DBNull.Value && Convert.ToInt32(row["ColumnSize"]) > 0
                        ? $"{Convert.ToInt32(row["ColumnSize"])}" : null;
                }

                bool isnullable = row.Table.Columns.Contains("AllowDBNull") && row["AllowDBNull"] != DBNull.Value
                    ? (bool)row["AllowDBNull"] : true;

                metadataList.Add(new MetadataColumn
                {
                    columnname = columnname,
                    datatype = datatype,
                    length = length?.ToString(),
                    nullable = isnullable,
                    axdatatype = GetAxpertDataType(datatype)
                });
            }

            return metadataList;
        }

        private string GetAxpertDataType(string dbDataType)
        {
            if (string.IsNullOrWhiteSpace(dbDataType))
                return "unknown";

            dbDataType = dbDataType.ToLower();

            if (dbDataType is "char" or "nchar" or "varchar" or "nvarchar" or "varchar2" or "nvarchar2" or "bpchar")
                return "character";

            if (dbDataType is "int" or "integer" or "smallint" or "bigint" or "decimal" or "number" or "numeric" or "float" or "double" or "real" or "money")
                return "numeric";

            if (dbDataType is "date" or "datetime" or "smalldatetime" or "timestamp" or "timestamptz" or "time")
                return "date";

            if (dbDataType is "text" or "clob" or "nclob" or "memo")
                return "text";

            if (dbDataType is "bit" or "boolean" or "bool")
                return "boolean";

            if (dbDataType is "blob" or "bytea" or "image" or "raw" or "long raw" or "bfile" or "graphic" or "varbinary" or "binary")
                return "image";

            return "unknown";
        }
    }
}
