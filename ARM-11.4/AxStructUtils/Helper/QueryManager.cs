
using System.Text.Json;

namespace AxStructUtils
{
    public interface IQueryManager
    {
        string GetQuery(DatabaseType dbType, QueryCategory category, QueryName queryName);
    }
    public enum DatabaseType
    {
        mssql,
        mysql,
        postgre,
        oracle
    }
    public enum QueryCategory
    {
        general,
        dropdown,
    }

    //Any new key added in dbquery.json file, that needs to be added here also
    public enum QueryName
    {
        METADATAMASTER,
        METADATAMASTER_SINGLE,
        INSERT_METADATAMASTER,
        UPDATE_METADATAMASTER,
        DELETE_METADATAMASTER,
        UPDATE_STATUS_METADATAMASTER,
        CHECKDUPTSTRUCT,
        CHECKDUPIVIEW,
        GET_TBL_ClMN_HINT,
        GET_TST_CAPTION,
        GET_TST_Fld_CAPTION,
        GET_DC_CAPTION,
        GET_DCISGRID,
        GET_IVIEW_CAPTION,
        GET_PAGE_CAPTION,
        USERLIST,
        ROLESLIST,
        TSTLIST,
        TSTLIST_W_1STDC_TBLNAME,
        TSTLIST_BUT_CURRENT,
        FLDLIST_BY_FORM,
        FLDLIST_BY_FORM_W_ASGRID,
        FLDLIST_BY_FORM_WITHOUT_CAP,
        FLDLIST_BY_FORM_N_DC,
        FLDLIST_W_DC_N_TBLNAME,
        FILLGRID_SRCFLD,
        APILISTSQL,
        APILISTMO,
        APILIST_BTN,
        FILLFROMAPISRC,
        CUSTOMDATATYPES,
        HLINK_STRUCTLIST_BY_TYPE,
        HLINK_PARAMLIST_BY_STRUCTTYPE,
        TSTLIST_MASK,
        FLDLIST_MASKED,
        CUSTOMDATATYPE_LIST,
        API_MO_INFO_BY_NAME,
        ENCRIPTED_FORM_LIST,
        ENCRIPTED_FIELD_LIST_BY_FORM,
        MASKED_FORM_LIST,
        MASKED_FIELD_LIST_BY_FORM,
        GET_DCCAPTION_BY_TABLENAME,
        GET_TST_FLD_CAPTION_N_SQLTEXT,
        FIELDLIST_BY_FORM_UNION_TABLEIDS,
        GET_TST_FLD_CAPTION_N_TBLNAME,
        FLDLIST_BY_FORM_W_TBLNAME,
        GET_TST_FLD_DEPENDENTS,
        GET_TST_FLD_CAPTION_N_ASGRID
    }
    public class QueryManager : IQueryManager
    {
        private readonly Dictionary<string, Dictionary<string, Dictionary<string, string>>> _queries;

        public QueryManager(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Query file not found.", filePath);

            var jsonContent = File.ReadAllText(filePath);

            // Deserialize to a nested dictionary
            _queries = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, Dictionary<string, string>>>>(jsonContent)
                       ?? throw new InvalidOperationException("Failed to deserialize queries.");
        }

        public string GetQuery(DatabaseType databaseType, QueryCategory category, QueryName queryName)
        {
            // Convert the database type to its string representation (e.g., "Postgre", "Oracle")
            var databaseKey = databaseType.ToString().ToLower();
            var categoryKey = category.ToString().ToLower();
            var _queryname = queryName.ToString();

            if (!_queries.TryGetValue(databaseKey, out var categories))
                throw new KeyNotFoundException($"Database type '{databaseType}' not found.");

            if (!categories.TryGetValue(categoryKey, out var queries))
                throw new KeyNotFoundException($"Category '{category}' not found for database type '{databaseType}'.");

            if (!queries.TryGetValue(_queryname, out var query))
                throw new KeyNotFoundException($"Query '{queryName}' not found in category '{category}' for database type '{databaseType}'.");

            return query;
        }
    }
}
