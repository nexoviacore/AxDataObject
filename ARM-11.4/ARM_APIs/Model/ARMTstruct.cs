using ARM_APIs.Interface;
using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMCommon.Model;
using Microsoft.AspNetCore.Rewrite;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NpgsqlTypes;
using StackExchange.Redis;
using System.Data;
using System.Dynamic;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using static ARMCommon.Helpers.Constants;

namespace ARM_APIs.Model
{
    public class ARMTstruct : IARMTstruct
    {
        private readonly IRedisHelper _redis;
        private readonly IPostgresHelper _postGres;
        private readonly IConfiguration _config;
        private readonly Utils _common;

        public ARMTstruct(IRedisHelper redis, IPostgresHelper postGres, IConfiguration config, Utils common)
        {
            _redis = redis;
            _postGres = postGres;
            _config = config;
            _common = common;   
        }

        public async Task<bool> IsValidAxpertConnection(ARMAxpertConnect axpert)
        {
            var axSession = await _redis.HashGetAsync(axpert.ARMSessionId, Constants.SESSION_DATA.AXPERT_SESSIONID.ToString());
            if (axSession == axpert.AxSessionId)
            {
                return true;
            }
            return false;
        }
        public async Task<string> GetTstructSQLQuery(string transId, string field , string sessionid)
        {
            //string connectionString = await _common.GetDBConfigurationBySessionId(sessionid);
            //string selectSql = $"select fldsql  from axpflds a where modeofentry in('accept','select') and lower(tstruct) = @transid and lower(fname) = @field";
            var loginuser = await _redis.HashGetAllDictAsync(sessionid);
            Dictionary<string, string> config = await _common.GetDBConfigurations(loginuser["APPNAME"]);
            string selectSql = Constants_SQL.GETTSTRUCTSQLQUERY.ToString();
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];
            string[] paramName = { "@transid", "@field" };
            object[] paramValue = { transId.ToLower(), field.ToLower() };
            DbType[] paramType = { DbType.String, DbType.String };

            var sql = "";
            //var dt = await _postGres.ExecuteSql(selectSql, connectionString, paramName, paramType, paramValue);
            IDbHelper dbHelper = DBHelper.CreateDbHelper(selectSql, dbType, connectionString, paramName, paramType, paramValue);
            var dt = await dbHelper.ExecuteQueryAsync(selectSql, connectionString, paramName, paramType, paramValue);
            if (dt.Rows.Count > 0)
            {
                sql = dt.Rows[0]["fldsql"].ToString();
            }

            if (string.IsNullOrEmpty(sql))
            {
                return Constants.RESULTS.NO_RECORDS.ToString();
            }
            var paramsList = GetParametersFromSQL(sql);
            await _redis.HashSetAsync(transId.ToUpper(), field.ToUpper(), sql);
            await _redis.HashSetAsync(transId.ToUpper(), $"{field.ToUpper()}-PARAMS", paramsList);
            return $"{sql}~~{paramsList}";
        }

        private string GetParametersFromSQL(string sql)
        {
            var regex = new Regex(@"[:?:](?<Parameter>[\S]+)");
            var matchCollection = regex.Matches(sql);
            var result = matchCollection.Cast<Match>().Select(x => x.Groups["Parameter"].Value).ToList<string>();
            return string.Join(",", result);
        }
        //public async Task<object> GetTstructSQLData(string transId, string field, Dictionary<string, string> sqlParams = null)
        //{
        //    var sql = await _redis.HashGetAsync(transId.ToUpper(), field.ToUpper());
        //    if (string.IsNullOrEmpty(sql))
        //    {
        //        sql = await GetTstructSQLQuery(transId, field);

        //        if (string.IsNullOrEmpty(sql))
        //        {
        //            return Constants.RESULTS.NO_RECORDS.ToString();
        //        }

        //        await _redis.HashSetAsync(transId.ToUpper(), field.ToUpper(), sql);

        //    }
        //}

        private ParamsDetails GetSQLParams(Dictionary<string, string> sqlParams)
        {
            ParamsDetails parameters = new ParamsDetails();
            parameters.ParamsNames = new List<ConnectionParamsList>();

            foreach (var sqlParam in sqlParams)
            {
                parameters.ParamsNames.Add(new ConnectionParamsList
                {
                    Name = "@"+ sqlParam.Key.Split("~")[0],
                    Type = GetNpgsqlDbType(sqlParam.Key.Split("~")[1]),
                    Value = sqlParam.Value
                });
            }
            return parameters;
        }

