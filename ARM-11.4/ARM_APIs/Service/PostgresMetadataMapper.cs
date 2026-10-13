using ARM_APIs.Interfaces;
using ARM_APIs.Model;
using System.Data;

namespace ARM_APIs.Service
{
    public class PostgresMetadataMapper : IPostgresMetadataMapper
    {
        public List<MetadataColumn> Map(DataTable schemaTable)
        {
            var metadataList = new List<MetadataColumn>();

            foreach (DataRow row in schemaTable.Rows)
            {
                object? length = null;
                string columnname = row["ColumnName"]?.ToString() ?? string.Empty;

                string pgTypeName = row.Table.Columns.Contains("DataTypeName") && row["DataTypeName"] != DBNull.Value
                    ? row["DataTypeName"].ToString().ToLower()
                    : ((Type)row["DataType"]).Name.ToLower();

                string datatype = pgTypeName switch
                {
                    "numeric" => "numeric",
                    "varchar" or "character varying" => "varchar",
                    "text" => "text",
                    "date" => "date",
                    "timestamp" or "timestamp without time zone" => "timestamp",
                    "timestamptz" or "timestamp with time zone" => "timestamptz",
                    "int4" or "integer" => "integer",
                    "int8" or "bigint" => "bigint",
                    "int2" or "smallint" => "smallint",
                    "float8" or "double precision" => "double precision",
                    "bool" or "boolean" => "boolean",
                    "real" => "real",
                    "bytea" => "bytea",
                    "bpchar" or "character" => "char",
                    _ => pgTypeName
                };

                if (datatype == "numeric" || datatype == "integer" || datatype == "bigint" || datatype == "smallint")
                {
                    int precision = row.Table.Columns.Contains("NumericPrecision") && row["NumericPrecision"] != DBNull.Value
                        ? Convert.ToInt32(row["NumericPrecision"]) : 0;

                    int scale = row.Table.Columns.Contains("NumericScale") && row["NumericScale"] != DBNull.Value
                        ? Convert.ToInt32(row["NumericScale"]) : 0;

                    if (precision > 0)
                        length = scale > 0 ? $"{precision},{scale}" : $"{precision}";
                }
                else if (datatype == "varchar")
                {
                    int colSize = row["ColumnSize"] != DBNull.Value ? Convert.ToInt32(row["ColumnSize"]) : 0;
                    length = colSize > 0 ? $"{colSize}" : null;
                }
                else if (datatype == "char")
                {
                    int colSize = row["ColumnSize"] != DBNull.Value ? Convert.ToInt32(row["ColumnSize"]) : 1;
                    length = $"{colSize}";
                }
                else if (datatype == "timestamp" || datatype == "timestamptz")
                {
                    int precision = row.Table.Columns.Contains("DateTimePrecision") && row["DateTimePrecision"] != DBNull.Value
                        ? Convert.ToInt32(row["DateTimePrecision"]) : 6;
                    length = $"{precision}";
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

