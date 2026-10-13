using ARM_APIs.Interface;
using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMCommon.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NpgsqlTypes;
using NPOI.POIFS.Crypt.Dsig;
using System;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;

namespace ARM_APIs.Model
{
    public class ARMMenu : IARMMenu
    {
        private readonly DataContext _context;
        private readonly IRedisHelper _redis;
        private readonly IConfiguration _config;
        private readonly IPostgresHelper _postGres;
        private readonly Utils _common;
        public ARMMenu(DataContext context, IRedisHelper redis, IConfiguration configuration, IPostgresHelper postGres, Utils common)
        {
            _context = context;
            _redis = redis;
            _config = configuration;
            _postGres = postGres;
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

        
        public async Task<DataTable> GetMenuForDefaultRole(string sessionId)
        {
            ParamsDetails parameters = new ParamsDetails();
            parameters.ParamsNames = new List<ConnectionParamsList>();

            var loginuser = await GetLoginUser(sessionId);
            Dictionary<string, string> config = await _common.GetDBConfigurations(loginuser["APPNAME"]);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];
            string sql = Constants_SQL.ARMGETMENUQUERY.ToString();
            IDbHelper dbHelper = DBHelper.CreateDbHelper(sql, dbType, connectionString, new string[] { }, new DbType[] { }, new object[] { });
            if (dbType.ToUpper() == Constants.DBTYPE.ORACLE.ToString())
            {
                sql = Constants_SQL.ARMGETMENUQUERY_ORACLE.ToString();
            }
            var table = await dbHelper.ExecuteQueryAsync(sql, connectionString, new string[] { }, new DbType[] { }, new object[] { });
            return table;
        }


        

        

        public async Task<DataTable> GetnamesForOtherRoles(string sessionId, string allRole)
        {
            ParamsDetails parameters = new ParamsDetails();
            parameters.ParamsNames = new List<ConnectionParamsList>();
            //string connectionString = await GetDBConnString(sessionId);
            var loginuser = await GetLoginUser(sessionId);
            Dictionary<string, string> config = await _common.GetDBConfigurations(loginuser["APPNAME"]);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];
            string sql = Constants_SQL.GETMENUUSERACCESS.ToString().Replace("$allRole$", allRole);
            IDbHelper dbHelper = DBHelper.CreateDbHelper(sql, dbType, connectionString, new string[] { }, new DbType[] { }, new object[] { });
           if (dbType.ToUpper() == Constants.DBTYPE.ORACLE.ToString())
            {
                sql = Constants_SQL.GETMENUUSERACCESS_ORACLE.ToString().Replace("$allRole$", allRole);
            }

            var table = await dbHelper.ExecuteQueryAsync(sql, connectionString, new string[] { }, new DbType[] { }, new object[] { });
            return table;
        }

        public async Task<DataTable> GetMenuForOtherRole(string sessionId, string names)
        {
            DataTable dt = new DataTable();
            ParamsDetails parameters = new ParamsDetails();
            parameters.ParamsNames = new List<ConnectionParamsList>();
            //string connectionString = await GetDBConnString(sessionId);
            var loginuser = await GetLoginUser(sessionId);
            Dictionary<string, string> config = await _common.GetDBConfigurations(loginuser["APPNAME"]);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];
            string userMenuSql = Constants_SQL.AXPAGES.ToString().Replace("$names$", names);
            IDbHelper dbHelper = DBHelper.CreateDbHelper(userMenuSql, dbType, connectionString, new string[] { }, new DbType[] { }, new object[] { });
            if (dbType.ToUpper() == Constants.DBTYPE.ORACLE.ToString())
            {
                userMenuSql = Constants_SQL.AXPAGES_ORACLE.ToString().Replace("$names$", names);
            }
            var table = await dbHelper.ExecuteQueryAsync(userMenuSql, connectionString, new string[] { }, new DbType[] { }, new object[] { });
            