        public static NpgsqlDbType GetNpgsqlDbType(string type)
        {
            switch (type)
            {
                case "number":
                    return NpgsqlDbType.Integer;
                case "text":
                    return NpgsqlDbType.Varchar;
                case "largetext":
                    return NpgsqlDbType.Text;
                case "select":
                    return NpgsqlDbType.Varchar;
                case "date":
                    return NpgsqlDbType.Date;
                case "timestamp":
                    return NpgsqlDbType.Timestamp;
                case "boolean":
                    return NpgsqlDbType.Boolean;
                case "float":
                    return NpgsqlDbType.Double;
                case "numeric":
                    return NpgsqlDbType.Numeric;
                case "uuid":
                    return NpgsqlDbType.Uuid;
                case "bytea":
                    return NpgsqlDbType.Bytea;
                case "json":
                    return NpgsqlDbType.Json;
                case "jsonb":
                    return NpgsqlDbType.Jsonb;
                case "default":
                    return NpgsqlDbType.Varchar;
            }
            return NpgsqlDbType.Varchar;
        }

        public async Task<APIResult> LoadData(TstructDef tstructDef)
        {
            var apiResult = new APIResult();

            try
            {
                var sessionVars = await GetSessionData(tstructDef.ARMSessionId);

                string axpertWebScriptsURL = sessionVars[SESSION_DATA.SCRIPTSURL.ToString()].ToString();
                string appName = sessionVars[SESSION_DATA.APPNAME.ToString()].ToString();

                var structureXML = "";
                var rulesJson = "";
                var structureDetails = await GetStructureXML(tstructDef);
                if (structureDetails.success)
                {
                    structureXML = structureDetails.data["xml"].ToString();
                    rulesJson = structureDetails.data["rules"].ToString();
                }

                dynamic loaddata = new ExpandoObject();
                loaddata.axpapp = appName;
                loaddata.username = sessionVars[SESSION_DATA.USERNAME.ToString()].ToString();
                loaddata.seed = "";
                loaddata.sessionid = tstructDef.ARMSessionId;
                loaddata.transid = tstructDef.TransId;
                loaddata.recordid = tstructDef.RecordId;
                loaddata.dcname = "";
                loaddata.trace = tstructDef.Trace.ToString();

                dynamic inputJson = new ExpandoObject();
                inputJson.loaddata = loaddata;

                //TODO - Error needs to be handled.
                //if (!string.IsNullOrEmpty(structureXML)) {
                //    inputJson.sxml = structureXML;
                //}

                inputJson.axprops = JObject.Parse(sessionVars[SESSION_DATA.AXPROPS.ToString()].ToString());
                inputJson.globalvars = JObject.Parse(sessionVars[SESSION_DATA.GLOBALVARS.ToString()].ToString());
                inputJson.uservars = JObject.Parse(sessionVars[SESSION_DATA.USERVARS.ToString()].ToString());
                ((IDictionary<string, object>)inputJson)[appName] = JObject.Parse(sessionVars[SESSION_DATA.AXAPPS.ToString()].ToString());
                
                var json = JsonConvert.SerializeObject(inputJson);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                using (HttpClient httpClient = new HttpClient()) 
                {
                    var url = axpertWebScriptsURL + "/ASBTStructRest.dll/datasnap/rest/TASBTStruct/LoadData";
                    var response = await httpClient.PostAsync(url, content);

                    if (response.IsSuccessStatusCode)
                    {
                        string result = await response.Content.ReadAsStringAsync();
                        var resultObj = JObject.Parse(result);
                        if (resultObj["result"][0]["error"] != null)
                        {
                            apiResult.success = false;
                            apiResult.data["error"] = resultObj["result"][0]["error"];
                        }
                        else {
                            apiResult.success = true;
                            apiResult.data["data"] = resultObj["result"][0]["result"];
                            if (!string.IsNullOrEmpty(rulesJson)) {
                                apiResult.data["rules"] = rulesJson;
                            }
                        }                        
                    }
                    else
                    {
                        apiResult.success = false;
                        apiResult.error = response.StatusCode.ToString();
                    }
                }
                
            }
            catch (Exception ex)
            {
                apiResult.success = false;
                apiResult.error = ex.Message;
            }

            return apiResult;

        }

