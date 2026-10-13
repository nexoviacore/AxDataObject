using ARM_APIs.Interface;
using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMCommon.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using ARM_APIs.Model;

namespace ARM_APIs.Services
{
    public class CardsService : ICardsService
    {
        private readonly IRedisHelper _redis;
        private readonly Utils _common;

        public CardsService(IRedisHelper redis, Utils common)
        {
            _redis = redis;
            _common = common;
        }

        private async Task<string> GetDBConnString(string sessionId)
        {
            string connectionString = await _common.GetDBConfigurationBySessionId(sessionId);
            return connectionString;

        }

        private async Task<Dictionary<string, string>> GetLoginUser(string ARMSessionId)
        {
            var dictSession = await _redis.HashGetAllDictAsync(ARMSessionId);
            return dictSession;
        }

        public async Task<DataTable> GetCardsList(Cards cards)
        {
            ParamsDetails parameters = new ParamsDetails();
            parameters.ParamsNames = new List<ARMCommon.Model.ConnectionParamsList>();
            var loginuser = await GetLoginUser(cards.ARMSessionId);
            Dictionary<string, string> config = await _common.GetDBConfigurations(loginuser["APPNAME"]);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];
            var dictSession = await _redis.HashGetAllDictAsync(cards.ARMSessionId);
            string roles = dictSession["USER_ROLES"];
            string[] arrRoles = roles.Split(','); //Split roles into an array

            string cardsFilter = "(";
            foreach (string role in arrRoles)
            {
                cardsFilter += "lower(accessstring) LIKE '%" + role + "%' OR ";
            }
            cardsFilter = cardsFilter.TrimEnd(" OR ".ToCharArray()) + ")";
            string sql = Constants_SQL.GET_DASHBOARDCARDSLIST.ToString();
            if (cards.IsHomePage != null && cards.IsHomePage == true)
            {
                cardsFilter += "and inhomepage = 'T' and cardtype = 'kpi' ";
            }

            sql = sql.ToString().Replace("$CARDSFILTER$", cardsFilter);
            IDbHelper dbHelper = DBHelper.CreateDbHelper(dbType);
            var table = await dbHelper.ExecuteQueryAsync(sql, connectionString);
            return table;
        }

