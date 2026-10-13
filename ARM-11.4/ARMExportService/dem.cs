//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace ARMExportService2
//{
//    internal class dem
//    {
//    }
//}
//using ARMCommon.Helpers;
//using ARMCommon.Helpers.RabbitMq;
//using ARMCommon.Interface;
//using ARMCommon.Model;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.DependencyInjection;
//using Newtonsoft.Json;
//using Newtonsoft.Json.Linq;
//using static Org.BouncyCastle.Math.EC.ECCurve;
//using System.Data;
//using Microsoft.AspNetCore.Http;
//using System.Security.Policy;
//using System.Net;
//using System.Xml.Linq;
//using System.Xml;
//using Microsoft.VisualBasic;
//using NPOI.SS.UserModel;
//using System.Collections;
//using System.Globalization;
//using System.Text.RegularExpressions;
//using System.Text;
//using HtmlAgilityPack;
//using ClosedXML.Excel;
//using NPOI.SS.Util;
//using System.DirectoryServices.Protocols;
//using Microsoft.AspNetCore.Routing.Constraints;
//using DocumentFormat.OpenXml.Office2010.Excel;
//using System.Data.Common;
//using System.Globalization;

//namespace ARMServices
//{
//    class Program
//    {
//        static void Main(string[] args)
//        {

//            var appSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
//            var json = File.ReadAllText(appSettingsPath);
//            dynamic config = JsonConvert.DeserializeObject(json);
//            string queueName = config.AppConfig["QueueName"];
//            string signalrUrl = config.AppConfig["SignalRURL"];
//            string fileServerPath = string.Empty;
//            string exportExcelPath = config.AppConfig["ExportPath"];
//            var builder = new ConfigurationBuilder()
//                .SetBasePath(Directory.GetCurrentDirectory())
//                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

//            var configuration = builder.Build();

//            var serviceCollection = new ServiceCollection();

//            serviceCollection.AddSingleton(configuration);
//            serviceCollection.AddSingleton<IRabbitMQConsumer, RabbitMQConsumer>();
//            serviceCollection.AddSingleton<IRabbitMQProducer, RabbitMQProducer>();
//            serviceCollection.AddSingleton<IConfiguration>(configuration);
//            var serviceProvider = serviceCollection.BuildServiceProvider();

//            var rabbitMQConsumer = serviceProvider.GetService<IRabbitMQConsumer>();
//            var rabbitMQProducer = serviceProvider.GetService<IRabbitMQProducer>();
//            async Task<string> OnConsuming(string message)
//            {
//                string _excelFilePath = string.Empty;
//                //Export logic here
//                #region excellogic
//                //Console.WriteLine("Before Restservice:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
//                JObject jsonObjExcel = JObject.Parse(message);
//                string DATA = jsonObjExcel["queuedata"].ToString();
//                JObject jsonObjExcelData = JObject.Parse(DATA);

//                signalrUrl = jsonObjExcelData["getreport"]["arm_url"].ToString() + "/api/v1/SendSignalR";

//                string axConfigFilePath = jsonObjExcelData["getreport"]["axConfigFilePath"].ToString();
//                if (axConfigFilePath != string.Empty)
//                {
//                    exportExcelPath = axConfigFilePath;
//                    fileServerPath = string.Empty;
//                }
//                else
//                {
//                    exportExcelPath = config.AppConfig["ExportPath"];
//                    fileServerPath = jsonObjExcelData["getreport"]["ARMScriptURL"].ToString();
//                }
//                string _ParamsCaption = jsonObjExcelData["getreport"]["ivParamCaption"].ToString();
//                jsonObjExcelData["getreport"]["ivParamCaption"] = "";
//                string _params = jsonObjExcelData["getreport"]["params"].ToString();
//                string userName = jsonObjExcelData["getreport"]["userauthkey"].ToString();
//                string _ivName = jsonObjExcelData["getreport"]["name"].ToString();
//                string _project = jsonObjExcelData["getreport"]["project"].ToString();
//                string _ivcaption = jsonObjExcelData["getreport"]["ivcaption"].ToString();
//                string dateformat = jsonObjExcelData["getreport"]["_dateformat"].ToString();
//                AxpertRestAPIToken axpertRestAPIToken = new AxpertRestAPIToken(userName);
//                UpdateOrAddJsonKey(ref jsonObjExcelData, "userauthkey", axpertRestAPIToken.userAuthKey);
//                UpdateOrAddJsonKey(ref jsonObjExcelData, "seed", axpertRestAPIToken.seed);
//                UpdateOrAddJsonKey(ref jsonObjExcelData, "token", axpertRestAPIToken.token);
//                string _RKey = string.Empty;

//                var redis = new RedisHelper(configuration);
//                var context = new ARMCommon.Helpers.DataContext(configuration);
//                Utils utils = new Utils(configuration, context, redis);
//                Dictionary<string, string> configcon = await utils.GetDBConfigurations(_project);
//                string connectionString = configcon["ConnectionString"];
//                string dbType = configcon["DBType"];

//                Dictionary<string, string> connectionStringParts = new Dictionary<string, string>();
//                string[] parts = connectionString.Split(';');
//                foreach (var part in parts)
//                {
//                    if (!string.IsNullOrEmpty(part))
//                    {
//                        string[] keyValue = part.Split('=');
//                        if (keyValue.Length == 2)
//                        {
//                            connectionStringParts[keyValue[0].Trim()] = keyValue[1].Trim();
//                        }
//                    }
//                }
//                string _paramsnode = string.Empty;
//                if (_params != "{}")
//                {
//                    JObject json = JObject.Parse(_params);
//                    foreach (var property in json.Properties())
//                    {
//                        if (_paramsnode.Length > 0)
//                        {
//                            _paramsnode += "~";
//                        }
//                        _paramsnode += $"{property.Name}♠{property.Value}";
//                    }
//                }


//                if (dbType == "postgresql" || dbType == "postgre")
//                {
//                    string database = connectionStringParts.ContainsKey("Database") ? connectionStringParts["Database"] : "Not found";
//                    string dbusername = connectionStringParts.ContainsKey("Username") ? connectionStringParts["Username"] : "Not found";
//                    if (_paramsnode == string.Empty)
//                        _RKey = dbusername + "~" + database + "-" + _ivName + "-ivarmexportexcel-" + userName;
//                    else
//                        _RKey = dbusername + "~" + database + "-" + _ivName + "-ivarmexportexcel-" + userName + "-" + _paramsnode;
//                }
//                else if (dbType.ToLower() == "oracle" || dbType.ToLower() == "ms sql" || dbType.ToLower() == "mssql")
//                {
//                    string database = connectionStringParts.ContainsKey("User Id") ? connectionStringParts["User Id"] : "Not found";
//                    if (_paramsnode == string.Empty)
//                        _RKey = database + "-" + _ivName + "-ivarmexportexcel-" + userName;
//                    else
//                        _RKey = database + "-" + _ivName + "-ivarmexportexcel-" + userName + "-" + _paramsnode;
//                }
//                else
//                {
//                    string database = connectionStringParts.ContainsKey("Database") ? connectionStringParts["Database"] : "Not found";
//                    if (_paramsnode == string.Empty)
//                        _RKey = database + "-" + _ivName + "-ivarmexportexcel-" + userName;
//                    else
//                        _RKey = database + "-" + _ivName + "-ivarmexportexcel-" + userName + "-" + _paramsnode;
//                }

//                await redis.StringSetAsync(_RKey, "requestinprocess", int.Parse(config.AppConfig["ExportKeyExpiryTime"].Value));

//                //string ExportType = jsonObjExcelData["getreport"]["ExportType"].ToString();

