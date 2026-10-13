namespace AxStructUtils
{
    public class CustomSQL_AddField
    {
        public static string GetSqlQuery(string dbType, string sqlType)
        {
            dbType = dbType.ToLower();
            sqlType = sqlType.ToLower();
            switch (sqlType)
            {
                case "checkduptstruct":
                    switch (dbType)
                    {
                        case "oracle":
                            return @"select case when sum(cnt) > 0 then 1 else 0 end from (select 1 cnt from AxmMetaDataMaster where lower (structName) = lower(:structname) and structType = :structtype union all select 1 from tstructs where lower(name) = lower(:structname))a";

                        case "postgre":
                        case "postgresql":
                            return @"select case when sum(cnt) > 0 then 1 else 0 end from (select 1 cnt from AxmMetaDataMaster where lower (structName) = lower(@structname) and structType = @structtype union all select 1 from tstructs where lower(name) = lower(@structname))a";

                        case "mssql":
                            return @"select case when sum(cnt) > 0 then 1 else 0 end from (select 1 cnt from AxmMetaDataMaster where lower (structName) = lower(@structname) and structType = @structtype union all select 1 from tstructs where lower(name) = lower(@structname))a";

                        default:
                            throw new NotSupportedException($"Unsupported DB Type: {dbType}");
                    };
                case "insert_metadatamaster":
                    switch (dbType)
                    {
                        case "oracle":
                            return @"insert into AxmMetaDataMaster (structType,structName,structCaption,structstatus,createdby,updatedby) values( :structType, :structName, :structCaption, :structstatus, :createdby, :updatedby)";
                        case "postgre":
                        case "postgresql":
                            return @"insert into AxmMetaDataMaster (structType,structName,structCaption,structstatus,createdby,updatedby) values(@structType,@structName,@structCaption,@structstatus,@createdby,@updatedby)";
                        case "mssql":
                            return @"insert into AxmMetaDataMaster (structType,structName,structCaption,structstatus,createdby,updatedby) values( @structType, @structName, @structCaption, @structstatus, @createdby, @updatedby)";
                        default:
                            throw new NotSupportedException($"Unsupported DB Type: {dbType}");
                    };
                case "update_metadatamaster":
                    switch (dbType)
                    {
                        case "oracle":
                            return @"update AxmMetaDataMaster set structName = :structName,structCaption =  :structCaption,updatedon =sysdate,updatedby = :updatedby where structType =  :structType and structName =  :structName";
                        case "postgre":
                        case "postgresql":
                            return @"update AxmMetaDataMaster set structName = @structName,structCaption = @structCaption,updatedon = CURRENT_DATE,updatedby = @updatedby where structType = @structType and structName = @structName";
                        case "mssql":
                            return @"update AxmMetaDataMaster set structName = @structName structCaption = @structCaption,updatedon = getdate(),updatedby = @updatedby where structType = @structType and structName = @structName";
                        default:
                            throw new NotSupportedException($"Unsupported DB Type: {dbType}");
                    }
                default:
                    throw new NotSupportedException($"Unsupported SQL Type: {sqlType}");
            }
        }
    }
}