        public async Task<APIResult> SaveData(TstructDef tstructDef)
        {
            var apiResult = new APIResult();

            try
            {
                var sessionVars = await GetSessionData(tstructDef.ARMSessionId);

                string axpertWebScriptsURL = sessionVars[SESSION_DATA.SCRIPTSURL.ToString()].ToString();
                string appName = sessionVars[SESSION_DATA.APPNAME.ToString()].ToString();


                dynamic savedata = new ExpandoObject();
                savedata.cachedsave = "true";
                savedata.axpapp = appName;
                savedata.username = sessionVars[SESSION_DATA.USERNAME.ToString()].ToString();
                savedata.password = sessionVars[SESSION_DATA.PASSWORD.ToString()].ToString();
                savedata.seed = "";
                savedata.s = tstructDef.ARMSessionId;
                savedata.changedrows = new Dictionary<string, string>();
                savedata.transid = tstructDef.TransId;
                savedata.recordid = tstructDef.RecordId;
                savedata.trace = tstructDef.Trace.ToString();
                savedata.recdata = JsonConvert.DeserializeObject<List<object>>(System.Text.Json.JsonSerializer.Serialize(tstructDef.RecData));

                dynamic inputJson = new ExpandoObject();
                inputJson.savedata = savedata;

                inputJson.axprops = JObject.Parse(sessionVars[SESSION_DATA.AXPROPS.ToString()].ToString());
                inputJson.globalvars = JObject.Parse(sessionVars[SESSION_DATA.GLOBALVARS.ToString()].ToString());
                inputJson.uservars = JObject.Parse(sessionVars[SESSION_DATA.USERVARS.ToString()].ToString());
                ((IDictionary<string, object>)inputJson)[appName] = JObject.Parse(sessionVars[SESSION_DATA.AXAPPS.ToString()].ToString());

                var saveObj  = new {
                    _parameters = new List<object> { inputJson }
                };
                var json = JsonConvert.SerializeObject(saveObj);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                using (HttpClient httpClient = new HttpClient())
                {
                    var url = axpertWebScriptsURL + "/ASBTStructRest.dll/datasnap/rest/TASBTstruct/savedata";
                    var response = await httpClient.PostAsync(url, content);

                    if (response.IsSuccessStatusCode)
                    {
                        string result = await response.Content.ReadAsStringAsync();
                        var resultObj = JObject.Parse(result);
                        if (resultObj["result"][0]["error"] != null)
                        {
                            apiResult.success = false;
                            apiResult.data["error"] = resultObj["result"][0]["error"]["msg"];
                        }
                        else
                        {
                            apiResult.success = true;
                            apiResult.data["message"] = resultObj["result"][0]["message"][0]["msg"];
                            apiResult.data["recordid"] = resultObj["result"][0]["message"][0]["recordid"];
                        }
                    }
                    else
                    {
                        apiResult.success = false;
                        apiResult.error = response.StatusCode.ToString();
                    }
                }

            }
            catch (Exception ex)
            {
                apiResult.success = false;
                apiResult.error = ex.Message;
            }

            return apiResult;

        }