//                DATA = jsonObjExcelData.ToString();
//                string _urlarm = jsonObjExcelData["getreport"]["ARMScriptURL"].ToString();
//                string URL = _urlarm + "ASBIviewRest.dll/datasnap/rest/TASBIViewREST/GetReport";
//                //string URL = config.AppConfig["ExportIViewAPI"];
//                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(URL);
//                request.Method = "POST";
//                request.ContentType = "application/json";
//                StreamWriter requestWriter = new StreamWriter(request.GetRequestStream(), System.Text.Encoding.ASCII);
//                requestWriter.Write(DATA);
//                requestWriter.Close();
//                string response = string.Empty;
//                try
//                {
//                    request.Timeout = Timeout.Infinite;
//                    request.KeepAlive = true;

//                    WebResponse webResponse = request.GetResponse();
//                    Stream webStream = webResponse.GetResponseStream();
//                    StreamReader responseReader = new StreamReader(webStream);
//                    response = responseReader.ReadToEnd();
//                    Console.Out.WriteLine(response);
//                    responseReader.Close();
//                    //Console.WriteLine("After Restservice:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
//                }
//                catch (Exception e)
//                {
//                    await redis.KeyDeleteAsync(_RKey);
//                    Console.WriteLine("Web service Error:" + e.Message);
//                }
//                if (response != string.Empty)
//                {
//                    //Console.WriteLine("Before json to XML:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
//                    JObject jsonObject = JObject.Parse(response);
//                    JArray resultArray = (JArray)jsonObject["result"];
//                    XDocument xmlDocument = new XDocument();
//                    XElement root = new XElement("result");
//                    foreach (JObject item in resultArray)
//                    {
//                        string status = (string)item["status"];
//                        if (status == "failed")
//                        {
//                            await redis.KeyDeleteAsync(_RKey);
//                            Console.WriteLine("Web service res:" + response);
//                            return "";
//                        }
//                        JObject result = (JObject)item["result"];
//                        XDocument resultXml = JsonConvert.DeserializeXNode(result.ToString(), "data");
//                        XElement rootElement = resultXml.Root;
//                        root.Add(rootElement);
//                    }
//                    xmlDocument.Add(root);
//                    string ires = xmlDocument.ToString();
//                    ires = ires.Replace("<result>\r\n  ", "").Replace("</result>", "");

//                    // Load XML content
//                    XmlDocument xmlDoc = new XmlDocument();
//                    xmlDoc.LoadXml(ires);
//                    string datarows = "";
//                    string totalrows = "";
//                    string reccount = "";
//                    XmlNodeList nodesToRemove = xmlDoc.SelectNodes("//reccount | //totalrows | //datarows");
//                    foreach (XmlNode node in nodesToRemove)
//                    {
//                        if (node.Name == "datarows")
//                            datarows = node.InnerText;
//                        if (node.Name == "totalrows")
//                            totalrows = node.InnerText;
//                        if (node.Name == "reccount")
//                            reccount = node.InnerText;

//                        node.ParentNode.RemoveChild(node);
//                    }

//                    // Select the first metadata node
//                    XmlNode metadataNode = xmlDoc.SelectSingleNode("//metadata");
//                    if (metadataNode != null)
//                    {
//                        // Create a new element named headrow
//                        XmlElement newElement = xmlDoc.CreateElement("headrow");
//                        // Copy all attributes
//                        foreach (XmlAttribute attribute in metadataNode.Attributes)
//                        {
//                            newElement.Attributes.Append((XmlAttribute)attribute.CloneNode(true));
//                        }
//                        // Copy all child nodes
//                        foreach (XmlNode childNode in metadataNode.ChildNodes)
//                        {
//                            newElement.AppendChild(childNode.CloneNode(true));
//                        }
//                        // Replace the old node with the new node
//                        metadataNode.ParentNode.ReplaceChild(newElement, metadataNode);
//                    }

//                    ires = xmlDoc.OuterXml;

//                    XDocument doc = XDocument.Parse(@ires);
//                    doc.Descendants("axrowtype").Remove();
//                    doc.Descendants("axp__font").Remove();

//                    ires = doc.ToString();

//                    //Console.WriteLine("After json to XML:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
//                    //Console.WriteLine("Before excel:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
//                    _excelFilePath = GetExcel(ires, datarows, totalrows, reccount, exportExcelPath, _ivName, _ParamsCaption, userName, dateformat, _ivcaption);
//                    //Console.WriteLine("After excel:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
//                }
//                #endregion

//                await redis.KeyDeleteAsync(_RKey);
//                if (_excelFilePath != "" && _excelFilePath != "IView Result Error")
//                {
//                    //Console.WriteLine("Before notification:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
//                    string[] _fielPaths = _excelFilePath.Split(',');
//                    try
//                    {
//                        string sql = Constants_SQL.INSERT_TO_AXACTIVEMESG; //Insert statement here.
//                        string _filefullpath = _fielPaths[0].Replace(@"\", "\\\\");
//                        if (fileServerPath != string.Empty)
//                        {
//                            _filefullpath = fileServerPath + "/" + "Exports/" + userName + "/" + _fielPaths[1];
//                        }
//                        string currentTime = DateTime.Now.ToString("yyyyMMddHHmmssfff");
//                        sql = string.Format(sql, currentTime, "export excel1", userName, userName, "export", "export excel2", "export excel-" + _ivName, _ivName, "export excel generated successfully", _ivcaption, _filefullpath);
//                        IDbHelper dbHelper = DBHelper.CreateDbHelper(dbType);
//                        var result = await dbHelper.ExecuteQueryAsync(sql, connectionString);
//                        string _Payload = "[{\"notifytype\":\"export excel\",\"type\":\"export\",\"icon\":\"\",\"title\":\"Export Excel\",\"message\":\"" + _ivcaption + "\",\"dt\":\"" + DateTime.Now.ToString() + "\",\"link\":{\"t\":\"e\",\"name\":\"" + _filefullpath + "\",\"p\":\"\",\"act\":\"\",\"axmsgid\":\"\"}}]";

//                        await SendSignalRMessage(_project, userName, _Payload);
//                        //Console.WriteLine("After notification:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
//                        return "Success";
//                    }
//                    catch (Exception ex)
//                    {
//                        await redis.KeyDeleteAsync(_RKey);
//                        Console.WriteLine("Error:" + ex.Message);
//                        return null;
//                    }
//                }
//                return "";
//            }
//            static void UpdateOrAddJsonKey(ref JObject jsonObject, string key, string value)
//            {
//                if (jsonObject["getreport"] is JObject getReport)
//                {
//                    getReport[key] = value;
//                }
//            }
//            static void WriteMessage(string message)
//            {
//                Console.WriteLine(DateTime.Now.ToString() + " - " + message);
//            }
//            rabbitMQConsumer.DoConsume(queueName, OnConsuming);

//            static string GetExcel(string ires, string datarows, string totalrows, string reccount, string exportExcelPath, string _ivName, string _params, string _userName, string dateformat, string _ivcaption)
//            {
//                string _returnRes = string.Empty;
//                StringBuilder sb = new StringBuilder();
//                string exportVerticalAlignStyle = "vertical-align: middle";
//                string ivtype = "Iview";//listview
//                ArrayList colHide = new ArrayList();
//                ArrayList colFld = new ArrayList();
//                ArrayList colHead = new ArrayList();
//                ArrayList colType = new ArrayList();
//                ArrayList colDec = new ArrayList();
//                ArrayList htmlColumns = new ArrayList();

//                if (ires != string.Empty)
//                {
//                    var xmlDoc = new XmlDocument();
//                    xmlDoc.LoadXml(ires);
//                    XmlNodeList productNodes;
//                    XmlNodeList baseDataNodes;
//                    productNodes = xmlDoc.SelectNodes("//headrow");
//                    int hcolNos;
//                    hcolNos = 0;

