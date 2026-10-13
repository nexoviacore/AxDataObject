using ARMCommon.Helpers;
using ARMCommon.Helpers.RabbitMq;
using ARMCommon.Model;
using Newtonsoft.Json;
using System.Net;
using ARMCommon.Interface;
using Newtonsoft.Json.Linq;
using ARMCommon.Services;
using System.Text;
using System.Xml.Linq;
using System.Xml;
using Microsoft.VisualBasic;
using System.Collections;
using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Web;
using HtmlAgilityPack;
using ClosedXML.Excel;
using NPOI.XWPF.UserModel;
using NPOI.OpenXmlFormats.Wordprocessing;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using DocumentFormat.OpenXml.Spreadsheet;
using ARMExportWorkerService;

public class Worker : BackgroundService
{
    private static readonly HttpClient httpClient = new HttpClient();
    private readonly IConfiguration _config;
    private readonly IRabbitMQConsumer _consumer;
    private readonly Utils _common;

    public Worker(IConfiguration config, IRabbitMQConsumer consumer, Utils common)
    {
        _config = config;
        _consumer = consumer;
        _common = common;
    }

    //protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    //{
    //    string queueName = _config["AppConfig:QueueName"];
    //    try
    //    {
    //        async Task<string> OnConsuming(string message)
    //        {
    //            try
    //            {
    //                return await ExportDataToExcel(message);
    //            }
    //            catch (Exception ex)
    //            {
    //                string exeDir = AppDomain.CurrentDomain.BaseDirectory;
    //                exeDir = Path.Combine(exeDir, "error.log");
    //                File.AppendAllText(exeDir, $"{ex}\n\n");
    //                return "error";
    //            }
    //        }
    //        _consumer.DoConsumeWorkerService(queueName, OnConsuming);
    //    }
    //    catch (Exception ex)
    //    {
    //        string exeDir = AppDomain.CurrentDomain.BaseDirectory;
    //        exeDir = Path.Combine(exeDir, "error.log");
    //        File.AppendAllText(exeDir, $"{ex}\n\n" + Environment.NewLine);
    //    }

