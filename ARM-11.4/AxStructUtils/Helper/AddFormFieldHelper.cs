using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMCommon.Model;
using ARMCommon.Services;
using ASBActionservice;
using ASBDefineservice;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Logging;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;
using System.ServiceModel.Channels;
using System.ServiceModel;
using System.Text.RegularExpressions;
using System.Xml;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace AxStructUtils
{
    public class AddFormFieldHelper
    {
        public ASBActionClient asbAction = new();
        public ASBDefineClient asbDefine = new();

        public IDbHelper _dbHelper;
        private readonly StructureRuntimeContext _runtimeContext;
        public AddFormFieldHelper(StructureRuntimeContext runtimeContext)
        {
            _runtimeContext = runtimeContext;
        }
        public async Task<string> CallWriteDefinitionWS(string scriptPath, string inputXML, string structure)
        {
            string result = string.Empty;
            try
            {
                using var asbAction = GetASBActionClient(scriptPath);
                asbAction.InnerChannel.OperationTimeout = TimeSpan.FromMinutes(5);
                result = (await asbAction.WriteDefinitionAsync(inputXML, structure)).@return;
            }
            catch (Exception ex)
            {
                result = "Exception :" + ex.Message;
            }
            return result;
        }
        public async Task<string> CallDeleteDefinitionWS(string scriptPath, string inputXML)
        {
            string result = string.Empty;
            try
            {
                using var asbAction = GetASBActionClient(scriptPath);
                asbAction.InnerChannel.OperationTimeout = TimeSpan.FromMinutes(5);
                result = (await asbAction.DeleteDefinitionAsync(inputXML)).@return;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            return result;
        }
        public async Task<string> CallGetStructureWS(string scriptPath, string transId, string inputXML)
        {
            string result = string.Empty;

            try
            {
                using var asbDefine = GetASBDefineClient(scriptPath);
                result = (await asbDefine.GetStructureAsync(inputXML)).@return;
            }
            catch (Exception ex)
            {
                result = ex.Message;
            }
            return result;
        }
        public ASBDefineClient GetASBDefineClient(string scriptPath)
        {
            string endpoint = $"{scriptPath.TrimEnd('/')}/ASBDefine.dll/soap/Asbdefine";
            //return new ASBDefineClient(ASBDefineClient.EndpointConfiguration.ASBDefinePort, endpoint);
            var binding = GetBindingForEndpoint(endpoint);
            return new ASBDefineClient(binding, new EndpointAddress(endpoint));
        }
        public ASBActionClient GetASBActionClient(string scriptPath)
        {
            string endpoint = $"{scriptPath.TrimEnd('/')}/ASBAction.dll/soap/ASBAction";
            //return new ASBActionClient(ASBActionClient.EndpointConfiguration.ASBActionPort, endpoint);
            var binding = GetBindingForEndpoint(endpoint);
            return new ASBActionClient(binding, new EndpointAddress(endpoint));
        }
        private static Binding GetBindingForEndpoint(string endpoint)
        {
            var binding = new BasicHttpBinding
            {
                MaxBufferSize = int.MaxValue,
                MaxReceivedMessageSize = int.MaxValue,
                ReaderQuotas = XmlDictionaryReaderQuotas.Max,
                AllowCookies = true
            };

            binding.Security.Mode =
                endpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                ? BasicHttpSecurityMode.Transport
                : BasicHttpSecurityMode.None;

            return binding;
        }
        public string getTstCompHashVal(string comp, string ordNo, string compCaption, string asGrid = "false", string tableName = "", string MOE = "", string fieldType = "")
        {
            string value = string.Empty;
            switch (comp)
            {
                case "dc":
                    value = $"{ordNo}♠{compCaption}♠{asGrid}♠{tableName}";
                    break;
                case "field":
                    value = $"{ordNo}♠{compCaption}♠{MOE}♠{fieldType}";
                    break;
                case "genmap":
                case "mdmap":
                case "fillgrid":
                case "script":
                case "udbutton":
                case "btngrp":
                    return $"{ordNo}♠{compCaption}";
                    break;
            }
            return value;
        }

        public string generateNodeKey(string structType, string structName, string compName = "", string comp = "")
        {
            string nodeKey = $"{GetStructKeyWord(structType)}♠{structName}";

            if (structType == "iview" && compName != "")
            {
                return nodeKey + "♠" + GetCompKeyWord(comp, structType) + "♠" + compName;
            }
            if (structType == "tstruct" && compName != "")
            {
                return nodeKey + "♠" + compName;
            }
            return nodeKey;
        }
        public string GetStructKeyWord(string structType)
        {
            switch (structType)
            {
                case "tstruct":
                    return "tst";
                case "iview":
                    return "iv";
                default:
                    return "";
                    break;
            }
        }
        public string GetCompKeyWord(string comp, string structType = "tstruct")
        {
            if (structType == "tstruct")
            {
                switch (comp)
                {
                    case "props":
                        return "axprops";
                    case "dc":
                        return "axdc";
                    case "genmap":
                        return "axgenmap";
                    case "mdmap":
                        return "axmdmap";
                    case "fillgrid":
                        return "axfillgrid";
                    case "script":
                        return "axscript";
                    case "field":
                        return "axfield";
                    case "defbtns":
                        return "defbtns";
                    case "udbutton":
                        return "axbtn";
                    case "btngrp":
                        return "axbtngrp";
                    case "actions":
                        return "axactions";
                    default:
                        throw new Exception("Exception in GetCompKeyWord: invalid component type.");
                }
            }
            else
            {
                switch (comp)
                {
                    case "props":
                        return "axprops";
                    case "param":
                        return "axparam";
                    case "subtotal":
                        return "axsubt";
                    case "script":
                        return "axscript";
                    case "defbtns":
                        return "defbtns";
                    case "udbutton":
                        return "axbtn";
                    case "btngrp":
                        return "axbtngrp";
                    case "actions":
                        return "axctions";
                    default:
                        throw new Exception("Exception in GetCompKeyWord: invalid component type.");
                }
            }
        }

        public async Task<string> ExecuteQuery(string projName, string userName, string gloStructFilter, QueryName queryName, JObject paramObj = null)
        {

            try
            {
                //var queryManager = (IQueryManager)ServiceProviderHelper.ServiceProvider.GetService(typeof(IQueryManager));
                //var (dbHelper, dbtype) = await _dbHelper.CreateDbHelper(projName, userName, gloStructFilter, this);

                //string query = queryManager.GetQuery(dbtype, QueryCategory.general, queryName);
                //_logHelper.AppendTrace("executing Query..");
                //_logHelper.AppendTrace(query);

                //char paramChar = getParamCharByDBType(dbtype);
                //var (paramNames, paramTypes, paramValues) = _queryParameterExtractor.ExtractParameters(query, (JObject)(paramObj ?? new JObject()), paramChar);

                //for (int i = 0; i < paramNames.Length; i++)
                //{
                //    _logHelper.AppendTrace($"{paramNames[i]} : {(i < paramValues.Length ? paramValues[i] : "")}");
                //}


                //var data = await dbHelper.ExecuteSQLAsync(query, paramNames, paramTypes, paramValues);
                //if (data.data.Rows.Count > 0)
                //{
                //    string caption = data.data.Rows[0]["caption"].ToString();
                //    return caption;
                //}
                //else
                //{
                //    _logHelper.AppendTrace("error while getting query data : Data not found.");
                //    return "";
                //}
                return "";

            }
            catch (Exception ex)
            {
                //_logHelper.AppendTrace("exception while getting query : " + ex.Message);
                return "";
            }

        }

        public async Task<SQLResult> ExecuteQueryNew(string projName, string userName, string gloStructFilter, QueryName queryName, QueryCategory queryCategory, JObject paramObj = null)
        {
            try
            {
                //var queryManager = (IQueryManager)ServiceProviderHelper.ServiceProvider.GetService(typeof(IQueryManager));
                //var (dbHelper, dbtype) = await DbHelper.CreateDbHelper(projName, userName, gloStructFilter, this);

                //string query = queryManager.GetQuery(dbtype, queryCategory, queryName);
                //_logHelper.AppendTrace("executing Query..");
                //_logHelper.AppendTrace(query);
                //char paramChar = getParamCharByDBType(dbtype);
                //var (paramNames, paramTypes, paramValues) = _queryParameterExtractor.ExtractParameters(query, (JObject)(paramObj ?? new JObject()), paramChar);
                //for (int i = 0; i < paramNames.Length; i++)
                //{
                //    _logHelper.AppendTrace($"{paramNames[i]} : {(i < paramValues.Length ? paramValues[i] : "")}");
                //}
                //var data = await dbHelper.ExecuteSQLAsync(query, paramNames, paramTypes, paramValues);
                //if (data.data.Rows.Count == 0)
                //{
                //    _logHelper.AppendTrace("error while getting query data : Data not found.");
                //}
                //return data;
                return new SQLResult();

            }
            catch (Exception ex)
            {
                //_logHelper.AppendTrace("Exception while executing query : " + ex.Message);
                return new SQLResult();
            }

        }

        public string StandardizeBoolean(string input)
        {
            string lowerCaseInput = input.ToLower();

            if (lowerCaseInput == "true" || lowerCaseInput == "t")
            {
                return "true";
            }
            else if (lowerCaseInput == "false" || lowerCaseInput == "f")
            {
                return "false";
            }
            else
            {
                return input; // Return original value if not recognized as boolean
            }
        }
        public string ToCanonicalBoolean(string input)
        {
            string lowerCaseInput = input.ToLower();
            if (lowerCaseInput == "true" || lowerCaseInput == "t")
            {
                return "True";
            }
            else if (lowerCaseInput == "false" || lowerCaseInput == "f")
            {
                return "False";
            }
            else
            {
                return input; // Return original value if not recognized as boolean
            }
        }
        public List<string> GetSqlParameters(string sql)
        {
            // Define the pattern to match SQL parameters prefixed with ":"
            string pattern = @":(\w+)";

            // Match the pattern in the SQL string
            MatchCollection matches = Regex.Matches(sql, pattern);

            // Extract parameter names from matches
            List<string> parameters = new List<string>();
            foreach (Match match in matches)
            {
                parameters.Add(match.Groups[1].Value);
            }

            return parameters;
        }

        public async Task<bool> isDuplicateTstFldName(string projName, string structName, string StructType, string fldName)
        {
            try
            {

                string compHashKey = $"{projName}♠{GetStructKeyWord(StructType)}♠{structName}♠comps";
                var fieldList = HashScan(compHashKey, $"axfield♠{fldName}♠*").ToList();
                if (fieldList.Count > 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                return true;
            }

        }
        public void RuntimeHashSet(string hashKey, string field, object value)
        {
            _runtimeContext.Set($"{hashKey}♠{field}", value);
        }

        public T RuntimeHashGet<T>(string hashKey, string field)
        {
            return _runtimeContext.Get<T>($"{hashKey}♠{field}");
        }
        public string HashGet(string hashKey, string field)
        {
            return _runtimeContext.Get<string>($"{hashKey}♠{field}");
        }
        public string[] HashGet(string hashKey, string[] fields)
        {
            return fields
                .Select(field => HashGet(hashKey, field))
                .ToArray();
        }

        public IEnumerable<(string Field, object Value)> HashScan(string hashKey, string pattern)
        {
            string fullPattern = $"{hashKey}♠{pattern}";

            string regexPattern = "^" +
                Regex.Escape(fullPattern)
                     .Replace(@"\*", ".*")
                     .Replace(@"\?", ".") +
                "$";

            Regex regex = new Regex(regexPattern, RegexOptions.IgnoreCase);

            return _runtimeContext.Scan(string.Empty)
                .Where(x => regex.IsMatch(x.Key))
                .Select(x =>
                {
                    string field = x.Key.Substring(hashKey.Length + 1);
                    return (field, x.Value);
                });
        }

        public IEnumerable<HashEntry> HashScanNew(string hashKey, string pattern)
        {
            string fullPattern = $"{hashKey}♠{pattern}";

            string regexPattern = "^" +
                Regex.Escape(fullPattern)
                     .Replace(@"\*", ".*")
                     .Replace(@"\?", ".") +
                "$";

            Regex regex = new Regex(regexPattern, RegexOptions.IgnoreCase);

            return _runtimeContext.Scan(string.Empty)
                .Where(x => regex.IsMatch(x.Key))
                .Select(x =>
                {
                    string field = x.Key.Substring(hashKey.Length + 1);

                    return new HashEntry(
                        (RedisValue)field,
                        (RedisValue)(x.Value?.ToString() ?? string.Empty));
                });
        }
        public bool KeyExists(string hashKey)
        {
            return _runtimeContext
                .Scan($"{hashKey}♠")
                .Any();
        }
        public void HashSet(string hashKey, string field, object value)
        {
            _runtimeContext.Set(
                $"{hashKey}♠{field}",
                value?.ToString());
        }
        public void HashSetNew(string hashKey, HashEntry[] entries)
        {
            foreach (var entry in entries)
            {
                _runtimeContext.Set(
                    $"{hashKey}♠{entry.Name}",
                    entry.Value.HasValue ? (object)entry.Value.ToString() : null);
            }
        }
        public void HashDelete(string hashKey, string field)
        {
            _runtimeContext.Remove($"{hashKey}♠{field}");
        }

        public string getTstCompHashSubKey(string comp, string compName, string parent = "")
        {
            switch (comp)
            {
                case "dc":
                case "genmap":
                case "mdmap":
                case "fillgrid":
                case "script":
                case "btngrp":
                    return $"{GetCompKeyWord(comp)}♠{compName}";
                case "udbutton":
                    return $"{GetCompKeyWord(comp)}♠{compName}♠{parent}";
                case "field":
                default:
                    return $"{GetCompKeyWord(comp)}♠{compName}♠{parent}";
            }
        }

        public string CreateLog(string text, string sid, string ScriptsPath, string fname, string newfile)
        {
            string filepath = "";
            try
            {
                string strLog = "Log\\";
                //Create a Folder
                string path = ScriptsPath + strLog + sid + "\\";
                DirectoryInfo di = new DirectoryInfo(path);
                //' Determine whether the directory exists.
                if (!di.Exists)
                {
                    // create the directory.
                    di.Create();
                }

                filepath = path + fname + ".txt";

                StreamWriter sw = default(StreamWriter);
                if (File.Exists(filepath))
                {
                    if (newfile != "new")
                    {
                        //True for appending
                        sw = new StreamWriter(filepath, true);
                    }
                    else
                    {
                        sw = new StreamWriter(filepath, false);
                        sw.WriteLine("Start Time : " + DateTime.Now.ToString());
                        sw.Flush();
                        sw.Close();
                        sw = new StreamWriter(filepath, true);
                    }
                    sw.WriteLine(text);
                    sw.Flush();
                    sw.Close();
                }
                else
                {
                    //End If
                    sw = new StreamWriter(filepath, false);
                    sw.WriteLine("Start Time : " + DateTime.Now.ToString());
                    sw.Flush();
                    sw.Close();
                    //Pass the file path and the file name to the StreamWriter constructor.
                    sw = new StreamWriter(filepath, true);
                    //True for appending
                    sw.WriteLine(text);
                    //Close the file.
                    sw.Flush();
                    sw.Close();
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
                filepath = "";
            }
            return filepath;
        }

        public string CreateLog(string text, string sid, string ScriptsPath, string fname, string newfile, string trace)
        {
            string filepath = "";

            if (trace.ToLower() != "true")
            {
                return filepath;
            }

            try
            {
                string strLog = "Log\\";
                //Create a Folder
                string path = ScriptsPath + strLog + sid + "\\";
                DirectoryInfo di = new DirectoryInfo(path);
                //' Determine whether the directory exists.
                if (!di.Exists)
                {
                    // create the directory.
                    di.Create();
                }

                filepath = path + fname + ".txt";

                StreamWriter sw = default(StreamWriter);
                if (File.Exists(filepath))
                {
                    if (newfile != "new")
                    {
                        //True for appending
                        sw = new StreamWriter(filepath, true);
                    }
                    else
                    {
                        sw = new StreamWriter(filepath, false);
                        sw.WriteLine("Start Time : " + DateTime.Now.ToString());
                        sw.Flush();
                        sw.Close();
                        sw = new StreamWriter(filepath, true);
                    }
                    sw.WriteLine(text);
                    sw.Flush();
                    sw.Close();
                }
                else
                {
                    //End If
                    sw = new StreamWriter(filepath, false);
                    sw.WriteLine("Start Time : " + DateTime.Now.ToString());
                    sw.Flush();
                    sw.Close();
                    //Pass the file path and the file name to the StreamWriter constructor.
                    sw = new StreamWriter(filepath, true);
                    //True for appending
                    sw.WriteLine(text);
                    //Close the file.
                    sw.Flush();
                    sw.Close();
                }
            }
            catch (Exception ex)
            {
                filepath = "";
            }
            return filepath;
        }
    }
}

public class StructureRuntimeContext
{
    private readonly Dictionary<string, object> _data =
        new(StringComparer.OrdinalIgnoreCase);

    public void Set(string key, object value)
    {
        _data[key] = value;
    }

    public T Get<T>(string key)
    {
        if (_data.TryGetValue(key, out var value))
            return (T)value;

        return default;
    }

    public IEnumerable<KeyValuePair<string, object>> Scan(string prefix)
    {
        return _data.Where(x => x.Key.StartsWith(prefix,
                        StringComparison.OrdinalIgnoreCase));
    }

    // Add this method
    public bool Remove(string key)
    {
        return _data.Remove(key);
    }
}