//                    //Console.WriteLine("Before header:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
//                    foreach (XmlNode productNode in productNodes)
//                    {
//                        baseDataNodes = productNode.ChildNodes;
//                        int iCount = -1;
//                        foreach (XmlNode baseDataNode in baseDataNodes)
//                        {
//                            if (baseDataNode.Name == "axrowtype" || baseDataNode.Name == "axp__font")
//                            {
//                                continue;
//                            }
//                            iCount += 1;
//                            if (baseDataNode.Name == "pivotghead")
//                            {
//                            }
//                            else if (baseDataNode.Name == "rowno")
//                            {
//                                baseDataNode.InnerText = "Sr. No.";
//                                colFld.Add(baseDataNode.Name);
//                                if (ivtype == "lview")
//                                {
//                                    colHide.Add("false");
//                                    colHead.Add("Sr. No.");
//                                    colType.Add("c");
//                                }
//                                else
//                                {
//                                    colHide.Add("true");
//                                    colHead.Add(baseDataNode.InnerText);
//                                    colType.Add("");
//                                }


//                                colDec.Add("");
//                            }
//                            else
//                            {
//                                if (baseDataNode.Name.StartsWith("html_"))
//                                {
//                                    htmlColumns.Add(baseDataNode.Name);
//                                }
//                                colFld.Add(baseDataNode.Name);
//                                colHead.Add(baseDataNode.InnerXml.Replace("<hide>true</hide>", "").Replace("<hide>false</hide>", ""));

//                                if (baseDataNode.Name.StartsWith("hide_"))
//                                {
//                                    colHide.Add("true");
//                                }
//                                else if (ivtype != "listview" & ivtype != "lview")
//                                {
//                                    colHide.Add(baseDataNode.Attributes["hide"].Value);
//                                }
//                                else
//                                {
//                                    colHide.Add(baseDataNode.Attributes["hide"].Value);
//                                }
//                                colType.Add(baseDataNode.Attributes["type"].Value);
//                                colDec.Add(baseDataNode.Attributes["dec"].Value);
//                                hcolNos = hcolNos + 1;
//                            }
//                        }
//                    }
//                    //Console.WriteLine("After header:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
//                    //Console.WriteLine("Before reporthf :" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
//                    int htIdx = 0;
//                    int cIdx = 0;
//                    var loopTo = htmlColumns.Count - 1;
//                    for (htIdx = 0; htIdx <= loopTo; htIdx++)
//                    {
//                        var loopTo1 = colFld.Count - 1;
//                        for (cIdx = 0; cIdx <= loopTo1; cIdx++)
//                        {
//                            string htmlColName = htmlColumns[htIdx].ToString();
//                            if (colFld[cIdx] == htmlColName)
//                            {
//                                colHide[cIdx] = "false";
//                            }
//                            else if (colFld[cIdx] == htmlColName.Replace("html_", ""))
//                            {
//                                colHide[cIdx] = "true";
//                            }

//                        }
//                    }

//                    // Get name from comps
//                    string iviewcap = "";
//                    string iviewAddCaption = "";
//                    string iviewAddFooter = "";
//                    var xmlDoc1 = new XmlDocument();
//                    XmlNodeList compNodes;
//                    XmlNodeList cbaseDataNodes;

//                    xmlDoc1.LoadXml(ires);
//                    compNodes = xmlDoc1.SelectNodes("//data/reporthf");
//                    int lblcnt = 0;
//                    int lblfcnt = 0;
//                    ArrayList hdrFont = new ArrayList();
//                    ArrayList hdrAlign = new ArrayList();

//                    ArrayList hdrFooterName = new ArrayList();
//                    ArrayList hdrFooterAlign = new ArrayList();
//                    ArrayList hdrFooterFont = new ArrayList();

//                    string _dateNodehide = "f";
//                    foreach (XmlNode compNode in compNodes)
//                    {
//                        cbaseDataNodes = compNode.ChildNodes;
//                        foreach (XmlNode cbaseDataNode in cbaseDataNodes)
//                        {
//                            if (Strings.Mid(cbaseDataNode.Name, 1, 7) == "x__head")
//                            {
//                                iviewcap = cbaseDataNode.Attributes["caption"].Value;
//                            }
//                            else if (Strings.Mid(cbaseDataNode.Name, 1, 3) == "lbl")
//                            {
//                                lblcnt += 1;
//                                if (string.IsNullOrEmpty(iviewAddCaption))
//                                {
//                                    iviewAddCaption = cbaseDataNode.Attributes["caption"].Value;
//                                }
//                                else
//                                {
//                                    iviewAddCaption = iviewAddCaption + "*,*" + cbaseDataNode.Attributes["caption"].Value;
//                                }
//                            }
//                            else if (cbaseDataNode.Name == "header")
//                            {
//                                lblcnt += 1;
//                                foreach (XmlNode childHeaders in cbaseDataNode)
//                                {
//                                    if (string.IsNullOrEmpty(iviewAddCaption))
//                                    {
//                                        string xmlContent = childHeaders.OuterXml;
//                                        XmlDocument _xmlDoc = new XmlDocument();
//                                        _xmlDoc.LoadXml(xmlContent);
//                                        XmlNode textNode = _xmlDoc.SelectSingleNode("//text");
//                                        XmlNode textNodefont = _xmlDoc.SelectSingleNode("//font");
//                                        if (textNodefont != null)
//                                            hdrFont.Add(textNodefont.InnerText);
//                                        else
//                                            hdrFont.Add("");
//                                        XmlNode textNodeheader_aline = _xmlDoc.SelectSingleNode("//header_aline");
//                                        if (textNodeheader_aline != null)
//                                            hdrAlign.Add(textNodeheader_aline.InnerText);
//                                        else
//                                            hdrAlign.Add("");
//                                        iviewAddCaption = textNode.InnerText.Trim();
//                                    }
//                                    else
//                                    {
//                                        string xmlContent = childHeaders.OuterXml;
//                                        XmlDocument _xmlDoc = new XmlDocument();
//                                        _xmlDoc.LoadXml(xmlContent);
//                                        XmlNode textNode = _xmlDoc.SelectSingleNode("//text");
//                                        XmlNode textNodefont = _xmlDoc.SelectSingleNode("//font");
//                                        if (textNodefont != null)
//                                            hdrFont.Add(textNodefont.InnerText);
//                                        else
//                                            hdrFont.Add("");
//                                        XmlNode textNodeheader_aline = _xmlDoc.SelectSingleNode("//header_aline");
//                                        if (textNodeheader_aline != null)
//                                            hdrAlign.Add(textNodeheader_aline.InnerText);
//                                        else
//                                        {
//                                            hdrAlign.Add(hdrAlign[0]);
//                                        }
//                                        string _iviewAddCaption = textNode.InnerText.Trim();
//                                        iviewAddCaption = iviewAddCaption + "*,*" + _iviewAddCaption;
//                                    }
//                                }
//                            }
//                            else if (cbaseDataNode.Name == "hidereportdate")
//                            {
//                                _dateNodehide = cbaseDataNode.InnerText;
//                                if (_dateNodehide.StartsWith("@"))
//                                    _dateNodehide = _dateNodehide.Substring(1);
//                            }
//                            if (cbaseDataNode.Name == "footer")
//                            {
//                                lblfcnt += 1;
//                                foreach (XmlNode childFooters in cbaseDataNode)
//                                {
//                                    if (string.IsNullOrEmpty(iviewAddFooter))
//                                    {
//                                        iviewAddFooter = childFooters.InnerText;
//                                    }
//                                    else
//                                    {
//                                        iviewAddFooter = iviewAddFooter + "~" + childFooters.InnerText;
//                                    }