            return table;
        }



        public async Task<DataTable> GetCardsList(string ARMSessionId, bool? IsHomePage)
        {
            ParamsDetails parameters = new ParamsDetails();
            parameters.ParamsNames = new List<ARMCommon.Model.ConnectionParamsList>();

            var loginuser = await GetLoginUser(ARMSessionId);
            Dictionary<string, string> config = await _common.GetDBConfigurations(loginuser["APPNAME"]);

            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];

            var dictSession = await _redis.HashGetAllDictAsync(ARMSessionId);
            string roles = dictSession["USER_ROLES"];
            string[] arrRoles = roles.Split(','); 

            string cardsFilter = "(";
            foreach (string role in arrRoles)
            {
                cardsFilter += "lower(accessstring) LIKE '%" + role + "%' OR ";
            }
            cardsFilter = cardsFilter.TrimEnd(" OR ".ToCharArray()) + ")";


            string sql = Constants_SQL.GETCARDLISTS.ToString();


            if (IsHomePage != null && IsHomePage == true)
            {
                cardsFilter += " and inhomepage = 'T' and cardtype = 'kpi' ";
            }
           

            sql = sql.Replace("$cardsFilter$", cardsFilter);

            // Execute the query using the appropriate DBHelper
            IDbHelper dbHelper = DBHelper.CreateDbHelper(dbType);
            var table = await dbHelper.ExecuteQueryAsync(sql, connectionString, new string[] { }, new DbType[] { }, new object[] { });

            return table;
        }




        public async Task<DataTable> GetCardSQL(string sessionId, string cardsql)
        {
            var loginuser = await GetLoginUser(sessionId);
            Dictionary<string, string> config = await _common.GetDBConfigurations(loginuser["APPNAME"]);
            string connectionString = config["ConnectionString"];

            return await _postGres.ExecuteSql(cardsql, connectionString, new string[] { }, new NpgsqlDbType[] { }, new object[] { });
        }

        public async Task<DataTable> GetCardData(string sessionId, string cardsql, Dictionary<string, string> sqlParams)
        {
            string connectionString = await GetDBConnString(sessionId);
            string sql = cardsql;
            sql = sql.Replace(":", "@");
            ParamsDetails parameters = GetSQLParams(sqlParams);
            return await _postGres.ExecuteSelectSql(sql, connectionString, parameters);

        }

        public async Task<DataTable> GetCardListById(string sessionId, string cardId)
        {
            ParamsDetails parameters = new ParamsDetails();
            parameters.ParamsNames = new List<ConnectionParamsList>();
            string connectionString = await GetDBConnString(sessionId);
            var dictSession = await _redis.HashGetAllDictAsync(sessionId);
            string roles = cardId;
            string[] roleArray = roles.Split(','); //Split roles into an array

            string cardsFilter = "";
            foreach (string role in roleArray)
            {
                cardsFilter += "axp_cardsid = " + role + " OR ";
            }
            cardsFilter = cardsFilter.TrimEnd(" OR ".ToCharArray()); // Remove trailing OR       
            string sql = Constants_SQL.GETCARDLISTS.ToString().Replace("$cardsFilter$", cardsFilter);
            return await _postGres.ExecuteSql(sql, connectionString, new string[] { }, new NpgsqlDbType[] { }, new object[] { });

        }

        public async Task<DataTable> GetProcessCards(ARMProcessFlowTask processFlow)
        {

            string connectionString = await GetDBConnString(processFlow.ARMSessionId);
            string sql = Constants_SQL.GET_PROCESSCARDS.ToString();
            string[] paramName = { "@processname", "@taskname" };
            NpgsqlDbType[] paramType = { NpgsqlDbType.Varchar, NpgsqlDbType.Varchar };
            object[] paramValue = { processFlow.ProcessName.ToLower(), processFlow.TaskName.ToLower() };
            return await _postGres.ExecuteSql(sql, connectionString, paramName, paramType, paramValue);

            //ParamsDetails parameters = new ParamsDetails();
            //parameters.ParamsNames = new List<ConnectionParamsList>();
            //string connectionString = await GetDBConnString(processFlow.ARMSessionId);
            //string cardsFilter = "";
            //DataTable dtCardIds = await GetProcessCardsIds(processFlow);
            //if (dtCardIds.Rows.Count > 0)
            //{
            //    foreach (DataRow card in dtCardIds.Rows)
            //    {
            //        cardsFilter += $"axp_cardsid = {card[0]} OR ";
            //    }
            //    cardsFilter = cardsFilter.TrimEnd(" OR ".ToCharArray()); // Remove trailing OR
            //    cardsFilter = $"({cardsFilter})";
            //    string sql = Constants_SQL.GETCARDLISTS.ToString().Replace("$cardsFilter$", cardsFilter);
            //    return await _postGres.ExecuteSql(sql, connectionString, new string[] { }, new NpgsqlDbType[] { }, new object[] { });
            //}
            //else
            //{
            //    return new DataTable();
            //}

        }

        //public async Task<DataTable> GetProcessCardsIds(ARMProcessFlowTask processFlow)
        //{
        //    ParamsDetails parameters = new ParamsDetails();
        //    parameters.ParamsNames = new List<ConnectionParamsList>();
        //    string connectionString = await GetDBConnString(processFlow.ARMSessionId);
        //    string sql = Constants_SQL.GET_PROCESSCARDIDS.ToString();
        //    string[] paramName = { "@processname", "@taskname" };
        //    NpgsqlDbType[] paramType = { NpgsqlDbType.Varchar, NpgsqlDbType.Varchar };
        //    object[] paramValue = { processFlow.ProcessName.ToLower(), processFlow.TaskName.ToLower() };
        //    return await _postGres.ExecuteSql(sql, connectionString, paramName, paramType, paramValue);
        //}

        public async Task<List<string>> GetallRole(string sessionId)
        {
            string sessionValues = await _redis.HashGetAsync(sessionId, Constants.SESSION_DATA.USER_ROLES.ToString());
            List<string> userroles = sessionValues.Split(',').ToList();
            return userroles;
        }
        public async Task<string> GetUserName(string sessionId)
        {
            var dictSession = await _redis.HashGetAllDictAsync(sessionId);
            return dictSession["USERNAME"];
        }
        private ParamsDetails GetSQLParams(Dictionary<string, string> sqlParams)
        {
            ParamsDetails parameters = new ParamsDetails();
            parameters.ParamsNames = new List<ConnectionParamsList>();

            foreach (var sqlParam in sqlParams)
            {
                parameters.ParamsNames.Add(new ConnectionParamsList
                {
                    Name = "@" + sqlParam.Key.Split("~")[0],
                    Type = NpgsqlDbType.Varchar, //GetNpgsqlDbType(sqlParam.Key.Split("~")[1]),
                    Value = sqlParam.Value
                });
            }
            return parameters;
        }

        public async Task<DataTable> GetCardsData(ARMProcessFlowTask processFlow, DataTable dtCards)
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
        public string GenerateCardSql(DataTable cardresult)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            sb.Append("\"fields\": [");
            foreach (DataColumn column in cardresult.Columns)
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

            if (cardresult.Rows.Count == 0)
            {
                sb.Append("[]");
            }
            else
            {
                sb.Append("[");
                foreach (DataRow dataRow in cardresult.Rows)
                {
                    sb.Append("{");
                    foreach (DataColumn column in cardresult.Columns)
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

        public async Task<DataTable> GetHomePage(string sessionId, string userName)
        {
            var loginuser = await GetLoginUser(sessionId);
            if (string.IsNullOrEmpty(userName))
            {
                userName = loginuser["USERNAME"].ToString();
            }
            Dictionary<string, string> config = await _common.GetDBConfigurations(loginuser["APPNAME"]);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];
            string sql = Constants_SQL.HOMEPAGECARDSV2.ToString();
            string[] paramName = { "@username" };
            DbType[] paramType = { DbType.String };
            object[] paramValue = { userName };

            if (dbType.ToUpper() == Constants.DBTYPE.ORACLE.ToString())
            {
                sql = Constants_SQL.GET_HOMEPAGECARDS_ORACLE.ToString();
            }

            IDbHelper dbHelper = DBHelper.CreateDbHelper(sql, dbType, connectionString, paramName, paramType, paramValue);
            var table = await dbHelper.ExecuteQueryAsync(sql, connectionString, paramName, paramType, paramValue);
            return table;
        }


    }
}