    //    while (!stoppingToken.IsCancellationRequested)
    //    {
    //        await Task.Delay(1000, stoppingToken);
    //    }
    //}
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        string queueName = _config["AppConfig:QueueName"];
        try
        {
            async Task<string> OnConsuming(string message)
            {
                try
                {
                    return await ExportDataToExcel(message);
                }
                catch (Exception ex)
                {
                    string _exeDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log");
                    Directory.CreateDirectory(_exeDir);
                    string _logFileName = "ExportDataToExcel-" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".log";
                    _exeDir = Path.Combine(_exeDir, _logFileName);
                    File.AppendAllText(_exeDir, $"{DateTime.Now} - {ex}\n\n");
                    return "error";
                }
            }
            //_consumer.DoConsumeWorkerServiceNew(queueName, OnConsuming, stoppingToken);
            _consumer.DoConsumeWorkerServiceStable(queueName, OnConsuming, stoppingToken);
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (Exception ex)
        {
            string _exeDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log");
            Directory.CreateDirectory(_exeDir);
            string _logFileName = "ExportDataToExcel-" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".log";
            _exeDir = Path.Combine(_exeDir, _logFileName);
            File.AppendAllText(_exeDir, $"{ex}\n\n" + Environment.NewLine);
        }
    }
    private async Task<string> ExportDataToExcel(string queueRequest)
    {
        string _exeDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log");
        Directory.CreateDirectory(_exeDir);
        string _logFileName = "ExportDataToExcel-" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".log";
        _exeDir = Path.Combine(_exeDir, _logFileName);

        JObject jsonObj = JObject.Parse(queueRequest);
        string DATA = jsonObj["queuedata"]?.ToString();
        JObject jsonObjExcelData = JObject.Parse(DATA);
        string fileServerPath = null;
        string exportExcelPath = string.Empty;
        string axConfigFilePath = string.Empty;
        string axConfigFileMapUser = string.Empty;
        string axConfigFileMapPwd = string.Empty;
        string signalrUrl = string.Empty;
        string _excelFilePath = string.Empty;
        string _smartViewSettings = string.Empty;
        if (jsonObjExcelData["getreport"] != null)
        {
            string _project = string.Empty;
            Utils _common = new Utils();
            try
            {
                //_exeDir = Path.Combine(_exeDir, logFileName);
                //File.AppendAllText(_exeDir, "Worker Service Start time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + Environment.NewLine);
                string _log = "Worker Service Start time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + Environment.NewLine;

                string _trace = jsonObjExcelData["getreport"]["trace"].ToString();
                bool trace = _trace == "true" ? true : false;
                _project = jsonObjExcelData["getreport"]["project"].ToString();

                var _appSettingsService = new AppSettingsService();
                var _appSettingsHelper = new AppSettingsHelper(_appSettingsService);
                _common._appName = _project;
                _common._appSettingsHelper = _appSettingsHelper;
                Dictionary<string, string> configcon = _appSettingsHelper.GetDBConfigurations(_project);
                string connectionString = configcon["ConnectionString"];
                string dbType = configcon["DBType"];

                var armSettings = _appSettingsHelper.GetARMSettings(_project);
                string ScriptsPath = armSettings.ScriptsPath;
                if (jsonObjExcelData["getreport"]["s"] != null)
                    ScriptsPath += "log\\" + jsonObjExcelData["getreport"]["s"].ToString() + "\\";
                else
                    ScriptsPath = Path.Combine(ScriptsPath, "log", "ExportWorkerService");
                if (!Directory.Exists(ScriptsPath))
                    Directory.CreateDirectory(ScriptsPath);
                if (ScriptsPath != "")
                {
                    string logFileName = "ExportDataToExcel-" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".txt";
                    _exeDir = Path.Combine(ScriptsPath, logFileName);
                    if (trace)
                        File.AppendAllText(_exeDir, _log);
                }

                signalrUrl = jsonObjExcelData["getreport"]["arm_url"].ToString() + "/ARM_APIs/api/v1/SendSignalR";
                axConfigFilePath = jsonObjExcelData["getreport"]["axConfigFilePath"].ToString();
                if (axConfigFilePath != string.Empty)
                {
                    exportExcelPath = axConfigFilePath;
                    fileServerPath = string.Empty;

                    axConfigFileMapUser = jsonObjExcelData["getreport"]["axConfigFileMapUser"].ToString();
                    axConfigFileMapPwd = jsonObjExcelData["getreport"]["axConfigFileMapPwd"].ToString();
                }
                else
                {
                    exportExcelPath = _config["AppConfig:ExportPath"];
                    fileServerPath = jsonObjExcelData["getreport"]["ARMScriptURL"].ToString();
                }
                string _ParamsCaption = jsonObjExcelData["getreport"]["ivParamCaption"].ToString();
                jsonObjExcelData["getreport"]["ivParamCaption"] = "";
                string _params = jsonObjExcelData["getreport"]["params"].ToString();
                string userName = jsonObjExcelData["getreport"]["userauthkey"].ToString();
                string _ivName = jsonObjExcelData["getreport"]["name"].ToString();
                string _ivcaption = jsonObjExcelData["getreport"]["ivcaption"].ToString();
                string dateformat = jsonObjExcelData["getreport"]["_dateformat"].ToString();
                string exportType = jsonObjExcelData["getreport"]["exportType"].ToString();
                _smartViewSettings = jsonObjExcelData["getreport"]["smartViewSettings"].ToString();

                AxpertRestAPIToken axpertRestAPIToken = new AxpertRestAPIToken(userName);
                UpdateOrAddJsonKey(ref jsonObjExcelData, "userauthkey", axpertRestAPIToken.userAuthKey);
                UpdateOrAddJsonKey(ref jsonObjExcelData, "seed", axpertRestAPIToken.seed);
                UpdateOrAddJsonKey(ref jsonObjExcelData, "token", axpertRestAPIToken.token);
                string _RKey = string.Empty;

                Dictionary<string, string> connectionStringParts = new Dictionary<string, string>();
                string[] parts = connectionString.Split(';');
                foreach (var part in parts)
                {
                    if (!string.IsNullOrEmpty(part))
                    {
                        string[] keyValue = part.Split('=');
                        if (keyValue.Length == 2)
                        {
                            connectionStringParts[keyValue[0].Trim()] = keyValue[1].Trim();
                        }
                    }
                }
                string _paramsnode = string.Empty;
                if (_params != "{}")
                {
                    JObject json = JObject.Parse(_params);
                    foreach (var property in json.Properties())
                    {
                        if (_paramsnode.Length > 0)
                        {
                            _paramsnode += "~";
                        }
                        _paramsnode += $"{property.Name}♠{property.Value}";
                    }
                }
                if (dbType.ToLower() == "postgresql" || dbType.ToLower() == "postgre")
                {
                    if (configcon.ContainsKey("Driver") && configcon["Driver"] == "ado")
                    {
                        string _DBUser = configcon["DBUser"];
                        if (_paramsnode == string.Empty)
                            _RKey = _DBUser + "-" + _ivName + "-ivarmexportexcel-" + userName;
                        else
                            _RKey = _DBUser + "-" + _ivName + "-ivarmexportexcel-" + userName + "-" + _paramsnode;
                    }
                    else
                    {
                        string database = connectionStringParts.ContainsKey("Database") ? connectionStringParts["Database"] : "Not found";
                        string dbusername = connectionStringParts.ContainsKey("Username") ? connectionStringParts["Username"] : (connectionStringParts.ContainsKey("Uid") ? connectionStringParts["Uid"] : "Not found");
                        if (_paramsnode == string.Empty)
                            _RKey = dbusername + "~" + database + "-" + _ivName + "-ivarmexportexcel-" + userName;
                        else
                            _RKey = dbusername + "~" + database + "-" + _ivName + "-ivarmexportexcel-" + userName + "-" + _paramsnode;
                    }
                }
                else if (dbType.ToLower() == "oracle" || dbType.ToLower() == "ms sql" || dbType.ToLower() == "mssql")
                {
                    string database = connectionStringParts.ContainsKey("User Id") ? connectionStringParts["User Id"] : "Not found";
                    if (_paramsnode == string.Empty)
                        _RKey = database + "-" + _ivName + "-ivarmexportexcel-" + userName;
                    else
                        _RKey = database + "-" + _ivName + "-ivarmexportexcel-" + userName + "-" + _paramsnode;
                }
                else
                {
                    string database = connectionStringParts.ContainsKey("Database") ? connectionStringParts["Database"] : "Not found";
                    if (_paramsnode == string.Empty)
                        _RKey = database + "-" + _ivName + "-ivarmexportexcel-" + userName;
                    else
                        _RKey = database + "-" + _ivName + "-ivarmexportexcel-" + userName + "-" + _paramsnode;
                }

                var redisConnected = await _common.OpenRedisConnectionAsync();
                if (redisConnected)
                {
                    await _common._redisCache.StringSetAsync(_RKey, "requestinprocess", int.Parse(_config["AppConfig:ExportKeyExpiryTime"]));
                }
                else
                {
                    if (trace)
                    {
                        //string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                        //exeDir = Path.Combine(exeDir, "ExportDataToExcel.log");
                        File.AppendAllText(_exeDir, "Redis connection is not proper." + Environment.NewLine);
                    }
                }

                DATA = jsonObjExcelData.ToString();
                string _urlarm = jsonObjExcelData["getreport"]["ARMScriptURL"].ToString();
                string URL = string.Empty;
                if (_urlarm.EndsWith("/"))
                    URL = _urlarm + "ASBIviewRest.dll/datasnap/rest/TASBIViewREST/GetReport";
                else
                    URL = _urlarm + "/ASBIviewRest.dll/datasnap/rest/TASBIViewREST/GetReport";

                if (trace)
                {
                    //string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                    //exeDir = Path.Combine(exeDir, "ExportDataToExcel.log");
                    File.AppendAllText(_exeDir, "API URL:" + URL + Environment.NewLine);
                    File.AppendAllText(_exeDir, "Request Json:" + DATA + Environment.NewLine);
                }

                //_exeDir = AppDomain.CurrentDomain.BaseDirectory;
                //_exeDir = Path.Combine(_exeDir, "ExportDataToExcel.log");
                File.AppendAllText(_exeDir, "Before Rest Service: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + Environment.NewLine);

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(URL);
                request.Method = "POST";
                request.ContentType = "application/json";
                StreamWriter requestWriter = new StreamWriter(request.GetRequestStream(), System.Text.Encoding.ASCII);
                requestWriter.Write(DATA);
                requestWriter.Close();

                string response = string.Empty;
                try
                {
                    request.Timeout = Timeout.Infinite;
                    request.ReadWriteTimeout = Timeout.Infinite;
                    request.KeepAlive = true;
                    request.AllowReadStreamBuffering = false;

                    WebResponse webResponse = request.GetResponse();
                    Stream webStream = webResponse.GetResponseStream();
                    StreamReader responseReader = new StreamReader(webStream);
                    response = responseReader.ReadToEnd();
                    //Console.Out.WriteLine(response);
                    if (trace)
                    {
                        //string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                        //exeDir = Path.Combine(exeDir, "ExportDataToExcel.log");
                        File.AppendAllText(_exeDir, "Rest response: " + response + Environment.NewLine);
                    }
                    responseReader.Close();

                    //_exeDir = AppDomain.CurrentDomain.BaseDirectory;
                    //_exeDir = Path.Combine(_exeDir, "ExportDataToExcel.log");
                    File.AppendAllText(_exeDir, "After Rest Service: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + Environment.NewLine);

                }
                catch (Exception e)
                {
                    if (redisConnected)
                    {
                        await _common._redisCache.KeyDeleteAsync(_RKey);
                    }
                    //if (trace)
                    //{
                    //string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                    //exeDir = Path.Combine(exeDir, "ExportDataToExcel.log");
                    //File.AppendAllText(_exeDir, "Web service Error:" + e.Message + e.StackTrace + Environment.NewLine + " Exception at:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + Environment.NewLine);
                    var details = new StringBuilder();
                    details.AppendLine("Web service Error:");
                    details.AppendLine(e.Message);
                    details.AppendLine("---- STACK TRACE ----");
                    details.AppendLine(e.StackTrace);
                    if (e.InnerException != null)
                    {
                        details.AppendLine("---- INNER EXCEPTION ----");
                        details.AppendLine(e.InnerException.Message);
                        details.AppendLine(e.InnerException.StackTrace);
                    }
                    details.AppendLine("Exception at: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                    File.AppendAllText(_exeDir, details.ToString() + Environment.NewLine);
                    //}
                }

                if (response != string.Empty)
                {
                    try
                    {
                        if (response.StartsWith("{\"result\":[{"))
                        {
                            response = response.Substring("{\"result\":[".Length);
                            response = response.Substring(0, response.Length - 2);
                        }

                        JObject jsonObject = JObject.Parse(response);
                        string status = (string)jsonObject["status"];
                        if (status == "failed")
                        {
                            if (redisConnected)
                            {
                                await _common._redisCache.KeyDeleteAsync(_RKey);
                            }
                            if (trace)
                            {
                                //string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                                //exeDir = Path.Combine(exeDir, "ExportDataToExcel.log");
                                File.AppendAllText(_exeDir, "Web service res:" + response + Environment.NewLine);
                            }
                            return "";
                        }
                        JObject result = (JObject)jsonObject["result"];
                        XDocument resultXml = JsonConvert.DeserializeXNode(result.ToString(), "data");
                        XElement rootElement = resultXml.Root;

                        XDocument xmlDocument = new XDocument();
                        XElement root = new XElement("result");
                        root.Add(rootElement);

                        xmlDocument.Add(root);

                        string ires = xmlDocument.ToString();
                        ires = ires.Replace("<result>\r\n  ", "").Replace("</result>", "");

                        XmlDocument xmlDoc = new XmlDocument();
                        xmlDoc.LoadXml(ires);
                        string datarows = "";
                        string totalrows = "";
                        string reccount = "";
                        XmlNodeList nodesToRemove = xmlDoc.SelectNodes("//reccount | //totalrows | //datarows");
                        foreach (XmlNode node in nodesToRemove)
                        {
                            if (node.Name == "datarows")
                                datarows = node.InnerText;
                            if (node.Name == "totalrows")
                                totalrows = node.InnerText;
                            if (node.Name == "reccount")
                                reccount = node.InnerText;
                            node.ParentNode.RemoveChild(node);
                        }

                        XmlNode metadataNode = xmlDoc.SelectSingleNode("//metadata");
                        if (metadataNode != null)
                        {
                            XmlElement newElement = xmlDoc.CreateElement("headrow");
                            foreach (XmlAttribute attribute in metadataNode.Attributes)
                            {
                                newElement.Attributes.Append((XmlAttribute)attribute.CloneNode(true));
                            }
                            foreach (XmlNode childNode in metadataNode.ChildNodes)
                            {
                                newElement.AppendChild(childNode.CloneNode(true));
                            }
                            metadataNode.ParentNode.ReplaceChild(newElement, metadataNode);
                        }
                        ires = xmlDoc.OuterXml;
                        XDocument doc = XDocument.Parse(@ires);
                        var elementsToRemove = doc.Descendants().Where(e => string.Equals(e.Name.LocalName, "axrowtype", StringComparison.OrdinalIgnoreCase) || string.Equals(e.Name.LocalName, "axp__font", StringComparison.OrdinalIgnoreCase) || string.Equals(e.Name.LocalName, "axp__color", StringComparison.OrdinalIgnoreCase) || string.Equals(e.Name.LocalName, "axrowoptions", StringComparison.OrdinalIgnoreCase)).ToList();
                        foreach (var el in elementsToRemove)
                            el.Remove();
                        ires = doc.ToString();

                        //_exeDir = AppDomain.CurrentDomain.BaseDirectory;
                        //_exeDir = Path.Combine(_exeDir, "ExportDataToExcel.log");
                        File.AppendAllText(_exeDir, "Before Export Excel: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + Environment.NewLine);

                        //_excelFilePath = GetExcel(ires, datarows, totalrows, reccount, exportExcelPath, _ivName, _ParamsCaption, userName, dateformat, _ivcaption);
                        if (exportType.ToLower() == "pdf")
                            _excelFilePath = PdfExportService.GetPDFFast(ires, exportExcelPath, _ivName, _ParamsCaption, userName, dateformat, _ivcaption, _exeDir, axConfigFileMapUser, axConfigFileMapPwd, _smartViewSettings);
                        else if (exportType.ToLower() == "word")
                            _excelFilePath = WordExportService.GetWordFast(ires, exportExcelPath, _ivName, _ParamsCaption, userName, dateformat, _ivcaption, _exeDir, axConfigFileMapUser, axConfigFileMapPwd, _smartViewSettings);
                        else
                            _excelFilePath = GetExcelFast(ires, exportExcelPath, _ivName, _ParamsCaption, userName, dateformat, _ivcaption, _exeDir, axConfigFileMapUser, axConfigFileMapPwd, _smartViewSettings);

                        //_exeDir = AppDomain.CurrentDomain.BaseDirectory;
                        //_exeDir = Path.Combine(_exeDir, "ExportDataToExcel.log");
                        File.AppendAllText(_exeDir, "After Export Excel: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " Exported file path:" + _excelFilePath + Environment.NewLine);
                    }
                    catch (Exception ex)
                    {
                        //if (trace)
                        //{
                        //string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                        //exeDir = Path.Combine(exeDir, "ExportDataToExcel.log");
                        File.AppendAllText(_exeDir, "Error while processing response: " + ex.Message + ex.StackTrace + Environment.NewLine + " Exception at:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + Environment.NewLine);
                        //}
                        return "";
                    }
                }
                if (redisConnected)
                {
                    await _common._redisCache.KeyDeleteAsync(_RKey);
                }
                if (_excelFilePath != "" && _excelFilePath != "IView Result Error")
                {
                    string[] _fielPaths = _excelFilePath.Split(',');
                    try
                    {
                        string sql = Constants_SQL.INSERT_TO_AXACTIVEMESG;
                        string _filefullpath = _fielPaths[0].Replace(@"\", "\\\\");
                        if (fileServerPath != string.Empty)
                        {
                            _filefullpath = fileServerPath + "/" + "Exports/" + userName + "/" + _fielPaths[1];
                        }
                        string currentTime = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                        if (exportType.ToLower() == "pdf")
                            sql = string.Format(sql, currentTime, "export pdf", userName, userName, "export", "export pdf", "export pdf-" + _ivName, _ivName, "export pdf generated successfully", _ivcaption, _filefullpath);
                        else if (exportType.ToLower() == "word")
                            sql = string.Format(sql, currentTime, "export word", userName, userName, "export", "export word", "export word-" + _ivName, _ivName, "export word generated successfully", _ivcaption, _filefullpath);
                        else
                            sql = string.Format(sql, currentTime, "export excel1", userName, userName, "export", "export excel2", "export excel-" + _ivName, _ivName, "export excel generated successfully", _ivcaption, _filefullpath);
                        if (trace)
                        {
                            //string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                            //exeDir = Path.Combine(exeDir, "ExportDataToExcel.log");
                            File.AppendAllText(_exeDir, "Inser Query:" + sql + Environment.NewLine);
                        }
                        IDbHelper dbHelper = DBHelper.CreateDbHelper(dbType);
                        var result = await dbHelper.ExecuteQueryAsync(sql, connectionString);
                        if (trace)
                        {
                            //string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                            //exeDir = Path.Combine(exeDir, "ExportDataToExcel.log");
                            File.AppendAllText(_exeDir, "Connection String:" + connectionString + Environment.NewLine);
                            File.AppendAllText(_exeDir, "DBType:" + dbType + Environment.NewLine);
                        }
                        string _Payload = "";
                        if (exportType.ToLower() == "pdf")
                            _Payload = "[{\"notifytype\":\"export pdf\",\"type\":\"export\",\"icon\":\"\",\"title\":\"Export PDF\",\"message\":\"" + _ivcaption + "\",\"dt\":\"" + DateTime.Now.ToString() + "\",\"link\":{\"t\":\"e\",\"name\":\"" + _filefullpath + "\",\"p\":\"\",\"act\":\"\",\"axmsgid\":\"\"}}]";
                        else if (exportType.ToLower() == "word")
                            _Payload = "[{\"notifytype\":\"export word\",\"type\":\"export\",\"icon\":\"\",\"title\":\"Export Word\",\"message\":\"" + _ivcaption + "\",\"dt\":\"" + DateTime.Now.ToString() + "\",\"link\":{\"t\":\"e\",\"name\":\"" + _filefullpath + "\",\"p\":\"\",\"act\":\"\",\"axmsgid\":\"\"}}]";
                        else
                            _Payload = "[{\"notifytype\":\"export excel\",\"type\":\"export\",\"icon\":\"\",\"title\":\"Export Excel\",\"message\":\"" + _ivcaption + "\",\"dt\":\"" + DateTime.Now.ToString() + "\",\"link\":{\"t\":\"e\",\"name\":\"" + _filefullpath + "\",\"p\":\"\",\"act\":\"\",\"axmsgid\":\"\"}}]";

                        await SendSignalRMessage(signalrUrl, _project, userName, _Payload);
                        if (trace)
                        {
                            //string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                            //exeDir = Path.Combine(exeDir, "ExportDataToExcel.log");
                            File.AppendAllText(_exeDir, "After notification:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + Environment.NewLine);
                        }
                        return "Success";
                    }
                    catch (Exception ex)
                    {
                        if (redisConnected)
                        {
                            await _common._redisCache.KeyDeleteAsync(_RKey);
                        }
                        //if (trace)
                        //{
                        //string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                        //exeDir = Path.Combine(exeDir, "ExportDataToExcel.log");
                        File.AppendAllText(_exeDir, "Error: SignalR:" + ex.Message + ex.StackTrace + Environment.NewLine + " Exception at:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + Environment.NewLine);
                        //}
                        return null;
                    }
                }
            }
            catch (Exception e)
            {
                //string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                //exeDir = Path.Combine(exeDir, "ExportDataToExcel.log");
                File.AppendAllText(_exeDir, "Error: outter catch:" + e.Message + Environment.NewLine + " Exception at:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + Environment.NewLine);
            }
            finally
            {
                await _common.CloseRedisConnectionAsync();
            }

            return "Success";
        }
        else if (jsonObjExcelData["Type"]?.ToString()?.Equals("list", StringComparison.OrdinalIgnoreCase) == true)
        {
            string outputPath;
            string page = jsonObjExcelData["Page"]?.ToString();
            string armSessionId = jsonObjExcelData["ARMSessionId"]?.ToString();
            string axSessionId = jsonObjExcelData["AxSessionId"]?.ToString();
            string trace = jsonObjExcelData["Trace"]?.ToString();
            bool _trace = trace == "true" ? true : false;
            string inputFileName = jsonObjExcelData["Output"]?.ToString();
            int pageNo = (int?)jsonObjExcelData["PageNo"] ?? 0;
            int pageSize = (int?)jsonObjExcelData["PageSize"] ?? 0;
            string project = jsonObjExcelData["AppName"]?.ToString();
            string roles = jsonObjExcelData["Roles"]?.ToString();
            string username = jsonObjExcelData["UserName"]?.ToString();
            string transid = jsonObjExcelData["TransId"]?.ToString();
            JArray propertiesList = jsonObjExcelData["PropertiesList"] as JArray;
            string token = jsonObjExcelData["Token"]?.ToString();
            string armurl = jsonObjExcelData["ARMUrl"]?.ToString();
            string armwebscript = jsonObjExcelData["ARMScriptURL"]?.ToString();
            string apiurl = jsonObjExcelData["APIURL"]?.ToString();
            string dateformat = jsonObjExcelData["DateFormat"]?.ToString();
            string configfilepath = jsonObjExcelData["AxConfigFilePath"]?.ToString();
            string pagename = Path.GetFileNameWithoutExtension(inputFileName);
            signalrUrl = armurl + "/ARM_APIs/api/v1/SendSignalR";

            if (!string.IsNullOrEmpty(configfilepath))
            {
                exportExcelPath = configfilepath;
                fileServerPath = string.Empty;
            }
            else
            {
                exportExcelPath = _config["AppConfig:ExportPath"];
                fileServerPath = armwebscript;
                if (!Directory.Exists(exportExcelPath))
                {
                    Directory.CreateDirectory(exportExcelPath);
                }
            }

            outputPath = Path.Combine(exportExcelPath, "Exports", username, inputFileName);
            outputPath = outputPath.Trim();
            outputPath = Path.GetFullPath(outputPath);
            outputPath = outputPath.Replace(@"\\", @"\");

            string outputDirectory = Path.GetDirectoryName(outputPath);


            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }



            JObject apiResponse = await GetAPIResult(apiurl, page, armSessionId, axSessionId, project, roles, username, transid, token, pageNo, pageSize, trace);

            if (apiResponse != null && apiResponse["result"] != null)
            {
                var data = apiResponse["result"]["data"];
                if (data != null)
                {
                    var metaDataArray = data["MetaData"] as JArray;
                    var listDataArray = data["ListData"] as JArray;

                    if (metaDataArray != null && listDataArray != null)
                    {
                        var fieldDictionary = metaDataArray.ToDictionary(item => (string)item["fldname"], item => (string)item["fldcap"]);
                        var dataTypeDictionary = metaDataArray.ToDictionary(item => (string)item["fldname"], item => (string)item["fdatatype"]);

                        var skipFlds = new List<string> { "transid", "recordid", "axpeg_processname", "axpeg_keyvalue", "axpeg_status", "axpeg_statustext", "pageno", "rno" };

                        if (inputFileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                        {
                            // Generate PDF
                            string GetPDFPath = await GetPDF(pagename, dateformat, outputPath, project, metaDataArray, listDataArray, skipFlds, fieldDictionary, dataTypeDictionary);


                            //var redis = new RedisHelper(configuration);
                            //var context = new ARMCommon.Helpers.DataContext(configuration);
                            //Utils utils = new Utils(configuration, context, redis);
                            var _appSettingsService = new AppSettingsService();
                            var _appSettingsHelper = new AppSettingsHelper(_appSettingsService);
                            Dictionary<string, string> configcon = _appSettingsHelper.GetDBConfigurations(project); //await utils.GetDBConfigurations(project);
                            string connectionString = configcon["ConnectionString"];
                            string dbType = configcon["DBType"];

                            if (!string.IsNullOrEmpty(GetPDFPath))
                            {
                                string[] _filePaths = GetPDFPath.Split(',');

                                try
                                {
                                    string _filefullpath = string.Empty;
                                    //string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                                    //string inputFileNameWithTimestamp = $"{Path.GetFileNameWithoutExtension(inputFileName)}_{timestamp}{Path.GetExtension(inputFileName)}";

                                    // Ensure array has at least one element
                                    if (_filePaths.Length > 0)
                                    {
                                        _filefullpath = _filePaths[0].Replace(@"\", "\\\\");

                                        if (fileServerPath != string.Empty)
                                        {
                                            //_filefullpath = fileServerPath + "/" + "Exports/" + username + "/" + inputFileNameWithTimestamp;
                                            string _fileext = inputFileName.Split('.').Last();
                                            string _fileNamepdf = Path.GetFileNameWithoutExtension(_filePaths[0]);
                                            _filefullpath = fileServerPath + "/" + "Exports/" + username + "/" + _fileNamepdf + "." + _fileext;
                                        }

                                    }
                                    else
                                    {
                                        //Console.WriteLine("Error: _filePaths array is empty.");
                                        if (_trace)
                                        {
                                            string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                                            exeDir = Path.Combine(exeDir, "ExportDataToExcel.log");
                                            File.AppendAllText(exeDir, "Error: _filePaths array is empty." + Environment.NewLine);
                                        }
                                    }

                                    string currentTime = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                                    string sql = string.Format(
                                        Constants_SQL.INSERT_TO_AXACTIVEMESG,
                                        currentTime,
                                        "export pdf1",
                                        username,
                                        username,
                                        "export",
                                        "export pdf2",
                                        "export pdf-" + transid,
                                        transid,
                                        "export pdf generated successfully",
                                        pagename,
                                        _filefullpath
                                    );
                                    if (_trace)
                                    {
                                        string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                                        exeDir = Path.Combine(exeDir, "ExportDataToExcel.log");
                                        File.AppendAllText(exeDir, "Insert Query: " + sql + Environment.NewLine);
                                    }

                                    IDbHelper dbHelper = DBHelper.CreateDbHelper(dbType);
                                    var result = await dbHelper.ExecuteQueryAsync(sql, connectionString);

                                    string payload = "[{\"notifytype\":\"export pdf\",\"type\":\"export\",\"icon\":\"\",\"title\":\"Export PDF\",\"message\":\""
                                        + pagename + "\",\"dt\":\""
                                        + DateTime.Now.ToString()
                                        + "\",\"link\":{\"t\":\"e\",\"name\":\""
                                        + _filefullpath
                                        + "\",\"p\":\"\",\"act\":\"\",\"axmsgid\":\"\"}}]";

                                    await SendSignalRMessage(signalrUrl, project, username, payload);
                                    //Console.WriteLine("After notification: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                                    if (_trace)
                                    {
                                        string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                                        exeDir = Path.Combine(exeDir, "ExportDataToExcel.log");
                                        File.AppendAllText(exeDir, "After notification: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + Environment.NewLine);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    //Console.WriteLine("Error: " + ex.Message + " " + ex.StackTrace);
                                    if (_trace)
                                    {
                                        string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                                        exeDir = Path.Combine(exeDir, "ExportDataToExcel.log");
                                        File.AppendAllText(exeDir, "Error: " + ex.Message + " " + ex.StackTrace + Environment.NewLine);
                                    }
                                }

                            }
                        }
                        else if (inputFileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
                        {
                            string GetWordpath = await GetWord(pagename, dateformat, outputPath, project, metaDataArray, listDataArray, skipFlds, fieldDictionary, dataTypeDictionary);

                            //var redis = new RedisHelper(configuration);
                            //var context = new ARMCommon.Helpers.DataContext(configuration);
                            //Utils utils = new Utils(configuration, context, redis);
                            var _appSettingsService = new AppSettingsService();
                            var _appSettingsHelper = new AppSettingsHelper(_appSettingsService);
                            Dictionary<string, string> configcon = _appSettingsHelper.GetDBConfigurations(project);// await utils.GetDBConfigurations(project);
                            string connectionString = configcon["ConnectionString"];
                            string dbType = configcon["DBType"];

                            if (!string.IsNullOrEmpty(GetWordpath))
                            {
                                string[] _filePaths = GetWordpath.Split(',');

                                try
                                {
                                    string _filefullpath = string.Empty;
                                    //string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                                    //string inputFileNameWithTimestamp = $"{Path.GetFileNameWithoutExtension(inputFileName)}_{timestamp}{Path.GetExtension(inputFileName)}";
                                    if (_filePaths.Length > 0)
                                    {
                                        _filefullpath = _filePaths[0].Replace(@"\", "\\\\");

                                        if (fileServerPath != string.Empty)
                                        {
                                            //_filefullpath = fileServerPath + "/" + "Exports/" + username + "/" + inputFileNameWithTimestamp;
                                            string _fileext = inputFileName.Split('.').Last();
                                            string _fileNamedocx = Path.GetFileNameWithoutExtension(_filePaths[0]);
                                            _filefullpath = fileServerPath + "/" + "Exports/" + username + "/" + _fileNamedocx + "." + _fileext;
                                        }

                                    }
                                    else
                                    {
                                        //Console.WriteLine("Error: _filePaths array is empty.");
                                        if (_trace)
                                        {
                                            string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                                            exeDir = Path.Combine(exeDir, "ExportDataToExcel.log");
                                            File.AppendAllText(exeDir, "Error: _filePaths array is empty." + Environment.NewLine);
                                        }
                                    }

                                    string currentTime = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                                    string sql = string.Format(
                                        Constants_SQL.INSERT_TO_AXACTIVEMESG,
                                        currentTime,
                                        "export word1",
                                        username,
                                        username,
                                        "export",
                                        "export word2",
                                        "export word-" + transid,
                                        transid,
                                        "export word generated successfully",
                                        pagename,
                                        _filefullpath
                                    );
                                    if (_trace)
                                    {
                                        string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                                        exeDir = Path.Combine(exeDir, "ExportDataToExcel.log");
                                        File.AppendAllText(exeDir, "Insert Query: " + sql + Environment.NewLine);
                                    }

                                    IDbHelper dbHelper = DBHelper.CreateDbHelper(dbType);
                                    var result = await dbHelper.ExecuteQueryAsync(sql, connectionString);

                                    string payload = "[{\"notifytype\":\"export word\",\"type\":\"export\",\"icon\":\"\",\"title\":\"Export Word\",\"message\":\""
                                        + pagename + "\",\"dt\":\""
                                        + DateTime.Now.ToString()
                                        + "\",\"link\":{\"t\":\"e\",\"name\":\""
                                        + _filefullpath
                                        + "\",\"p\":\"\",\"act\":\"\",\"axmsgid\":\"\"}}]";

                                    await SendSignalRMessage(signalrUrl, project, username, payload);
                                    //Console.WriteLine("After notification: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                                    if (_trace)
                                    {
                                        string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                                        exeDir = Path.Combine(exeDir, "ExportDataToExcel.log");
                                        File.AppendAllText(exeDir, "After notification: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + Environment.NewLine);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    //Console.WriteLine("Error: " + ex.Message + " " + ex.StackTrace);
                                    if (_trace)
                                    {
                                        string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                                        exeDir = Path.Combine(exeDir, "ExportDataToExcel.log");
                                        File.AppendAllText(exeDir, "Error: " + ex.Message + " " + ex.StackTrace + Environment.NewLine);
                                    }
                                }

                            }
                        }
                        else
                        {
                            string excelPath = await GetExcelTstruct(pagename, dateformat, outputPath, project, metaDataArray, listDataArray, skipFlds, fieldDictionary, dataTypeDictionary, fileServerPath, transid);
                            //var redis = new RedisHelper(configuration);
                            //var context = new ARMCommon.Helpers.DataContext(configuration);
                            //Utils utils = new Utils(configuration, context, redis);
                            var _appSettingsService = new AppSettingsService();
                            var _appSettingsHelper = new AppSettingsHelper(_appSettingsService);
                            Dictionary<string, string> configcon = _appSettingsHelper.GetDBConfigurations(project);// await utils.GetDBConfigurations(project);
                            string connectionString = configcon["ConnectionString"];
                            string dbType = configcon["DBType"];

                            if (!string.IsNullOrEmpty(excelPath) && excelPath != "IView Result Error")
                            {
                                string[] _filePaths = excelPath.Split(',');

                                try
                                {
                                    string _filefullpath = string.Empty;

                                    if (_filePaths.Length > 0)
                                    {
                                        _filefullpath = _filePaths[0].Replace(@"\", "\\\\");
                                        //string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                                        //string inputFileNameWithTimestamp = $"{Path.GetFileNameWithoutExtension(inputFileName)}_{timestamp}{Path.GetExtension(inputFileName)}";

                                        if (fileServerPath != string.Empty)
                                        {
                                            string _fileext = inputFileName.Split('.').Last();
                                            string _fileNameexcel = Path.GetFileNameWithoutExtension(_filePaths[0]);
                                            _filefullpath = fileServerPath + "/" + "Exports/" + username + "/" + _fileNameexcel + "." + _fileext;
                                        }

                                    }
                                    else
                                    {
                                        //Console.WriteLine("Error: _filePaths array is empty.");
                                        if (_trace)
                                        {
                                            string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                                            exeDir = Path.Combine(exeDir, "ExportDataToExcel.log");
                                            File.AppendAllText(exeDir, "Error: _filePaths array is empty." + Environment.NewLine);
                                        }
                                    }

                                    string currentTime = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                                    string sql = string.Format(
                                        Constants_SQL.INSERT_TO_AXACTIVEMESG,
                                        currentTime,
                                        "export excel1",
                                        username,
                                        username,
                                        "export",
                                        "export excel2",
                                        "export excel-" + transid,
                                        transid,
                                        "export excel generated successfully",
                                        pagename,
                                        _filefullpath
                                    );
                                    if (_trace)
                                    {
                                        string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                                        exeDir = Path.Combine(exeDir, "ExportDataToExcel.log");
                                        File.AppendAllText(exeDir, "Insert Query: " + sql + Environment.NewLine);
                                    }

                                    IDbHelper dbHelper = DBHelper.CreateDbHelper(dbType);
                                    var result = await dbHelper.ExecuteQueryAsync(sql, connectionString);

                                    string payload = "[{\"notifytype\":\"export excel\",\"type\":\"export\",\"icon\":\"\",\"title\":\"Export Excel\",\"message\":\""
                                        + pagename + "\",\"dt\":\""
                                        + DateTime.Now.ToString()
                                        + "\",\"link\":{\"t\":\"e\",\"name\":\""
                                        + _filefullpath
                                        + "\",\"p\":\"\",\"act\":\"\",\"axmsgid\":\"\"}}]";

                                    await SendSignalRMessage(signalrUrl, project, username, payload);
                                    //Console.WriteLine("After notification: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                                    if (_trace)
                                    {
                                        string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                                        exeDir = Path.Combine(exeDir, "ExportDataToExcel.log");
                                        File.AppendAllText(exeDir, "After notification: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + Environment.NewLine);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    //Console.WriteLine("Error: " + ex.Message + " " + ex.StackTrace);
                                    if (_trace)
                                    {
                                        string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                                        exeDir = Path.Combine(exeDir, "ExportDataToExcel.log");
                                        File.AppendAllText(exeDir, "Error: " + ex.Message + " " + ex.StackTrace + Environment.NewLine);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        if (_trace)
                        {
                            string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                            exeDir = Path.Combine(exeDir, "ExportDataToExcel.log");
                            File.AppendAllText(exeDir, "MetaData or ListData is empty. " + Environment.NewLine);
                        }
                    }
                }
                else
                {
                    if (_trace)
                    {
                        string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                        exeDir = Path.Combine(exeDir, "ExportDataToExcel.log");
                        File.AppendAllText(exeDir, "Data added" + Environment.NewLine);
                    }
                }
            }
            return "Success";
        }
        else
        {
            string exeDir = AppDomain.CurrentDomain.BaseDirectory;
            exeDir = Path.Combine(exeDir, "ExportDataToExcel.log");
            File.AppendAllText(exeDir, "ARMExportWorkerService input is not proper:" + queueRequest + Environment.NewLine);
            return "error";
        }
    }
    static void UpdateOrAddJsonKey(ref JObject jsonObject, string key, string value)
    {
        if (jsonObject["getreport"] is JObject getReport)
        {
            getReport[key] = value;
        }
    }
    private async Task SendSignalRMessage(string signalrUrl, string project, string clientId, string message)
    {
        if (!string.IsNullOrEmpty(clientId))
        {
            var singalRMessage = new
            {
                project = project,
                UserId = clientId,
                Message = message
            };
            API _api = new API();
            ARMResult armresult = await _api.POSTData(signalrUrl, JsonConvert.SerializeObject(singalRMessage), "application/json");
        }
    }

    static string GetExcelFast(string ires, string exportExcelPath, string _ivName, string _params, string _userName, string dateformat, string ivCaption, string _exeDir, string axConfigFileMapUser, string axConfigFileMapPwd, string _smartViewSettings)
    {
        string outputPath = string.Empty;
        using var wb = new XLWorkbook();
        try
        {
            if (string.IsNullOrWhiteSpace(ires))
                return "IView Result Error";

            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(ires);

            var headRow = xmlDoc.SelectSingleNode("//headrow");
            var dataRows = xmlDoc.SelectNodes("//row");
            var reportHF = xmlDoc.SelectNodes("//data/reporthf");

            if (headRow == null || dataRows == null)
                return "Invalid XML structure";

            var columns = new List<ColDef>();
            var htmlCols = new HashSet<string>();
            HashSet<string> hiddenColumnSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrEmpty(_smartViewSettings))
            {
                hiddenColumnSet = new HashSet<string>(_smartViewSettings.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries), StringComparer.OrdinalIgnoreCase);
            }
            foreach (XmlNode col in headRow.ChildNodes)
            {
                if (col.Name is "axrowtype" or "axp__font" or "axp__color" or "axrowoptions" or "rowno" or "pivotghead")
                    continue;

                if (col.Name.StartsWith("html_"))
                    htmlCols.Add(col.Name);

                bool hidden = col.Name.StartsWith("hide_") ||
                              col.Attributes?["hide"]?.Value == "true";
                hidden = hidden || hiddenColumnSet.Contains(col.Name);
                string colAlign = col.Attributes?["align"]?.Value == null ? "left" : col.Attributes?["align"]?.Value;
                string colType = col.Attributes?["type"]?.Value == null ? "c" : col.Attributes?["type"]?.Value;

                columns.Add(new ColDef
                {
                    Name = col.Name,
                    Caption = CleanExcelValue(
                        col.InnerXml.Replace("<hide>true</hide>", "")
                                    .Replace("<hide>false</hide>", "")
                    ),
                    Hidden = hidden,
                    Align = colAlign,
                    dType = colType
                });
            }

            foreach (var c in columns)
            {
                if (htmlCols.Contains(c.Name)) c.Hidden = false;
                if (htmlCols.Contains("html_" + c.Name)) c.Hidden = true;
            }

            var visibleCols = columns.Where(c => !c.Hidden).ToList();
            int colCount = visibleCols.Count;

            var ws = wb.Worksheets.Add("Report");

            int rowIndex = 1;

            // ===== HEADER =====
            bool headerWritten = false;
            if (reportHF != null)
            {
                foreach (XmlNode hf in reportHF)
                {
                    var headerNode = hf.SelectSingleNode("header");
                    if (headerNode == null) continue;

                    foreach (XmlNode h in headerNode.ChildNodes)
                    {
                        string caption = CleanExcelValue(h.SelectSingleNode("text")?.InnerText);
                        string align = h.SelectSingleNode("header_aline")?.InnerText ?? "@center";

                        if (string.IsNullOrWhiteSpace(caption))
                            continue;

                        var r = ws.Range(rowIndex, 1, rowIndex, colCount);
                        r.Merge();
                        r.Value = caption;

                        // EXACT OLD ALIGN LOGIC
                        if (align == "" || align == "@center")
                            r.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        else if (align == "@left")
                            r.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                        else if (align == "@right")
                            r.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                        r.Style.Font.Bold = true;
                        r.Style.Font.FontColor = XLColor.Black;

                        rowIndex++;
                        headerWritten = true;
                    }
                }
            }

            if (!headerWritten)
            {
                var r = ws.Range(rowIndex, 1, rowIndex, colCount);
                r.Merge();
                r.Value = CleanExcelValue(ivCaption);
                r.Style.Font.Bold = true;
                r.Style.Font.FontSize = 14;
                r.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                rowIndex++;
            }

            // ===== PARAMS =====
            if (!string.IsNullOrWhiteSpace(_params) && _params != "{}")
            {
                var json = JObject.Parse(_params);
                foreach (var p in json)
                {
                    var r = ws.Range(rowIndex, 1, rowIndex, colCount);
                    r.Merge();
                    r.Value = $"{CleanExcelValue(p.Key)}: {CleanExcelValue(p.Value.ToString())}";
                    r.Style.Font.Bold = true;
                    rowIndex++;
                }
                rowIndex++;
            }

            // ===== PIVOT HEADERS =====
            var pivotNode = headRow.SelectSingleNode("pivotghead");

            if (pivotNode != null)
            {
                var allCols = new List<string>();

                foreach (XmlNode col in headRow.ChildNodes)
                    if (!(col.Name is "axrowtype" or "axp__font" or "axp__color" or "axrowoptions" or "rowno" or "pivotghead"))
                        allCols.Add(col.Name);

                var indexMap = new Dictionary<int, int>();
                int excelCol = 1;

                for (int i = 0; i < allCols.Count; i++)
                    if (!columns.First(c => c.Name == allCols[i]).Hidden)
                        indexMap[i + 1] = excelCol++;

                var pxml = new XmlDocument();
                pxml.LoadXml("<pivot>" + pivotNode.InnerXml + "</pivot>");

                foreach (XmlNode head in pxml.SelectNodes("//head"))
                {
                    int sn = int.Parse(head.SelectSingleNode("snno")?.InnerText ?? "0");
                    int en = int.Parse(head.SelectSingleNode("enno")?.InnerText ?? "0");

                    string ghead = CleanExcelValue(head.SelectSingleNode("ghead")?.InnerText);
                    if (string.IsNullOrWhiteSpace(ghead)) continue;

                    var cols = indexMap.Where(m => m.Key >= sn && m.Key <= en)
                                       .Select(m => m.Value).OrderBy(x => x).ToList();

                    if (!cols.Any()) continue;

                    var r = ws.Range(rowIndex, cols.First(), rowIndex, cols.Last());
                    r.Merge();
                    r.Value = ghead;

                    r.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    r.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    r.Style.Alignment.WrapText = true;
                    r.Style.Font.Bold = true;
                    r.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                }

                rowIndex++;
            }

            // ===== COLUMN HEADERS =====
            int colIndex = 1;
            foreach (var c in visibleCols)
            {
                var cell = ws.Cell(rowIndex, colIndex++);
                cell.Value = c.Caption;
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                switch (c.Align?.Trim().ToLowerInvariant())
                {
                    case "right":
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                        break;

                    case "center":
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        break;

                    default:
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                        break;
                }
            }

            int headerRowIndex = rowIndex;
            rowIndex++;

            // ===== DATA =====
            foreach (XmlNode row in dataRows)
            {
                colIndex = 1;
                foreach (var c in visibleCols)
                {
                    string raw = CleanExcelValue(row.SelectSingleNode(c.Name)?.InnerText);
                    var cell = ws.Cell(rowIndex, colIndex++);
                    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    if (string.IsNullOrWhiteSpace(raw))
                    {
                        cell.Value = "";
                        continue;
                    }
                    if (c.dType?.Equals("n", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        bool bracketNeg = raw.StartsWith("(") && raw.EndsWith(")");
                        bool hasComma = raw.Contains(",");
                        bool hasDecimal = raw.Contains(".");
                        if (bracketNeg)
                            raw = raw[1..^1];
                        if (decimal.TryParse(raw, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out decimal dec))
                        {
                            if (bracketNeg)
                                dec *= -1;
                            cell.Value = dec;
                            string format;
                            if (hasComma)
                            {
                                format = hasDecimal ? "#,##0.################" : "#,##0";
                            }
                            else
                            {
                                format = hasDecimal ? "0.################" : "0";
                            }
                            cell.Style.NumberFormat.Format = format;
                        }
                        else
                        {
                            cell.Value = raw;
                        }
                    }
                    else if (c.dType?.Equals("d", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        if (DateTime.TryParseExact(raw, dateformat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt))
                        {
                            cell.Value = dt;
                            cell.Style.DateFormat.Format = dateformat;
                        }
                        else
                        {
                            cell.Value = raw;
                        }
                    }
                    else
                    {
                        cell.Value = raw;
                    }
                    switch (c.Align?.Trim().ToLowerInvariant())
                    {
                        case "right":
                            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                            break;

                        case "center":
                            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            break;

                        default:
                            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                            break;
                    }
                }
                rowIndex++;
            }

            // ===== FOOTER =====
            if (reportHF != null)
            {
                foreach (XmlNode hf in reportHF)
                {
                    var footer = hf.SelectSingleNode("footer");
                    if (footer == null) continue;

                    foreach (XmlNode f in footer.ChildNodes)
                    {
                        string caption = CleanExcelValue(f.SelectSingleNode("text")?.InnerText);
                        string align = f.SelectSingleNode("footer_aline")?.InnerText ?? "@center";

                        if (string.IsNullOrWhiteSpace(caption))
                            continue;

                        var r = ws.Range(rowIndex, 1, rowIndex, colCount);
                        r.Merge();
                        r.Value = caption;

                        // EXACT OLD ALIGN BEHAVIOR
                        if (align == "" || align == "@center")
                            r.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        else if (align == "@left")
                            r.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                        else if (align == "@right")
                            r.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                        r.Style.Font.Bold = true;
                        r.Style.Font.FontColor = XLColor.Black;

                        rowIndex++;
                    }
                }
            }

            ws.Columns().AdjustToContents();
            ws.SheetView.FreezeRows(headerRowIndex);

            string fileBase = ivCaption + "_" + DateTime.Now.ToString("yyyyMMddHHmmssfff");
            string fileName = (string.IsNullOrWhiteSpace(fileBase) ? _ivName : fileBase) + ".xlsx";
            fileName = fileName.Replace("/", "_");

            if (axConfigFileMapUser == string.Empty || axConfigFileMapPwd == string.Empty)
            {
                string folder = Path.Combine(exportExcelPath, "Exports", _userName);
                Directory.CreateDirectory(folder);
                outputPath = Path.Combine(folder, fileName);
                wb.SaveAs(outputPath);
            }
            else
            {
                ExecuteWithOptionalShareAccess(exportExcelPath, axConfigFileMapUser, axConfigFileMapPwd, () =>
                {
                    string destFolder = Path.Combine(exportExcelPath, "Exports", _userName);
                    if (!Directory.Exists(destFolder))
                        Directory.CreateDirectory(destFolder);
                    string destFile = Path.Combine(destFolder, fileName);
                    wb.SaveAs(destFile);
                    outputPath = destFile;
                });
            }
            outputPath = outputPath + "," + fileName;
            wb.Dispose();
        }
        catch (Exception ex)
        {
            wb.Dispose();
            //string exeDir = AppDomain.CurrentDomain.BaseDirectory;
            //exeDir = Path.Combine(exeDir, "ExportDataToExcel.log");
            File.AppendAllText(_exeDir, "Error in file path: " + outputPath + " exportExcelPath:" + exportExcelPath + " iResult:" + ires + " Exception:" + ex.Message + Environment.NewLine);
        }
        return outputPath;
    }

    public static void ExecuteWithOptionalShareAccess(string uncPath, string user, string pwd, Action action)
    {
        if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pwd))
        {
            action();
            return;
        }
        string sharePath = GetSharePath(uncPath);
        var credentials = new NetworkCredential(user, pwd);
        using (new NetworkConnection(sharePath, credentials))
        {
            action();
        }
    }
    private static string GetSharePath(string fullPath)
    {
        if (!fullPath.StartsWith(@"\\"))
            throw new Exception("Invalid UNC path");

        var parts = fullPath.Split('\\', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length < 2)
            throw new Exception("Invalid UNC format");
        return $"\\\\{parts[0]}\\{parts[1]}";
    }
    static string CleanExcelValue(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "";

        string prev;
        do
        {
            prev = input;
            input = WebUtility.HtmlDecode(input);
        } while (input != prev);

        var doc = new HtmlDocument();
        doc.LoadHtml(input);

        // Remove MS Office conditional junk
        foreach (var c in doc.DocumentNode.SelectNodes("//comment()") ?? Enumerable.Empty<HtmlNode>())
            if (c.InnerHtml.Contains("if gte mso"))
                c.Remove();

        // Remove style/script
        foreach (var n in doc.DocumentNode.SelectNodes("//style|//script") ?? Enumerable.Empty<HtmlNode>())
            n.Remove();

        // br → newline
        foreach (var br in doc.DocumentNode.SelectNodes("//br") ?? Enumerable.Empty<HtmlNode>())
            br.ParentNode.ReplaceChild(doc.CreateTextNode("\n"), br);

        // a → keep visible text
        foreach (var a in doc.DocumentNode.SelectNodes("//a") ?? Enumerable.Empty<HtmlNode>())
            a.ParentNode.ReplaceChild(doc.CreateTextNode(a.InnerText), a);

        // strip formatting tags but keep content
        foreach (var n in doc.DocumentNode.SelectNodes("//span|//font|//b|//strong|//p|//li|//ul|//ol")
                 ?? Enumerable.Empty<HtmlNode>())
            n.ParentNode.ReplaceChild(doc.CreateTextNode(n.InnerText), n);

        string clean = doc.DocumentNode.InnerText;

        clean = clean.Replace("~", Environment.NewLine);
        clean = Regex.Replace(clean, @"\s+", " ").Trim();

        return clean;
    }


    static string GetExcel(string ires, string datarows, string totalrows, string reccount, string exportExcelPath, string _ivName, string _params, string _userName, string dateformat, string _ivcaption)
    {
        string _returnRes = string.Empty;
        StringBuilder sb = new StringBuilder();
        string exportVerticalAlignStyle = "vertical-align: middle";
        string ivtype = "Iview";//listview
        ArrayList colHide = new ArrayList();
        ArrayList colFld = new ArrayList();
        ArrayList colHead = new ArrayList();
        ArrayList colType = new ArrayList();
        ArrayList colDec = new ArrayList();
        ArrayList htmlColumns = new ArrayList();

        if (ires != string.Empty)
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(ires);
            XmlNodeList productNodes;
            XmlNodeList baseDataNodes;
            productNodes = xmlDoc.SelectNodes("//headrow");
            int hcolNos;
            hcolNos = 0;

            foreach (XmlNode productNode in productNodes)
            {
                baseDataNodes = productNode.ChildNodes;
                int iCount = -1;
                foreach (XmlNode baseDataNode in baseDataNodes)
                {
                    if (baseDataNode.Name == "axrowtype" || baseDataNode.Name == "axp__font" || baseDataNode.Name == "axp__color" || baseDataNode.Name == "axrowoptions")
                    {
                        continue;
                    }
                    iCount += 1;
                    if (baseDataNode.Name == "pivotghead")
                    {
                    }
                    else if (baseDataNode.Name == "rowno")
                    {
                        baseDataNode.InnerText = "Sr. No.";
                        colFld.Add(baseDataNode.Name);
                        if (ivtype == "lview")
                        {
                            colHide.Add("false");
                            colHead.Add("Sr. No.");
                            colType.Add("c");
                        }
                        else
                        {
                            colHide.Add("true");
                            colHead.Add(baseDataNode.InnerText);
                            colType.Add("");
                        }


                        colDec.Add("");
                    }
                    else
                    {
                        if (baseDataNode.Name.StartsWith("html_"))
                        {
                            htmlColumns.Add(baseDataNode.Name);
                        }
                        colFld.Add(baseDataNode.Name);
                        colHead.Add(baseDataNode.InnerXml.Replace("<hide>true</hide>", "").Replace("<hide>false</hide>", ""));

                        if (baseDataNode.Name.StartsWith("hide_"))
                        {
                            colHide.Add("true");
                        }
                        else if (ivtype != "listview" & ivtype != "lview")
                        {
                            colHide.Add(baseDataNode.Attributes["hide"].Value);
                        }
                        else
                        {
                            colHide.Add(baseDataNode.Attributes["hide"].Value);
                        }
                        colType.Add(baseDataNode.Attributes["type"].Value);
                        colDec.Add(baseDataNode.Attributes["dec"].Value);
                        hcolNos = hcolNos + 1;
                    }
                }
            }
            int htIdx = 0;
            int cIdx = 0;
            var loopTo = htmlColumns.Count - 1;
            for (htIdx = 0; htIdx <= loopTo; htIdx++)
            {
                var loopTo1 = colFld.Count - 1;
                for (cIdx = 0; cIdx <= loopTo1; cIdx++)
                {
                    string htmlColName = htmlColumns[htIdx].ToString();
                    if (colFld[cIdx] == htmlColName)
                    {
                        colHide[cIdx] = "false";
                    }
                    else if (colFld[cIdx] == htmlColName.Replace("html_", ""))
                    {
                        colHide[cIdx] = "true";
                    }

                }
            }

            // Get name from comps
            string iviewcap = "";
            string iviewAddCaption = "";
            string iviewAddFooter = "";
            var xmlDoc1 = new XmlDocument();
            XmlNodeList compNodes;
            XmlNodeList cbaseDataNodes;

            xmlDoc1.LoadXml(ires);
            compNodes = xmlDoc1.SelectNodes("//data/reporthf");
            int lblcnt = 0;
            int lblfcnt = 0;
            ArrayList hdrFont = new ArrayList();
            ArrayList hdrAlign = new ArrayList();

            ArrayList hdrFooterName = new ArrayList();
            ArrayList hdrFooterAlign = new ArrayList();
            ArrayList hdrFooterFont = new ArrayList();

            string _dateNodehide = "f";
            foreach (XmlNode compNode in compNodes)
            {
                cbaseDataNodes = compNode.ChildNodes;
                foreach (XmlNode cbaseDataNode in cbaseDataNodes)
                {
                    if (Strings.Mid(cbaseDataNode.Name, 1, 7) == "x__head")
                    {
                        iviewcap = cbaseDataNode.Attributes["caption"].Value;
                    }
                    else if (Strings.Mid(cbaseDataNode.Name, 1, 3) == "lbl")
                    {
                        lblcnt += 1;
                        if (string.IsNullOrEmpty(iviewAddCaption))
                        {
                            iviewAddCaption = cbaseDataNode.Attributes["caption"].Value;
                        }
                        else
                        {
                            iviewAddCaption = iviewAddCaption + "*,*" + cbaseDataNode.Attributes["caption"].Value;
                        }
                    }
                    else if (cbaseDataNode.Name == "header")
                    {
                        lblcnt += 1;
                        foreach (XmlNode childHeaders in cbaseDataNode)
                        {
                            if (string.IsNullOrEmpty(iviewAddCaption))
                            {
                                string xmlContent = childHeaders.OuterXml;
                                XmlDocument _xmlDoc = new XmlDocument();
                                _xmlDoc.LoadXml(xmlContent);
                                XmlNode textNode = _xmlDoc.SelectSingleNode("//text");
                                XmlNode textNodefont = _xmlDoc.SelectSingleNode("//font");
                                if (textNodefont != null)
                                    hdrFont.Add(textNodefont.InnerText);
                                else
                                    hdrFont.Add("");
                                XmlNode textNodeheader_aline = _xmlDoc.SelectSingleNode("//header_aline");
                                if (textNodeheader_aline != null)
                                    hdrAlign.Add(textNodeheader_aline.InnerText);
                                else
                                    hdrAlign.Add("");
                                iviewAddCaption = textNode.InnerText.Trim();
                            }
                            else
                            {
                                string xmlContent = childHeaders.OuterXml;
                                XmlDocument _xmlDoc = new XmlDocument();
                                _xmlDoc.LoadXml(xmlContent);
                                XmlNode textNode = _xmlDoc.SelectSingleNode("//text");
                                XmlNode textNodefont = _xmlDoc.SelectSingleNode("//font");
                                if (textNodefont != null)
                                    hdrFont.Add(textNodefont.InnerText);
                                else
                                    hdrFont.Add("");
                                XmlNode textNodeheader_aline = _xmlDoc.SelectSingleNode("//header_aline");
                                if (textNodeheader_aline != null)
                                    hdrAlign.Add(textNodeheader_aline.InnerText);
                                else
                                {
                                    hdrAlign.Add(hdrAlign[0]);
                                }
                                string _iviewAddCaption = textNode.InnerText.Trim();
                                iviewAddCaption = iviewAddCaption + "*,*" + _iviewAddCaption;
                            }
                        }
                    }
                    else if (cbaseDataNode.Name == "hidereportdate")
                    {
                        _dateNodehide = cbaseDataNode.InnerText;
                        if (_dateNodehide.StartsWith("@"))
                            _dateNodehide = _dateNodehide.Substring(1);
                    }
                    if (cbaseDataNode.Name == "footer")
                    {
                        lblfcnt += 1;
                        foreach (XmlNode childFooters in cbaseDataNode)
                        {
                            if (string.IsNullOrEmpty(iviewAddFooter))
                            {
                                iviewAddFooter = childFooters.InnerText;
                            }
                            else
                            {
                                iviewAddFooter = iviewAddFooter + "~" + childFooters.InnerText;
                            }

                            string xmlContent = childFooters.OuterXml;
                            XmlDocument _xmlDoc = new XmlDocument();
                            _xmlDoc.LoadXml(xmlContent);
                            XmlNode textNode = _xmlDoc.SelectSingleNode("//text");
                            hdrFooterName.Add(textNode.InnerText);
                            XmlNode textNodefont = _xmlDoc.SelectSingleNode("//font");
                            if (textNodefont != null)
                                hdrFooterFont.Add(textNodefont.InnerText);
                            else
                                hdrFooterFont.Add("");
                            XmlNode textNodeheader_aline = _xmlDoc.SelectSingleNode("//footer_aline");
                            if (textNodeheader_aline != null)
                                hdrFooterAlign.Add(textNodeheader_aline.InnerText);
                            else
                                hdrFooterAlign.Add(hdrFooterAlign[0]);
                        }
                    }
                }
            }
            string _hdrNames = iviewAddCaption;
            if (lblcnt <= 3)
            {
                iviewAddCaption += "*,*";
            }

            ArrayList hdrNames = new ArrayList();
            if (_hdrNames != "")
            {
                foreach (var ivAc in _hdrNames.Split("*,*"))
                {
                    hdrNames.Add(ivAc);
                }
            }
            XmlNodeList reporthf = xmlDoc.SelectNodes("//reporthf");
            foreach (XmlNode node in reporthf)
            {
                node.ParentNode.RemoveChild(node);
            }
            // to remove attributes from headrow
            XmlNodeList productNodes2;
            XmlNodeList baseDataNodes2;
            productNodes2 = xmlDoc.SelectNodes("//headrow");

            string projectName = string.Empty;
            string resparamVal = string.Empty;
            ArrayList _ivParamCap = new ArrayList();
            if (_params != "" && _params != "{}")
            {
                JObject jsonObject = JObject.Parse(_params);
                foreach (var pnod in jsonObject)
                {
                    _ivParamCap.Add(pnod.Key + ":" + pnod.Value);
                }
            }


            XmlAttribute nAppTitle;
            nAppTitle = xmlDoc.CreateAttribute("projectName");
            nAppTitle.Value = projectName;

            XmlAttribute nCaption;
            nCaption = xmlDoc.CreateAttribute("caption");
            nCaption.Value = iviewcap;


            XmlAttribute crdate;
            crdate = xmlDoc.CreateAttribute("crdate");
            crdate.Value = DateTime.Now.ToString();
            var time = DateTime.Now;
            string format = dateformat;
            crdate.Value = time.ToString(format, CultureInfo.InvariantCulture);

            XmlAttribute paramnode;
            paramnode = xmlDoc.CreateAttribute("params");
            paramnode.Value = resparamVal;

            XmlAttribute footNode;
            footNode = xmlDoc.CreateAttribute("footer");
            footNode.Value = iviewAddFooter;

            XmlAttribute vtype;
            vtype = xmlDoc.CreateAttribute("vtype");
            vtype.Value = "c";

            XmlAttribute tmptotalrows;
            tmptotalrows = xmlDoc.CreateAttribute("tmptotalrows");
            tmptotalrows.Value = "";

            XmlAttribute xldatarows;
            xldatarows = xmlDoc.CreateAttribute("datarows");
            xldatarows.Value = datarows;

            XmlAttribute xltotalrows;
            xltotalrows = xmlDoc.CreateAttribute("totalrows");
            xltotalrows.Value = totalrows;

            XmlAttribute xlreccount;
            xlreccount = xmlDoc.CreateAttribute("reccount");
            xlreccount.Value = reccount;

            foreach (XmlNode productNode2 in productNodes2)
            {
                productNode2.Attributes.Append(nCaption);
                productNode2.Attributes.Append(crdate);
                productNode2.Attributes.Append(paramnode);
                productNode2.Attributes.Append(footNode);
                productNode2.Attributes.Append(vtype);
                productNode2.Attributes.Append(tmptotalrows);
                productNode2.Attributes.Append(xldatarows);
                productNode2.Attributes.Append(xltotalrows);
                productNode2.Attributes.Append(xlreccount);

                baseDataNodes2 = productNode2.ChildNodes;
                foreach (XmlNode baseDataNode2 in baseDataNodes2)
                    baseDataNode2.Attributes.RemoveAll();
            }


            // Add Headers
            // If Not objIview.ReportHdrs Is Nothing And objIview.ReportHdrs.Count > 0 Then
            XmlElement printHeaders = xmlDoc.CreateElement("printHeaders");
            var printHeadersString = new StringBuilder();

            printHeadersString.Append("<headerData></headerData>");

            printHeaders.InnerXml = printHeadersString.ToString();
            xmlDoc.DocumentElement.PrependChild(printHeaders);
            // End If

            //Console.WriteLine("After reporthf :" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            //Console.WriteLine("Before headrow :" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            string pivotXml = "";
            int hrep;
            var loopTo3 = hcolNos;
            for (hrep = 1; hrep <= loopTo3; hrep++)
            {
                XmlNodeList productNodes3;
                XmlNodeList baseDataNodes3;
                productNodes3 = xmlDoc.SelectNodes("//headrow");
                int hidx;

                foreach (XmlNode productNode3 in productNodes3)
                {
                    baseDataNodes3 = productNode3.ChildNodes;
                    foreach (XmlNode baseDataNode3 in baseDataNodes3)
                    {
                        if (baseDataNode3.Name == "axrowtype" || baseDataNode3.Name == "axp__font" || baseDataNode3.Name == "axp__color" || baseDataNode3.Name == "axrowoptions")
                        {
                            continue;
                        }
                        string nodeString = baseDataNode3.InnerXml.Replace("<hide>true</hide>", "").Replace("<hide>false</hide>", "").Replace("~", "<br/>");
                        if (baseDataNode3.Name != "pivotghead")
                        {
                            baseDataNode3.InnerText = nodeString;
                        }

                        if (baseDataNode3.Name == "pivotghead")
                        {
                            pivotXml = baseDataNode3.InnerXml;
                            baseDataNode3.ParentNode.RemoveChild(baseDataNode3);
                        }
                        else
                        {
                            var loopTo4 = colFld.Count - 1;
                            for (hidx = 0; hidx <= loopTo4; hidx++)
                            {
                                if (colFld[hidx] == baseDataNode3.Name)
                                {
                                    if (colHide[hidx].ToString() == "true")
                                    {
                                        baseDataNode3.ParentNode.RemoveChild(baseDataNode3);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // Process Pivot
            pivotXml = "<pivot>" + pivotXml + "</pivot>";
            var pxmlDoc = new XmlDocument();
            pxmlDoc.LoadXml(pivotXml);

            XmlNodeList ptproductNodes;
            XmlNodeList ptbaseDataNodes;
            ptproductNodes = pxmlDoc.SelectNodes("//head");

            int iph = 1;
            string newPXml = "";
            foreach (XmlNode ptproductNode in ptproductNodes)
            {
                string snno = "0";
                string enno = "0";
                try
                {
                    //snno = ptproductNode.Attributes["snno"].Value;
                    //enno = ptproductNode.Attributes["enno"].Value;
                    if (ptproductNode.SelectSingleNode("snno") != null)
                        snno = ptproductNode.SelectSingleNode("snno")?.InnerText;
                    if (ptproductNode.SelectSingleNode("enno") != null)
                        enno = ptproductNode.SelectSingleNode("enno")?.InnerText;
                }
                catch (Exception)
                {
                    //snno = ptproductNode.SelectSingleNode("snno")?.InnerText;
                    //enno = ptproductNode.SelectSingleNode("enno")?.InnerText;
                }

                int cspan = int.Parse(enno) - int.Parse(snno) + 1;
                newPXml = newPXml + "<head>";
                ptbaseDataNodes = ptproductNode.ChildNodes;
                bool isGhead = false;
                foreach (XmlNode ptbaseDataNode in ptbaseDataNodes)
                {
                    if (ptbaseDataNode.Name == "ghead")
                    {
                        isGhead = true;
                        ptbaseDataNode.InnerText = CheckSpecialChars(ptbaseDataNode.InnerText);
                        ptbaseDataNode.InnerText = ptbaseDataNode.InnerText.Replace("~", "<br/>");
                        newPXml = newPXml + "<ghead class=\"gridHeader\"  colspan=\"" + cspan + "\">" + ptbaseDataNode.InnerText + "</ghead>";
                    }
                }
                if (isGhead == false && colHide[iph].ToString() == "false")
                {
                    newPXml = newPXml + "<ghead class=\"gridHeader\"></ghead>";
                }
                iph++;
                newPXml = newPXml + "</head>";
            }
            // Add pivot node
            XmlElement pivotNode = xmlDoc.CreateElement("pivot");
            if (!string.IsNullOrEmpty(newPXml))
            {
                pivotNode.InnerXml = newPXml;
            }
            else
            {
                pivotNode.InnerXml = "<head><ghead class=\"\" /></head>";
            }

            xmlDoc.DocumentElement.PrependChild(pivotNode);

            //Console.WriteLine("After headrow :" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            //Console.WriteLine("Before row :" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            // To set class
            XmlNodeList productNodes5;
            XmlNodeList baseDataNodes5;
            productNodes5 = xmlDoc.SelectNodes("//row");
            foreach (XmlNode productNode5 in productNodes5)
            {
                baseDataNodes5 = productNode5.ChildNodes;
                string rowclass = "searchresultitem";
                string colalignvalue = "";
                foreach (XmlNode baseDataNode5 in baseDataNodes5)
                {
                    string axrtype = "";
                    string dsa1 = baseDataNode5.Name.ToString();
                    string dfs1 = baseDataNode5.InnerText.ToString();
                    if (baseDataNode5.Name.ToString().ToLower() == "axrowtype" & baseDataNode5.InnerText.ToString() != "")
                    {
                        axrtype = baseDataNode5.InnerText.ToString();
                    }
                    if (!string.IsNullOrEmpty(axrtype))
                    {
                        rowclass = axrtype;
                    }
                    colalignvalue = "left";
                    XmlAttribute classnode;
                    classnode = xmlDoc.CreateAttribute("class");
                    classnode.Value = (rowclass + "-" + colalignvalue).ToLower();
                    baseDataNode5.Attributes.Append(classnode);
                }
            }
            // Format numeric fields
            XmlNodeList productNodesNum;
            XmlNodeList baseDataNodesNum;
            productNodesNum = xmlDoc.SelectNodes("//row");
            //Console.WriteLine("After row :" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            //Console.WriteLine("Before row1 :" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            int idxx;

            var val = default(double);
            foreach (XmlNode productNodeNum in productNodesNum)
            {
                baseDataNodesNum = productNodeNum.ChildNodes;
                foreach (XmlNode baseDataNodeNum in baseDataNodesNum)
                {
                    var loopTo5 = colFld.Count - 1;
                    for (idxx = 0; idxx <= loopTo5; idxx++)
                    {
                        if (colFld[idxx] == baseDataNodeNum.Name.ToLower())
                        {
                            if (colType[idxx].ToString() == "n" & baseDataNodeNum.InnerText != "" & colDec[idxx].ToString() != "0")
                            {
                                string flvalue = baseDataNodeNum.InnerText.ToString();
                                char firstChar = Convert.ToChar(flvalue.Substring(0, 1));
                                char lastChar = Convert.ToChar(flvalue.Substring(flvalue.Length - 1, 1));
                                bool isNumber = char.IsDigit(firstChar);
                                if (!isNumber & firstChar.ToString() == "(" & lastChar.ToString() == ")")
                                {
                                    flvalue = flvalue.Substring(1, flvalue.Length - 2);
                                    val = double.Parse(flvalue);
                                }
                                else if (flvalue == "&nbsp;")
                                {
                                    baseDataNodeNum.InnerText = "";
                                }
                                else if (!isNumber)
                                {
                                    flvalue = flvalue.Substring(1);
                                    val = double.Parse(flvalue);
                                }
                                else
                                {
                                    val = double.Parse(flvalue);
                                }
                                if (!isNumber & firstChar.ToString() == "(" & lastChar.ToString() == ")")
                                {
                                    baseDataNodeNum.InnerText = Convert.ToString(firstChar) + val.ToString("N" + colDec[idxx], CultureInfo.InvariantCulture) + Convert.ToString(lastChar);
                                }
                                else
                                {
                                    baseDataNodeNum.InnerText = (isNumber ? "" : Convert.ToString(firstChar)) + val.ToString("N" + colDec[idxx], CultureInfo.InvariantCulture);
                                }
                            }
                            else if (colType[idxx].ToString() == "c" & baseDataNodeNum.InnerText != "")
                            {
                                string nodeValue = baseDataNodeNum.InnerText;
                                if (Information.IsNumeric(nodeValue))
                                {
                                    baseDataNodeNum.InnerText = "&nbsp;" + baseDataNodeNum.InnerText + "";
                                    //int indexOfDecimalPoint = nodeValue.IndexOf(".");
                                    //var regex = new Regex("[0-9.-]*(e|E)[0-9.-]*");
                                    //Match match = regex.Match(baseDataNodeNum.InnerText);
                                    //if (indexOfDecimalPoint > -1)
                                    //{
                                    //    int numberOfDecimals = nodeValue.Substring(indexOfDecimalPoint + 1).Length;
                                    //    string decimalFormat = "";
                                    //    for (int j = 1, loopTo6 = numberOfDecimals; j <= loopTo6; j++)
                                    //        decimalFormat += "0";
                                    //    baseDataNodeNum.InnerText = "=TEXT(\"" + baseDataNodeNum.InnerText + "\",\"0." + decimalFormat + "\")";
                                    //}
                                    //else if (match.Success & match.Index == 0 | baseDataNodeNum.InnerText.StartsWith("0") | baseDataNodeNum.InnerText.Length > 15)
                                    //{
                                    //    baseDataNodeNum.InnerText = "&nbsp;" + baseDataNodeNum.InnerText + "";
                                    //}
                                    //else
                                    //{
                                    //    baseDataNodeNum.InnerText = "=TEXT(\"" + baseDataNodeNum.InnerText + "\",\"0\")";
                                    //}
                                }
                                else
                                {
                                    string _strValue = StringReplaceSpecialChar(baseDataNodeNum.InnerText);
                                    baseDataNodeNum.InnerText = _strValue.Replace("~", "<br/>");
                                }
                            }
                        }
                    }
                }
            }

            //Console.WriteLine("After row1 :" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            //Console.WriteLine("Before remove hidden fields :" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            // remove hidden fields
            int rep;
            // change  later try new code to remove this for
            var loopTo7 = hcolNos;
            try
            {

                List<string> colFld1 = colFld.Cast<string>().ToList();
                HashSet<string> colFldSet = new HashSet<string>(colFld1.Select(d => d.ToLower()));
                HashSet<string> colHideSet = new HashSet<string>();

                for (int i = 0; i < colFld.Count; i++)
                {
                    if (colHide[i].ToString().ToLower() == "true")
                    {
                        colHideSet.Add(colFld[i].ToString().ToLower());
                    }
                }

                XmlNodeList productNodes1 = xmlDoc.SelectNodes("//row");
                foreach (XmlNode productNode1 in productNodes1)
                {
                    for (int i = productNode1.ChildNodes.Count - 1; i >= 0; i--)
                    {
                        XmlNode baseDataNode1 = productNode1.ChildNodes[i];
                        if (colFldSet.Contains(baseDataNode1.Name.ToLower()) && colHideSet.Contains(baseDataNode1.Name.ToLower()))
                        {
                            baseDataNode1.ParentNode.RemoveChild(baseDataNode1);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine("Error looping XML data for hinden columns: " + ex.Message);

                string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                exeDir = Path.Combine(exeDir, "ExportDataToExcel.log");
                File.AppendAllText(exeDir, "Error looping XML data for hinden columns: " + ex.Message + Environment.NewLine);
            }
            //Console.WriteLine("After remove hidden fields :" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            //Console.WriteLine("Before refill :" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            // Code to add new nodes in xml which aws missed
            XmlNode headSinglenode;
            headSinglenode = xmlDoc.SelectSingleNode("//headrow");

            try
            {
                XmlAttribute vAlign;
                vAlign = xmlDoc.CreateAttribute("valign");
                vAlign.Value = exportVerticalAlignStyle;
                headSinglenode.Attributes.Append(vAlign);
            }

            catch (Exception ex)
            {
                //Console.WriteLine("Error valign: " + ex.Message);
                string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                exeDir = Path.Combine(exeDir, "ExportDataToExcel.log");
                File.AppendAllText(exeDir, "Error valign: " + ex.Message + Environment.NewLine);
            }

            int headNodecount = headSinglenode.ChildNodes.Count;

            XmlNodeList productNodesRow;
            XmlNodeList baseDataNodesRow;
            productNodesRow = xmlDoc.SelectNodes("//row");
            foreach (XmlNode productNodeRow in productNodesRow)
            {
                baseDataNodesRow = productNodeRow.ChildNodes;
                int rowCounta = baseDataNodesRow.Count;
                string rowclass = "searchresultitem";
                if (rowCounta < headNodecount)
                {
                    int d = headNodecount - rowCounta;
                    int b;
                    var loopTo9 = d;
                    for (b = 1; b <= loopTo9; b++)
                    {
                        XmlElement tempNode = xmlDoc.CreateElement("refill");
                        tempNode.InnerText = "";
                        XmlAttribute classnode;
                        classnode = xmlDoc.CreateAttribute("class");
                        classnode.Value = rowclass.ToLower();
                        tempNode.Attributes.Append(classnode);
                        productNodeRow.AppendChild(tempNode);
                    }
                }
            }
            //Console.WriteLine("After refill :" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            //Console.WriteLine("Before Ds :" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            var sw = new StringWriter();
            var xw = new XmlTextWriter(sw);
            xmlDoc.WriteTo(xw);

            string nXml;
            nXml = sw.ToString();

            var sr = new StringReader(nXml);
            var ds = new DataSet();

            try
            {
                ds.ReadXml(sr);
            }
            catch (Exception ex)
            {
                //Console.WriteLine("Error reading XML into DataSet: " + ex.Message);
                string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                exeDir = Path.Combine(exeDir, "ExportDataToExcel.log");
                File.AppendAllText(exeDir, "Error reading XML into DataSet: " + ex.Message + Environment.NewLine);
            }
            foreach (System.Data.DataTable tables in ds.Tables)
            {
                if (tables.TableName.Equals("headrow"))
                {
                    foreach (DataColumn columns in tables.Columns)
                    {
                        if (tables.Rows[0][columns].ToString().Contains("axp_slno"))
                        {
                            tables.Rows[0][columns] = "Sr. No.";
                            break;
                        }
                        if (ivtype == "lview")
                        {
                            if (tables.Rows[0][columns].ToString().Contains("rowno"))
                            {
                                tables.Rows[0][columns] = "Sr. No.";
                                break;
                            }
                        }
                    }
                }
            }
            //Console.WriteLine("After Ds :" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            //Console.WriteLine("Before LoadHtml :" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));

            // Load HTML into HtmlDocument
            HtmlDocument htmlDoc = new HtmlDocument();
            try
            {
                XmlDataDocument xdd;
                xdd = new XmlDataDocument(ds);

                var xt = new System.Xml.Xsl.XslCompiledTransform();
                var builder = new ConfigurationBuilder()
        .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
        .AddJsonFile("appsettings.json");

                var configuration = builder.Build();

                string relativePath = configuration["XsltFilePath"];
                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string xsltPath = Path.Combine(baseDirectory, relativePath);

                xt.Load(xsltPath);

                var swr = new StringWriter(sb);
                var writer = new XmlTextWriter(swr);
                xt.Transform(xdd, writer);
                sw.Close();
                writer.Close();

                htmlDoc.LoadHtml(sb.ToString());
            }
            catch (Exception ex)
            {
                //Console.WriteLine("Error in loadhtml Exception:" + ex.Message);
                string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                exeDir = Path.Combine(exeDir, "ExportDataToExcel.log");
                File.AppendAllText(exeDir, "Error in loadhtml Exception:" + ex.Message + Environment.NewLine);
            }
            //Console.WriteLine("After LoadHtml :" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            //Console.WriteLine("Before Excel generate :" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Sheet1");
                int falseCount = colHide.OfType<string>().Count(b => bool.TryParse(b, out bool result) && !result);
                var table = htmlDoc.DocumentNode.SelectSingleNode("//table");
                if (table != null)
                {
                    int rowIndex = 1;

                    for (int i = 0; i < hdrNames.Count; i++)
                    {
                        AddCaption(worksheet, ref rowIndex, hdrNames[i].ToString(), hdrFont[i].ToString(), hdrAlign[i].ToString(), falseCount);
                    }

                    if (hdrNames.Count == 0)
                    {
                        if (iviewcap == "")
                            iviewcap = _ivcaption;
                        AddCaption(worksheet, ref rowIndex, iviewcap, "", "", falseCount);
                    }

                    if (_dateNodehide == "f")
                        AddDate(worksheet, ref rowIndex, dateformat, falseCount);
                    for (int j = 0; j < _ivParamCap.Count; j++)
                    {
                        var hValue = HtmlToText(_ivParamCap[j].ToString());
                        hValue = CleanCellText(hValue);
                        hValue = ReplaceSpecialChars(hValue);
                        hValue = StringReplaceSpecialChar(hValue);
                        AddCaptionParam(worksheet, ref rowIndex, hValue, falseCount);
                    }

                    foreach (var row in table.SelectNodes("tr"))
                    {
                        int colIndex = 1;

                        foreach (var cell in row.SelectNodes("th|td"))
                        {
                            int colspan = cell.GetAttributeValue("colspan", 1);
                            var cellValue = HtmlToText(cell.InnerText);
                            cellValue = CleanCellText(cellValue);
                            cellValue = ReplaceSpecialChars(cellValue);
                            cellValue = StringReplaceSpecialChar(cellValue);
                            var wsCell = worksheet.Cell(rowIndex, colIndex);

                            if (double.TryParse(cellValue, out double numericValue))
                            {
                                wsCell.SetValue(numericValue);
                                bool containsComma = cellValue.Contains(",");
                                if (numericValue == Math.Floor(numericValue))
                                {
                                    wsCell.Style.NumberFormat.Format = containsComma ? "#,##0" : "0";
                                }
                                else
                                {
                                    int decimalPlaces = cellValue.Contains(".")
                                        ? cellValue.Substring(cellValue.IndexOf('.') + 1).Length
                                        : 0;
                                    string _format = containsComma ? "#,##0." : "0.";
                                    _format += new string('0', decimalPlaces);
                                    wsCell.Style.NumberFormat.Format = _format;
                                }
                            }
                            else if (DateTime.TryParseExact(cellValue, dateformat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dateValue1))
                            {
                                wsCell.Value = dateValue1;
                                wsCell.Style.NumberFormat.Format = dateformat;
                            }
                            else
                            {
                                wsCell.SetValue(cellValue);
                            }

                            // Apply styles based on class names
                            if (cell.Attributes["class"] != null)
                            {
                                string className = cell.Attributes["class"].Value;
                                ApplyStyles(wsCell, className);
                            }

                            // Enable text wrapping to handle newlines
                            wsCell.Style.Alignment.WrapText = true;

                            // Handle colspan
                            if (colspan > 1)
                            {
                                worksheet.Range(rowIndex, colIndex, rowIndex, colIndex + colspan - 1).Merge();
                                colIndex += colspan;
                            }
                            else
                            {
                                colIndex++;
                            }
                        }
                        // Adjust the row height to fit the content
                        worksheet.Row(rowIndex).AdjustToContents();

                        rowIndex++;
                    }

                    for (int i = 0; i < hdrFooterName.Count; i++)
                    {
                        AddCaption(worksheet, ref rowIndex, hdrFooterName[i].ToString(), hdrFooterFont[i].ToString(), hdrFooterAlign[i].ToString(), falseCount);
                    }
                }
                string _thisIvCap = _ivcaption;
                if (_thisIvCap == string.Empty)
                {
                    if (iviewAddCaption.Contains("*,*"))
                        _thisIvCap = iviewAddCaption.Split("*,*")[0];
                    else
                        _thisIvCap = iviewAddCaption;
                }
                _thisIvCap = _thisIvCap + "_" + DateTime.Now.ToString("yyyyMMddHHmmssfff");
                string outputPath = _thisIvCap + ".xlsx";
                string _fileName = "";
                try
                {
                    if (outputPath == ".xlsx")
                        outputPath = _ivName + ".xlsx";
                    outputPath = outputPath.Replace("/", "_");
                    _fileName = outputPath;
                    outputPath = exportExcelPath + "\\Exports\\" + _userName + "\\" + outputPath;
                    worksheet.Columns().AdjustToContents();
                    workbook.SaveAs(outputPath);
                }
                catch (Exception ex)
                {
                    string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                    exeDir = Path.Combine(exeDir, "ExportDataToExcel.log");
                    File.AppendAllText(exeDir, "Error in file path: " + outputPath + " Exception:" + ex.Message + Environment.NewLine);
                }
                _returnRes = outputPath + "," + _fileName;
            }
        }
        else
        {
            _returnRes = "IView Result Error";
        }
        return _returnRes;
    }
    static string CheckSpecialChars(string str)
    {
        if (string.IsNullOrEmpty(str))
            return string.Empty;
        str = HttpUtility.HtmlEncode(str);
        str = str.Replace("\\", ";bkslh");
        return str;
    }
    static string StringReplaceSpecialChar(string value)
    {
        value = parseHTML(value); ;
        if (value.Contains("<!--[if gte mso 9]>"))
        {
            value = value.Replace("<!--[if gte mso 9]>", "ô");
            int totalCount = value.Count(x => x == 'ô');
            for (int i = 0; i < totalCount; i++)
            {
                int startIndex = value.IndexOf("ô");
                int endIndex = value.IndexOf("<![endif]-->", startIndex);
                value = value.Remove(startIndex, (endIndex - startIndex) + 12);
            }
        }

        if (value.Contains("<!--[if gte mso 10]>"))
        {
            value = value.Replace("<!--[if gte mso 10]>", "├");
            int totalCount = value.Count(x => x == '├');
            for (int i = 0; i < totalCount; i++)
            {
                int startIndex = value.IndexOf("├");
                int endIndex = value.IndexOf("<![endif]-->", startIndex);
                value = value.Remove(startIndex, (endIndex - startIndex) + 12);
            }
        }

        if (value.Contains("<style>"))
        {
            value = value.Replace("<style>", "☻");
            int totalCount = value.Count(x => x == '☻');
            for (int i = 0; i < totalCount; i++)
            {
                int startIndex = value.IndexOf("☻");
                int endIndex = value.IndexOf("</style>", startIndex);
                value = value.Remove(startIndex, (endIndex - startIndex) + 8);
            }
        }
        if (value.Contains("<a "))
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(value);
            var aTags = doc.DocumentNode.SelectNodes("//a");
            if (aTags != null)
            {
                foreach (var aTag in aTags)
                {
                    var spanTag = aTag.SelectSingleNode(".//span");
                    if (spanTag == null)
                    {
                        string innerText = aTag.InnerText.Trim();
                        aTag.ParentNode.ReplaceChild(HtmlTextNode.CreateNode(innerText), aTag);
                    }
                    else
                    {
                        aTag.Remove();
                    }
                }
            }
            string result = doc.DocumentNode.InnerHtml;
            value = result.Trim();
        }

        if (value.Contains("<font "))
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(value);
            string combinedContent = string.Empty;
            foreach (var node in doc.DocumentNode.ChildNodes)
            {
                if (node.Name.Equals("font", StringComparison.OrdinalIgnoreCase))
                {
                    combinedContent += node.InnerText.Trim() + " ";
                }
                else if (!string.IsNullOrWhiteSpace(node.InnerText))
                {
                    combinedContent += node.InnerText.Trim() + " ";
                }
            }
            value = combinedContent.Trim();
        }

        value = value.Replace("<o:p>", "");
        value = value.Replace("</o:p>", "");

        value = value.Replace("<![endif]-->", " ");
        value = value.Replace("<!--[if !supportLists]-->", " ");

        value = value.Replace("<span>", " ");
        value = value.Replace("</span>", " ");

        value = value.Replace("<strong>", "");
        value = value.Replace("</strong>", "");

        value = value.Replace("<b>", "");
        value = value.Replace("</b>", "");
        value = value.Replace("<br>", "");
        value = value.Replace("</br>", "");

        value = value.Replace("<p>", "");
        value = value.Replace("</p>", "");

        value = value.Replace("<ul>", "");
        value = value.Replace("</ul>", "");

        value = value.Replace("<li>", " ");
        value = value.Replace("</li>", "");

        value = value.Replace("<ol>", "");
        value = value.Replace("</ol>", "");
        value = value.Replace("paraLine", "");
        value = value.Replace("&nbsp;", " ");
        value = value.Replace("&zwj;", "");
        value = value.Replace("&#39;", "'");
        value = value.Replace("&rsquo;", "'");
        value = value.Replace("&rdquo;", "\"");
        value = value.Replace("&ldquo;", "\"");
        value = value.Replace("&ndash", "-");
        value = value.Replace("&ndash;", "-");
        value = value.Replace("!--[endif]--", "");
        return value;
    }
    static string parseHTML(string html)
    {
        HtmlDocument doc = new HtmlDocument();
        doc.LoadHtml(html);
        HtmlNodeCollection htmlNodes = doc.DocumentNode.SelectNodes("//div | //ul | //p | //table | //ol | //span | //strong | //b");
        using (StringWriter sw = new StringWriter())
        {
            if (htmlNodes != null && htmlNodes.Count > 0)
                foreach (var node in htmlNodes)
                    if (node.HasAttributes)
                        node.Attributes.RemoveAll();

            doc.Save(sw);
            return sw.ToString();
        }
    }
    static void AddCaption(IXLWorksheet worksheet, ref int rowIndex, string caption, string font, string align, int celllength)
    {
        if (!string.IsNullOrEmpty(caption))
        {
            var cell = worksheet.Cell(rowIndex++, 1);
            cell.Value = caption;
            if (align == "" || align == "@center")
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            else if (align == "@left")
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            else if (align == "@right")
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.Black;
            worksheet.Range(rowIndex - 1, 1, rowIndex - 1, celllength).Merge();
        }
    }
    static void AddCaptionParam(IXLWorksheet worksheet, ref int rowIndex, string ivPacap, int celllength)
    {
        if (!string.IsNullOrEmpty(ivPacap))
        {
            var cell = worksheet.Cell(rowIndex++, 1);
            cell.Value = ivPacap;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.Black;
            worksheet.Range(rowIndex - 1, 1, rowIndex - 1, celllength).Merge();
        }
    }
    static void AddDate(IXLWorksheet worksheet, ref int rowIndex, string dateformat, int celllength)
    {
        var time = DateTime.Now;
        string format = dateformat;
        var cell = worksheet.Cell(rowIndex++, 1);
        cell.Value = "Date: " + time.ToString(format, CultureInfo.InvariantCulture);
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        cell.Style.Font.Bold = false;
        cell.Style.Font.FontColor = XLColor.Black;
        worksheet.Range(rowIndex - 1, 1, rowIndex - 1, celllength).Merge();
    }
    static string HtmlToText(string html)
    {
        if (string.IsNullOrEmpty(html))
            return string.Empty;

        // Replace <br> tags with newline characters
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var brNodes = doc.DocumentNode.SelectNodes("//br");
        if (brNodes != null)
        {
            foreach (var br in brNodes)
            {
                br.ParentNode.ReplaceChild(doc.CreateTextNode("\n"), br);
            }
        }

        return doc.DocumentNode.InnerText;
    }
    static string CleanCellText(string text)
    {
        // Split by the '~' delimiter to handle new lines
        var parts = text.Split('~');
        for (int i = 0; i < parts.Length; i++)
        {
            parts[i] = TrimInnerText(parts[i]);
        }
        return string.Join(Environment.NewLine, parts);
    }
    static string TrimInnerText(string text)
    {
        // Trim spaces and handle specific text cases
        var trimmedText = text.Trim();

        // Example: Trim spaces between "Date :" and the actual date value
        var datePattern = @"(Date\s*:\s*)(\d{2}/\d{2}/\d{4})";
        trimmedText = Regex.Replace(trimmedText, datePattern, m => $"{m.Groups[1].Value.Trim()} {m.Groups[2].Value.Trim()}");

        // Add other specific patterns here if needed

        return trimmedText;
    }
    static string ReplaceSpecialChars(string str)
    {
        if (string.IsNullOrEmpty(str))
            return string.Empty;
        string prev;
        do
        {
            prev = str;
            str = HttpUtility.HtmlDecode(str);
        } while (str != prev);
        str = str.Replace(";bkslh", "\\");
        return str;
    }
    static void ApplyStyles(IXLCell cell, string className)
    {
        switch (className)
        {
            case "stdPageMainHdr":
                cell.Style.Font.FontColor = XLColor.Gray;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Font.FontSize = 22;
                break;
            case "stdPVTblLCell":
                cell.Style.Fill.BackgroundColor = XLColor.Aqua;
                cell.Style.Font.FontColor = XLColor.Gray;
                cell.Style.Font.Bold = true;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Font.FontSize = 13;
                break;
            case "stdPageHdr":
                cell.Style.Font.FontColor = XLColor.Gray;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Font.FontSize = 12;
                break;
            case "stdAddlHdr":
                cell.Style.Font.FontColor = XLColor.Black;
                cell.Style.Font.Bold = true;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Font.FontSize = 13.5;
                break;
            case "gridHeader":
                cell.Style.Font.FontColor = XLColor.Black;
                cell.Style.Font.Bold = true;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                break;
            case "subhead":
            case "subhead-left":
            case "subhead-right":
            case "subhead-middle":
            case "subhead-center":
                cell.Style.Font.FontColor = XLColor.DarkRed;
                cell.Style.Font.FontSize = 12;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Font.Underline = XLFontUnderlineValues.Single;
                break;
            case "stot":
            case "stot-left":
            case "stot-right":
            case "stot-middle":
            case "stot-center":
                cell.Style.Font.FontColor = XLColor.DarkRed;
                cell.Style.Font.FontSize = 12;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                break;
            case "gtot":
            case "gtot-left":
            case "gtot-right":
            case "gtot-middle":
            case "gtot-center":
                cell.Style.Font.FontColor = XLColor.Green;
                cell.Style.Font.FontSize = 12;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Font.Underline = XLFontUnderlineValues.Single;
                break;
            case "searchresultitem":
            case "searchresultitem-left":
            case "searchresultitem-right":
            case "searchresultitem-middle":
            case "searchresultitem-center":
            case "searchresultaltitem":
            case "searchresultaltitem-left":
            case "searchresultaltitem-right":
            case "searchresultaltitem-middle":
            case "searchresultaltitem-center":
                cell.Style.Font.FontColor = XLColor.Black;
                cell.Style.Font.FontSize = 8;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                break;
            case "stdPageftr":
                cell.Style.Font.FontColor = XLColor.Gray;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Font.FontSize = 12;
                break;
            case "right":
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                break;
            case "left":
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                break;
            case "middle":
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                break;
            case "headerClass":
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                break;
            case "dataClass":
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                break;
            case "someClass":
                cell.Style.Font.Bold = true;
                break;
            default:
                break;
        }
    }

    static async Task<JObject> GetAPIResult(string apiurl, string page, string armSessionId, string axSessionId, string project, string roles, string username, string transid, string token, int pageNo, int pageSize, string trace)
    {
        var apiDetails = new JObject
        {
            ["Page"] = page,
            ["ARMSessionId"] = armSessionId,
            ["AxSessionId"] = axSessionId,
            ["Trace"] = bool.Parse(trace),
            ["PageNo"] = 0,
            ["PageSize"] = 0,
            ["AppName"] = project,
            ["Roles"] = roles,
            ["UserName"] = username,
            ["TransId"] = transid,
            ["PropertiesList"] = new JArray(),
            ["onlydata"] = false
        };

        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var content = new StringContent(apiDetails.ToString(), Encoding.UTF8, "application/json");
            var response = await client.PostAsync(apiurl, content);

            if (response.IsSuccessStatusCode)
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                return JObject.Parse(responseContent);
            }
            else
            {
                return new JObject
                    {
                        {
                                "error", $"Error: {response.StatusCode}" }

                    };
            }
        }
    }
    static async Task<string> GetExcelTstruct(string pagename, string dateformat, string outputPath, string project, JArray metaDataArray, JArray listDataArray, List<string> skipFlds, Dictionary<string, string> fieldDictionary, Dictionary<string, string> dataTypeDictionary, string fileServerPath, string transid)
    {
        try
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Sheet1");
                int currentRow = 1;

                var visibleTypeDictionary = metaDataArray.ToDictionary(
                    item => (string)item["fldname"],
                    item => item["hide"]?.ToString() ?? "F"
                );

                int totalColumns = 0;
                List<string> columnOrder = new List<string>();

                if (listDataArray.Count > 0 && listDataArray[0]["data_json"] != null)
                {
                    var firstDataJsonArray = JArray.Parse(listDataArray[0]["data_json"]?.ToString() ?? "[]");
                    if (firstDataJsonArray.Count > 0)
                    {
                        var firstRowObject = firstDataJsonArray[0] as JObject;
                        columnOrder = firstRowObject.Properties()
                            .Select(p => p.Name)
                            .Where(name => !skipFlds.Contains(name)
                                        && name != "createdon" && name != "createdby"
                                        && name != "modifiedby" && name != "modifiedon"
                                        && (!visibleTypeDictionary.TryGetValue(name, out string hideValue) || hideValue != "T"))
                            .ToList();

                        columnOrder.Add("modifiedby");
                        columnOrder.Add("modifiedon");
                        columnOrder.Add("createdby");
                        columnOrder.Add("createdon");
                        totalColumns = columnOrder.Count;
                    }
                }

                var headerBackgroundColor = XLColor.FromArgb(61, 61, 61);
                var headerTextColor = XLColor.White;

                worksheet.Cell(currentRow, 1).Value = project;
                worksheet.Range(currentRow, 1, currentRow, totalColumns).Merge();
                worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
                worksheet.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell(currentRow, 1).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                worksheet.Cell(currentRow, 1).Style.Fill.BackgroundColor = headerBackgroundColor;
                worksheet.Cell(currentRow, 1).Style.Font.FontColor = headerTextColor;
                currentRow++;

                worksheet.Cell(currentRow, 1).Value = pagename;
                worksheet.Range(currentRow, 1, currentRow, totalColumns).Merge();
                worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
                worksheet.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell(currentRow, 1).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                worksheet.Cell(currentRow, 1).Style.Fill.BackgroundColor = headerBackgroundColor;
                worksheet.Cell(currentRow, 1).Style.Font.FontColor = headerTextColor;
                currentRow++;

                worksheet.Cell(currentRow, 1).Value = "Date: " + DateTime.Now.ToString(dateformat);
                worksheet.Range(currentRow, 1, currentRow, totalColumns).Merge();
                worksheet.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                worksheet.Cell(currentRow, 1).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                worksheet.Cell(currentRow, 1).Style.Fill.BackgroundColor = headerBackgroundColor;
                worksheet.Cell(currentRow, 1).Style.Font.FontColor = headerTextColor;
                currentRow++;

                int currentColumn = 1;
                foreach (var field in columnOrder)
                {
                    worksheet.Cell(currentRow, currentColumn).Value = fieldDictionary.ContainsKey(field) ? fieldDictionary[field] : field;
                    worksheet.Cell(currentRow, currentColumn).Style.Font.Bold = true;
                    worksheet.Cell(currentRow, currentColumn).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    worksheet.Cell(currentRow, currentColumn).Style.Fill.BackgroundColor = headerBackgroundColor;
                    worksheet.Cell(currentRow, currentColumn).Style.Font.FontColor = headerTextColor;
                    currentColumn++;
                }
                currentRow++;

                foreach (var listData in listDataArray)
                {
                    if (listData["data_json"] != null)
                    {
                        var dataJsonArray = JArray.Parse(listData["data_json"]?.ToString() ?? "[]");

                        foreach (var row in dataJsonArray)
                        {
                            var rowObject = row as JObject;
                            int column = 1;

                            foreach (var field in columnOrder)
                            {
                                var cellValue = rowObject[field]?.ToString();
                                if (dataTypeDictionary.TryGetValue(field, out string fldType))
                                {
                                    if (fldType == "d" && DateTime.TryParse(cellValue, out var dateValue))
                                    {
                                        worksheet.Cell(currentRow, column).Value = dateValue;
                                        worksheet.Cell(currentRow, column).Style.DateFormat.Format = dateformat;
                                    }
                                    else if (fldType == "n" && int.TryParse(cellValue, out var intValue))
                                    {
                                        worksheet.Cell(currentRow, column).Value = intValue;
                                    }
                                    else
                                    {
                                        worksheet.Cell(currentRow, column).Value = cellValue;
                                    }
                                }
                                else
                                {
                                    worksheet.Cell(currentRow, column).Value = cellValue;
                                }

                                worksheet.Cell(currentRow, column).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                                column++;
                            }

                            currentRow++;
                        }
                    }
                }

                worksheet.Columns().AdjustToContents();
                string timestamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                outputPath = $"{Path.GetDirectoryName(outputPath)}\\{Path.GetFileNameWithoutExtension(outputPath)}_{timestamp}{Path.GetExtension(outputPath)}";
                workbook.SaveAs(outputPath);
                //Console.WriteLine("Excel File Generated at the FilePath: " + outputPath);

                return outputPath;
            }
        }
        catch (Exception ex)
        {
            //Console.WriteLine($"Error generating Excel file: {ex.Message} ,{ex.StackTrace}");
            string exeDir = AppDomain.CurrentDomain.BaseDirectory;
            exeDir = Path.Combine(exeDir, "ExportDataToExcel.log");
            File.AppendAllText(exeDir, $"Error generating Excel file: {ex.Message} ,{ex.StackTrace}" + Environment.NewLine);
            return string.Empty;
        }
    }
    static async Task<string> GetPDF(string pagename, string dateformat, string outputPath, string project, JArray metaDataArray, JArray listDataArray, List<string> skipFlds, Dictionary<string, string> fieldDictionary, Dictionary<string, string> dataTypeDictionary)
    {
        string timestamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");
        outputPath = Path.Combine(Path.GetDirectoryName(outputPath) ?? "",
                                   $"{Path.GetFileNameWithoutExtension(outputPath)}_{timestamp}{Path.GetExtension(outputPath)}");

        var visibleTypeDictionary = metaDataArray.ToDictionary(
            item => (string)item["fldname"],
            item => (string)item["hide"]);

        iTextSharp.text.Document document = new iTextSharp.text.Document(PageSize.A4, 50, 50, 50, 50);
        try
        {
            using var fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
            PdfWriter.GetInstance(document, fs);
            document.Open();

            var titleFont = FontFactory.GetFont(FontFactory.TIMES_ROMAN, 16, BaseColor.WHITE);
            var headerFont = FontFactory.GetFont(FontFactory.TIMES_ROMAN, 12, BaseColor.WHITE);
            var normalFont = FontFactory.GetFont(FontFactory.TIMES_ROMAN, 8);
            var backgroundColor = new BaseColor(61, 61, 61);

            void AddTitle(string text)
            {
                var table = new PdfPTable(1) { WidthPercentage = 100 };
                var cell = new PdfPCell(new Phrase(text, titleFont))
                {
                    BackgroundColor = backgroundColor,
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    Border = PdfPCell.NO_BORDER
                };
                table.AddCell(cell);
                document.Add(table);
            }

            AddTitle(project);
            AddTitle(pagename);

            bool isHeaderCreated = false;
            PdfPTable table = null;

            foreach (var listData in listDataArray)
            {
                var dataJsonArray = JArray.Parse((string)listData["data_json"]);

                foreach (JObject row in dataJsonArray)
                {
                    if (!isHeaderCreated)
                    {
                        var columns = row.Properties().Where(p =>
                            !skipFlds.Contains(p.Name) &&
                            visibleTypeDictionary.GetValueOrDefault(p.Name) == "F" &&
                            fieldDictionary.ContainsKey(p.Name)).ToList();

                        // Increase column count by 4 for the additional columns.
                        table = new PdfPTable(columns.Count + 4) { WidthPercentage = 100 };
                        table.SetWidths(Enumerable.Repeat(1f, columns.Count + 4).ToArray());

                        // Add original data columns
                        foreach (var column in columns)
                        {
                            table.AddCell(new PdfPCell(new Phrase(fieldDictionary[column.Name], headerFont))
                            {
                                BackgroundColor = backgroundColor
                            });
                        }
                        // Append the extra header columns in the desired order:
                        table.AddCell(new PdfPCell(new Phrase("Modified By", headerFont)) { BackgroundColor = backgroundColor });
                        table.AddCell(new PdfPCell(new Phrase("Modified On", headerFont)) { BackgroundColor = backgroundColor });
                        table.AddCell(new PdfPCell(new Phrase("Created By", headerFont)) { BackgroundColor = backgroundColor });
                        table.AddCell(new PdfPCell(new Phrase("Created On", headerFont)) { BackgroundColor = backgroundColor });

                        isHeaderCreated = true;
                    }

                    // Add the original data columns for the row
                    foreach (var item in row.Properties())
                    {
                        if (visibleTypeDictionary.GetValueOrDefault(item.Name) == "F")
                        {
                            var value = item.Value?.ToString() ?? string.Empty;
                            if (dataTypeDictionary.GetValueOrDefault(item.Name) == "d" && DateTime.TryParse(value, out var date))
                            {
                                value = date.ToString(dateformat);
                            }
                            table.AddCell(new PdfPCell(new Phrase(value, normalFont)));
                        }
                    }

                    // Append additional cells in the specified order.
                    table.AddCell(new PdfPCell(new Phrase(row["modifiedby"]?.ToString() ?? string.Empty, normalFont)));
                    table.AddCell(new PdfPCell(new Phrase(ParseDate(row["modifiedon"], dateformat), normalFont)));
                    table.AddCell(new PdfPCell(new Phrase(row["createdby"]?.ToString() ?? string.Empty, normalFont)));
                    table.AddCell(new PdfPCell(new Phrase(ParseDate(row["createdon"], dateformat), normalFont)));
                }
            }

            if (table != null)
            {
                document.Add(table);
            }

            document.Close();

            return outputPath;
        }
        catch (Exception ex)
        {
            //Console.WriteLine("Error generating PDF: " + ex.Message);
            string exeDir = AppDomain.CurrentDomain.BaseDirectory;
            exeDir = Path.Combine(exeDir, "ExportDataToExcel.log");
            File.AppendAllText(exeDir, "Error generating PDF: " + ex.Message + Environment.NewLine);
            throw;
        }

        static string ParseDate(JToken? token, string format)
        {
            return DateTime.TryParse(token?.ToString(), out var date) ? date.ToString($"{format} hh:mm:ss") : string.Empty;
        }
    }
    static async Task<string> GetWord(string pagename, string dateformat, string outputPath, string project, JArray metaDataArray, JArray listDataArray, List<string> skipFlds, Dictionary<string, string> fieldDictionary, Dictionary<string, string> dataTypeDictionary)
    {
        try
        {
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");
            outputPath = $"{Path.GetDirectoryName(outputPath)}\\{Path.GetFileNameWithoutExtension(outputPath)}_{timestamp}{Path.GetExtension(outputPath)}";

            var visibleTypeDictionary = metaDataArray.ToDictionary(item => (string)item["fldname"], item => (string)item["hide"]);

            XWPFDocument document = new XWPFDocument();

            // Title
            var titleParagraph = document.CreateParagraph();
            titleParagraph.Alignment = ParagraphAlignment.CENTER;
            var titleRun = titleParagraph.CreateRun();
            titleRun.IsBold = true;
            titleRun.SetText(project);
            titleRun.FontSize = 16;

            // Page Name
            var pageNameParagraph = document.CreateParagraph();
            pageNameParagraph.Alignment = ParagraphAlignment.CENTER;
            var pageNameRun = pageNameParagraph.CreateRun();
            pageNameRun.IsBold = true;
            pageNameRun.SetText(pagename);
            pageNameRun.FontSize = 14;

            // Date
            var dateParagraph = document.CreateParagraph();
            dateParagraph.Alignment = ParagraphAlignment.RIGHT;
            var dateRun = dateParagraph.CreateRun();
            dateRun.SetText("Date: " + DateTime.Now.ToString(dateformat));
            dateRun.FontSize = 12;

            bool isHeaderRowCreated = false;
            XWPFTable table = null;
            List<string> includedFields = new List<string>();

            foreach (var listData in listDataArray)
            {
                if (listData["data_json"] != null)
                {
                    var dataJsonArray = JArray.Parse(listData["data_json"]?.ToString() ?? string.Empty);

                    foreach (var row in dataJsonArray)
                    {
                        var rowObject = row as JObject;

                        // Create header row once
                        if (!isHeaderRowCreated)
                        {
                            // Extract fields dynamically, **EXCLUDING** `createdby`, `createdon`, `modifiedby`, `modifiedon`
                            includedFields = rowObject.Properties()
                                .Select(p => p.Name)
                                .Where(f =>
                                    f != "createdby" && f != "createdon" &&
                                    f != "modifiedby" && f != "modifiedon" &&  // ✅ Exclude from dynamic fields
                                    !skipFlds.Contains(f) &&
                                    (!visibleTypeDictionary.ContainsKey(f) || visibleTypeDictionary[f] != "T"))
                                .ToList();

                            // ✅ Append `modifiedby`, `modifiedon`, `createdby`, `createdon` **ONLY at the end**
                            includedFields.Add("modifiedby");
                            includedFields.Add("modifiedon");
                            includedFields.Add("createdby");
                            includedFields.Add("createdon");

                            table = document.CreateTable(1, includedFields.Count);
                            var headerRow = table.GetRow(0);

                            for (int i = 0; i < includedFields.Count; i++)
                            {
                                string fieldName = includedFields[i];
                                string displayName = fieldDictionary.ContainsKey(fieldName) ? fieldDictionary[fieldName] : fieldName;

                                var cell = headerRow.GetCell(i) ?? headerRow.CreateCell();
                                cell.SetText(displayName);
                                var paragraph = cell.Paragraphs[0];
                                paragraph.Alignment = ParagraphAlignment.CENTER;
                                var run = paragraph.CreateRun();
                                run.IsBold = true;
                            }

                            isHeaderRowCreated = true;
                        }

                        // Create a new row
                        var dataRow = table.CreateRow();

                        for (int i = 0; i < includedFields.Count; i++)
                        {
                            string fieldName = includedFields[i];
                            string cellValue = "";

                            // **Ensure `modifiedby`, `modifiedon`, `createdby`, `createdon` come only at the end**
                            if (rowObject.ContainsKey(fieldName))
                            {
                                cellValue = rowObject[fieldName]?.ToString() ?? string.Empty;
                            }

                            // Handle date format if necessary
                            if (dataTypeDictionary.ContainsKey(fieldName) && dataTypeDictionary[fieldName] == "d" &&
                                DateTime.TryParse(cellValue, out var dateValue))
                            {
                                cellValue = dateValue.ToString(dateformat);
                            }

                            var cell = dataRow.GetCell(i) ?? dataRow.CreateCell();
                            cell.SetText(cellValue);
                        }
                    }
                }
            }

            SetTableBorders(table);

            // Save document
            using (FileStream fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
            {
                document.Write(fs);
            }

            return outputPath;
        }
        catch (Exception ex)
        {
            return $"Error generating Word file: {ex.Message}";
        }
    }
    static void SetTableBorders(XWPFTable table)
    {
        var tblBorders = table.GetCTTbl().tblPr.tblBorders;
        if (tblBorders == null)
        {
            tblBorders = table.GetCTTbl().tblPr.AddNewTblBorders();
        }

        tblBorders.top = new CT_Border { val = ST_Border.single, sz = 4, space = 0 };
        tblBorders.left = new CT_Border { val = ST_Border.single, sz = 4, space = 0 };
        tblBorders.bottom = new CT_Border { val = ST_Border.single, sz = 4, space = 0 };
        tblBorders.right = new CT_Border { val = ST_Border.single, sz = 4, space = 0 };
        tblBorders.insideH = new CT_Border { val = ST_Border.single, sz = 4, space = 0 };
        tblBorders.insideV = new CT_Border { val = ST_Border.single, sz = 4, space = 0 };
    }

}
class ColDef
{
    public string Name;
    public string Caption;
    public bool Hidden;
    public string Align;
    public string dType;
}