//                                    string xmlContent = childFooters.OuterXml;
//                                    XmlDocument _xmlDoc = new XmlDocument();
//                                    _xmlDoc.LoadXml(xmlContent);
//                                    XmlNode textNode = _xmlDoc.SelectSingleNode("//text");
//                                    hdrFooterName.Add(textNode.InnerText);
//                                    XmlNode textNodefont = _xmlDoc.SelectSingleNode("//font");
//                                    if (textNodefont != null)
//                                        hdrFooterFont.Add(textNodefont.InnerText);
//                                    else
//                                        hdrFooterFont.Add("");
//                                    XmlNode textNodeheader_aline = _xmlDoc.SelectSingleNode("//footer_aline");
//                                    if (textNodeheader_aline != null)
//                                        hdrFooterAlign.Add(textNodeheader_aline.InnerText);
//                                    else
//                                        hdrFooterAlign.Add(hdrFooterAlign[0]);
//                                }
//                            }
//                        }
//                    }
//                    string _hdrNames = iviewAddCaption;
//                    if (lblcnt <= 3)
//                    {
//                        iviewAddCaption += "*,*";
//                    }

//                    ArrayList hdrNames = new ArrayList();
//                    if (_hdrNames != "")
//                    {
//                        foreach (var ivAc in _hdrNames.Split("*,*"))
//                        {
//                            hdrNames.Add(ivAc);
//                        }
//                    }
//                    XmlNodeList reporthf = xmlDoc.SelectNodes("//reporthf");
//                    foreach (XmlNode node in reporthf)
//                    {
//                        node.ParentNode.RemoveChild(node);
//                    }
//                    // to remove attributes from headrow
//                    XmlNodeList productNodes2;
//                    XmlNodeList baseDataNodes2;
//                    productNodes2 = xmlDoc.SelectNodes("//headrow");

//                    string projectName = string.Empty;
//                    string resparamVal = string.Empty;
//                    ArrayList _ivParamCap = new ArrayList();
//                    if (_params != "" && _params != "{}")
//                    {
//                        JObject jsonObject = JObject.Parse(_params);
//                        foreach (var pnod in jsonObject)
//                        {
//                            _ivParamCap.Add(pnod.Key + ":" + pnod.Value);
//                        }
//                    }


//                    XmlAttribute nAppTitle;
//                    nAppTitle = xmlDoc.CreateAttribute("projectName");
//                    nAppTitle.Value = projectName;

//                    XmlAttribute nCaption;
//                    nCaption = xmlDoc.CreateAttribute("caption");
//                    nCaption.Value = iviewcap;


//                    XmlAttribute crdate;
//                    crdate = xmlDoc.CreateAttribute("crdate");
//                    crdate.Value = DateTime.Now.ToString();
//                    var time = DateTime.Now;
//                    string format = dateformat;
//                    crdate.Value = time.ToString(format, CultureInfo.InvariantCulture);

//                    XmlAttribute paramnode;
//                    paramnode = xmlDoc.CreateAttribute("params");
//                    paramnode.Value = resparamVal;

//                    XmlAttribute footNode;
//                    footNode = xmlDoc.CreateAttribute("footer");
//                    footNode.Value = iviewAddFooter;

//                    XmlAttribute vtype;
//                    vtype = xmlDoc.CreateAttribute("vtype");
//                    vtype.Value = "c";

//                    XmlAttribute tmptotalrows;
//                    tmptotalrows = xmlDoc.CreateAttribute("tmptotalrows");
//                    tmptotalrows.Value = "";

//                    XmlAttribute xldatarows;
//                    xldatarows = xmlDoc.CreateAttribute("datarows");
//                    xldatarows.Value = datarows;

//                    XmlAttribute xltotalrows;
//                    xltotalrows = xmlDoc.CreateAttribute("totalrows");
//                    xltotalrows.Value = totalrows;

//                    XmlAttribute xlreccount;
//                    xlreccount = xmlDoc.CreateAttribute("reccount");
//                    xlreccount.Value = reccount;

//                    foreach (XmlNode productNode2 in productNodes2)
//                    {
//                        productNode2.Attributes.Append(nCaption);
//                        productNode2.Attributes.Append(crdate);
//                        productNode2.Attributes.Append(paramnode);
//                        productNode2.Attributes.Append(footNode);
//                        productNode2.Attributes.Append(vtype);
//                        productNode2.Attributes.Append(tmptotalrows);
//                        productNode2.Attributes.Append(xldatarows);
//                        productNode2.Attributes.Append(xltotalrows);
//                        productNode2.Attributes.Append(xlreccount);

//                        baseDataNodes2 = productNode2.ChildNodes;
//                        foreach (XmlNode baseDataNode2 in baseDataNodes2)
//                            baseDataNode2.Attributes.RemoveAll();
//                    }


//                    // Add Headers
//                    // If Not objIview.ReportHdrs Is Nothing And objIview.ReportHdrs.Count > 0 Then
//                    XmlElement printHeaders = xmlDoc.CreateElement("printHeaders");
//                    var printHeadersString = new StringBuilder();

//                    printHeadersString.Append("<headerData></headerData>");

//                    printHeaders.InnerXml = printHeadersString.ToString();
//                    xmlDoc.DocumentElement.PrependChild(printHeaders);
//                    // End If

//                    //Console.WriteLine("After reporthf :" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
//                    //Console.WriteLine("Before headrow :" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
//                    string pivotXml = "";
//                    int hrep;
//                    var loopTo3 = hcolNos;
//                    for (hrep = 1; hrep <= loopTo3; hrep++)
//                    {
//                        XmlNodeList productNodes3;
//                        XmlNodeList baseDataNodes3;
//                        productNodes3 = xmlDoc.SelectNodes("//headrow");
//                        int hidx;

//                        foreach (XmlNode productNode3 in productNodes3)
//                        {
//                            baseDataNodes3 = productNode3.ChildNodes;
//                            foreach (XmlNode baseDataNode3 in baseDataNodes3)
//                            {
//                                if (baseDataNode3.Name == "axrowtype" || baseDataNode3.Name == "axp__font")
//                                {
//                                    continue;
//                                }
//                                string nodeString = baseDataNode3.InnerXml.Replace("<hide>true</hide>", "").Replace("<hide>false</hide>", "").Replace("~", "<br/>");
//                                if (baseDataNode3.Name != "pivotghead")
//                                {
//                                    baseDataNode3.InnerText = nodeString;
//                                }

//                                if (baseDataNode3.Name == "pivotghead")
//                                {
//                                    pivotXml = baseDataNode3.InnerXml;
//                                    baseDataNode3.ParentNode.RemoveChild(baseDataNode3);
//                                }
//                                else
//                                {
//                                    var loopTo4 = colFld.Count - 1;
//                                    for (hidx = 0; hidx <= loopTo4; hidx++)
//                                    {
//                                        if (colFld[hidx] == baseDataNode3.Name)
//                                        {
//                                            if (colHide[hidx].ToString() == "true")
//                                            {
//                                                baseDataNode3.ParentNode.RemoveChild(baseDataNode3);
//                                            }
//                                        }
//                                    }
//                                }
//                            }
//                        }
//                    }

//                    // Process Pivot
//                    pivotXml = "<pivot>" + pivotXml + "</pivot>";
//                    var pxmlDoc = new XmlDocument();
//                    pxmlDoc.LoadXml(pivotXml);

//                    XmlNodeList ptproductNodes;
//                    XmlNodeList ptbaseDataNodes;
//                    ptproductNodes = pxmlDoc.SelectNodes("//head");