        public async Task<APIResult> GetStructureXML(TstructDef tstructDef)
        {
            var apiResult = new APIResult();

            try                
            {
                var sessionVars = await GetSessionData(tstructDef.ARMSessionId);
                var schemaName = sessionVars[SESSION_DATA.SCHEMANAME.ToString()].ToString();
                var language = sessionVars[SESSION_DATA.LANGUAGE.ToString()].ToString();
                var redisKey = $"{schemaName}-ARM-StructureXML-{tstructDef.TransId}-{language}";
                var structXML = await _redis.StringGetAsync(redisKey);
                var redisKeyRulesDef = $"{schemaName}-ARM-StructureXMLRules-{tstructDef.TransId}-{language}";
                var rulesJson = await _redis.StringGetAsync(redisKeyRulesDef);
                if (string.IsNullOrEmpty(structXML) || string.IsNullOrEmpty(rulesJson))
                {
                    string axpertWebScriptsURL = sessionVars[SESSION_DATA.SCRIPTSURL.ToString()].ToString();
                    string appName = sessionVars[SESSION_DATA.APPNAME.ToString()].ToString();

                    dynamic getstructurexml = new ExpandoObject();
                    getstructurexml.axpapp = appName;
                    getstructurexml.username = sessionVars[SESSION_DATA.USERNAME.ToString()].ToString();
                    getstructurexml.password = sessionVars[SESSION_DATA.PASSWORD.ToString()].ToString();
                    getstructurexml.seed = "";
                    getstructurexml.sname = "tstructs";
                    getstructurexml.name = tstructDef.TransId;
                    getstructurexml.trace = tstructDef.Trace.ToString();

                    var inputJson = new
                    {
                        getstructurexml = getstructurexml
                    };

                    var json = JsonConvert.SerializeObject(inputJson);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    using (HttpClient httpClient = new HttpClient())
                    {
                        var url = axpertWebScriptsURL + "/ASBDefineRest.dll/datasnap/rest/TASBDefineRest/GetStructureXML";
                        var response = await httpClient.PostAsync(url, content);

                        if (response.IsSuccessStatusCode)
                        {
                            string result = await response.Content.ReadAsStringAsync();
                            var resultObj = JObject.Parse(result);
                            if (resultObj["result"][0]["error"] != null)
                            {
                                apiResult.success = false;
                                apiResult.data["error"] = resultObj["result"][0]["error"];
                            }
                            else
                            {
                                apiResult.success = true;
                                var xml = resultObj["result"][0]["result"]["msg"].ToString();
                                var rules = GetRulesDefNodesFromXml(xml);

                                await _redis.StringSetAsync(redisKey, xml);
                                await _redis.StringSetAsync(redisKeyRulesDef, rules);

                                apiResult.data["xml"] = xml;
                                apiResult.data["rules"] = rules;
                            }
                        }
                        else
                        {
                            apiResult.success = false;
                            apiResult.error = response.StatusCode.ToString();
                        }
                    }
                }
                else {
                    apiResult.success = true;
                    apiResult.data["xml"] = structXML;
                    apiResult.data["rules"] = rulesJson;
                }

            }
            catch (Exception ex)
            {
                apiResult.success = false;
                apiResult.error = ex.Message;
            }

            return apiResult;

        }


        public string MD5Hash(string text)
        {
            MD5 md5 = new MD5CryptoServiceProvider();

            //compute hash from the bytes of text  
            md5.ComputeHash(ASCIIEncoding.ASCII.GetBytes(text));

            //get hash result after compute it  
            byte[] result = md5.Hash;

            StringBuilder strBuilder = new StringBuilder();
            for (int i = 0; i < result.Length; i++)
            {
                //change it into 2 hexadecimal digits  
                //for each byte  
                strBuilder.Append(result[i].ToString("x2"));
            }

            return strBuilder.ToString();
        }

        private static string GetRulesDefNodesFromXml(string xml)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            // Create a new XmlDocument to hold the extracted nodes
            XmlDocument newDoc = new XmlDocument();
            XmlElement wrapperNode = newDoc.CreateElement("Wrapper");

            XmlNode axrulesdefNode = doc.SelectSingleNode("//axrulesdef");
            XmlNode axrulesdefBtnMsgsNode = doc.SelectSingleNode("//axrulesdef_btnmsgs");

            if (axrulesdefNode != null)
            {
                XmlNode importedNode = newDoc.ImportNode(axrulesdefNode, true);
                wrapperNode.AppendChild(importedNode);
            }

            if (axrulesdefBtnMsgsNode != null)
            {
                XmlNode importedNode = newDoc.ImportNode(axrulesdefBtnMsgsNode, true);
                wrapperNode.AppendChild(importedNode);
            }

            // Add the wrapper node as the single root
            newDoc.AppendChild(wrapperNode);

            // Convert to JSON without including the wrapper node
            string json = JsonConvert.SerializeXmlNode(wrapperNode, Newtonsoft.Json.Formatting.Indented, true);

            return json;
        }

        public async Task<Dictionary<string, object>> GetSessionData(string sessionId)
        {
            HashEntry[] hashEntries = await _redis.HashGetAllAsync(sessionId);

            Dictionary<string, object> sessionVars = new Dictionary<string, object>();
            foreach (var entry in hashEntries)
            {
                sessionVars.Add(entry.Name, entry.Value);
            }

            return sessionVars;
        }
    }
   
}
