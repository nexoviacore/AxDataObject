using ARMCommon.Helpers;
using ARMCommon.Interface;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;
using System.Data;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace AxStructUtils.Helper
{
    public class TstXMLBuilder
    {
        #region variable declaration

        [ExcludeFromReset]
        string fieldValJson = "{}";
        [ExcludeFromReset]
        JObject fieldValJsonDoc = new();
        [ExcludeFromReset]
        JObject fieldTypeJsonDoc = new();
        [ExcludeFromReset]
        JArray FormLstJson = new();
        [ExcludeFromReset]
        XDocument? mainStructXmldocument;
        [ExcludeFromReset]
        XElement mainRootElement;
        [ExcludeFromReset]
        public string projName = string.Empty;
        [ExcludeFromReset]
        public string userName = string.Empty;
        [ExcludeFromReset]
        public string structName = string.Empty;
        [ExcludeFromReset]
        string AppSessionKey = string.Empty;
        [ExcludeFromReset]
        XElement compele = null;
        [ExcludeFromReset]
        bool cldFrmRunTime = false;
        [ExcludeFromReset]
        string oldStructXml = string.Empty;
        [ExcludeFromReset]
        bool isOldXMLExists = false;
        [ExcludeFromReset]
        List<string> searchColList = new List<string>();
        [ExcludeFromReset]
        bool trace = false;

        public string Name = string.Empty;
        public string Caption = string.Empty;
        public string Purpose = string.Empty;

        public string RuntimeMode = string.Empty;
        public string RuntimeTstruct = "F";
        public string CachedSave = string.Empty;


        //tstruct properties
        public string Folder = string.Empty;
        public string SaveControl = string.Empty;
        public string DelControl = string.Empty;
        public string Track = "False";
        public string trackchangesfield = string.Empty;
        public string trackchangesmadeby = string.Empty;
        public string NoChange = string.Empty;
        public string ExprSet = string.Empty;
        public string DateField = string.Empty;

        public string OnOpen = string.Empty;
        public string AfterLoadData = string.Empty;
        public string BeforeSave = string.Empty;
        public string AfterSave = string.Empty;
        public string BeforeDelete = string.Empty;
        public string AfterDelete = string.Empty;
        public string navigate = "t";
        public string TreeParents = string.Empty;
        public string Attach = string.Empty;
        public string SearchCondition = string.Empty;
        public string ListView = "True";
        public string TrackUsers = string.Empty;
        public string TrackFields = string.Empty;
        public string BlockSize = string.Empty;
        public string Schema = string.Empty;
        public string WorkFlow = "False";
        public string SaveDraft = "False";
        public string Traceability = "False";     //ch1
        public string CachingType = "None";
        public string DcNavigation = "Default";
        public string Pos = "False";
        //Extended Save
        public string savemode = string.Empty;

        public string defpage = "true";
        public string menuposition = "default";
        public string selectuserlist = string.Empty;
        public string selectfieldlist = string.Empty;

        //newly added properties. only required in web.
        public string saveCtrlFldCaption = string.Empty;
        public string dltCtrlFldCaption = string.Empty;
        public string trackFldCaps = string.Empty;
        public string pform = "yes";

        public string rootNodeAttr = string.Empty;

        #region dcProperties

        public string FrameNo = string.Empty;
        public string PageNo = string.Empty;
        public string Table = string.Empty;
        public string AsGrid = "False";
        public string AllowChange = "True";
        public string Pop = string.Empty;
        public string PopUp = string.Empty;
        public string PopParent = string.Empty;
        public string PopControl = string.Empty;
        public string PopOn = string.Empty;
        public string PopValidate = string.Empty;
        public string dcborder = string.Empty;

        public string OnEnterDC = string.Empty;
        public string OnExitDC = string.Empty;
        public string DcAllowEmpty = "True";
        public string Heading = string.Empty;
        public string ParentDC = string.Empty;
        public string ParentFlds = string.Empty;
        public string ShowButton = string.Empty;
        public string PopAutoShow = string.Empty;
        public string PopCondition = string.Empty;
        public string AutoFill = string.Empty;
        public string DispSum = string.Empty;
        public string AddDcRows = "True";
        public string DeleteDcRows = "True";

        //newly added properties. only required in web.
        public string dcOrdNo = string.Empty;
        public string booleandc = "FALSE";
        public string defaultstate = "Expand";

        #endregion


        #region field Properties
        public string CustomDataType = string.Empty;
        public string CustomDataType2 = string.Empty;
        public string DataType = string.Empty;
        public string DataWidth = "10";
        public string Dec = "0";
        public string MOE = "Accept";
        public string Details = string.Empty;
        public string Hide = "False";
        public string Empty = "True";
        public string ReadOnly = "False";
        public string SetCarry = "False";
        public string Save = "True";
        public string Expr = string.Empty;
        public string VExp = string.Empty;
        public string Duplicate = "True";
        public string OnlyPositive = "False";
        public string SourceKey = "False";
        public string CField = string.Empty;
        public string DispTot = "False";
        public string SGWidth = "0";
        public string SGHeight = "0";
        public string SQL = string.Empty;
        public string AutoSelect = string.Empty;
        public string comboBox = string.Empty;
        public string Refresh = string.Empty;
        public string List = string.Empty;
        public string SourceForm = string.Empty;
        public string Source = string.Empty;
        public string MasterForm = string.Empty;
        public string MasterFormCaption = string.Empty;
        public string MasterField = string.Empty;
        public string MasterFieldCaption = string.Empty;
        public string oldmasterfieldAttr = string.Empty;
        public string FParent = string.Empty;
        public string FSource = string.Empty;
        public string SourceTable = string.Empty;
        public string Suggestive = string.Empty;
        public string gui = string.Empty;
        public string cwidth = string.Empty;
        public string tlhw = string.Empty;
        public string cfont = string.Empty;
        public string ccolor = string.Empty;
        public string bordercolor = string.Empty;
        public string showsno = string.Empty;
        public string Trans = string.Empty;
        public string Sequence = string.Empty;
        public string Prefix = string.Empty;
        public string Desc = string.Empty;
        public string StartNo = string.Empty;
        public string SActive = string.Empty;
        public string PrefixField = string.Empty;
        public string Digits = string.Empty;

        public string OnEnterField = string.Empty;
        public string OnExitField = string.Empty;

        public string Mask = string.Empty;

        public string lastcharmask = string.Empty;
        public string firstcharmask = string.Empty;
        public string maskchar = string.Empty;
        public string maskroles = string.Empty;

        public string Pattern = string.Empty;
        public string PWordChar = string.Empty;
        public string Hint = string.Empty;
        public string ApplyComma = string.Empty;
        public string CValidate = string.Empty;
        public string FormCtrl = string.Empty;
        public string ExprFlds = string.Empty;
        public string Deps = string.Empty;
        public string DetStruct = string.Empty;
        public string DetTransid = string.Empty;
        public string DetMap = string.Empty;
        public string Dynamic = string.Empty;
        public string SearchSQL = string.Empty;
        public string displaydetail = string.Empty;
        public string ParentDeps = string.Empty; //only for web
        public string Tabstop = "True";     // focus the read only field
        public string CustDType = string.Empty;
        public string TableTypeDetails = string.Empty;


        public string runtimecriteria = string.Empty;
        public string runtimefld = string.Empty;
        public string applyrule = string.Empty;
        public string encrypted = string.Empty;
        public string fldDcName = string.Empty;
        public string dbDec = "2";
        public string separator = string.Empty;
        public string radiotype = string.Empty;
        public string apiName = string.Empty;
        public string apiURL = string.Empty;
        public string apiReqStr = string.Empty;
        public string apiRespType = string.Empty;
        public string apiRespFormat = string.Empty;
        public string intelligence = string.Empty;
        public string acceptChoice = string.Empty;
        public string fillFrmApiSrc = string.Empty;
        public string fillFrmApiSrcCap = string.Empty;

        //newly added properties. only required in web.
        public string dcCaption = string.Empty;
        public string fParentCaption = string.Empty;
        public string DSFormCaption = string.Empty;
        public string DSFieldCaption = string.Empty;
        public string CFieldCaption = string.Empty;
        public string sformCaption = string.Empty;


        public string fldPosition = string.Empty;//only for runtime api
        public string fldPosRef = string.Empty;//only for runtime api
        public string fldSQLParam = string.Empty;
        public string fldSQLParamCnt = string.Empty;
        public string ctypeValue = string.Empty;
        public string displayTotal = string.Empty;
        public string hideInSearch = string.Empty;
        public string splDataType = string.Empty;


        #endregion

        #region genmap Properties
        public string GenTarget = string.Empty;
        public string GenParent = string.Empty;
        public string GenControl = string.Empty;
        public string Active = string.Empty;
        public string GenCField = string.Empty;
        public string GMap = string.Empty;
        public string OnPost = string.Empty;
        public string GenTransId = string.Empty;
        public string OnApprove = string.Empty; // this is applicable for mdmap also.
        public string OnReject = string.Empty; // this is applicable for mdmap also.

        public List<string> targetfields = new List<string>();

        //newly added properties. only required in web.
        public string GenTargetCaption = string.Empty;
        public string GenControlCaption = string.Empty;
        public string postEvent = string.Empty;
        public string genRowCtrlData = string.Empty;
        public string mapGroupFlds = string.Empty;

        #endregion

        #region fillgrid properties

        public string IView = string.Empty;
        public string FGCaption = string.Empty;
        public string FGSQL = string.Empty;
        public string FGTarget = string.Empty;
        public string FMap = string.Empty;
        public string Multi = string.Empty;
        public string AutoShow = string.Empty;
        public string SourceDC = string.Empty;
        public string FGValidate = string.Empty;
        public string OnFill = string.Empty;
        public string ExeOnSave = string.Empty;
        public string FirmBind = string.Empty;
        public string SelectOn = string.Empty;
        public string FooterStr = string.Empty;
        public string FGVExp = string.Empty;
        public string AddRows = string.Empty;
        public string FGColProps = string.Empty;
        public string SelectAllRows = string.Empty;

        public List<string> fgsourcecaption = new List<string>();
        public List<string> fgsourcefield = new List<string>();
        public List<string> fgtargetfield = new List<string>();
        public string fggrpfield = string.Empty;
        public string FG_a5NodeAttr = string.Empty;

        public string FG_isMUltilineSQL = string.Empty;

        //newly added properties. only required in web.
        public string FGTargetCap = string.Empty;
        public string SourceDCCap = string.Empty;
        public List<string> fgtargetfieldCap = new List<string>();


        #endregion


        #region MDMap Properties

        public string Master = string.Empty;
        public string MField = string.Empty;
        public string DField = string.Empty;
        public string MSField = string.Empty;
        public string DSField = string.Empty;
        public string MTable = string.Empty;
        public string Type = string.Empty;
        public string Tree = string.Empty;
        public string MDControl = string.Empty;
        public string Append = string.Empty;
        public string InitOnDel = string.Empty;
        public string MasterFieldDec = "0";
        public string MapText = string.Empty;
        public string Extended = string.Empty;

        //newly added properties. only required in web.
        public string MasterCap = string.Empty;
        public string MFieldCap = string.Empty;
        public string DFieldCap = string.Empty;
        public string DSFieldCap = string.Empty;
        public string MSFieldCap = string.Empty;
        public string MDControlCap = string.Empty;

        #endregion

        #region Script Properties
        public string validate = string.Empty;
        public string controlType = string.Empty;
        public string breakonerror = "no";
        public string expression = string.Empty;
        public string appliedon = string.Empty;
        public string Event = string.Empty;
        public string pushtoqueue = string.Empty;
        public string queuename = string.Empty;
        public string confirmmsgreq = string.Empty;
        public string confirmmsg = string.Empty;
        public string fcExpression = string.Empty;

        public string scrActive = string.Empty;
        #endregion
        public string oldSQL = string.Empty;
        #endregion
        private readonly AddFormFieldHelper _addFormFieldHelper;
        public TstXMLBuilder(string projName, string userName, AddFormFieldHelper addFormFieldHelper, bool cldFrmRunTime = false, bool trace = false)
        {
            (this.projName, this.userName, this.cldFrmRunTime, this.trace) = (projName, userName, cldFrmRunTime, trace);
            _addFormFieldHelper = addFormFieldHelper;
        }

        public async Task<AxAddFieldApiResponse<string>> createXml(string structName)
        {
            string message = string.Empty;
            bool success = false;
            string logdata = $"creating structure xml for {structName}..";
            this.structName = structName;
            string structHashKey = $"{projName}♠tst♠{structName}";
            string compHashKey = $"{structHashKey}♠comps";
            string changelogHashKey = $"{projName}♠changedstructurelog";
            logdata += $"\nstructHashKey: {structHashKey}\ncompHashKey: {compHashKey}\nchangelogHashKey: {changelogHashKey}";
            try
            {
                var sw = Stopwatch.StartNew();
                logdata = $"\nChecking for old structure xml for..";
                oldStructXml = "";// _redisHelper.RedisDB.HashGet(structHashKey, Constants.Tstruct.Redis.STRUCTXML_SUBKEY);
                sw.Stop();
                logdata = $"\nchecking for old xml took: {sw.ElapsedMilliseconds} ms";

                if (oldStructXml != null && oldStructXml != string.Empty)
                {
                    isOldXMLExists = true;
                }
                else
                {
                    isOldXMLExists = false;
                }
                logdata = $"\ngetting tsruct props data from redis..";
                sw.Restart();
                fieldValJson = _addFormFieldHelper.HashGet(structHashKey, "♠props♠");
                sw.Stop();
                logdata = $"\nfetching tstruct props json took : {sw.ElapsedMilliseconds} ms";

                logdata += trace ? $"\ntstruct props json: {fieldValJson}" : "";
                if (fieldValJson == null || fieldValJson?.ToString() == "")
                {
                    throw new Exception($"Structure information not found in redis.");
                }
                fieldValJsonDoc = JObject.Parse(fieldValJson);
                parsefieldValueJSON();
                JObject rootNodeAttrObj = new JObject();
                if (isOldXMLExists && rootNodeAttr != string.Empty)
                {
                    rootNodeAttrObj = JObject.Parse(rootNodeAttr);
                }
                rootNodeAttrObj["pform"] = pform;
                rootNodeAttrObj["wflow"] = WorkFlow == "True" ? "T" : "F";
                //rootNodeAttrObj["defpage"] = defpage == "true" ? "y" : "n";
                rootNodeAttrObj["name"] = Name;
                rootNodeAttrObj["caption"] = Caption;
                rootNodeAttrObj["runtimemod"] = RuntimeMode;
                rootNodeAttrObj["runtimetstruct"] = RuntimeTstruct;
                rootNodeAttrObj["cachedsave"] = CachedSave;
                rootNodeAttrObj["vno"] = "11.4";
                //rootNodeAttrObj["createdon"] = updatedon;
                //rootNodeAttrObj["createdby"] = userName;
                //rootNodeAttrObj["updatedon"] = updatedon;
                //rootNodeAttrObj["updatedby"] = userName;

                mainRootElement = new XElement("root",
                                            rootNodeAttrObj.Properties().Select(prop => new XAttribute(prop.Name, prop.Value.ToString()))
                                        );
                mainStructXmldocument = new(mainRootElement);
                logdata += $"\nAdding tsruct props node..";
                AddTstructProperties();
                //AddToolbar();
                logdata += $"\nAdding button node..";
                sw.Restart();
                await Addbuttons(structHashKey, compHashKey);
                sw.Stop();
                logdata = $"\nAdding button node took {sw.ElapsedMilliseconds} ms";
                logdata += $"\nAdding Iframe node..";
                await AddIframeNode();
                if (isOldXMLExists)
                {
                    logdata += $"\nAdding action nodes of old xml..";
                    await checkAndAddActionNode();
                }
                logdata += $"\nAdding temp node..";
                AddUpdateTempNode();
                logdata += $"\nGetting dc list..";

                sw.Restart();
                var dcList = _addFormFieldHelper.HashScan(compHashKey, "axdc♠*").OrderBy(x => int.Parse(x.Value.ToString().Split('♠')[0])).Select(x => x.Field.Split('♠')[1]).ToArray();
                sw.Stop();
                logdata += $"\nHashScan \"axdc♠*\" took {sw.ElapsedMilliseconds} ms";
                string[] dcJsons = dcList.Select(dc => _addFormFieldHelper.HashGet(structHashKey, dc)).ToArray();
                logdata += $"\nDC count: {dcList.Length}";

                int dcCnt = 0;
                logdata += $"\nstart Adding DC and field nodes";
                logdata += $"\nstart time: {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}";
                for (int i = 0; i < dcList.Length; i++)
                {
                    string dcName = dcList[i].ToString();
                    logdata += $"\nAdding {dcName} node...";
                    fieldValJson = dcJsons[i];
                    logdata += trace ? $"\n{dcName} json : {fieldValJson}" : "";
                    fieldValJsonDoc = JObject.Parse(fieldValJson);
                    dcOrdNo = (++dcCnt).ToString();
                    parsefieldValueJSON();
                    AddDcProperties();
                    logdata += $"\nGetting filed list for {dcName}...";
                    sw.Restart();
                    var fieldList = _addFormFieldHelper.HashScan(compHashKey, $"axfield♠*♠{dcName}").OrderBy(entry => int.Parse(entry.Value.ToString().Split('♠')[0])).Select(entry => entry.Field.Split('♠')[1]).ToArray();
                    sw.Stop();
                    logdata += $"\nHashScan axfield♠*♠{dcName} took {sw.ElapsedMilliseconds} ms";
                    logdata += $"\nField count : {fieldList.Length}";
                    string[] fieldJsons = _addFormFieldHelper.HashGet(structHashKey, fieldList);
                    if (fieldList.Length == 0)
                    {
                        if (cldFrmRunTime) continue;
                        else throw new Exception($"{dcName} should contain at least one field.");
                    }
                    for (int j = 0; j < fieldList.Length; j++)
                    {
                        sw.Restart();
                        string fldName = fieldList[j].ToString();
                        logdata += $"\nAdding {fldName} node...";
                        fieldValJson = fieldJsons[j];
                        logdata += trace ? $"\n{fldName} json : {fieldValJson}" : "";
                        fieldValJsonDoc = JObject.Parse(fieldValJson);
                        parsefieldValueJSON();
                        AddFldProperties();
                        sw.Stop();
                        logdata += $"\nProcessing {fldName} took {sw.ElapsedMilliseconds} ms";
                    }
                }
                logdata += $"\nEnd adding DC and field nodes";
                logdata += $"\nEnd time: {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}";

                logdata += $"\nGetting  mdmap list...";
                var mdmaplist = _addFormFieldHelper.HashScan(compHashKey, "axmdmap♠*").OrderBy(entry => int.Parse(entry.Value.ToString().Split('♠')[0])).Select(entry => entry.Field.Split('♠')[1]).ToArray();
                string[] mdmapJsons = _addFormFieldHelper.HashGet(structHashKey, mdmaplist);

                logdata += $"\nmdmap count : {mdmaplist.Length}";
                for (int i = 0; i < mdmaplist.Length; i++)
                {
                    string mdmapName = mdmaplist[i].ToString();
                    logdata += $"\nAdding {mdmapName} node...";
                    fieldValJson = mdmapJsons[i].ToString();
                    logdata += trace ? $"\n{mdmapName} json : {fieldValJson}" : "";
                    fieldValJsonDoc = JObject.Parse(fieldValJson);
                    parsefieldValueJSON();
                    AddMdMapProperties();
                }

                logdata += $"\nGetting  fillgrid list...";
                var fillgridlist = _addFormFieldHelper.HashScan(compHashKey, "axfillgrid♠*").OrderBy(entry => int.Parse(entry.Value.ToString().Split('♠')[0])).Select(entry => entry.Field.Split('♠')[1]).ToArray();

                string[] fillGridJsons = _addFormFieldHelper.HashGet(structHashKey, fillgridlist);

                logdata += $"\nfillgrid count : {fillgridlist.Length}";
                for (int i = 0; i < fillgridlist.Length; i++)
                {
                    string fillGridName = fillgridlist[i].ToString();
                    logdata += $"\nAdding {fillGridName} node...";
                    fieldValJson = fillGridJsons[i].ToString();
                    logdata += trace ? $"\n{fillGridName} json : {fieldValJson}" : "";
                    fieldValJsonDoc = JObject.Parse(fieldValJson);
                    parsefieldValueJSON();
                    AddFillgridProperties();
                }

                logdata += $"\nGetting  genmap list...";
                var genmaplist = _addFormFieldHelper.HashScan(compHashKey, "axgenmap♠*").OrderBy(entry => int.Parse(entry.Value.ToString().Split('♠')[0])).Select(entry => entry.Field.Split('♠')[1]).ToArray();

                string[] genMapJsons = _addFormFieldHelper.HashGet(structHashKey, genmaplist);
                for (int i = 0; i < genmaplist.Length; i++)
                {
                    string genmapName = genmaplist[i].ToString();
                    logdata += $"\nAdding {genmapName} node...";
                    fieldValJson = genMapJsons[i].ToString();
                    logdata += trace ? $"\n{genmapName} json : {fieldValJson}" : "";
                    fieldValJsonDoc = JObject.Parse(fieldValJson);
                    parsefieldValueJSON();
                    await AddGenmapProperties();
                }

                logdata += $"\nGetting  script list...";
                var scriptlist = _addFormFieldHelper.HashScan(compHashKey, "axscript♠*").OrderBy(entry => int.Parse(entry.Value.ToString().Split('♠')[0])).Select(entry => entry.Field.Split('♠')[1]).ToArray();


                string[] scriptJsons = _addFormFieldHelper.HashGet(structHashKey, scriptlist);

                for (int i = 0; i < scriptlist.Length; i++)
                {
                    string scriptName = scriptlist[i].ToString();
                    logdata += $"\nAdding {scriptName} node...";
                    fieldValJson = scriptJsons[i].ToString();
                    logdata += trace ? $"\n{scriptName} json : {fieldValJson}" : "";
                    fieldValJsonDoc = JObject.Parse(fieldValJson);
                    parsefieldValueJSON();
                    AddScriptProperties();
                }
                logdata += $"\nAdding config nodes..";
                await addConfigNode(structHashKey);
                logdata += $"\nStructure Xml for {structName} created successfully..";
                logdata += $"\nEnd time: {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}";

                success = true;

            }
            catch (Exception ex)
            {
                success = false;
                logdata += "\nException: " + ex.Message;
            }

            var response = new AxAddFieldApiResponse<string>
            {
                success = success,
                message = logdata,
                data = mainStructXmldocument?.ToString()
            };
            return response;
        }

        public string AddfieldToExistingXML(string structName, string fldPropsJSON, string structXmlStr)
        {
            try
            {
                mainStructXmldocument = XDocument.Parse(structXmlStr);
                mainRootElement = mainStructXmldocument?.Root;
                fieldValJson = fldPropsJSON;
                fieldValJsonDoc = JObject.Parse(fieldValJson);
                parsefieldValueJSON();
                XElement fldElemet = createFieldNodeXml();

                if (fldPosition != string.Empty && fldPosRef != string.Empty)
                {
                    if (mainRootElement.Elements(Name).Any())
                    {
                        mainRootElement.Element(Name).Remove();
                    }
                    string pattern = @"\((.*?)\)";
                    Match match = Regex.Match(fldPosRef, pattern);
                    if (match.Success)
                    {
                        fldPosRef = match.Groups[1].Value; // Extracted string inside ()
                    }
                    XElement refFldNode = mainRootElement.Descendants(fldPosRef).FirstOrDefault();

                    if (refFldNode == null) throw new Exception($"Error while adding {fldPosition} {fldPosRef}");

                    if (fldPosition == "After field")
                    {
                        refFldNode.AddAfterSelf(fldElemet);
                    }
                    else
                    {
                        refFldNode.AddBeforeSelf(fldElemet);

                    }
                }
                else
                {
                    if (mainRootElement.Elements(Name).Any())
                    {
                        mainRootElement.Element(Name).ReplaceWith(fldElemet);
                    }
                    else
                    {

                        XElement nxtdcElemnt = mainRootElement.Descendants("dc" + (int.Parse(FrameNo) + 1)).FirstOrDefault();

                        if (nxtdcElemnt != null)
                        {
                            nxtdcElemnt.AddBeforeSelf(fldElemet);
                        }
                        else
                        {
                            mainRootElement.Descendants("comps")?.FirstOrDefault()?.AddBeforeSelf(fldElemet);
                        }

                    }

                }

                //AddIframeNode();
                var tstructNode = mainRootElement.Descendants()
                                   .FirstOrDefault(x => x.Attribute("cat")?.Value == "tstruct");
                Caption = tstructNode.Element("a2")?.Value;
                ListView = tstructNode.Element("a20")?.Value;
                menuposition = "default";
                defpage = mainRootElement?.Attribute(defpage)?.Value == "n" ? "false" : "true";
                AddUpdateTempNode();
                mainRootElement.Elements("iframes").Remove();
                var iframeEle = new XElement("iframes", new XAttribute("cat", "iframes"), new XAttribute("layout", "default"));
                mainRootElement.Descendants("temp")?.FirstOrDefault()?.AddBeforeSelf(iframeEle);
                updateConfigNodeInExistingXML(structName);
                return mainStructXmldocument.ToString();

            }
            catch (Exception ex)
            {
                throw new Exception($"Error adding field to XML: {ex.Message}");
            }


        }

        public void updateConfigNodeInExistingXML(string structName)
        {
            string structHashKey = $"{projName}♠tst♠{structName}";
            var confignode = _addFormFieldHelper.HashGet(structHashKey, "♠confignode♠");

            XElement configElement = null;
            if (confignode != null)
            {
                configElement = XElement.Parse(confignode);

                var existingSearchColsNode = configElement.Elements("CONFIGNAME")
                    .Where(x => x.Value.Trim() == "searchcols")
                    .Select(x => x.ElementsAfterSelf("CVALUE").FirstOrDefault())
                    .FirstOrDefault();

                if (existingSearchColsNode != null)
                {
                    // Update CVALUE
                    var _searchColList = existingSearchColsNode.Value.Split(",");
                    if (hideInSearch.ToLower() == "false" && !_searchColList.Contains(Name))
                    {
                        _searchColList = _searchColList.Concat(new[] { Name }).ToArray();
                    }
                    else if (hideInSearch.ToLower() == "true" && _searchColList.Contains(Name))
                    {
                        _searchColList = _searchColList.Where(_fld => fldDcName != Name).ToArray();
                    }
                    existingSearchColsNode.Value = string.Join(",", _searchColList);
                }
                else
                {
                    if (hideInSearch.ToLower() == "false")
                    {
                        configElement.Add(
                        new XElement("CONFIGNAME", "searchcols"),
                        new XElement("CVALUE", Name)
                        );
                    }
                }
            }
            else
            {
                if (hideInSearch.ToLower() == "false")
                {
                    configElement = new XElement($"axpconfigs_{structName}",
                    new XAttribute("cat", "config"),
                    new XElement("CONFIGNAME", "searchcols"),
                    new XElement("CVALUE", Name)
                     );
                }
            }
            if (configElement != null)
            {
                _addFormFieldHelper.HashSet(structHashKey, "♠confignode♠", configElement.ToString());
                mainRootElement.Elements($"axpconfigs_{structName}").Remove();
                mainRootElement.Add(configElement);
            }
        }


        private async Task Addbuttons(string structHashKey, string compHashKey)
        {
            string defbtns = _addFormFieldHelper.HashGet(structHashKey, "♠defbtns♠");
            JObject defbtnJsonDoc = JObject.Parse(defbtns);
            IEnumerable<XElement> btns = new List<XElement> { };
            int btncnt = 0;
            foreach (var btn in defbtnJsonDoc)
            {
                string btnVals = btn.Value.ToString();
                //projName = btn.Key.ToString();
                if (btnVals != "")
                {
                    JObject btnValsDoc = JObject.Parse(btnVals);
                    bool visibility = btnValsDoc["visibility"].Value<bool>();
                    if (btnValsDoc["task"].ToString() == "task")
                    {
                        var taskbtn = _addFormFieldHelper.HashGet(structHashKey, "♠taskbtnnode♠");
                        if (!string.IsNullOrWhiteSpace(taskbtn))
                        {
                            ++btncnt;
                            string visibleVal = btnValsDoc["visibility"]?.ToString()?.ToLower() ?? "";
                            taskbtn = taskbtn.ToString().Replace("visible=\"true\"", "visible=\"" + visibleVal + "\"");
                            var taskbtnObj = XElement.Parse(taskbtn);
                            XElement btnele = new XElement(taskbtnObj);
                            btns = btns.Append(btnele);
                        }
                    }
                    else if (visibility)
                    {
                        XElement btnele = getbuttonxml(btnValsDoc, ++btncnt, false, false, null, true);
                        btns = btns.Append(btnele);
                    }

                }
            }

            List<string> btngrps = _addFormFieldHelper.HashScan(compHashKey, "axbtngrp♠*").OrderBy(entry => int.Parse(entry.Value.ToString().Split('♠')[0])).Select(entry => entry.Field.Split('♠')[1]).ToList();
            foreach (string grp in btngrps)
            {
                string grpvals = _addFormFieldHelper.HashGet(structHashKey, grp);
                JObject grpvaldocs = JObject.Parse(grpvals);
                string grpname = grpvaldocs["name"]?.ToString();
                List<string> childBtns = _addFormFieldHelper.HashScan(compHashKey, $"axbtn♠*♠{grpname}").OrderBy(entry => int.Parse(entry.Value.ToString().Split('♠')[0])).Select(entry => entry.Field.Split('♠')[1]).ToList();
                IEnumerable<XElement> childeles = Enumerable.Empty<XElement>();
                foreach (var btn in childBtns)
                {
                    string childbtnValJson = _addFormFieldHelper.HashGet(structHashKey, btn);
                    JObject childbtnValJsonDoc = JObject.Parse(childbtnValJson);
                    childeles = childeles.Append(getbuttonxml(childbtnValJsonDoc, childeles.Count() + 1, false, true));
                }
                var grpele = getbuttonxml(grpvaldocs, ++btncnt, true, false, childeles);
                btns = btns.Append(grpele);
            }

            List<string> udbtns = _addFormFieldHelper.HashScan(compHashKey, "axbtn♠*♠NA").OrderBy(entry => int.Parse(entry.Value.ToString().Split('♠')[0])).Select(entry => entry.Field.Split("♠")[1]).ToList();
            List<string> dcbtns = _addFormFieldHelper.HashScan(compHashKey, "axbtn♠*♠dc*").OrderBy(entry => int.Parse(entry.Value.ToString().Split('♠')[0])).Select(entry => entry.Field.Split("♠")[1]).ToList();
            var mergedbtns = udbtns.Concat(dcbtns);
            foreach (string btn in mergedbtns)
            {
                string udbtnValJson = _addFormFieldHelper.HashGet(structHashKey, btn);
                JObject udbtnValJsonDoc = JObject.Parse(udbtnValJson);
                btns = btns.Append(getbuttonxml(udbtnValJsonDoc, ++btncnt));
            }
            compele = mainRootElement.Element("comps");
            if (compele != null)
            {
                compele.ReplaceWith(new XElement("comps", new XAttribute("cat", "comps"), new XAttribute("dwbtb", "true"), btns));
            }
            else
            {
                mainRootElement.Add(new XElement("comps", new XAttribute("cat", "comps"), new XAttribute("dwbtb", "true"), new XAttribute("btn", btncnt.ToString()), btns));
                compele = mainRootElement.Element("comps");
            }
            var extCompEles = _addFormFieldHelper.HashScan(structHashKey, "ExtCompsNode♠*");
            foreach (var extCompEle in extCompEles)
            {
                XElement node = XElement.Parse(extCompEle.Value.ToString());
                compele.Add(node);
            }
        }

        private XElement getbuttonxml(JObject btnValsDoc, int btncnt, bool isgrp = false, bool ischild = false, IEnumerable<XElement> childeles = null, bool isdefaultbtn = false)
        {
            string btnname = $"btn{btncnt}";
            if (!isdefaultbtn)
            {
                if (!ischild)
                {
                    btnname = $"{btnValsDoc["name"]?.ToString()}";
                }
                else if (ischild)
                {
                    btnname = $"pop{btncnt}";

                }
            }

            string btnPosition = btnValsDoc["position"]?.ToString();
            if (btnPosition.StartsWith("grp"))
            {
                btnPosition = "Default";
            }

            string btnscript = btnValsDoc["script"]?.ToString();
            bool isScript = false;
            if (!string.IsNullOrEmpty(btnscript))
            {
                isScript = true;
            }
            else
            {
                btnscript = btnValsDoc["action"]?.ToString() ?? "";
            }
            string btnparent = btnValsDoc.ContainsKey("parentAttr_oldxml") && !string.IsNullOrEmpty(btnValsDoc.Value<string>("parentAttr_oldxml"))
               ? btnValsDoc.Value<string>("parentAttr_oldxml")
               : "x__heading";
            return new XElement(btnname, !ischild ? new XAttribute("cat", "btn") : null,
                          !ischild ? new XAttribute("parent", btnparent) : null,
                          new XAttribute("key", isdefaultbtn ? btnname : btnValsDoc["name"]?.ToString() ?? ""),
                          new XAttribute("caption", btnValsDoc["caption"]?.ToString() ?? ""),
                          new XAttribute("sname", "t" + structName),
                          new XAttribute("tlhw", "4,9310210,26,28"),
                          new XAttribute("flat", "True"),
                          new XAttribute("img", ""),
                          new XAttribute("position", btnPosition),
                          new XAttribute("container", ""),
                          new XAttribute("hint", btnValsDoc["hint"]?.ToString() ?? ""),
                          new XAttribute("action", btnscript),
                          new XAttribute("task", btnValsDoc["task"]?.ToString() ?? ""),
                          isScript ? new XAttribute("script", isScript.ToString()) : null,
                          new XAttribute("scut", "Ctrl+"),
                          new XAttribute("def", ""),
                          new XAttribute("font", "Tahoma,8,clBlack,ffff"),
                          new XAttribute("footer", btnValsDoc["position"]?.ToString().ToLower() == "footer" ? "true" : "false"),
                          new XAttribute("visible", btnValsDoc["visibility"]?.ToString()?.ToLower() ?? ""),
                          new XAttribute("api", btnValsDoc["api"]?.ToString() ?? ""),
                          isgrp ? new XAttribute("folder", "true") : null,
                          new XElement("icon", new XElement("text", btnValsDoc["icon"]?.ToString() ?? ""), new XElement("addClass", "material-icons")), childeles);
        }
        private void parsefieldValueJSON()
        {
            //ResetAllVariablesToDefaultValues();
            string propVal = string.Empty;

            foreach (var property in fieldValJsonDoc)
            {
                propVal = property.Value.ToString();
                if (propVal != "")
                {
                    string[] PropList = Constants.Tstruct.STRUCTXML_BOOLEAN_ATTRIBUTE_LIST.ToString().Split(',');

                    if (!PropList.Contains(property.Key))
                    {
                        propVal = _addFormFieldHelper.ToCanonicalBoolean(propVal);
                    }

                    switch (property.Key?.ToLower())
                    {
                        case "name":
                            Name = propVal;
                            break;
                        case "caption":
                            Caption = propVal;
                            break;
                        case "allowconfig":
                            RuntimeMode = propVal == "True" ? "T" : "F";
                            break;
                        case "runtimetstruct":
                            RuntimeTstruct = propVal == "True" ? "T" : "F";
                            break;
                        case "enablecache":
                            CachedSave = propVal == "True" ? "T" : "F";
                            break;
                        case "menuaccess":
                            defpage = (propVal == "yes" || propVal == "True") ? "true" : "false";
                            break;
                        case "pform":
                            pform = propVal;
                            break;
                        case "savectrlfld":
                            SaveControl = propVal;
                            break;
                        case "savectrlfldcaption":
                            saveCtrlFldCaption = propVal;
                            break;
                        case "dltctrlfld":
                            DelControl = propVal;
                            break;
                        case "dltctrlfldcaption":
                            dltCtrlFldCaption = propVal;
                            break;
                        case "tracking":
                            Track = propVal;
                            break;
                        case "trackusrs":
                            selectuserlist = propVal;
                            break;
                        case "trackfldoption":
                            trackchangesfield = propVal;
                            break;
                        case "trackby":
                            trackchangesmadeby = propVal;
                            break;
                        case "trackflds":
                            selectfieldlist = propVal;
                            break;
                        case "trackfldcaps":
                            trackFldCaps = propVal;
                            break;
                        case "allowattch":
                            Attach = propVal;
                            break;
                        case "enablelistvw":
                            ListView = propVal;
                            break;
                        case "schema":
                            Schema = propVal;
                            break;
                        case "enablewf":
                            WorkFlow = propVal;
                            break;
                        case "rootnodeattr":
                            rootNodeAttr = propVal;
                            break;
                        case "dcno":
                            FrameNo = propVal;
                            break;
                        case "dccaption":
                            dcCaption = propVal;
                            break;
                        case "tablename":
                            Table = propVal;
                            break;
                        case "allowedit":
                            AllowChange = propVal;
                            break;
                        case "allowempty":
                            DcAllowEmpty = propVal;
                            Empty = propVal;
                            break;
                        case "allowadd":
                            AddDcRows = propVal;
                            break;
                        case "allowdelete":
                            DeleteDcRows = propVal;
                            break;
                        case "popup":
                            PopUp = propVal;
                            break;
                        case "purpose":
                            Purpose = propVal;
                            break;
                        case "asgrid":
                            AsGrid = propVal;
                            break;
                        case "booleandc":
                            booleandc = propVal.ToUpper();
                            break;
                        case "defbooldc":
                            defaultstate = propVal;
                            break;
                        case "fieldtype":
                            CustomDataType = propVal;
                            break;
                        case "customdatatype":
                            CustomDataType2 = propVal;
                            break;
                        case "datatype":
                            DataType = propVal;
                            break;
                        case "width":
                            DataWidth = propVal;
                            break;
                        case "decimal":
                            Dec = propVal;
                            break;
                        case "dbdecimal":
                            dbDec = propVal;
                            break;
                        case "moe":
                        case "source":
                            MOE = propVal;
                            break;
                        case "acceptchoice":
                            acceptChoice = propVal;
                            break;
                        case "apinamesql":
                        case "apinamemo":
                            apiName = propVal;
                            break;
                        case "apiurl":
                            apiURL = propVal;
                            break;
                        case "apireqstr":
                            apiReqStr = propVal;
                            break;
                        case "apiresptype":
                            apiRespType = propVal;
                            break;
                        case "apirespformat":
                            apiRespFormat = propVal;
                            break;
                        case "fillfrmapisrc":
                            fillFrmApiSrc = propVal;
                            break;
                        case "fillfrmapisrccap":
                            fillFrmApiSrcCap = propVal;
                            break;
                        case "intelligence":
                            intelligence = propVal;
                            break;
                        case "hidden":
                            Hide = propVal;
                            break;
                        case "readonly":
                            ReadOnly = propVal;
                            break;
                        case "setcarry":
                            SetCarry = propVal;
                            break;
                        case "savevalue":
                            Save = propVal;
                            break;
                        case "expression":
                            Expr = propVal;
                            break;
                        case "validateexp":
                            VExp = propVal;
                            break;
                        case "valexprsn":
                            FGVExp = propVal;
                            break;
                        case "allowduplicate":
                            Duplicate = propVal;
                            break;
                        case "onlypositive":
                            OnlyPositive = propVal;
                            break;
                        case "sqltext":
                            SQL = propVal;
                            FGSQL = propVal;
                            break;
                        case "isMultilineSQL"://this attribute will be only avaliable for old structures.
                            FG_isMUltilineSQL = propVal;
                            break;
                        case "a5nodeattr":
                            FG_a5NodeAttr = propVal;
                            break;
                        case "oldsqltext":
                            oldSQL = propVal;
                            break;
                        case "sqlparam":
                            fldSQLParam = propVal;
                            break;
                        case "sqlparamcnt":
                            fldSQLParamCnt = propVal;
                            break;
                        case "dynamicparams":
                            Dynamic = propVal;
                            break;
                        case "savenomralized":
                            SourceKey = propVal;
                            break;
                        case "separator":
                            separator = propVal;
                            break;
                        case "autoselect":
                            AutoSelect = propVal;
                            break;
                        case "combobox":
                            comboBox = propVal;
                            break;
                        case "ctypevalue":
                            ctypeValue = propVal;
                            break;
                        case "refreshonsave":
                            Refresh = propVal;
                            break;
                        case "deflist":
                            List = propVal;
                            break;
                        case "dsform":
                            SourceForm = propVal;
                            break;
                        case "dsformcaption":
                            DSFormCaption = propVal;
                            break;
                        //case "sourcefield":
                        case "dsfield":
                        case "srcfield":
                            Source = propVal;
                            fgsourcefield.Add(propVal);
                            break;
                        case "dsfieldcaption":
                            DSFieldCaption = propVal;
                            break;
                        case "dstable":
                        case "srctable":
                            SourceTable = propVal;
                            break;
                        case "datasource":
                            CField = propVal;
                            break;
                        case "datasourcecaption":
                            CFieldCaption = propVal;
                            break;
                        case "srcformcaption":
                            sformCaption = propVal;
                            break;
                        case "mdmctrlfld":
                            MDControl = propVal;
                            break;
                        case "mdmctrlfldcap":
                            MDControlCap = propVal;
                            break;
                        case "suggestive":
                            Suggestive = propVal;
                            break;
                        case "sequence":
                            Sequence = propVal;
                            break;
                        case "masking":
                            Mask = propVal;
                            break;
                        case "lastcharmask":
                            lastcharmask = propVal;
                            break;
                        case "firstcharmask":
                            firstcharmask = propVal;
                            break;
                        case "maskchar":
                            maskchar = propVal;
                            break;
                        case "maskroles":
                            maskroles = propVal;
                            break;
                        case "pattern":
                            Pattern = propVal;
                            break;
                        case "pwdchar":
                            PWordChar = propVal;
                            break;
                        case "hint":
                            Hint = propVal;
                            break;
                        case "applycomma":
                            ApplyComma = propVal;
                            break;
                        case "clientvalidation":
                            CValidate = propVal;
                            break;
                        case "searchsql":
                            SearchSQL = propVal;
                            break;
                        case "displaydetails":
                            displaydetail = propVal;
                            break;
                        case "tabstop":
                            Tabstop = propVal;
                            break;
                        case "runtimecriteria":
                            runtimecriteria = propVal;
                            break;
                        case "runtimefld":
                            runtimefld = propVal;
                            break;
                        case "applyrule":
                            applyrule = propVal;
                            break;
                        case "encrypted":
                            encrypted = propVal;
                            break;
                        case "dcname":
                            fldDcName = propVal;
                            break;
                        case "radiotype":
                            radiotype = propVal;
                            break;
                        case "deftable":
                            TableTypeDetails = propVal;
                            break;
                        // case "targettrasid":
                        case "posttst":
                            GenTarget = propVal;
                            break;
                        case "posttstcap":
                            GenTargetCaption = propVal;
                            break;
                        case "basedondc":
                            GenParent = propVal;
                            break;
                        case "ctrlfield":
                            GenControl = propVal;
                            break;
                        case "ctrlfieldcap":
                            GenControlCaption = propVal;
                            break;
                        case "active":
                            Active = propVal;
                            scrActive = propVal;
                            break;

                        case "mapdata":
                            GMap = propVal;
                            break;
                        case "postevent":
                            postEvent = propVal;
                            break;
                        case "onpostscript":
                            OnPost = propVal;
                            break;
                        case "targetfield":
                            targetfields.Add(propVal);
                            fgtargetfield.Add(propVal);
                            break;
                        case "rowctrldata":
                            genRowCtrlData = propVal;
                            break;
                        case "mapgrpflds":
                            mapGroupFlds = propVal;
                            break;
                        case "targetdc":
                            FGTarget = propVal;
                            break;
                        case "targetdccap":
                            FGTargetCap = propVal;
                            break;
                        case "multiselect":
                            Multi = propVal;
                            break;
                        case "autoshow":
                            AutoShow = propVal;
                            break;
                        case "sourcedc":
                            SourceDC = propVal;
                            break;
                        case "sourcedccap":
                            SourceDCCap = propVal;
                            break;
                        case "vvalidate":
                            FGValidate = propVal;
                            break;
                        case "execonsave":
                            ExeOnSave = propVal;
                            break;
                        case "validate":
                            FGValidate = propVal;
                            break;
                        case "firmbind":
                            FirmBind = propVal;
                            break;
                        case "selecton":
                            SelectOn = propVal;
                            break;
                        case "footerstring":
                            FooterStr = propVal;
                            break;
                        case "autoselallrow":
                            SelectAllRows = propVal;
                            break;
                        case "addrows":
                            AddRows = propVal;
                            break;
                        case "fillgridmapdetails":
                            parseFGMapDetails(propVal);
                            break;
                        case "sourcecaption":
                            fgsourcecaption.Add(propVal);
                            break;
                        case "grpfldinfo":
                            fggrpfield = propVal;
                            break;
                        case "mastertransid":
                            MField = propVal;
                            break;
                        case "masterform":
                            MasterForm = propVal;
                            break;
                        case "masterformcaption":
                            MasterFormCaption = propVal;
                            break;
                        case "masterfield":          //masterfield is not used in xml
                            MasterField = propVal;
                            break;
                        case "masterfieldcaption":
                            MasterFieldCaption = propVal;
                            break;
                        case "oldmasterfieldattr":
                            oldmasterfieldAttr = propVal;
                            break;
                        case "fillmaster":
                            FParent = propVal;
                            break;
                        case "fillmastercaption":
                            fParentCaption = propVal;
                            break;
                        case "fillsource":
                            FSource = propVal;
                            break;
                        case "initondel":
                            InitOnDel = propVal;
                            break;
                        case "masterfielddec":
                            MasterFieldDec = propVal;
                            break;
                        case "extended":
                            Extended = propVal;
                            break;
                        case "mdmmasterform":
                            Master = propVal;
                            break;
                        case "mdmmasterformcaption":
                            MasterCap = propVal;
                            break;
                        case "mdmmasterfield":
                            MField = propVal;
                            break;
                        case "mdmmasterfieldcap":
                            MFieldCap = propVal;
                            break;
                        case "mdmmasterfielddecimal":
                            MasterFieldDec = propVal;
                            break;
                        case "mdmdetailfield":
                            DField = propVal;
                            break;
                        case "mdmdetailfieldCap":
                            DFieldCap = propVal;
                            break;
                        case "mdmdetailsrchfield":
                            DSField = propVal;
                            break;
                        case "mdmdetailsrchfieldcap":
                            DSFieldCap = propVal;
                            break;
                        case "mdmmastersrchfield":
                            MSField = propVal;
                            break;
                        case "mdmmastersrchfieldcap":
                            MSFieldCap = propVal;
                            break;
                        case "mdmmastertable":
                            MTable = propVal;
                            break;
                        case "updttype":
                            Type = propVal;
                            break;
                        case "append":
                            Append = propVal;
                            break;
                        case "initondlt":
                            InitOnDel = propVal;
                            break;
                        case "exeonapprove":
                            OnApprove = propVal;
                            break;
                        case "exeonreject":
                            OnReject = propVal;
                            break;
                        case "defbyscrpt":
                            Extended = propVal;
                            break;
                        case "mdmmaptext":
                            MapText = propVal;
                            break;
                        case "scriptevent":
                            Event = propVal;
                            break;
                        case "appliedon":
                            appliedon = propVal;
                            break;
                        case "formcontrol":
                            controlType = propVal.ToLower();
                            break;
                        case "pushtoqueue":
                            pushtoqueue = propVal.ToLower();
                            break;
                        case "queuename":
                            queuename = propVal;
                            break;
                        case "confirmmsgreq":
                            confirmmsgreq = propVal.ToLower();
                            break;
                        case "confirmmsg":
                            confirmmsg = propVal;
                            break;
                        case "script":
                            expression = propVal;
                            break;
                        case "fcscript":
                            fcExpression = propVal;
                            break;
                        case "position":
                            fldPosRef = propVal; break;
                        case "fldposition":
                            fldPosition = propVal; break;
                        case "displaytotal":
                            displayTotal = propVal; break;
                        case "hideinsearch":
                            hideInSearch = propVal; break;
                        case "spldatatype":
                            splDataType = propVal; break;
                    }
                }
            }
        }
        private void AddTstructProperties()
        {
            XElement tstElemet = new(Name, new XAttribute("cat", "tstruct"),
            new XElement(Constants.Xml_TstProperties.Transid, Name),
            new XElement(Constants.Xml_TstProperties.Caption, Caption),
            new XElement(Constants.Xml_TstProperties.SaveControl, new XAttribute("fcaption", saveCtrlFldCaption), SaveControl),
            new XElement(Constants.Xml_TstProperties.DelControl, new XAttribute("fcaption", dltCtrlFldCaption), DelControl),
            (Track == "False" ? new XElement(Constants.Xml_TstProperties.Track, Track) : getTrackElement(Track)),
            new XElement(Constants.Xml_TstProperties.DateField, DateField),
            new XElement(Constants.Xml_TstProperties.OnOpen, OnOpen),
            new XElement(Constants.Xml_TstProperties.AfterLoadData, AfterLoadData),
            new XElement(Constants.Xml_TstProperties.BeforeSave, BeforeSave),
            new XElement(Constants.Xml_TstProperties.AfterSave, AfterSave),
            new XElement(Constants.Xml_TstProperties.BeforeDelete, BeforeDelete),
            new XElement(Constants.Xml_TstProperties.AfterDelete, AfterDelete),
            new XElement(Constants.Xml_TstProperties.navigate, navigate),
            new XElement(Constants.Xml_TstProperties.TreeParents, TreeParents),
            new XElement(Constants.Xml_TstProperties.Attach, Attach),
            new XElement(Constants.Xml_TstProperties.SearchCondition, new XElement("l", "")),
            new XElement(Constants.Xml_TstProperties.ListView, ListView),
            new XElement(Constants.Xml_TstProperties.Schema, Schema),
            new XElement(Constants.Xml_TstProperties.WorkFlow, _addFormFieldHelper.ToCanonicalBoolean(WorkFlow)),
            new XElement(Constants.Xml_TstProperties.SaveDraft, SaveDraft),
            new XElement(Constants.Xml_TstProperties.Traceability, Traceability),
            new XElement(Constants.Xml_TstProperties.CachingType, CachingType),
            new XElement(Constants.Xml_TstProperties.DcNavigation, DcNavigation),
            new XElement(Constants.Xml_TstProperties.Pos, Pos),
            new XElement(Constants.Xml_TstProperties.savemode, new XAttribute("savemode", ""),
               new XElement("apiqueuename", ""), new XElement("savequeuename", "")),
            new XElement(Constants.Xml_TstProperties.Purpose, Purpose));

            if (mainRootElement.Elements(Name).Any())
            {
                tstElemet.Add(
                    new XAttribute("runtimemod", RuntimeMode), new XAttribute("runtimetstruct", RuntimeTstruct),
                    new XAttribute("cachedsave", CachedSave)
                //new XAttribute("defpage", defpage)
                );


                mainRootElement.Element(Name).ReplaceWith(tstElemet);
            }
            else
            {
                mainRootElement.Add(tstElemet);

            }


        }
        private void AddToolbar()
        {
            XElement toolbarEle = XElement.Parse(Constants.Tstruct.XML_DEFAULT_COMPNODE.Replace("♦♦stransid♦♦", Name));
            mainRootElement.Add(toolbarEle);
        }
        /// Add DC node to struct xml
        private void AddDcProperties()
        {
            XElement dcElemet = new(Name, new XAttribute("cat", "dc"), new XAttribute("ordno", dcOrdNo),
            new XElement(Constants.Xml_TstProperties.Name, Name),
            new XElement(Constants.Xml_TstProperties.Caption, Caption),
            new XElement(Constants.Xml_TstProperties.dcProperties.FrameNo, FrameNo),
            new XElement(Constants.Xml_TstProperties.dcProperties.PageNo, PageNo),
            new XElement(Constants.Xml_TstProperties.dcProperties.Table, Table),
            new XElement(Constants.Xml_TstProperties.dcProperties.AsGrid, AsGrid),
            new XElement(Constants.Xml_TstProperties.dcProperties.AllowChange, AllowChange),
            new XElement(Constants.Xml_TstProperties.dcProperties.PopUp, PopUp),
            new XElement(Constants.Xml_TstProperties.dcProperties.dcborder, dcborder),
            new XElement(Constants.Xml_TstProperties.dcProperties.OnEnterDC, OnEnterDC),
            new XElement(Constants.Xml_TstProperties.dcProperties.OnExitDC, OnExitDC),
            new XElement(Constants.Xml_TstProperties.dcProperties.DcAllowEmpty, DcAllowEmpty),
            new XElement(Constants.Xml_TstProperties.dcProperties.AddDcRows, AddDcRows),
            new XElement(Constants.Xml_TstProperties.dcProperties.DeleteDcRows, DeleteDcRows),
            new XElement(Constants.Xml_TstProperties.Purpose, Purpose));

            if (Name != "dc1")
            {
                dcElemet.SetAttributeValue("booleandc", booleandc);
                if (booleandc == "TRUE")
                    dcElemet.SetAttributeValue("defaultstate", defaultstate);
            }

            if (mainRootElement.Elements(Name).Any())
            {
                mainRootElement.Element(Name).ReplaceWith(dcElemet);
            }
            else
            {
                mainRootElement.Descendants("comps")?.FirstOrDefault()?.AddBeforeSelf(dcElemet);

            }

        }
        private async Task AddIframeNode()
        {
            if (isOldXMLExists)
            {
                string structHashKey = $"{projName}♠tst♠{structName}";
                var iframeNode = _addFormFieldHelper.HashGet(structHashKey, "♠iframenode♠");
                if (!string.IsNullOrEmpty(iframeNode))
                {

                    XElement _iframeNode = XElement.Parse(iframeNode.ToString());
                    var layout = _iframeNode.Attribute("layout")?.Value?.ToString();
                    if (string.IsNullOrEmpty(layout) || layout.ToLower() == "default")
                    {
                        mainRootElement.Add(new XElement("iframes", new XAttribute("cat", "iframes"), new XAttribute("layout", "default")));
                    }
                    else if (layout.ToLower() == "tabbed")
                    {
                        mainRootElement.Add(new XElement("iframes", new XAttribute("cat", "iframes"), new XAttribute("layout", "tabbed")));
                    }
                    else
                    {
                        mainRootElement.Add(XElement.Parse(iframeNode.ToString()));
                    }
                }
                else
                {
                    mainRootElement.Add(new XElement("iframes", new XAttribute("cat", "iframes"), new XAttribute("layout", "default")));
                }
            }
            else
            {
                mainRootElement.Add(new XElement("iframes", new XAttribute("cat", "iframes"), new XAttribute("layout", "default")));
            }
        }
        private void AddUpdateTempNode()
        {
            var tempEle = new XElement("temp",
                new XElement("menupos", menuposition),
                new XElement("menulist", ""),
                new XElement("listview", ListView.ToLower()),
                new XElement("layout", "Default"),
                new XElement("fcaption", Caption),
                new XElement("defpage", defpage),
                new XElement("includepage", "F"),
                new XElement("changeddc", "")
                );
            if (mainRootElement.Elements("temp").Any())
            {
                mainRootElement.Element("temp").ReplaceWith(tempEle);
            }
            else
            {
                mainRootElement.Add(tempEle);
            }

        }
        private void AddFldProperties()
        {
            XElement fldElemet = createFieldNodeXml();
            if (mainRootElement.Elements(Name).Any())
            {
                mainRootElement.Element(Name).ReplaceWith(fldElemet);
            }
            else
            {
                mainRootElement.Descendants("comps")?.FirstOrDefault()?.AddBeforeSelf(fldElemet);
            }

            if (hideInSearch.ToLower() == "false")
            {
                searchColList.Add(Name);
            }
        }
        private XElement createFieldNodeXml()
        {
            XElement fldElemet = new(Name, new XAttribute("cat", "field"), new XAttribute("dcno", FrameNo), new XAttribute("dccap", dcCaption), new XAttribute("fieldtype", CustomDataType), new XAttribute("customdatatype", CustomDataType2), new XAttribute("runtimecriteria", runtimecriteria == "true" ? "T" : "F"),
            new XAttribute("runtimefld", runtimefld.ToLower() == "true" ? "T" : "F"), new XAttribute("applyrule", applyrule.ToLower() == "true" ? "T" : "F"), new XAttribute("encrypted", encrypted.ToLower() == "true" ? "T" : "F"),
            new XAttribute("displaytotal", displayTotal.ToLower() == "true" ? "T" : "F"),
            new XElement(Constants.Xml_TstProperties.Name, Name),
            new XElement(Constants.Xml_TstProperties.Caption, Caption),
            new XElement(Constants.Xml_TstProperties.fieldProperties.DataType, DataType),
            new XElement(Constants.Xml_TstProperties.fieldProperties.DataWidth, DataWidth),
            new XElement(Constants.Xml_TstProperties.fieldProperties.Dec, Dec),
            new XElement(Constants.Xml_TstProperties.fieldProperties.MOE, getMOEAttributes(), MOE.ToLower().StartsWith("select") ? "Select" : MOE.ToLower() == "from api" ? "Accept" : MOE),
            new XElement(Constants.Xml_TstProperties.fieldProperties.Details,
                new XElement(Constants.Xml_TstProperties.fieldProperties.SQL, getSQLAttributes()),
                new XElement(Constants.Xml_TstProperties.fieldProperties.CField, new XAttribute("cfcap", CFieldCaption), CField),
                new XElement(Constants.Xml_TstProperties.fieldProperties.SGWidth, SGWidth),
                new XElement(Constants.Xml_TstProperties.fieldProperties.SGHeight, SGHeight),
                new XElement(Constants.Xml_TstProperties.fieldProperties.AutoSelect, MOE.ToLower() == "calculate" ? "" : AutoSelect),
                new XElement(Constants.Xml_TstProperties.fieldProperties.Refresh, MOE.ToLower() == "calculate" ? "" : Refresh),
                new XElement(Constants.Xml_TstProperties.fieldProperties.List, List != "" ? constructListElements(List) : ""),
                new XElement(Constants.Xml_TstProperties.fieldProperties.Source, getSourceAttributes(), Source),
                new XElement(Constants.Xml_TstProperties.fieldProperties.FParent, new XAttribute("fcaption", fParentCaption), FParent),
                new XElement(Constants.Xml_TstProperties.fieldProperties.FSource, FSource),
                new XElement(Constants.Xml_TstProperties.fieldProperties.SourceTable, CustomDataType == "DropDown" ? "" : SourceTable),
                new XElement(Constants.Xml_TstProperties.fieldProperties.Suggestive, getSuggestiveValue()),
                //new XElement(Constants.Xml_TstProperties.fieldProperties.Trans, CustomDataType == "DropDown" ? "" : Trans),
                new XElement(Constants.Xml_TstProperties.fieldProperties.Trans, MOE == "select from sql" ? SourceTable : ""),
                new XElement(Constants.Xml_TstProperties.fieldProperties.Sequence, Sequence != "" ? constructSeqElements(Sequence) : ""),
                new XElement(Constants.Xml_TstProperties.fieldProperties.Dynamic, getDynamicFilterEles())
            ),
            new XElement(Constants.Xml_TstProperties.fieldProperties.Hide, Hide),
            new XElement(Constants.Xml_TstProperties.fieldProperties.Empty, Empty),
            new XElement(Constants.Xml_TstProperties.fieldProperties.ReadOnly, ReadOnly),
            new XElement(Constants.Xml_TstProperties.fieldProperties.SetCarry, SetCarry),
            new XElement(Constants.Xml_TstProperties.fieldProperties.Save, Save),
            new XElement(Constants.Xml_TstProperties.fieldProperties.Expr, Expr),
            new XElement(Constants.Xml_TstProperties.fieldProperties.VExp, VExp),
            new XElement(Constants.Xml_TstProperties.fieldProperties.Duplicate, Duplicate),
            new XElement(Constants.Xml_TstProperties.fieldProperties.OnlyPositive, OnlyPositive),
            new XElement(Constants.Xml_TstProperties.fieldProperties.SourceKey, SourceKey),
            new XElement(Constants.Xml_TstProperties.fieldProperties.DispTot, DispTot),
            new XElement(Constants.Xml_TstProperties.fieldProperties.gui, gui),
            new XElement(Constants.Xml_TstProperties.fieldProperties.cwidth, cwidth),
            new XElement(Constants.Xml_TstProperties.fieldProperties.tlhw, tlhw),
            new XElement(Constants.Xml_TstProperties.fieldProperties.cfont, cfont),
            new XElement(Constants.Xml_TstProperties.fieldProperties.ccolor, ccolor),
            new XElement(Constants.Xml_TstProperties.fieldProperties.bordercolor, bordercolor),
            new XElement(Constants.Xml_TstProperties.fieldProperties.showsno, showsno),
            new XElement(Constants.Xml_TstProperties.fieldProperties.OnEnterField, OnEnterField),
            new XElement(Constants.Xml_TstProperties.fieldProperties.OnExitField, OnExitField),
            new XElement(Constants.Xml_TstProperties.fieldProperties.Mask, new XAttribute("masking", Mask), new XAttribute("lastcharmask", lastcharmask), new XAttribute("firstcharmask", firstcharmask), new XAttribute("maskchar", maskchar), new XAttribute("maskroles", maskroles)),
            new XElement(Constants.Xml_TstProperties.fieldProperties.Pattern, Pattern),
            new XElement(Constants.Xml_TstProperties.fieldProperties.PWordChar, PWordChar),
            new XElement(Constants.Xml_TstProperties.fieldProperties.Hint, Hint),
            new XElement(Constants.Xml_TstProperties.fieldProperties.ApplyComma, ApplyComma),
            new XElement(Constants.Xml_TstProperties.fieldProperties.CValidate, CValidate),
            new XElement(Constants.Xml_TstProperties.fieldProperties.FormCtrl, FormCtrl),
            new XElement(Constants.Xml_TstProperties.fieldProperties.ExprFlds, ExprFlds),
            new XElement(Constants.Xml_TstProperties.fieldProperties.Deps, Deps),
            new XElement(Constants.Xml_TstProperties.fieldProperties.DetStruct, DetStruct),
            new XElement(Constants.Xml_TstProperties.fieldProperties.SearchSQL, SearchSQL),
            new XElement(Constants.Xml_TstProperties.fieldProperties.displaydetail, displaydetail),
            new XElement(Constants.Xml_TstProperties.fieldProperties.ParentDeps, ParentDeps),
            new XElement(Constants.Xml_TstProperties.fieldProperties.Tabstop, Tabstop),
            new XElement(Constants.Xml_TstProperties.fieldProperties.CustDType, splDataType),
            new XElement(Constants.Xml_TstProperties.fieldProperties.TableTypeDetails, TableTypeDetails),
            new XElement(Constants.Xml_TstProperties.Purpose, Purpose)
            );

            if (CustomDataType == "Decimal Number" || CustomDataType == "Whole Number")
            {
                fldElemet.SetAttributeValue("dbdecimal", dbDec);
            }


            if (CustomDataType == "RadioGroup")
            {
                fldElemet.Element(Constants.Xml_TstProperties.fieldProperties.gui).Add(new XAttribute("ctype", "Radio Group"),
                    new XAttribute("type", !string.IsNullOrEmpty(radiotype) ? radiotype.ToLower().First() : 'h'));
            }
            if (CustomDataType == "CheckList")
            {
                fldElemet.Element(Constants.Xml_TstProperties.fieldProperties.gui).Add(new XAttribute("ctype", "Check list"),
                    new XAttribute("type", "v"), new XAttribute("sep", separator));
            }
            if (CustomDataType == "Check box")
            {
                fldElemet.Element(Constants.Xml_TstProperties.fieldProperties.gui).Add(new XAttribute("ctype", "Check box"),
                    new XAttribute("type", "v"));
            }
            if (CustomDataType == "Large Text" && ctypeValue != "")
            {
                fldElemet.Element(Constants.Xml_TstProperties.fieldProperties.gui).Add(new XAttribute("ctype", ctypeValue),
                    new XAttribute("type", "v"));
            }

            if (CustomDataType2 != "NA" && DataType.ToLower() == "numeric")
            {
                fldElemet.SetAttributeValue("customdecimal", "T");
            }
            else
            {
                fldElemet.SetAttributeValue("customdecimal", "F");

            }

            return fldElemet;
        }
        private async Task AddGenmapProperties()
        {
            if (postEvent == "on approve & reject")
            {
                OnApprove = OnReject = "True";
            }
            else if (postEvent == "on approve only")
            {
                OnApprove = "True";
                OnReject = "False";
            }
            else if (postEvent == "on reject only")
            {
                OnApprove = "False";
                OnReject = "True";
            }
            else
            {
                OnApprove = OnReject = "False";
            }

            XElement genmapElemet = new(Name, new XAttribute("cat", "genmap"),
            new XElement(Constants.Xml_TstProperties.Name, Name),
            new XElement(Constants.Xml_TstProperties.Caption, Caption),
            new XElement(Constants.Xml_TstProperties.genmapProperties.GenTarget, new XAttribute("tcap", GenTargetCaption), GenTarget),
            new XElement(Constants.Xml_TstProperties.genmapProperties.GenParent, GenParent),
            new XElement(Constants.Xml_TstProperties.genmapProperties.GenControl, new XAttribute("gctrlcap", GenControlCaption), GenControl),
            new XElement(Constants.Xml_TstProperties.genmapProperties.Active, Active),
            new XElement(Constants.Xml_TstProperties.genmapProperties.GenCField, GenCField),
            new XElement(Constants.Xml_TstProperties.genmapProperties.GMap, await getGMPMapXml(GenTarget)),
            new XElement(Constants.Xml_TstProperties.genmapProperties.OnPost, OnPost),
            new XElement(Constants.Xml_TstProperties.genmapProperties.OnApprove, OnApprove),
            new XElement(Constants.Xml_TstProperties.genmapProperties.OnReject, OnReject),
            new XElement(Constants.Xml_TstProperties.Purpose, Purpose));

            var existingElement = mainRootElement.Element(Name);
            if (existingElement != null)
            {
                existingElement.ReplaceWith(genmapElemet);
            }
            else
            {
                var scriptElement = mainRootElement.Descendants("scripts").FirstOrDefault();
                var insertBeforeElement = scriptElement ?? mainRootElement.Descendants("comps").FirstOrDefault();

                if (insertBeforeElement != null)
                {
                    insertBeforeElement.AddBeforeSelf(genmapElemet);
                }
            }

        }
        private void AddFillgridProperties()
        {
            var a5NodeAttrJSON = new JObject();
            if (FG_a5NodeAttr != string.Empty) a5NodeAttrJSON = JObject.Parse(FG_a5NodeAttr);
            if (oldSQL != FGSQL)
            {
                var sqlparameters = _addFormFieldHelper.GetSqlParameters(FGSQL);
                a5NodeAttrJSON["param"] = sqlparameters.Count > 0 ? "y" : "n";
                a5NodeAttrJSON["pcount"] = sqlparameters.Count.ToString();
                a5NodeAttrJSON["plist"] = string.Join(", ", sqlparameters);
            }
            var a5NodeAttr = a5NodeAttrJSON.Properties()
            .Select(p => new XAttribute(p.Name, p.Value.ToString())) // Convert each property to XAttribute
            .ToList(); // Store as List<XAttribute>
            dynamic a5NodeVal;
            if (FG_isMUltilineSQL == "True")
            {
                a5NodeVal = FGSQL.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select((ele, ind) => new XElement("l" + ind, ele));
            }
            else
            {
                a5NodeVal = FGSQL;
            }
            XElement fgElemet = new(Name, new XAttribute("cat", "fillgrid"),
            new XElement(Constants.Xml_TstProperties.Name, Name),
            new XElement(Constants.Xml_TstProperties.fillgridProperties.IView, "False"),
            new XElement("a3", ""),
            new XElement(Constants.Xml_TstProperties.fillgridProperties.FGCaption, Caption),
            new XElement(Constants.Xml_TstProperties.fillgridProperties.FGSQL, a5NodeAttr, a5NodeVal),
            new XElement(Constants.Xml_TstProperties.fillgridProperties.FGTarget, new XAttribute("tcap", FGTargetCap), FGTarget),
            new XElement(Constants.Xml_TstProperties.fillgridProperties.FMap, new XAttribute("gfld", fggrpfield),
                        fgtargetfield.Select((ele, ind) => new XElement(ele, new XAttribute("cap", fgsourcecaption.ElementAtOrDefault(ind) ?? ""), new XAttribute("tfcap", fgtargetfieldCap.ElementAtOrDefault(ind) ?? ""), fgsourcefield.ElementAtOrDefault(ind) ?? ""))
            ),
            new XElement(Constants.Xml_TstProperties.fillgridProperties.Multi, Multi),
            new XElement(Constants.Xml_TstProperties.fillgridProperties.AutoShow, AutoShow),
            new XElement(Constants.Xml_TstProperties.fillgridProperties.SourceDC, new XAttribute("scap", SourceDCCap), SourceDC),
            new XElement(Constants.Xml_TstProperties.fillgridProperties.FGValidate, FGValidate),
            new XElement(Constants.Xml_TstProperties.fillgridProperties.OnFill, OnFill),
            new XElement(Constants.Xml_TstProperties.fillgridProperties.ExeOnSave, ExeOnSave),
            new XElement(Constants.Xml_TstProperties.fillgridProperties.FirmBind, FirmBind),
            new XElement(Constants.Xml_TstProperties.fillgridProperties.SelectOn, SelectOn),
            new XElement(Constants.Xml_TstProperties.fillgridProperties.FooterStr, FooterStr),
            new XElement(Constants.Xml_TstProperties.fillgridProperties.FGVExp, FGVExp),
            new XElement(Constants.Xml_TstProperties.fillgridProperties.AddRows, AddRows),
            new XElement(Constants.Xml_TstProperties.fillgridProperties.FGColProps, FGColProps),
            new XElement(Constants.Xml_TstProperties.fillgridProperties.SelectAllRows, SelectAllRows),
            new XElement(Constants.Xml_TstProperties.Purpose, Purpose));
            var existingElement = mainRootElement.Element(Name);
            if (existingElement != null)
            {
                existingElement.ReplaceWith(fgElemet);
            }
            else
            {
                var scriptElement = mainRootElement.Descendants("scripts").FirstOrDefault();
                var insertBeforeElement = scriptElement ?? mainRootElement.Descendants("comps").FirstOrDefault();

                if (insertBeforeElement != null)
                {
                    insertBeforeElement.AddBeforeSelf(fgElemet);
                }
            }
        }

        private void AddMdMapProperties()
        {
            var sqlparameters = _addFormFieldHelper.GetSqlParameters(FGSQL);
            XElement mdElemet = new(Name, new XAttribute("cat", "mdmap"),
            new XElement(Constants.Xml_TstProperties.Name, Name),
            new XElement(Constants.Xml_TstProperties.Caption, Caption),
            new XElement(Constants.Xml_TstProperties.mdmapProperties.Master, new XAttribute("mcap", MasterCap), Master),
            new XElement(Constants.Xml_TstProperties.mdmapProperties.MField, new XAttribute("mfcap", MFieldCap), MField),
            new XElement(Constants.Xml_TstProperties.mdmapProperties.DField, new XAttribute("dfcap", DFieldCap), DField),
            new XElement(Constants.Xml_TstProperties.mdmapProperties.MSField, new XAttribute("msfcap", MSFieldCap), MSField),
            new XElement(Constants.Xml_TstProperties.mdmapProperties.DSField, new XAttribute("dsfcap", DSFieldCap), DSField),
            new XElement(Constants.Xml_TstProperties.mdmapProperties.MTable, MTable),
            new XElement(Constants.Xml_TstProperties.mdmapProperties.Type, Type),
            new XElement(Constants.Xml_TstProperties.mdmapProperties.Tree, Tree),
            new XElement(Constants.Xml_TstProperties.mdmapProperties.MDControl, new XAttribute("cfcap", MDControlCap), MDControl),
            new XElement(Constants.Xml_TstProperties.mdmapProperties.Append, Append),
            new XElement(Constants.Xml_TstProperties.mdmapProperties.InitOnDel, InitOnDel),
            new XElement(Constants.Xml_TstProperties.mdmapProperties.MasterFieldDec, MasterFieldDec),
            new XElement(Constants.Xml_TstProperties.mdmapProperties.MapText, MapText),
            new XElement(Constants.Xml_TstProperties.mdmapProperties.Extended, Extended == "yes" ? "true" : "False"),
            new XElement(Constants.Xml_TstProperties.mdmapProperties.OnApprove, OnApprove),
            new XElement(Constants.Xml_TstProperties.mdmapProperties.OnReject, OnReject),
            new XElement(Constants.Xml_TstProperties.Purpose, Purpose));
            var existingElement = mainRootElement.Element(Name);
            if (existingElement != null)
            {
                existingElement.ReplaceWith(mdElemet);
            }
            else
            {
                var scriptElement = mainRootElement.Descendants("scripts").FirstOrDefault();
                var insertBeforeElement = scriptElement ?? mainRootElement.Descendants("comps").FirstOrDefault();

                if (insertBeforeElement != null)
                {
                    insertBeforeElement.AddBeforeSelf(mdElemet);
                }
            }
        }

        private void AddScriptProperties()
        {
            string _scriptExprsn = controlType == "false" ? expression : fcExpression;
            XElement newScriptElement = new(Name,
                new XAttribute("cap", Caption),
                new XAttribute("type", "tstruct"),
                new XAttribute("apply", Event),
                new XAttribute("applyon", appliedon),
                new XAttribute("sname", structName),
                new XAttribute("scripts", "yes"),
                new XAttribute("desc", ""),
                new XAttribute("failmessage", ""),
                new XAttribute("validate", "no"),
                new XAttribute("formcontrol", controlType),
                new XAttribute("pushtoqueue", pushtoqueue == "true" ? "t" : "f"),
                new XAttribute("queuename", queuename),
                new XAttribute("confreq", confirmmsgreq == "true" ? "t" : "f"),
                new XAttribute("confmsg", confirmmsg),
                new XAttribute("breakonerror", "no"),
                new XAttribute("localnum", "2"),
                new XAttribute("active", scrActive),
                new XElement("r1", new XAttribute("localnum", "2"),
                    new XAttribute("id", "a1"),
                    new XAttribute("op", "5"),
                    new XAttribute("task", "scripts"),
                    new XAttribute("type", ""),
                    new XAttribute("sname", ""),
                    new XAttribute("apply", ""),
                    new XAttribute("applyon", ""),
                    new XElement("param1", new XAttribute("task", "scripts"),
                        new XAttribute("op", "7"),
                        new XElement("exprset",
                        _scriptExprsn.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select((ele, ind) => new XElement("l" + ind, ele))
                        )
                    )
                ));
            var scriptParent = mainRootElement.Element("scripts");
            if (scriptParent != null)
            {
                var scriptEle = scriptParent.Element(Name);
                if (scriptEle != null)
                {
                    scriptEle.ReplaceWith(newScriptElement);
                }
                else
                {
                    scriptParent.Add(newScriptElement);
                }

            }
            else
            {
                if (compele == null)
                {
                    compele = mainRootElement.Element("comps");
                }
                compele.AddBeforeSelf(new XElement("scripts", new XAttribute("cat", "actions"), newScriptElement));

            }

        }
        private async Task<List<XElement>> getGMPMapXml(string trgtStructName)
        {
            var result = new List<XElement>();
            //iElements = targetfields.Select((ele, ind) =>
            //{
            //    return new XElement(ele, new XAttribute("trow", targetRows[ind]), new XAttribute("srow", targetRows[ind]),
            //            new XAttribute("type", "field"), new XAttribute("control", ""), new XAttribute("group", ""),
            //            new XAttribute("ord", ""), new XAttribute("id", "False"), fsource[ind]);
            //});
            //return iElements;

            var jArray = JArray.Parse(GMap);
            var sortedGmapArray = new JArray(
                    jArray.OrderBy(j =>
                    {
                        string rowStr = j["trgtRow"]?.ToString() ?? "";
                        return rowStr == "1" ? -1 : rowStr == "0" ? int.MaxValue - 1 : int.TryParse(rowStr, out int val) ? val : int.MaxValue;
                    })
                );
            var rowCtrlData = JArray.Parse(genRowCtrlData);
            DataTable trgtStrctFieldList = null;

            foreach (JObject obj in sortedGmapArray)
            {
                string name = obj["trgtFldHdn"]?.ToString() ?? string.Empty;
                string srcFld = obj["srcFld"]?.ToString() ?? string.Empty;
                bool isSrcFldGridDc = false;
                string type = "field";


                if (!string.IsNullOrEmpty(srcFld))
                {
                    try
                    {
                        var srcFldObj = JObject.Parse(srcFld);
                        type = srcFldObj["type"]?.ToString() ?? "field";
                        isSrcFldGridDc = (bool?)srcFldObj["isgrid"] ?? false;
                    }
                    catch (Exception ex)
                    {
                        type = "field";
                        isSrcFldGridDc = false;
                    }
                }
                var trgtrow = obj["trgtRow"]?.ToString() ?? "";
                var trgtFldObj = JObject.Parse(obj["trgtFld"]?.ToString() ?? "{}");
                string trgtFieldIsGridVal = trgtFldObj["asgrid"]?.ToString();
                var rowctrl = "";
                var rowctrlCap = "";
                string ord = "";
                var rowControlObj = rowCtrlData.FirstOrDefault(j => (string?)j["trgtRow"] == trgtrow);
                if (rowControlObj != null)
                {
                    if (trgtFieldIsGridVal == null)
                    {
                        if (trgtStrctFieldList == null)
                        {
                            var _queryResult = await _addFormFieldHelper.ExecuteQueryNew(projName, userName, "tstruct", QueryName.FLDLIST_BY_FORM_W_ASGRID, QueryCategory.dropdown, new JObject { { "structname", trgtStructName } });
                            if (_queryResult.data.Rows.Count == 0)
                            {
                                throw new Exception($"Error while creating genmap node : target field '{name}' data not found in db ");
                            }
                            trgtStrctFieldList = _queryResult.data;
                        }
                        var row = trgtStrctFieldList.AsEnumerable().FirstOrDefault(r => r.Field<string>("name") == name);
                        trgtFieldIsGridVal = row?["asgrid"]?.ToString();
                    }
                    if (string.Equals(trgtFieldIsGridVal, "t", StringComparison.OrdinalIgnoreCase))
                    {

                        rowctrl = rowControlObj?["rowCtrlFldHdn"]?.ToString() ?? "";
                        rowctrlCap = rowControlObj?["rowCtrlFldCaption"]?.ToString() ?? "";
                        ord = rowCtrlData.IndexOf(rowControlObj).ToString();

                    }

                }


                var allMapAttr = obj["allMapAttr"];

                XElement element;
                if (allMapAttr != null)
                {
                    var allMapAttrObj = JObject.Parse(allMapAttr.ToString());
                    allMapAttrObj["trow"] = trgtrow;
                    allMapAttrObj["srow"] = isSrcFldGridDc ? "0" : "1";
                    allMapAttrObj["type"] = type;
                    allMapAttrObj["tcap"] = obj["trgtFldCaption"]?.ToString() ?? "";
                    allMapAttrObj["scap"] = obj["srcFldCaption"]?.ToString() ?? "";
                    allMapAttrObj["control"] = rowctrl;
                    allMapAttrObj["controlcap"] = rowctrlCap;
                    allMapAttrObj["group"] = mapGroupFlds;
                    allMapAttrObj["ord"] = ord;
                    if (type == "field")
                    {
                        allMapAttrObj["sasgrid"] = isSrcFldGridDc.ToString().ToLower();
                    }


                    element = new XElement(name,
                                                allMapAttrObj.Properties().Select(prop => new XAttribute(prop.Name, prop.Value.ToString())),
                                                new XText(type == "field" ? obj["srcFldHdn"]?.ToString() ?? "" : obj["srcVal"]?.ToString() ?? "")
                                            );
                }
                else
                {
                    element = new XElement(name,
                   new XAttribute("trow", trgtrow),
                   new XAttribute("srow", isSrcFldGridDc ? "0" : "1"),
                   new XAttribute("control", rowctrl),
                   new XAttribute("controlcap", rowctrlCap),
                   new XAttribute("group", mapGroupFlds),
                   new XAttribute("type", type),
                   new XAttribute("ord", ord),
                   new XAttribute("id", "False"),
                   new XAttribute("tcap", obj["trgtFldCaption"]?.ToString() ?? ""),
                   new XAttribute("scap", obj["srcFldCaption"]?.ToString() ?? ""),
                   new XAttribute("sasgrid", isSrcFldGridDc),
                   new XText(type == "field" ? obj["srcFldHdn"]?.ToString() ?? "" : obj["srcVal"]?.ToString() ?? "")
               );

                }


                result.Add(element);
            }
            return result;
        }


        private IEnumerable<XElement> constructListElements(string list)
        {
            IEnumerable<XElement> iElements = null;
            iElements = list.Split(',')?.Select(element => new XElement("l", element));
            return iElements;
        }
        private IEnumerable<XElement> constructSeqElements(string Sequence)
        {
            IEnumerable<XElement> iElements = null;
            try
            {
                iElements = Sequence.Split('^').Select(element =>
                {
                    string[] seqArr = element.Split('|');
                    XElement iElement = new XElement("l", new XAttribute("r", "f"),
                        new XElement(Constants.Xml_TstProperties.fieldProperties.Prefix, seqArr[0]),
                        new XElement(Constants.Xml_TstProperties.fieldProperties.PrefixField, seqArr[1]),
                        new XElement(Constants.Xml_TstProperties.fieldProperties.Desc, ""),
                        new XElement(Constants.Xml_TstProperties.fieldProperties.StartNo, seqArr[2]),
                        new XElement(Constants.Xml_TstProperties.fieldProperties.SActive, seqArr[3]),
                        new XElement(Constants.Xml_TstProperties.fieldProperties.Digits, seqArr[4])
                        );
                    return iElement;
                });

            }
            catch (Exception)
            {
                iElements = null;
                throw;
            }

            return iElements;
        }

        private IEnumerable<XObject> getSQLAttributes()
        {
            string isCombo = string.Empty;
            if (MOE.ToLower().StartsWith("select") && MOE.ToLower() != "select from list")
            {
                isCombo = comboBox == "True" ? "f" : "t";
            }
            if (SQL != oldSQL)
            {
                var sqlparameters = _addFormFieldHelper.GetSqlParameters(SQL);
                fldSQLParam = sqlparameters.Count > 0 ? "y" : "n";
                fldSQLParamCnt = sqlparameters.Count.ToString();

            }

            IEnumerable<XObject> attrs = new List<XObject>
                    {
                        new XAttribute("firesql", "false"),
                        new XAttribute("txt", isCombo),
                        new XAttribute("param", fldSQLParam ),
                        new XAttribute("pcount",fldSQLParamCnt ),
                    };
            if (CustomDataType == "Multi Select")
            {
                attrs = attrs.Concat(new XObject[] { new XAttribute("mulselect", "true"),
                                            new XAttribute("mulsep", separator),
                                            //new XAttribute("param", 'n'),
                                            //new XAttribute("pcount", '0'),

                                        });
            }
            if (MOE.ToLower() == "select from api" || MOE.ToLower() == "from api")
            {
                attrs = attrs.Concat(new XObject[] { new XAttribute("stype", MOE),
                                            new XAttribute("apiname", apiName),
                                            new XAttribute("url", apiURL),
                                            new XAttribute("paramstr",""),
                                            new XAttribute("headerstr", ""),
                                            new XAttribute("reqstr",apiReqStr),
                                            new XAttribute("mapname",""),
                                            new XAttribute("restype", apiRespType),
                                            new XAttribute("resformat", apiRespFormat),

                                        });
            }
            //SQL = Regex.Replace(SQL, @"\s+", " ");
            attrs = attrs.Concat(new XObject[] { new XElement("l", Regex.Replace(SQL, @"\s+", " ")) });
            //else
            //{
            //    attrs = attrs.Concat(new XObject[] { new XAttribute("mulselect", "true"),
            //                                new XAttribute("mulsep", separator),
            //                                new XElement("l", SQL)
            //                            });

            //}
            return attrs;
        }
        private IEnumerable<XAttribute> getSourceAttributes()
        {
            string capattr = string.Empty;
            string scolattr = "0";
            string sfcap = string.Empty;
            string fcaption = string.Empty;
            string table = string.Empty;

            if (MOE == "select from form")
            {
                capattr = SourceForm;
                scolattr = SourceKey == "True" ? "1" : "0";
                sfcap = DSFormCaption;
                fcaption = DSFieldCaption;
                table = SourceTable;
            }
            else if (MOE == "select from sql")
            {
                if (SourceKey == "True")
                {
                    capattr = Trans;
                    scolattr = "1";
                    table = SourceTable;

                }
                else
                {
                    scolattr = "0";

                }
            }
            IEnumerable<XAttribute> attrs = new List<XAttribute>
                    {
                        new XAttribute("table", table),
                        new XAttribute("cap",  capattr),
                        MOE == "select from form" ? new XAttribute("sfcap", sfcap) : null,
                         new XAttribute("fcaption", fcaption),
                        new XAttribute("stype", getSourceType(MOE)),
                        new XAttribute("scol", scolattr)

                    };
            //IEnumerable<XAttribute> attrs = new List<XAttribute>
            //        {
            //            new XAttribute("table",CustomDataType == "RadioGroup" ? "" : SourceTable),
            //            new XAttribute("cap", CustomDataType == "RadioGroup" ? "" : Trans),
            //            new XAttribute("sfcap", CustomDataType == "RadioGroup" ? "" : sformCaption),
            //            new XAttribute("tcaption", CustomDataType == "RadioGroup" ? "" :DSFormCaption),
            //             new XAttribute("fcaption", CustomDataType == "RadioGroup" ? "" :DSFieldCaption),
            //            new XAttribute("stype", getSourceType(MOE)),
            //            new XAttribute("scol", "0"),
            //        };

            if (MOE == "select from sql")
            {
                //string _source = Source;
                //if(Source == string.Empty)
                //{
                //    _source = oldmasterfieldAttr;
                //}
                attrs = attrs.Concat(new[] {new XAttribute("masterfield", MasterField),
                                            new XAttribute("mfldcap", MasterFieldCaption),
                                            new XAttribute("mastertstruct", MasterForm),
                                            new XAttribute("mtcap", MasterFormCaption)});
            }
            return attrs;
        }

        private IEnumerable<XAttribute> getMOEAttributes()
        {
            IEnumerable<XAttribute> attrs = new List<XAttribute> { };
            if (MOE.ToLower() == "from api" || (MOE.ToLower() == "accept" && acceptChoice.ToLower() == "api"))
            {
                if (MOE.ToLower() == "from api")
                {
                    attrs = attrs.Concat(new[] { new XAttribute("fromapi", "true") });
                }
                attrs = attrs.Concat(new[] {new XAttribute("apiname", apiName),
                                            new XAttribute("intelligence", intelligence == "True" ? "T" : "F"),
                });
            }
            else if (MOE.ToLower() == "fill from api")
            {
                attrs = attrs.Concat(new[] {new XAttribute("fillfromapi", "true"),
                                            new XAttribute("fillfromapi_source", fillFrmApiSrc),
                                             new XAttribute("fillfromapi_source_cap", fillFrmApiSrcCap)

                });
            }
            return attrs;
        }

        private string getSourceType(string srcStr)
        {
            switch (srcStr.ToLower())
            {
                case "select from form":
                    return "tstruct";
                case "select from list":
                    return "list";
                case "select from sql":
                    return "sql";
                case "select from api":
                    return "api";
                default:
                    return "";

            }
        }

        private XElement getTrackElement(string track)
        {
            XElement trackele = new XElement(Constants.Xml_TstProperties.Track,
                    new XAttribute("tchng", "y"), new XAttribute("track", "t"));
            var trkFldCaptionArr = trackFldCaps.Split(",");
            IEnumerable<XElement> usrElements = selectuserlist.Split(',')?.Select((element, index) => new XElement($"l{index + 1}", element));
            IEnumerable<XElement> fldElements = selectfieldlist.Split(',')?.Select((element, index) => new XElement($"l{index + 1}", new XAttribute("fcaption", trkFldCaptionArr[index]), element));

            if (trackchangesmadeby.ToLower() == "all users")
            {
                trackele.Add(new XAttribute("ausers", "t"));
            }
            else
            {
                trackele.Add(new XElement(Constants.Xml_TstProperties.TrackUsers, usrElements));

            }

            if (trackchangesfield.ToLower() == "all fields")
            {
                trackele.Add(new XAttribute("aflds", "t"));
            }
            else
            {
                trackele.Add(new XElement(Constants.Xml_TstProperties.TrackFields, fldElements));

            }

            return trackele;
        }


        private void parseFGMapDetails(string detailArr)
        {
            var detailArrObj = JArray.Parse(detailArr);
            string propVal = string.Empty;
            foreach (JObject obj in detailArrObj)
            {
                foreach (var property in obj.Properties())
                {
                    propVal = property.Value.ToString();
                    if (propVal != "")
                    {
                        string[] PropList = Constants.Tstruct.STRUCTXML_BOOLEAN_ATTRIBUTE_LIST.ToString().Split(',');

                        if (!PropList.Contains(property.Name?.ToLower()))
                        {
                            propVal = _addFormFieldHelper.ToCanonicalBoolean(propVal);
                        }
                        switch (property.Name?.ToLower())
                        {
                            case "srcfldhdn":
                                fgsourcefield.Add(propVal);
                                break;
                            case "trgtfieldhdn":
                                fgtargetfield.Add(propVal);
                                break;
                            case "trgtfieldcaption":
                                fgtargetfieldCap.Add(propVal);
                                break;
                            case "srccaption":
                                fgsourcecaption.Add(propVal);
                                break;
                        }
                    }

                }

            }
        }
        private string getSuggestiveValue()
        {
            string _moe = MOE.ToLower();
            if (!_moe.StartsWith("select") && ((_moe == "accept" && acceptChoice == "sql") || _moe == "fill"))
            {
                return Suggestive;
            }
            else
            {
                return "";
            }
        }
        private void ResetAllVariablesToDefaultValues()
        {
            Type type = this.GetType();

            var newObject = new TstXMLBuilder(projName, userName, _addFormFieldHelper);
            Type newtype = newObject.GetType();

            foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (field.GetCustomAttribute<ExcludeFromResetAttribute>() != null)
                {
                    continue;
                }

                //object defaultValue = field.FieldType.IsValueType ? Activator.CreateInstance(field.FieldType) : null;
                //field.SetValue(this, defaultValue);

                PropertyInfo property = newtype.GetProperty(field.Name);
                if (property != null)
                {
                    field.SetValue(this, property.GetValue(newObject));
                }
                else
                {
                    FieldInfo fieldInfo = newtype.GetField(field.Name);
                    if (field != null)
                    {
                        field.SetValue(this, fieldInfo.GetValue(newObject));
                    }
                }
            }
        }

        private IEnumerable<XElement> getDynamicFilterEles()
        {
            if (oldSQL != SQL)
            {
                var match = Regex.Match(SQL, @"\{dynamicfilter\s+([^}]+)\}");
                Dynamic = match.Success
                    ? string.Join(",", match.Groups[1].Value.Split(',')
                          .Where(p => p.Contains("~"))
                          .Select(p => p.Split('~')[1].Trim()))
                    : string.Empty;
            }
            if (string.IsNullOrEmpty(Dynamic))
            {
                return Enumerable.Empty<XElement>();
            }
            return Dynamic.Split(",").Select(cn => new XElement("l", new XAttribute("cn", cn.Trim())));

        }

        private async Task checkAndAddActionNode()
        {
            string structHashKey = $"{projName}♠tst♠{structName}";
            var actNode = _addFormFieldHelper.HashGet(structHashKey, "♠actionnode♠");
            if (!string.IsNullOrWhiteSpace(actNode))
            {
                XElement _actNode = XElement.Parse(actNode.ToString());
                mainRootElement.Add(_actNode);
            }

            //XElement actNode = oldStructXmlRoot.Element("actions");
            //if (actNode != null)
            //{
            //    mainRootElement.Add(actNode);
            //}
        }

        private async Task addConfigNode(string structHashKey)
        {

            var confignode = _addFormFieldHelper.HashGet(
                      structHashKey,
                      "♠confignode♠");

            XElement configElement;

            if (!string.IsNullOrWhiteSpace(confignode))
            {
                configElement = XElement.Parse(confignode);

                var existingSearchColsNode = configElement.Elements("CONFIGNAME")
                    .Where(x => x.Value.Trim() == "searchcols")
                    .Select(x => x.ElementsAfterSelf("CVALUE").FirstOrDefault())
                    .FirstOrDefault();

                if (existingSearchColsNode != null)
                {
                    // Update CVALUE
                    existingSearchColsNode.Value = string.Join(",", searchColList);
                }
                else
                {
                    configElement.Add(
                        new XElement("CONFIGNAME", "searchcols"),
                        new XElement("CVALUE", string.Join(",", searchColList))
                    );
                }
            }
            else
            {
                configElement = new XElement($"axpconfigs_{structName}",
                    new XAttribute("cat", "config"),
                    new XElement("CONFIGNAME", "searchcols"),
                    new XElement("CVALUE", string.Join(",", searchColList))
                );

            }
            _addFormFieldHelper.HashSet(structHashKey, "♠confignode♠", configElement.ToString());
            mainRootElement.Add(configElement);
        }
    }
    [AttributeUsage(AttributeTargets.Field)]
    public class ExcludeFromResetAttribute : Attribute
    {
        //this class, is resposible tp define a custom attribute that can be used to mark fields which will be ignored while reseting to default.
    }
}