//                    string newPXml = "";
//                    foreach (XmlNode ptproductNode in ptproductNodes)
//                    {
//                        string snno = ptproductNode.Attributes["snno"].Value;
//                        string enno = ptproductNode.Attributes["enno"].Value;
//                        int cspan = int.Parse(enno) - int.Parse(snno) + 1;
//                        newPXml = newPXml + "<head>";
//                        ptbaseDataNodes = ptproductNode.ChildNodes;
//                        foreach (XmlNode ptbaseDataNode in ptbaseDataNodes)
//                        {
//                            if (ptbaseDataNode.Name == "ghead")
//                            {
//                                ptbaseDataNode.InnerText = CheckSpecialChars(ptbaseDataNode.InnerText);
//                                ptbaseDataNode.InnerText = ptbaseDataNode.InnerText.Replace("~", "<br/>");
//                                newPXml = newPXml + "<ghead class=\"gridHeader\"  colspan=\"" + cspan + "\">" + ptbaseDataNode.InnerText + "</ghead>";
//                            }
//                        }
//                        newPXml = newPXml + "</head>";
//                    }
//                    // Add pivot node
//                    XmlElement pivotNode = xmlDoc.CreateElement("pivot");
//                    if (!string.IsNullOrEmpty(newPXml))
//                    {
//                        pivotNode.InnerXml = newPXml;
//                    }
//                    else
//                    {
//                        pivotNode.InnerXml = "<head><ghead class=\"\" /></head>";
//                    }

//                    xmlDoc.DocumentElement.PrependChild(pivotNode);

//                    //Console.WriteLine("After headrow :" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
//                    //Console.WriteLine("Before row :" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
//                    // To set class
//                    XmlNodeList productNodes5;
//                    XmlNodeList baseDataNodes5;
//                    productNodes5 = xmlDoc.SelectNodes("//row");
//                    foreach (XmlNode productNode5 in productNodes5)
//                    {
//                        baseDataNodes5 = productNode5.ChildNodes;
//                        string rowclass = "searchresultitem";
//                        string colalignvalue = "";
//                        foreach (XmlNode baseDataNode5 in baseDataNodes5)
//                        {
//                            string axrtype = "";
//                            string dsa1 = baseDataNode5.Name.ToString();
//                            string dfs1 = baseDataNode5.InnerText.ToString();
//                            if (baseDataNode5.Name.ToString().ToLower() == "axrowtype" & baseDataNode5.InnerText.ToString() != "")
//                            {
//                                axrtype = baseDataNode5.InnerText.ToString();
//                            }
//                            if (!string.IsNullOrEmpty(axrtype))
//                            {
//                                rowclass = axrtype;
//                            }
//                            colalignvalue = "left";
//                            XmlAttribute classnode;
//                            classnode = xmlDoc.CreateAttribute("class");
//                            classnode.Value = (rowclass + "-" + colalignvalue).ToLower();
//                            baseDataNode5.Attributes.Append(classnode);
//                        }
//                    }
//                    // Format numeric fields
//                    XmlNodeList productNodesNum;
//                    XmlNodeList baseDataNodesNum;
//                    productNodesNum = xmlDoc.SelectNodes("//row");
//                    //Console.WriteLine("After row :" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
//                    //Console.WriteLine("Before row1 :" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
//                    int idxx;

//                    var val = default(double);
//                    foreach (XmlNode productNodeNum in productNodesNum)
//                    {
//                        baseDataNodesNum = productNodeNum.ChildNodes;
//                        foreach (XmlNode baseDataNodeNum in baseDataNodesNum)
//                        {
//                            var loopTo5 = colFld.Count - 1;
//                            for (idxx = 0; idxx <= loopTo5; idxx++)
//                            {
//                                if (colFld[idxx] == baseDataNodeNum.Name.ToLower())
//                                {
//                                    if (colType[idxx] == "n" & baseDataNodeNum.InnerText != "" & colDec[idxx] != "0")
//                                    {
//                                        string flvalue = baseDataNodeNum.InnerText.ToString();
//                                        char firstChar = Convert.ToChar(flvalue.Substring(0, 1));
//                                        char lastChar = Convert.ToChar(flvalue.Substring(flvalue.Length - 1, 1));
//                                        bool isNumber = char.IsDigit(firstChar);
//                                        if (!isNumber & firstChar.ToString() == "(" & lastChar.ToString() == ")")
//                                        {
//                                            flvalue = flvalue.Substring(1, flvalue.Length - 2);
//                                            val = double.Parse(flvalue);
//                                        }
//                                        else if (flvalue == "&nbsp;")
//                                        {
//                                            baseDataNodeNum.InnerText = "";
//                                        }
//                                        else if (!isNumber)
//                                        {
//                                            flvalue = flvalue.Substring(1);
//                                            val = double.Parse(flvalue);
//                                        }
//                                        else
//                                        {
//                                            val = double.Parse(flvalue);
//                                        }
//                                        if (!isNumber & firstChar.ToString() == "(" & lastChar.ToString() == ")")
//                                        {
//                                            baseDataNodeNum.InnerText = Convert.ToString(firstChar) + val.ToString("N" + colDec[idxx], CultureInfo.InvariantCulture) + Convert.ToString(lastChar);
//                                        }
//                                        else
//                                        {
//                                            baseDataNodeNum.InnerText = (isNumber ? "" : Convert.ToString(firstChar)) + val.ToString("N" + colDec[idxx], CultureInfo.InvariantCulture);
//                                        }
//                                    }
//                                    else if (colType[idxx] == "c" & baseDataNodeNum.InnerText != "")
//                                    {
//                                        string nodeValue = baseDataNodeNum.InnerText;
//                                        if (Information.IsNumeric(nodeValue))
//                                        {
//                                            int indexOfDecimalPoint = nodeValue.IndexOf(".");
//                                            var regex = new Regex("[0-9.-]*(e|E)[0-9.-]*");
//                                            Match match = regex.Match(baseDataNodeNum.InnerText);
//                                            if (indexOfDecimalPoint > -1)
//                                            {
//                                                int numberOfDecimals = nodeValue.Substring(indexOfDecimalPoint + 1).Length;
//                                                string decimalFormat = "";
//                                                for (int j = 1, loopTo6 = numberOfDecimals; j <= loopTo6; j++)
//                                                    decimalFormat += "0";
//                                                baseDataNodeNum.InnerText = "=TEXT(\"" + baseDataNodeNum.InnerText + "\",\"0." + decimalFormat + "\")";
//                                            }
//                                            else if (match.Success & match.Index == 0 | baseDataNodeNum.InnerText.StartsWith("0") | baseDataNodeNum.InnerText.Length > 15)
//                                            {
//                                                baseDataNodeNum.InnerText = "&nbsp;" + baseDataNodeNum.InnerText + "";
//                                            }
//                                            else
//                                            {
//                                                baseDataNodeNum.InnerText = "=TEXT(\"" + baseDataNodeNum.InnerText + "\",\"0\")";
//                                            }
//                                        }
//                                        else
//                                        {
//                                            baseDataNodeNum.InnerText = baseDataNodeNum.InnerText.Replace("~", "<br/>");
//                                        }
//                                    }
//                                }
//                            }
//                        }
//                    }

//                    //Console.WriteLine("After row1 :" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
//                    //Console.WriteLine("Before remove hidden fields :" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
//                    // remove hidden fields
//                    int rep;
//                    // change  later try new code to remove this for
//                    var loopTo7 = hcolNos;
//                    try
//                    {

//                        List<string> colFld1 = colFld.Cast<string>().ToList();
//                        HashSet<string> colFldSet = new HashSet<string>(colFld1.Select(d => d.ToLower()));
//                        HashSet<string> colHideSet = new HashSet<string>();

//                        for (int i = 0; i < colFld.Count; i++)
//                        {
//                            if (colHide[i].ToString().ToLower() == "true")
//                            {
//                                colHideSet.Add(colFld[i].ToString().ToLower());
//                            }
//                        }

