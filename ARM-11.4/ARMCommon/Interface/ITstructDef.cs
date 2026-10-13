namespace ARMCommon.Interface
{
    public interface ITstructDef
    {
        string LoadStructure(string xml);
        Dictionary<string, string> LoadFieldSql(string xml);

        string LoadMetaDataFromJson(string transId, string json);
        string LoadFieldSqlFromJson(string json, string fieldName);
    }
}