        public async Task<DataTable> GetCardData(string sessionId, string cardsql)
        {
            var loginuser = await GetLoginUser(sessionId);
            Dictionary<string, string> config = await _common.GetDBConfigurations(loginuser["APPNAME"]);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];
            IDbHelper dbHelper = DBHelper.CreateDbHelper(dbType);
            return await dbHelper.ExecuteQueryAsync(cardsql, connectionString);
        }

        public async Task<DataTable> GetCardData(string sessionId, string cardsql, Dictionary<string, string> sqlParams)
        {
            var loginuser = await GetLoginUser(sessionId);
            Dictionary<string, string> config = await _common.GetDBConfigurations(loginuser["APPNAME"]);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];
            string sql = cardsql;
            sql = sql.Replace(":", "@");
            DBParamsDetails parameters = GetSQLParams(sqlParams);
            IDbHelper dbHelper = DBHelper.CreateDbHelper(dbType);
            var table = await dbHelper.ExecuteQueryAsync(sql, connectionString);
            return table;
        }

        public async Task<DataTable> GetCardsListByIds(Cards cards)
        {
            var loginuser = await GetLoginUser(cards.ARMSessionId);
            Dictionary<string, string> config = await _common.GetDBConfigurations(loginuser["APPNAME"]);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];

            string cardsFilter = "";
            foreach (string cardId in cards.CardId.Split(','))
            {
                cardsFilter += "axp_cardsid = " + cardId + " OR ";
            }
            cardsFilter = cardsFilter.TrimEnd(" OR ".ToCharArray());
            string sql = Constants_SQL.GETCARDLISTS.ToString().Replace("$CARDSFILTER$", cardsFilter);
            IDbHelper dbHelper = DBHelper.CreateDbHelper(dbType);
            var table = await dbHelper.ExecuteQueryAsync(sql, connectionString);
            return table;

        }

        public async Task<DataTable> GetProcessCards(ARMProcessFlowTask processFlow)
        {
            var loginuser = await GetLoginUser(processFlow.ARMSessionId);
            Dictionary<string, string> config = await _common.GetDBConfigurations(loginuser["APPNAME"]);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];

            string sql = Constants_SQL.GET_PROCESSCARDS.ToString();
            string[] paramName = { "@processname", "@taskname" };
            DbType[] paramType = { DbType.String, DbType.String };
            object[] paramValue = { processFlow.ProcessName.ToLower(), processFlow.TaskName.ToLower() };
            IDbHelper dbHelper = DBHelper.CreateDbHelper(dbType);
            return await dbHelper.ExecuteQueryAsync(sql, connectionString, paramName, paramType, paramValue);

        }

        public async Task<string> GetUserName(string sessionId)
        {
            var dictSession = await _redis.HashGetAllDictAsync(sessionId);
            return dictSession["USERNAME"];
        }

        private DBParamsDetails GetSQLParams(Dictionary<string, string> sqlParams)
        {
            DBParamsDetails parameters = new DBParamsDetails();
            parameters.ParamsNames = new List<DBParamsList>();

            foreach (var sqlParam in sqlParams)
            {
                parameters.ParamsNames.Add(new DBParamsList
                {
                    Name = "@" + sqlParam.Key.Split("~")[0],
                    Type = DbType.String,
                    Value = sqlParam.Value
                });
            }
            return parameters;
        }

        public async Task<DataTable> GetProcessCardsData(ARMProcessFlowTask processFlow, DataTable dtCards)
        {
            var numericTypes = new List<Type> { typeof(int), typeof(Int16), typeof(Int32), typeof(Int64), typeof(decimal), typeof(long), typeof(float), typeof(double) };
            foreach (DataRow row in dtCards.Rows)
            {
                string cardsql = row["cardsql"].ToString();
                if (!string.IsNullOrEmpty(cardsql))
                {
                    int colonIndex = cardsql.IndexOf(':');
                    if (colonIndex != -1)
                    {
                        string variableName = cardsql.Substring(colonIndex);
                        string username = await GetUserName(processFlow.ARMSessionId);
                        //cardsql = cardsql.Replace(":username", $"'{username}'");
                    }
                    try
                    {
                        DataTable dtCardResult = await GetCardData(processFlow.ARMSessionId, cardsql, processFlow.SqlParams);
                        StringBuilder sb = new StringBuilder();
                        sb.Append("{");
                        sb.Append("\"fields\": [");
                        foreach (DataColumn column in dtCardResult.Columns)
                        {
                            sb.Append("{");
                            sb.Append("\"name\": \"" + column.ColumnName + "\",");
                            sb.Append("\"datatype\": \"" + column.DataType.Name + "\"");
                            sb.Append("},");
                        }
                        sb.Remove(sb.Length - 1, 1); // remove the last comma
                        sb.Append("]");
                        sb.Append(",");
                        sb.Append("\"row\": ");

                        if (dtCardResult.Rows.Count == 0)
                        {
                            sb.Append("[]");
                        }
                        else
                        {
                            sb.Append("[");
                            foreach (DataRow dataRow in dtCardResult.Rows)
                            {
                                sb.Append("{");
                                foreach (DataColumn column in dtCardResult.Columns)
                                {
                                    if (numericTypes.Contains(column.DataType))
                                        sb.Append("\"" + column.ColumnName + "\":" + dataRow[column.ColumnName].ToString().Replace("\"", "\\\"") + ",");
                                    else
                                        sb.Append("\"" + column.ColumnName + "\":\"" + dataRow[column.ColumnName].ToString().Replace("\"", "\\\"") + "\",");
                                }
                                sb.Remove(sb.Length - 1, 1); // remove the last comma
                                sb.Append("},");
                            }
                            sb.Remove(sb.Length - 1, 1); // remove the last comma
                            sb.Append("]");
                        }

                        if (row["cardsql"].ToString().Contains(":"))
                        {
                            sb.Append(",");
                            sb.Append("\"parameters\": \"" + GetParametersFromSQL(row["cardsql"].ToString()) + "\"");
                        }
                        sb.Append("}");

                        string json = sb.ToString();
                        row["cardsql"] = JObject.Parse(json);
                        if (string.IsNullOrEmpty(row["charttype"].ToString()))
                        {
                            row["charttype"] = "";
                        }
                    }
                    catch (Exception ex)
                    {
                        row["cardsql"] = JsonConvert.SerializeObject(ex.Message, Formatting.Indented);
                    }
                }
            }
            return dtCards;
        }
        private string GetParametersFromSQL(string sql)
        {
            var regex = new Regex(@":\w+");
            var matches = regex.Matches(sql);
            var uniqueMatches = matches.OfType<System.Text.RegularExpressions.Match>().Select(m => m.Value).Distinct();
            string result = "";
            foreach (string match in uniqueMatches)
            {
                result += match.Substring(1) + ","; // Remove the colon character
            }

            return result.Trim(',');
        }
        public string GenerateCardSql(DataTable cardResult)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            sb.Append("\"fields\": [");
            foreach (DataColumn column in cardResult.Columns)
            {
                sb.Append("{");
                sb.Append("\"name\": \"" + column.ColumnName + "\",");
                sb.Append("\"datatype\": \"" + column.DataType.Name + "\"");
                sb.Append("},");
            }
            sb.Remove(sb.Length - 1, 1); // remove the last comma
            sb.Append("]");
            sb.Append(",");
            sb.Append("\"row\": ");

            if (cardResult.Rows.Count == 0)
            {
                sb.Append("[]");
            }
            else
            {
                sb.Append("[");
                foreach (DataRow dataRow in cardResult.Rows)
                {
                    sb.Append("{");
                    foreach (DataColumn column in cardResult.Columns)
                    {
                        sb.Append("\"" + column.ColumnName + "\":\"" + dataRow[column.ColumnName].ToString().Replace("\r\n", "") + "\",").Replace("\r\n", "");
                    }
                    sb.Remove(sb.Length - 1, 1); // remove the last comma
                    sb.Append("},");
                }
                sb.Remove(sb.Length - 1, 1); // remove the last comma
                sb.Append("]");
            }

            sb.Append("}");
            return sb.ToString();

        }

        private async Task<DataTable> GetCardsWiseData(DataTable dtCards, Cards cards)
        {
            DataTable cardResult = new DataTable();
            foreach (DataRow row in dtCards.Rows)
            {
                string cardsql = row["cardsql"]?.ToString();
                if (!string.IsNullOrEmpty(cardsql))
                {
                    int colonIndex = cardsql.IndexOf(':');

                    if (colonIndex != -1)
                    {
                        string variableName = cardsql.Substring(colonIndex);
                        string username = await GetUserName(cards.ARMSessionId);
                        cardsql = cardsql.Replace(":username", $"'{username}'");
                    }
                    try
                    {
                        cardResult = await GetCardData(cards.ARMSessionId, cardsql);
                        string charttype = row["charttype"].ToString().ToLower();
                        if (row["cardtype"].ToString() == "chart" && !string.IsNullOrEmpty(charttype))
                        {
                            string[] columnNames = null;
                            Type[] columnTypes = null;
                            bool isValidMetaData = true;
                            if (charttype == "line" || charttype == "bar" || charttype == "stacked-bar" || charttype == "column" || charttype == "stacked-column" || charttype == "stacked-percentage-column" || charttype == "area")
                            {
                                columnNames = new string[] { "data_label", "x_axis", "value", "link" };
                                columnTypes = new Type[] { typeof(string), typeof(string), typeof(int), typeof(string) };
                            }
                            else if (charttype == "pie" || charttype == "semi-donut" || charttype == "donut")
                            {
                                columnNames = new string[] { "data_label", "value", "link" };
                                columnTypes = new Type[] { typeof(string), typeof(int), typeof(string) };
                            }
                            else if (charttype == "stacked-group-column")
                            {
                                columnNames = new string[] { "data_label", "group_column", "value", "link" };
                                columnTypes = new Type[] { typeof(string), typeof(string), typeof(int), typeof(string) };
                            }
                            else if (charttype == "scatter-plot" || charttype == "scatter-plot-3D")
                            {
                                columnNames = new string[] { "data_label", "x_axis_data", "y_axis_data", "z_axis_data" };
                                columnTypes = new Type[] { typeof(string), typeof(string), typeof(int), typeof(string) };
                            }
                            else if (charttype == "funnel")
                            {
                                columnNames = new string[] { "data_label", "value" };
                                columnTypes = new Type[] { typeof(string), typeof(int) };
                            }
                            foreach (DataColumn column in cardResult.Columns)
                            {
                                if (!columnNames.Contains(column.ColumnName))
                                {
                                    isValidMetaData = false;
                                    break;
                                }
                            }

                            if (!isValidMetaData)
                            {
                                for (int i = 0; i < columnNames.Length; i++)
                                {
                                    if (i < cardResult.Columns.Count && cardResult.Columns[i] != null)
                                    {
                                        if (columnNames[i] == "value")
                                        {
                                            if (cardResult.Columns[i].DataType == typeof(int) || cardResult.Columns[i].DataType == typeof(Int32) || cardResult.Columns[i].DataType == typeof(Int64) || cardResult.Columns[i].DataType == typeof(Decimal) || cardResult.Columns[i].DataType == typeof(float) || cardResult.Columns[i].DataType == typeof(Double))
                                            {
                                                if (cardResult.Columns.Contains(columnNames[i]))
                                                {
                                                    cardResult.Columns[columnNames[i]].ColumnName = columnNames[i] + i;//Renaming columns
                                                }
                                                cardResult.Columns[i].ColumnName = columnNames[i];
                                            }
                                            else
                                            {
                                                try
                                                {
                                                    cardResult.Columns.Add(columnNames[i], columnTypes[i]).SetOrdinal(i);
                                                }
                                                catch { }
                                            }
                                        }
                                        else
                                        {
                                            if (cardResult.Columns.Contains(columnNames[i]))
                                            {
                                                cardResult.Columns[columnNames[i]].ColumnName = columnNames[i] + i;//Renaming columns
                                            }
                                        }
                                        cardResult.Columns[i].ColumnName = columnNames[i];

                                    }
                                    else
                                    {
                                        try
                                        {
                                            cardResult.Columns.Add(columnNames[i], columnTypes[i]).SetOrdinal(i);
                                        }
                                        catch { }
                                    }
                                }
                            }
                            else
                            {
                                for (int i = 0; i < columnNames.Length; i++)
                                {
                                    if (!cardResult.Columns.Contains(columnNames[i]))
                                    {
                                        cardResult.Columns.Add(columnNames[i], columnTypes[i]);
                                    }
                                }
                            }

                        }

                        row["cardsql"] = GetCardDataString(cardResult);

                        if (string.IsNullOrEmpty(row["charttype"].ToString()))
                        {
                            row["charttype"] = "";
                        }
                    }
                    catch (Exception ex)
                    {
                        row["cardsql"] = JsonConvert.SerializeObject(ex.Message, Formatting.Indented);
                    }
                }
            }

            return dtCards;
        }

        public async Task<SQLResult> GetCardsData(Cards cards)
        {
            SQLResult sqlResult = new SQLResult();
            DataTable dtCards = new DataTable();
            if (string.IsNullOrEmpty(cards.CardId))
            {
                dtCards = await GetCardsList(cards);
            }
            else
            {
                dtCards = await GetCardsListByIds(cards);
            }
            if (dtCards.Rows.Count > 0)
            {
                sqlResult.data = await GetCardsWiseData(dtCards, cards);
            }
            else
            {
                sqlResult.error = Constants.RESULTS.NO_RECORDS.ToString();
            }

            return sqlResult;
        }

        public async Task<SQLResult> GetProcessCardsData(ARMProcessFlowTask processFlow)
        {
            SQLResult sqlResult = new SQLResult();
            DataTable dtCards = await GetProcessCards(processFlow);
            if (dtCards.Rows.Count > 0)
            {
                Cards cards = new Cards();
                cards.ARMSessionId = processFlow.ARMSessionId;
                sqlResult.data = await GetCardsWiseData(dtCards, cards);
            }
            else
            {
                sqlResult.error = Constants.RESULTS.NO_RECORDS.ToString();
            }

            return sqlResult;
        }

        

        private string GetCardDataString(DataTable cardResult)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            sb.Append("\"fields\": [");
            foreach (DataColumn column in cardResult.Columns)
            {
                sb.Append("{");
                sb.Append("\"name\": \"" + column.ColumnName + "\",");
                sb.Append("\"datatype\": \"" + column.DataType.Name + "\"");
                sb.Append("},");
            }
            sb.Remove(sb.Length - 1, 1); // remove the last comma
            sb.Append("]");
            sb.Append(",");
            sb.Append("\"row\": ");

            if (cardResult.Rows.Count == 0)
            {
                sb.Append("[]");
            }
            else
            {
                sb.Append("[");
                foreach (DataRow dataRow in cardResult.Rows)
                {
                    sb.Append("{");
                    foreach (DataColumn column in cardResult.Columns)
                    {
                        sb.Append("\"" + column.ColumnName + "\":\"" + dataRow[column.ColumnName].ToString().Replace("\r\n", "") + "\",").Replace("\r\n", "");
                    }
                    sb.Remove(sb.Length - 1, 1); // remove the last comma
                    sb.Append("},");
                }
                sb.Remove(sb.Length - 1, 1); // remove the last comma
                sb.Append("]");
            }

            sb.Append("}");
            return sb.ToString();
        }

        public async Task<SQLResult> GetCardsWithData(Cards cards)
        {
            var result = await GetCardsListFromDB(cards);
            if (string.IsNullOrEmpty(result.error) && result.data != null)
            {
                result.data = await GetCardsListData(cards, result.data);
                result.data = _common.ConvertNumericColumnsToString(result.data);
            }            

            return result;
        }


        private async Task<SQLResult> GetCardsListFromDB(Cards cards)
        {
            var loginuser = await GetLoginUser(cards.ARMSessionId);
            Dictionary<string, string> config = await _common.GetDBConfigurations(loginuser["APPNAME"]);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];

            string sql = "";

            if (cards.HomePageCards == false)
            {
                sql = Constants_SQL.GET_DASHBOARDCARDSLIST.ToString();
                if (dbType.ToUpper() == Constants.DBTYPE.ORACLE.ToString())
                {
                    sql = Constants_SQL.GET_DASHBOARDCARDSLIST_ORACLE.ToString();
                }
            }
            else
            {
                sql = Constants_SQL.GET_HOMEPAGECARDSLIST.ToString();
                if (dbType.ToUpper() == Constants.DBTYPE.ORACLE.ToString())
                {
                    sql = Constants_SQL.GET_HOMEPAGECARDSLIST_ORACLE.ToString();
                }
            }

            if (string.IsNullOrEmpty(cards.Roles))
            {
                cards.Roles = await _redis.HashGetAsync(cards.ARMSessionId, Constants.SESSION_DATA.USER_ROLES.ToString());
            }

            string roles = string.Join(",", cards.Roles.Split(',').Select(r => $"'{r}'"));
            sql = sql.Replace("$ROLES$", roles);

            IDbHelper dbHelper = DBHelper.CreateDbHelper(dbType);
            return await dbHelper.ExecuteSQLAsync(sql, connectionString);
        }


        public async Task<DataTable> GetCardsListData(Cards cards, DataTable cardsList)
        {
            var loginuser = await GetLoginUser(cards.ARMSessionId);
            Dictionary<string, string> config = await _common.GetDBConfigurations(loginuser["APPNAME"]);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];
            
            try
            {
                cards.GlobalParams = await _common.GetGlobalParams(cards.ARMSessionId);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error fetching GlobalVars: " + ex.Message);
            }

            if (cardsList != null && cardsList.Rows.Count > 0)
            {
                cardsList.Columns.Add("carddata", typeof(string));
                IDbHelper dbHelper = DBHelper.CreateDbHelper(dbType);

                if(string.IsNullOrEmpty(cards.UserName))
                    cards.UserName = loginuser[Constants.SESSION_DATA.USERNAME.ToString()];
                
                if (string.IsNullOrEmpty(cards.AppName))
                    cards.AppName = loginuser[Constants.SESSION_DATA.APPNAME.ToString()];

                if (string.IsNullOrEmpty(cards.SchemaName))
                    cards.SchemaName = loginuser[Constants.SESSION_DATA.SCHEMANAME.ToString()];

                foreach (DataRow dr in cardsList.Rows)
                {
                    string cardData = "";
                    string sql = dr["sqltext"]?.ToString() ?? "";
                    string cardId = dr["axp_cardsid"]?.ToString() ?? "";
                    string refreshInterval = dr["autorefresh"]?.ToString() ?? "";

                    string redisKey = !string.IsNullOrEmpty(cards.SchemaName)
                        ? $"{cards.SchemaName}-ARM-CARDDATA-{cards.UserName}-{cardId}"
                        : $"{cards.AppName}-ARM-CARDDATA-{cards.UserName}-{cardId}";

                    if (!string.IsNullOrEmpty(sql))
                    {
                        if (cards.RefreshData == false && !string.IsNullOrEmpty(refreshInterval) && refreshInterval != "0" && _redis.KeyExists(redisKey))
                        {
                            cardData = await _redis.StringGetAsync(redisKey);
                            dr["carddata"] = cardData;
                        }
                        else
                        {
                            if (cards.GlobalParams != null && cards.GlobalParams.Count > 0)
                            {
                                sql = _common.ReplaceSqlParameters(sql, cards.GlobalParams);
                            }

                            var result = await dbHelper.ExecuteSQLAsync(sql, connectionString);
                            if (string.IsNullOrEmpty(result.error))
                            {
                                cardData = JsonConvert.SerializeObject(result.data);

                                if (!string.IsNullOrEmpty(refreshInterval) && refreshInterval != "0")
                                {
                                    int.TryParse(refreshInterval, out int expiry);
                                    if (expiry > 0)
                                        expiry *= 60; // Convert minutes to seconds

                                    await _redis.StringSetAsync(redisKey, cardData, expiry);
                                }
                            }
                            else
                            {
                                cardData += result.error;
                            }
                            dr["carddata"] = cardData;
                        }
                    }
                }

                cardsList.Columns.Remove("sqltext");
                if (cards.IsMobile == true)
                    cardsList.Columns.Remove("html_editor_card");
            }

            return cardsList;
        }


        public string ReplaceSqlParameters(string sql, Dictionary<string, string> parameters)
        {
            Regex regex = new Regex(@"\:\w+");
            StringBuilder sb = new StringBuilder(sql);
            MatchCollection matches = regex.Matches(sql);

            // Create a case-insensitive dictionary lookup
            var caseInsensitiveParams = new Dictionary<string, string>(parameters, StringComparer.OrdinalIgnoreCase);

            // Iterate in reverse to avoid messing up string indexes
            for (int i = matches.Count - 1; i >= 0; i--)
            {
                Match match = matches[i];
                string paramKey = match.Value.Substring(1); // Remove leading ':'

                if (caseInsensitiveParams.TryGetValue(paramKey, out string value))
                {
                    sb.Remove(match.Index, match.Length);
                    sb.Insert(match.Index, "'" + value.Replace("'", "''") + "'"); // Escape single quotes for SQL
                }
            }

            return sb.ToString();
        }
        public async Task<NonQueryResult> AddorEditHTMLPlugin(HTMLPlugin plugin)
        {
            var loginuser = await GetLoginUser(plugin.ARMSessionId);
            Dictionary<string, string> config = await _common.GetDBConfigurations(loginuser["APPNAME"]);
            string connectionString = config["ConnectionString"];
            var result = new NonQueryResult();
            string dbType = config["DBType"];
            if (plugin.Action == "DELETE")
            {
                string sql = Constants_SQL.DELETE_HTMLPLUGINS.ToString();
                string[] paramName = { "@name" };
                DbType[] paramType = { DbType.String };
                object[] paramValue = { plugin.Name };
                IDbHelper dbHelper = DBHelper.CreateDbHelper(dbType);
                result = await dbHelper.ExecuteNonQueryAsync(sql, connectionString, paramName, paramType, paramValue);
            }
            else
            {
                string sql = "";
                if (plugin.Action == "INSERT")
                {
                    sql = Constants_SQL.INSERT_HTMLPLUGINS.ToString();
                    string[] paramName = { "@name", "@context", "@htmltext" };
                    DbType[] paramType = { DbType.String, DbType.String, DbType.String };
                    object[] paramValue = { plugin.Name, plugin.Context, plugin.HTMLText };
                    IDbHelper dbHelper = DBHelper.CreateDbHelper(dbType);
                    result = await dbHelper.ExecuteNonQueryAsync(sql, connectionString, paramName, paramType, paramValue);
                }
                else if (plugin.Action == "UPDATE")
                {
                    sql = Constants_SQL.UPDATE_HTMLPLUGINS.ToString();
                    string[] paramName = { "@context", "@htmltext", "@name" };
                    DbType[] paramType = { DbType.String, DbType.String, DbType.String };
                    object[] paramValue = { plugin.Context, plugin.HTMLText, plugin.Name };
                    IDbHelper dbHelper = DBHelper.CreateDbHelper(dbType);
                    result = await dbHelper.ExecuteNonQueryAsync(sql, connectionString, paramName, paramType, paramValue);
                }
            }
            return result;
        }

        public async Task<SQLResult> GetHTMLPlugins(HTMLPlugin plugin)
        {
            var loginuser = await GetLoginUser(plugin.ARMSessionId);
            Dictionary<string, string> config = await _common.GetDBConfigurations(loginuser["APPNAME"]);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];

            string sql = Constants_SQL.GET_HTMLPLUGINS.ToString();
            IDbHelper dbHelper = DBHelper.CreateDbHelper(dbType);
            return await dbHelper.ExecuteSQLAsync(sql, connectionString);
        }

    }


}

public class ApiResponse
{
    public List<ResultWrapper> result { get; set; }
}

public class ResultWrapper
{
    public ResultData result { get; set; }
}

public class ResultData
{
    public string status { get; set; }
    public string s { get; set; }
    public string ugroup { get; set; }
    public Dictionary<string, string> globalvars { get; set; }
}