//                        XmlNodeList productNodes1 = xmlDoc.SelectNodes("//row");
//                        foreach (XmlNode productNode1 in productNodes1)
//                        {
//                            for (int i = productNode1.ChildNodes.Count - 1; i >= 0; i--)
//                            {
//                                XmlNode baseDataNode1 = productNode1.ChildNodes[i];
//                                if (colFldSet.Contains(baseDataNode1.Name.ToLower()) && colHideSet.Contains(baseDataNode1.Name.ToLower()))
//                                {
//                                    baseDataNode1.ParentNode.RemoveChild(baseDataNode1);
//                                }
//                            }
//                        }
//                    }
//                    catch (Exception ex)
//                    {
//                        Console.WriteLine("Error looping XML data for hinden columns: " + ex.Message);
//                    }
//                    //Console.WriteLine("After remove hidden fields :" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
//                    //Console.WriteLine("Before refill :" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
//                    // Code to add new nodes in xml which aws missed
//                    XmlNode headSinglenode;
//                    headSinglenode = xmlDoc.SelectSingleNode("//headrow");

//                    try
//                    {
//                        XmlAttribute vAlign;
//                        vAlign = xmlDoc.CreateAttribute("valign");
//                        vAlign.Value = exportVerticalAlignStyle;
//                        headSinglenode.Attributes.Append(vAlign);
//                    }

//                    catch (Exception ex)
//                    {
//                        Console.WriteLine("Error valign: " + ex.Message);
//                    }

//                    int headNodecount = headSinglenode.ChildNodes.Count;

//                    XmlNodeList productNodesRow;
//                    XmlNodeList baseDataNodesRow;
//                    productNodesRow = xmlDoc.SelectNodes("//row");
//                    foreach (XmlNode productNodeRow in productNodesRow)
//                    {
//                        baseDataNodesRow = productNodeRow.ChildNodes;
//                        int rowCounta = baseDataNodesRow.Count;
//                        string rowclass = "searchresultitem";
//                        if (rowCounta < headNodecount)
//                        {
//                            int d = headNodecount - rowCounta;
//                            int b;
//                            var loopTo9 = d;
//                            for (b = 1; b <= loopTo9; b++)
//                            {
//                                XmlElement tempNode = xmlDoc.CreateElement("refill");
//                                tempNode.InnerText = "";
//                                XmlAttribute classnode;
//                                classnode = xmlDoc.CreateAttribute("class");
//                                classnode.Value = rowclass.ToLower();
//                                tempNode.Attributes.Append(classnode);
//                                productNodeRow.AppendChild(tempNode);
//                            }
//                        }
//                    }
//                    //Console.WriteLine("After refill :" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
//                    //Console.WriteLine("Before Ds :" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
//                    var sw = new StringWriter();
//                    var xw = new XmlTextWriter(sw);
//                    xmlDoc.WriteTo(xw);

//                    string nXml;
//                    nXml = sw.ToString();

//                    var sr = new StringReader(nXml);
//                    var ds = new DataSet();

//                    try
//                    {
//                        ds.ReadXml(sr);
//                    }
//                    catch (Exception ex)
//                    {
//                        Console.WriteLine("Error reading XML into DataSet: " + ex.Message);
//                    }
//                    foreach (System.Data.DataTable tables in ds.Tables)
//                    {
//                        if (tables.TableName.Equals("headrow"))
//                        {
//                            foreach (DataColumn columns in tables.Columns)
//                            {
//                                if (tables.Rows[0][columns].ToString().Contains("axp_slno"))
//                                {
//                                    tables.Rows[0][columns] = "Sr. No.";
//                                    break;
//                                }
//                                if (ivtype == "lview")
//                                {
//                                    if (tables.Rows[0][columns].ToString().Contains("rowno"))
//                                    {
//                                        tables.Rows[0][columns] = "Sr. No.";
//                                        break;
//                                    }
//                                }
//                            }
//                        }
//                    }
//                    //Console.WriteLine("After Ds :" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
//                    //Console.WriteLine("Before LoadHtml :" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));

//                    // Load HTML into HtmlDocument
//                    HtmlDocument htmlDoc = new HtmlDocument();
//                    try
//                    {
//                        XmlDataDocument xdd;
//                        xdd = new XmlDataDocument(ds);

//                        var xt = new System.Xml.Xsl.XslCompiledTransform();
//                        var builder = new ConfigurationBuilder()
//                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
//                .AddJsonFile("appsettings.json");

//                        var configuration = builder.Build();

//                        string relativePath = configuration["XsltFilePath"];
//                        string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
//                        string xsltPath = Path.Combine(baseDirectory, relativePath);

//                        xt.Load(xsltPath);

//                        var swr = new StringWriter(sb);
//                        var writer = new XmlTextWriter(swr);
//                        xt.Transform(xdd, writer);
//                        sw.Close();
//                        writer.Close();

//                        htmlDoc.LoadHtml(sb.ToString());
//                    }
//                    catch (Exception ex)
//                    {
//                        Console.WriteLine("Error in loadhtml Exception:" + ex.Message);
//                    }
//                    //Console.WriteLine("After LoadHtml :" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
//                    //Console.WriteLine("Before Excel generate :" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
//                    using (var workbook = new XLWorkbook())
//                    {
//                        var worksheet = workbook.Worksheets.Add("Sheet1");
//                        int falseCount = colHide.OfType<string>().Count(b => bool.TryParse(b, out bool result) && !result);
//                        var table = htmlDoc.DocumentNode.SelectSingleNode("//table");
//                        if (table != null)
//                        {
//                            int rowIndex = 1;

//                            for (int i = 0; i < hdrNames.Count; i++)
//                            {
//                                AddCaption(worksheet, ref rowIndex, hdrNames[i].ToString(), hdrFont[i].ToString(), hdrAlign[i].ToString(), falseCount);
//                            }

//                            if (hdrNames.Count == 0)
//                            {
//                                if (iviewcap == "")
//                                    iviewcap = _ivcaption;
//                                AddCaption(worksheet, ref rowIndex, iviewcap, "", "", falseCount);
//                            }

//                            if (_dateNodehide == "f")
//                                AddDate(worksheet, ref rowIndex, dateformat, falseCount);
//                            for (int j = 0; j < _ivParamCap.Count; j++)
//                            {
//                                AddCaptionParam(worksheet, ref rowIndex, _ivParamCap[j].ToString(), falseCount);
//                            }

//                            foreach (var row in table.SelectNodes("tr"))
//                            {
//                                int colIndex = 1;

//                                foreach (var cell in row.SelectNodes("th|td"))
//                                {
//                                    int colspan = cell.GetAttributeValue("colspan", 1);
//                                    var cellValue = HtmlToText(cell.InnerText);
//                                    cellValue = CleanCellText(cellValue);
//                                    cellValue = replaceSpecialChars(cellValue);
//                                    var wsCell = worksheet.Cell(rowIndex, colIndex);

//                                    if (double.TryParse(cellValue, out double numericValue))
//                                    {
//                                        wsCell.SetValue(numericValue);
//                                        bool containsComma = cellValue.Contains(",");
//                                        if (numericValue == Math.Floor(numericValue))
//                                        {
//                                            wsCell.Style.NumberFormat.Format = containsComma ? "#,##0" : "0";
//                                        }
//                                        else
//                                        {
//                                            int decimalPlaces = cellValue.Contains(".")
//                                                ? cellValue.Substring(cellValue.IndexOf('.') + 1).Length
//                                                : 0;
//                                            string _format = containsComma ? "#,##0." : "0.";
//                                            _format += new string('0', decimalPlaces);
//                                            wsCell.Style.NumberFormat.Format = _format;
//                                        }
//                                    }
//                                    else if (DateTime.TryParseExact(cellValue, dateformat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dateValue1))
//                                    {
//                                        wsCell.Value = dateValue1;
//                                        wsCell.Style.NumberFormat.Format = dateformat;
//                                    }
//                                    else
//                                    {
//                                        wsCell.SetValue(cellValue);
//                                    }

//                                    // Apply styles based on class names
//                                    if (cell.Attributes["class"] != null)
//                                    {
//                                        string className = cell.Attributes["class"].Value;
//                                        ApplyStyles(wsCell, className);
//                                    }

//                                    // Enable text wrapping to handle newlines
//                                    wsCell.Style.Alignment.WrapText = true;

//                                    // Handle colspan
//                                    if (colspan > 1)
//                                    {
//                                        worksheet.Range(rowIndex, colIndex, rowIndex, colIndex + colspan - 1).Merge();
//                                        colIndex += colspan;
//                                    }
//                                    else
//                                    {
//                                        colIndex++;
//                                    }
//                                }
//                                // Adjust the row height to fit the content
//                                worksheet.Row(rowIndex).AdjustToContents();

//                                rowIndex++;
//                            }

//                            for (int i = 0; i < hdrFooterName.Count; i++)
//                            {
//                                AddCaption(worksheet, ref rowIndex, hdrFooterName[i].ToString(), hdrFooterFont[i].ToString(), hdrFooterAlign[i].ToString(), falseCount);
//                            }
//                        }
//                        string _thisIvCap = _ivcaption;
//                        if (_thisIvCap == string.Empty)
//                        {
//                            if (iviewAddCaption.Contains("*,*"))
//                                _thisIvCap = iviewAddCaption.Split("*,*")[0];
//                            else
//                                _thisIvCap = iviewAddCaption;
//                        }
//                        _thisIvCap = _thisIvCap + "_" + DateTime.Now.ToString("yyyyMMddHHmmssfff");
//                        string outputPath = _thisIvCap + ".xlsx";
//                        string _fileName = "";
//                        try
//                        {
//                            if (outputPath == ".xlsx")
//                                outputPath = _ivName + ".xlsx";
//                            _fileName = outputPath;
//                            outputPath = exportExcelPath + "\\Exports\\" + _userName + "\\" + outputPath;
//                            worksheet.Columns().AdjustToContents();
//                            workbook.SaveAs(outputPath);
//                        }
//                        catch (Exception ex)
//                        {
//                            Console.WriteLine("Error in file path: " + outputPath + " Exception:" + ex.Message);
//                        }
//                        _returnRes = outputPath + "," + _fileName;
//                    }

//                    //Console.WriteLine("After Excel Generate :" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
//                }
//                else
//                {
//                    _returnRes = "IView Result Error";
//                }
//                return _returnRes;
//            }

//            static void AddCaption(IXLWorksheet worksheet, ref int rowIndex, string caption, string font, string align, int celllength)
//            {
//                if (!string.IsNullOrEmpty(caption))
//                {
//                    var cell = worksheet.Cell(rowIndex++, 1);
//                    cell.Value = caption;
//                    if (align == "" || align == "@center")
//                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
//                    else if (align == "@left")
//                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
//                    else if (align == "@right")
//                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
//                    cell.Style.Font.Bold = true;
//                    cell.Style.Font.FontColor = XLColor.Black;
//                    worksheet.Range(rowIndex - 1, 1, rowIndex - 1, celllength).Merge();
//                }
//            }

//            static void AddCaptionParam(IXLWorksheet worksheet, ref int rowIndex, string ivPacap, int celllength)
//            {
//                if (!string.IsNullOrEmpty(ivPacap))
//                {
//                    var cell = worksheet.Cell(rowIndex++, 1);
//                    cell.Value = ivPacap;
//                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
//                    cell.Style.Font.Bold = true;
//                    cell.Style.Font.FontColor = XLColor.Black;
//                    worksheet.Range(rowIndex - 1, 1, rowIndex - 1, celllength).Merge();
//                }
//            }

//            static void AddDate(IXLWorksheet worksheet, ref int rowIndex, string dateformat, int celllength)
//            {
//                var time = DateTime.Now;
//                string format = dateformat;
//                var cell = worksheet.Cell(rowIndex++, 1);
//                cell.Value = "Date: " + time.ToString(format, CultureInfo.InvariantCulture);
//                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
//                cell.Style.Font.Bold = false;
//                cell.Style.Font.FontColor = XLColor.Black;
//                worksheet.Range(rowIndex - 1, 1, rowIndex - 1, celllength).Merge();
//            }

//            static string CleanCellText(string text)
//            {
//                // Split by the '~' delimiter to handle new lines
//                var parts = text.Split('~');
//                for (int i = 0; i < parts.Length; i++)
//                {
//                    parts[i] = TrimInnerText(parts[i]);
//                }
//                return string.Join(Environment.NewLine, parts);
//            }

//            static string replaceSpecialChars(string str)
//            {
//                if (str == null)
//                    str = "";
//                str = Regex.Replace(str, "&amp;", "&");
//                str = Regex.Replace(str, "&lt;", "<");
//                str = Regex.Replace(str, "&gt;", ">");
//                str = Regex.Replace(str, "&apos;", "'");
//                str = Regex.Replace(str, "&quot;", "\"");
//                str = Regex.Replace(str, "&nbsp;", " ");
//                return str;
//            }

//            static string TrimInnerText(string text)
//            {
//                // Trim spaces and handle specific text cases
//                var trimmedText = text.Trim();

//                // Example: Trim spaces between "Date :" and the actual date value
//                var datePattern = @"(Date\s*:\s*)(\d{2}/\d{2}/\d{4})";
//                trimmedText = Regex.Replace(trimmedText, datePattern, m => $"{m.Groups[1].Value.Trim()} {m.Groups[2].Value.Trim()}");

//                // Add other specific patterns here if needed

//                return trimmedText;
//            }

//            static string HtmlToText(string html)
//            {
//                if (string.IsNullOrEmpty(html))
//                    return string.Empty;

//                // Replace <br> tags with newline characters
//                var doc = new HtmlDocument();
//                doc.LoadHtml(html);

//                var brNodes = doc.DocumentNode.SelectNodes("//br");
//                if (brNodes != null)
//                {
//                    foreach (var br in brNodes)
//                    {
//                        br.ParentNode.ReplaceChild(doc.CreateTextNode("\n"), br);
//                    }
//                }

//                return doc.DocumentNode.InnerText;
//            }

//            static void ApplyStyles(IXLCell cell, string className)
//            {
//                switch (className)
//                {
//                    case "stdPageMainHdr":
//                        cell.Style.Font.FontColor = XLColor.Gray;
//                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
//                        cell.Style.Font.FontSize = 22;
//                        break;
//                    case "stdPVTblLCell":
//                        cell.Style.Fill.BackgroundColor = XLColor.Aqua;
//                        cell.Style.Font.FontColor = XLColor.Gray;
//                        cell.Style.Font.Bold = true;
//                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
//                        cell.Style.Font.FontSize = 13;
//                        break;
//                    case "stdPageHdr":
//                        cell.Style.Font.FontColor = XLColor.Gray;
//                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
//                        cell.Style.Font.FontSize = 12;
//                        break;
//                    case "stdAddlHdr":
//                        cell.Style.Font.FontColor = XLColor.Black;
//                        cell.Style.Font.Bold = true;
//                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
//                        cell.Style.Font.FontSize = 13.5;
//                        break;
//                    case "gridHeader":
//                        cell.Style.Font.FontColor = XLColor.Black;
//                        cell.Style.Font.Bold = true;
//                        cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
//                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
//                        cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
//                        break;
//                    case "subhead":
//                    case "subhead-left":
//                    case "subhead-right":
//                    case "subhead-middle":
//                    case "subhead-center":
//                        cell.Style.Font.FontColor = XLColor.DarkRed;
//                        cell.Style.Font.FontSize = 12;
//                        cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
//                        cell.Style.Font.Underline = XLFontUnderlineValues.Single;
//                        break;
//                    case "stot":
//                    case "stot-left":
//                    case "stot-right":
//                    case "stot-middle":
//                    case "stot-center":
//                        cell.Style.Font.FontColor = XLColor.DarkRed;
//                        cell.Style.Font.FontSize = 12;
//                        cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
//                        break;
//                    case "gtot":
//                    case "gtot-left":
//                    case "gtot-right":
//                    case "gtot-middle":
//                    case "gtot-center":
//                        cell.Style.Font.FontColor = XLColor.Green;
//                        cell.Style.Font.FontSize = 12;
//                        cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
//                        cell.Style.Font.Underline = XLFontUnderlineValues.Single;
//                        break;
//                    case "searchresultitem":
//                    case "searchresultitem-left":
//                    case "searchresultitem-right":
//                    case "searchresultitem-middle":
//                    case "searchresultitem-center":
//                    case "searchresultaltitem":
//                    case "searchresultaltitem-left":
//                    case "searchresultaltitem-right":
//                    case "searchresultaltitem-middle":
//                    case "searchresultaltitem-center":
//                        cell.Style.Font.FontColor = XLColor.Black;
//                        cell.Style.Font.FontSize = 8;
//                        cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
//                        break;
//                    case "stdPageftr":
//                        cell.Style.Font.FontColor = XLColor.Gray;
//                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
//                        cell.Style.Font.FontSize = 12;
//                        break;
//                    case "right":
//                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
//                        break;
//                    case "left":
//                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
//                        break;
//                    case "middle":
//                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
//                        break;
//                    case "headerClass":
//                        cell.Style.Font.Bold = true;
//                        cell.Style.Fill.BackgroundColor = XLColor.LightGray;
//                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
//                        break;
//                    case "dataClass":
//                        cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
//                        break;
//                    case "someClass":
//                        cell.Style.Font.Bold = true;
//                        break;
//                    default:
//                        break;
//                }
//            }

//            static string CheckSpecialChars(string str)
//            {
//                if (str == null)
//                {
//                    str = "";
//                }

//                str = Regex.Replace(str, "&", "&amp;");
//                str = Regex.Replace(str, "<", "&lt;");
//                str = Regex.Replace(str, ">", "&gt;");
//                str = Regex.Replace(str, "'", "&apos;");
//                str = Regex.Replace(str, "\"", "&quot;");
//                string pattern = "\\\\";
//                str = Regex.Replace(str, pattern, ";bkslh");
//                return str;
//            }

//            async Task SendSignalRMessage(string project, string clientId, string message)
//            {
//                if (!string.IsNullOrEmpty(clientId))
//                {
//                    var singalRMessage = new
//                    {
//                        project = project,
//                        UserId = clientId,
//                        Message = message
//                    };
//                    API _api = new API();
//                    await _api.POSTData(signalrUrl, JsonConvert.SerializeObject(singalRMessage), "application/json");
//                }
//            }

//            //static string GetWord(string ires, string datarows, string totalrows, string reccount, string exportExcelPath, string _ivName, string _params, string _userName)
//            //{
//            //    string _returnRes = string.Empty;

//            //    //string Ikey = string.Empty;
//            //    //if (Session["ivKey"] != null)
//            //    //    Ikey = Session["ivKey"].ToString();
//            //    //else if (Request.QueryString["ivKey"] != null)
//            //    //    Ikey = Request.QueryString["ivKey"].ToString();
//            //    //objIview = (IviewData)Session[Ikey];
//            //    //objIview.ShowHiddengridCols = new ArrayList();
//            //    //objIview.ReportHdrs = new ArrayList();

//            //    string sid = HttpContext.Current.Session["nsessionid"].ToString();

//            //    DataTable dt = new DataTable();
//            //    //dt = objIview.DtCurrentdata.Clone();

//            //    if (objIview.IsIviewStagLoad)
//            //    {

//            //        for (int rCnt = 0; rCnt < objIview.DtCurrentdata.Rows.Count; rCnt++)
//            //        {
//            //            dt.ImportRow(objIview.DtCurrentdata.Rows[rCnt]);
//            //        }
//            //        //colHide = (ArrayList)objIview.ColHide.Clone();
//            //    }
//            //    else
//            //    {
//            //        dt = GetFullData();
//            //    }

//            //    int dtcount = dt.Columns.Count;
//            //    if (dt != null && dt.Rows.Count > 0)
//            //    {
//            //        for (int j = 0; j < dt.Columns.Count; j++)
//            //        {
//            //            if (dt.Columns[j].ColumnName.Contains("html_"))
//            //            {
//            //                colHide[j] = "false";
//            //                string oldColName = dt.Columns[j].ColumnName.Replace("html_", "");

//            //                // to hide the columns if column is hidden and column with prefix as html_
//            //                if (objIview.ShowHideCols != null)
//            //                {
//            //                    if (objIview.ShowHideCols.IndexOf(oldColName) > -1)
//            //                        colHide[j] = "true";
//            //                }
//            //            }
//            //        }

//            //        int count = 0;
//            //        foreach (DataRow item in dt.Rows)
//            //        {
//            //            foreach (var cell in item.ItemArray)
//            //            {
//            //                if (cell.ToString().ToLower() == "subhead")
//            //                    arrSubhead.Add(count);
//            //            }
//            //            count++;
//            //        }

//            //        if (dt.Columns.Count == colHide.Count)
//            //        {
//            //            for (int i = colHide.Count - 1; i >= 0; i--)
//            //                if (colHide[i].ToString() == "true" || dt.Columns[i].ColumnName == "axrowtype" || dt.Columns[i].ColumnName == "rowno" || dt.Columns[i].ColumnName == "axp__font")
//            //                    //if hidden column are used in hide column 
//            //                    if (objIview.ShowHiddengridCols != null)
//            //                        if (objIview.ShowHiddengridCols.IndexOf(dt.Columns[i].ColumnName) > -1)
//            //                        {
//            //                            // To remove the normal column if column cantains html_column and is visible
//            //                            int indx = -1;
//            //                            string removeColumn = string.Empty;
//            //                            if (dt.Columns[i].ColumnName.Contains("html_"))
//            //                            {
//            //                                //do nothing
//            //                            }
//            //                            else
//            //                            {
//            //                                removeColumn = "html_" + dt.Columns[i].ColumnName;
//            //                            }
//            //                        }
//            //                        else
//            //                        {
//            //                            dt.Columns.RemoveAt(i);
//            //                            colHead.RemoveAt(i);
//            //                        }
//            //                    else
//            //                    {
//            //                        dt.Columns.RemoveAt(i);
//            //                        colHead.RemoveAt(i);
//            //                    }
//            //        }


//            //        for (int hIdx = dt.Columns.Count - 1; hIdx >= 0; hIdx--)
//            //        {
//            //            int cIdx = -1, sHIdx = -1;
//            //            cIdx = colHide.IndexOf(dt.Columns[hIdx].ColumnName);

//            //            //condition for removeing hide columns which are checked in gridview 
//            //            if (objIview.ShowHideCols != null)
//            //                sHIdx = objIview.ShowHideCols.IndexOf(dt.Columns[hIdx].ColumnName);

//            //            if ((cIdx != -1) || (sHIdx != -1))
//            //            {
//            //                dt.Columns.RemoveAt(hIdx);
//            //                colHead.RemoveAt(hIdx);
//            //            }
//            //        }
//            //        CreateDocument(dt);
//            //    }

//            //    return _returnRes;
//            //}
//        }
//    }

//}
