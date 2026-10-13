using Microsoft.VisualBasic.FileIO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Text.Json.Nodes;
using System.Xml.Linq;

namespace AxStructUtils
{
    public class TstXMLParser
    {

        #region variable declaration

        JObject fieldValJsonDoc = new();

        JObject fieldTypeJsonDoc = new();

        JArray FormLstJson = new();

        XDocument? mainStructXmldocument;

        XElement mainRootElement;

        public string projName = string.Empty;

        public string userName = string.Empty;

        public string structName = string.Empty;

        public string structCaption = string.Empty;

        public string updatedon = string.Empty;

        string sessionID = string.Empty;

        string AppSessionKey = string.Empty;

        string mainStructHashkey = string.Empty;
        string compHashKey = string.Empty;
        public bool allowConfig = false;
        public bool enablecache = false;
        public bool enableListvw = true;
        public string menuaccess = "true";
        public string pform = "yes";//either newlly create in web devloper or old one.
        public int dcCount = 0;
        public int fldCntForcurrDC = 0;
        public int genmapCnt = 0;
        public int mdmapCnt = 0;
        public int fgCnt = 0;
        public int scriptCnt = 0;
        public object Result;
        public string outputKey = string.Empty;
        public List<JsonObject> compList = new List<JsonObject>();
        public string rootNodeAttr = "{}";
        public List<string> searchColList = new List<string>();
        bool isSrchColExists = false;
        public string currentDCNo = "1";
        public string currentDCCaption = string.Empty;

        public Dictionary<string, string> selectFldListWithSql = new Dictionary<string, string>();
        public Dictionary<string, string> scriptList = new Dictionary<string, string>();
        bool isXMLParseError = false;

        public Dictionary<string, Dictionary<string, object>> fieldList = new Dictionary<string, Dictionary<string, object>>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, Dictionary<string, object>> dcList = new();
        public string xmlVno = string.Empty;

        public readonly AddFormFieldHelper _addFormFieldHelper;

        #endregion
        public TstXMLParser(string projName, string userName, AddFormFieldHelper addFormFieldHelper)
        {
            this.projName = projName;
            this.userName = userName;
            _addFormFieldHelper = addFormFieldHelper;
        }
        public async Task<(object, bool)> parseXml(string structName, string structXmlStr)
        {

            var parsed = new ParsedStructure();
            this.structName = structName;
            mainStructHashkey = $"{projName}♠tst♠{structName}";
            compHashKey = $"{mainStructHashkey}♠comps";
            try
            {
                mainStructXmldocument = XDocument.Parse(structXmlStr);
                mainRootElement = mainStructXmldocument?.Root;

                bool.TryParse(GetAttrValue(mainRootElement, "", "runtimemod"), out allowConfig);
                bool.TryParse(GetAttrValue(mainRootElement, "", "cachedsave"), out enablecache);
                menuaccess = GetAttrValue(mainRootElement, "", "defpage") == "n" ? "false" : "true";
                pform = GetAttrValue(mainRootElement, "", "pform");
                xmlVno = GetAttrValue(mainRootElement, "", "vno");
                rootNodeAttr = new JObject(mainRootElement.Attributes().Select(attr => new JProperty(attr.Name.LocalName, attr.Value))).ToString();
            
                var _configNode = mainRootElement.Element($"axpconfigs_{structName}");
                if (_configNode != null)
                {

                    await ParseConfigNode(_configNode);

                }
                foreach (var node in mainRootElement.Elements())
                {
                    var category = node.Attribute("cat")?.Value;
                    if (string.IsNullOrEmpty(category)) continue;
                    switch (category)
                    {
                        case "tstruct":
                            await ParseTstructNode(node);
                            //parsed
                            break;
                        case "dc":
                            await ParseDcNode(node);
                            break;
                        case "field":
                            await ParseFieldNode(node);
                            break;
                        case "genmap":
                            await ParseGenmapNode(node);
                            break;
                        case "fillgrid":
                            await ParseFillgridNode(node);
                            break;
                        case "mdmap":
                            await ParseMDmapNode(node);
                            break;
                        case "actions":
                            await ParseScriptNode(node);
                            break;
                        case "comps":
                            await ParseBtnNode(node);
                            break;
                        case "iframes":
                            _addFormFieldHelper.RuntimeHashSet(mainStructHashkey, "♠iframenode♠", node.ToString());
                            break;
                        default:
                            continue;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return (Result, isXMLParseError);
        }


        private async void addToCompList(string comp, string compName, string compCaption, string ordNo)
        {
            compList.Add(new JsonObject
            {
                ["key"] = _addFormFieldHelper.generateNodeKey("tstruct", structName, compName),
                ["label"] = $"{compCaption}({compName})",
                ["leaf"] = comp != "dc",
                ["structtype"] = "tstruct",
                ["ordno"] = ordNo,
                ["structname"] = structName,
                ["structcaption"] = structCaption,
                ["comp"] = comp,
                // ["comptype"] =  "dc",
                ["compname"] = compName,
                ["compcaption"] = compCaption,
            });
        }
        private async Task ParseTstructNode(XElement node)
        {
            structCaption = GetElementValue(node, "a2");
            var jsonObject = new Dictionary<string, object>
            {
                {"name", GetElementValue(node, "a1")},
                {"caption", structCaption },
                    //{"Folder", GetElementValue(node, "a3")},
            };

            bool.TryParse(GetElementValue(node, "a20"), out enableListvw);
            jsonObject = await setSavectrlfld(node, jsonObject);
            jsonObject = await setDltCtrlFld(node, jsonObject);
            jsonObject = await setTrackOptions(node.Element("a6"), jsonObject);

            jsonObject.Add("pform", pform);
            jsonObject.Add("allowattch", bool.TryParse(GetElementValue(node, "a18"), out bool allowattach) && allowattach);
            jsonObject.Add("enableListVW", enableListvw);
            jsonObject.Add("schema", GetElementValue(node, "a24"));
            jsonObject.Add("enablewf", bool.TryParse(GetElementValue(node, "a25"), out bool enablewf) && enablewf);
            jsonObject.Add("allowconfig", allowConfig);
            jsonObject.Add("enablecache", enablecache);
            jsonObject.Add("menuaccess", menuaccess);
            jsonObject.Add("purpose", GetElementValue(node, "a59"));
            jsonObject.Add("rootNodeAttr", rootNodeAttr);
            //await _redisHelper.RedisDB.HashSetAsync(mainStructHashkey, "♠props♠", JsonConvert.SerializeObject(jsonObject));
            _addFormFieldHelper.RuntimeHashSet(mainStructHashkey, "♠props♠", JsonConvert.SerializeObject(jsonObject));
            Result = jsonObject;
            //await _redisHelper.RedisDB.HashSetAsync(mainStructHashkey, "♠props♠", JsonConvert.SerializeObject(Result));
            _addFormFieldHelper.RuntimeHashSet(mainStructHashkey, "♠props♠", JsonConvert.SerializeObject(Result));

        }
        private async Task ParseMDmapNode(XElement node)
        {
            string name = GetElementValue(node, "a1");
            string caption = GetElementValue(node, "a2");
            var jsonObject = new Dictionary<string, object>
                    {
                        {"name", name},
                        {"caption", caption },
                    };
            string MDMmasterForm = GetElementValue(node, "a3");
            jsonObject.Add("MDMmasterForm", MDMmasterForm);
            if (MDMmasterForm != "")
            {
                string MDMmasterFormCap = GetAttrValue(node, "a3", "mcap");
                if (MDMmasterFormCap == "")
                {
                    //_logHelper.AppendTrace($"getting master field caption using query..");
                    string _MDMmasterFormCap = await _addFormFieldHelper.ExecuteQuery(projName, userName, "tstruct", QueryName.GET_TST_CAPTION, new JObject { { "structname", MDMmasterForm } });
                    if (string.IsNullOrEmpty(_MDMmasterFormCap))
                    {
                        isXMLParseError = true;
                    }
                    MDMmasterFormCap = $"{_MDMmasterFormCap}-({MDMmasterForm})";
                }
                jsonObject.Add("MDMmasterFormCap", MDMmasterFormCap);
                jsonObject.Add("MDMmasterFormDDN", new { name = MDMmasterForm, caption = MDMmasterFormCap });

            }
            string MDMMasterFldDec = GetElementValue(node, "a14");
            if (string.IsNullOrEmpty(MDMMasterFldDec)) MDMMasterFldDec = "0";
            string MDMmasterField = GetElementValue(node, "a4");
            jsonObject.Add("MDMmasterField", MDMmasterField);
            if (MDMmasterField != "")
            {
                string MDMmasterFieldCap = GetAttrValue(node, "a4", "mfcap");
                string MDMmastertable = GetElementValue(node, "a8");
                if (MDMmasterFieldCap == "" || MDMmastertable == "")
                {
                    //_logHelper.AppendTrace("getting master field caption and tablename using query..");
                    var _queryResult = await _addFormFieldHelper.ExecuteQueryNew(projName, userName, "tstruct", QueryName.GET_TST_FLD_CAPTION_N_TBLNAME, QueryCategory.general, new JObject { { "structname", MDMmasterForm }, { "fieldname", MDMmasterField } });
                    if (_queryResult != null && _queryResult.data.Rows.Count > 0)
                    {
                        MDMmasterFieldCap = $"{_queryResult.data.Rows[0]["caption"].ToString()}-({MDMmasterField})";
                        MDMmastertable = _queryResult.data.Rows[0]["tablename"].ToString();
                    }
                    else
                    {
                        if (MDMmasterField.ToLower().EndsWith("id"))
                        {
                            MDMmasterFieldCap = $"{MDMmasterField}-({MDMmasterField})";
                        }
                        else
                        {
                            isXMLParseError = true;
                        }
                    }

                }
                jsonObject.Add("MDMmasterFieldCap", MDMmasterFieldCap);
                jsonObject.Add("MDMmasterFieldDDN", new { name = MDMmasterField, caption = MDMmasterFieldCap, flddecimal = MDMMasterFldDec, tablename = MDMmastertable });
                jsonObject.Add("MDMmastertable", MDMmastertable);
                jsonObject.Add("MDMmasterFieldDecimal", MDMMasterFldDec);


            }
            string MDMdetailField = GetElementValue(node, "a5");
            if (MDMdetailField != "")
            {
                string MDMdetailFieldCap = GetAttrValue(node, "a5", "dfcap");
                if (MDMdetailFieldCap == "")
                {
                    //_logHelper.AppendTrace("getting detail field caption using query..");

                    var _queryResult = await _addFormFieldHelper.ExecuteQueryNew(projName, userName, "tstruct", QueryName.GET_TST_Fld_CAPTION, QueryCategory.general, new JObject { { "structname", structName }, { "fldname", MDMdetailField } });
                    if (_queryResult != null && _queryResult.data.Rows.Count > 0)
                    {
                        string _MDMdetailFieldCap = _queryResult.data.Rows[0]["caption"].ToString();
                        MDMdetailField = _queryResult.data.Rows[0]["name"].ToString();
                        MDMdetailFieldCap = $"{_MDMdetailFieldCap}-({MDMdetailField})";
                    }
                    else
                    {
                        isXMLParseError = true;
                    }

                }
                jsonObject.Add("MDMdetailFieldCap", MDMdetailFieldCap);
                jsonObject.Add("MDMdetailFieldDDN", new { name = MDMdetailField, caption = MDMdetailFieldCap });

            }
            jsonObject.Add("MDMdetailField", MDMdetailField);
            string MDMmasterSrchField = GetElementValue(node, "a6");
            if (MDMmasterSrchField != "")
            {
                string MDMmasterSrchFieldCap = GetAttrValue(node, "a6", "msfcap");
                if (MDMmasterSrchFieldCap == "")
                {
                    //_logHelper.AppendTrace("getting detail master search field caption using query..");
                    var _queryResult = await _addFormFieldHelper.ExecuteQueryNew(projName, userName, "tstruct", QueryName.GET_TST_Fld_CAPTION, QueryCategory.general, new JObject { { "structname", MDMmasterForm }, { "fldname", MDMmasterSrchField } });
                    if (_queryResult != null && _queryResult.data.Rows.Count > 0)
                    {
                        string _MDMmasterSrchFieldCap = _queryResult.data.Rows[0]["caption"].ToString();
                        if (string.IsNullOrEmpty(_MDMmasterSrchFieldCap))
                        {
                            if (MDMmasterSrchField.ToLower().EndsWith("id") || MDMmasterSrchField.ToLower() == "default")
                            {
                                _MDMmasterSrchFieldCap = MDMmasterSrchField;
                            }
                        }
                        MDMmasterSrchField = _queryResult.data.Rows[0]["name"].ToString();
                        MDMmasterSrchFieldCap = $"{_MDMmasterSrchFieldCap}-({MDMmasterSrchField})";
                    }
                    else
                    {
                        if (MDMmasterSrchField.ToLower().EndsWith("id") || MDMmasterSrchField.ToLower() == "default")
                        {
                            MDMmasterSrchFieldCap = $"{MDMmasterSrchField}-({MDMmasterSrchField})";
                        }
                        else
                        {
                            isXMLParseError = true;
                        }

                    }

                }
                jsonObject.Add("MDMmasterSrchFieldCap", MDMmasterSrchFieldCap);
                jsonObject.Add("MDMmasterSrchFieldDDN", new { name = MDMmasterSrchField, caption = MDMmasterSrchFieldCap });

            }
            jsonObject.Add("MDMmasterSrchField", MDMmasterSrchField);
            string MDMdetailSrchField = GetElementValue(node, "a7");
            if (MDMdetailSrchField != "")
            {
                string MDMdetailSrchFieldCap = GetAttrValue(node, "a7", "dsfcap");
                if (MDMdetailSrchFieldCap == "")
                {
                    //_logHelper.AppendTrace("getting detail search field caption using query..");
                    var _queryResult = await _addFormFieldHelper.ExecuteQueryNew(projName, userName, "tstruct", QueryName.GET_TST_Fld_CAPTION, QueryCategory.general, new JObject { { "structname", structName }, { "fldname", MDMdetailSrchField } });
                    if (_queryResult != null && _queryResult.data.Rows.Count > 0)
                    {
                        string _MDMdetailSrchFieldCap = _queryResult.data.Rows[0]["caption"].ToString();
                        MDMdetailSrchField = _queryResult.data.Rows[0]["name"].ToString();
                        MDMdetailSrchFieldCap = $"{_MDMdetailSrchFieldCap}-({MDMdetailSrchField})";
                    }
                    else
                    {
                        isXMLParseError = true;
                    }
                }
                jsonObject.Add("MDMdetailSrchFieldCap", MDMdetailSrchFieldCap);
                jsonObject.Add("MDMdetailSrchFieldDDN", new { name = MDMdetailSrchField, caption = MDMdetailSrchFieldCap });

            }
            jsonObject.Add("MDMdetailSrchField", MDMdetailSrchField);
            jsonObject.Add("updtType", GetElementValue(node, "a9"));

            string MDMctrlFld = GetElementValue(node, "a11");
            if (MDMctrlFld != "")
            {
                string MDMctrlFldCap = GetAttrValue(node, "a11", "cfcap");
                if (MDMctrlFldCap == "")
                {
                    //_logHelper.AppendTrace("getting control field caption using query..");
                    var _queryResult = await _addFormFieldHelper.ExecuteQueryNew(projName, userName, "tstruct", QueryName.GET_TST_Fld_CAPTION, QueryCategory.general, new JObject { { "structname", structName }, { "fldname", MDMctrlFld } });
                    if (_queryResult != null && _queryResult.data.Rows.Count > 0)
                    {
                        string _MDMctrlFldCap = _queryResult.data.Rows[0]["caption"].ToString();
                        MDMctrlFld = _queryResult.data.Rows[0]["name"].ToString();
                        MDMctrlFldCap = $"{_MDMctrlFldCap}-({MDMctrlFld})";

                    }
                    else
                    {
                        isXMLParseError = true;
                    }

                }
                jsonObject.Add("MDMctrlFldCap", MDMctrlFldCap);
                jsonObject.Add("MDMctrlFldDDN", new { name = MDMctrlFld, caption = MDMctrlFldCap });

            }
            jsonObject.Add("MDMctrlFld", MDMctrlFld);
            jsonObject.Add("append", bool.TryParse(GetElementValue(node, "a12"), out bool append) && append);
            jsonObject.Add("initOnDlt", bool.TryParse(GetElementValue(node, "a13"), out bool initOnDlt) && initOnDlt);
            jsonObject.Add("MDMmapTxt", GetElementValue(node, "a15"));
            jsonObject.Add("defByScript", GetElementValue(node, "a16") == "true" ? "yes" : "no");
            jsonObject.Add("exeOnApprove", bool.TryParse(GetElementValue(node, "a17"), out bool exeOnApprove) && exeOnApprove);
            jsonObject.Add("exeOnReject", bool.TryParse(GetElementValue(node, "a18"), out bool exeOnReject) && exeOnReject);
            jsonObject.Add("purpose", GetElementValue(node, "a59"));

            string ordno = (++mdmapCnt).ToString();
            //await _redisHelper.RedisDB.HashSetAsync(mainStructHashkey, name, JsonConvert.SerializeObject(jsonObject));
            _addFormFieldHelper.RuntimeHashSet(mainStructHashkey, name, JsonConvert.SerializeObject(jsonObject));
            //await _redisHelper.RedisDB.HashSetAsync(compHashKey, getTstCompHashSubKey("mdmap", name), $"{ordno}♠{caption}");
            _addFormFieldHelper.RuntimeHashSet(compHashKey, getTstCompHashSubKey("mdmap", name), $"{ordno}♠{caption}");
            //if (outputKey == "COMPLIST")
            //{
            //    addToCompList("mdmap", name, caption, ordno);
            //}

        }
        private async Task ParseGenmapNode(XElement node)
        {
            // Define mappings for 'genmap' category
            string name = GetElementValue(node, "a1");
            string caption = GetElementValue(node, "a2");
            var jsonObject = new Dictionary<string, object>
                    {
                        {"name", name},
                        {"caption", caption },
                        {"active",bool.TryParse(GetElementValue(node, "a6"), out bool active) && active},
                        {"purpose",GetElementValue(node, "a59")},
                        
                        // Add other genmap-specific mappings
                    };
            string postTst = GetElementValue(node, "a3");
            jsonObject.Add("postTst", postTst);
            if (postTst != "")
            {
                string postTstCap = GetAttrValue(node, "a3", "tcap");
                if (postTstCap == "")
                {
                    //_logHelper.AppendTrace("getting post tstructure caption using query..");
                    string _postTstCap = await _addFormFieldHelper.ExecuteQuery(projName, userName, "tstruct", QueryName.GET_TST_CAPTION, new JObject { { "structname", postTst } });
                    if (string.IsNullOrEmpty(_postTstCap))
                    {
                        isXMLParseError = true;
                    }
                    postTstCap = $"{_postTstCap}-({postTst})";
                }
                jsonObject.Add("postTstCap", postTstCap);
                jsonObject.Add("postTstDDN", new { name = postTst, caption = postTstCap });

            }

            string basedondc = GetElementValue(node, "a4");
            jsonObject.Add("basedondc", basedondc);

            string genControl = GetElementValue(node, "a5");
            if (genControl != "")
            {
                string GenControlCaption = GetAttrValue(node, "a5", "gctrlcap");
                if (GenControlCaption == "")
                {
                    //_logHelper.AppendTrace("getting control field caption using query..");

                    var _queryResult = await _addFormFieldHelper.ExecuteQueryNew(projName, userName, "tstruct", QueryName.GET_TST_Fld_CAPTION, QueryCategory.general,
                                                                      new JObject {
                                                                        { "structname", structName },
                                                                        {  "fldname", genControl }
                                                                      });
                    if (_queryResult != null && _queryResult.data.Rows.Count > 0)
                    {
                        string _GenControlCaption = _queryResult.data.Rows[0]["caption"].ToString();
                        genControl = _queryResult.data.Rows[0]["name"].ToString();
                        GenControlCaption = $"{_GenControlCaption}-({genControl})";
                    }
                    else
                    {
                        isXMLParseError = true;
                    }
                }
                jsonObject.Add("ctrlFieldCap", GenControlCaption);
                jsonObject.Add("ctrlFieldDDN", new { name = genControl, caption = GenControlCaption });
            }
            jsonObject.Add("ctrlField", genControl);
            if (basedondc == "dc1")
            {
                jsonObject.Add("postType", "Post one transaction in target");
                jsonObject.Add("postTypeDDN", new { name = "Post one transaction in target", caption = "Post one transaction in target" });
            }
            else
            {
                jsonObject.Add("postType", "Post one transaction for every row in " + basedondc);
                jsonObject.Add("postTypeDDN", new { name = "Post one transaction for every row in " + basedondc, caption = "Post one transaction for every row in " + basedondc });
            }

            jsonObject = await getMapDataForGenmap(node.Element("a8"), postTst, jsonObject);

            string selPostEvnt = string.Empty;
            //if(GetElementValue(node, "a9") != "")
            //{
            //    jsonObject.Add("postType", "Post one transaction in target");
            //}
            //else 

            if (GetElementValue(node, "a17") == "true")
            {
                if (GetElementValue(node, "a18") == "true")
                {
                    jsonObject.Add("postEvent", "on approve & reject");
                }
                else
                {
                    jsonObject.Add("postEvent", "on approve only");
                }
            }
            else if (GetElementValue(node, "a18") == "true")
            {
                jsonObject.Add("postEvent", "on reject only");
            }
            else
            {
                jsonObject.Add("postEvent", "default");
            }

            jsonObject.Add("onPostScript", GetElementValue(node, "a9"));
            string ordno = (++genmapCnt).ToString();
            //await _redisHelper.RedisDB.HashSetAsync(mainStructHashkey, name, JsonConvert.SerializeObject(jsonObject));
            _addFormFieldHelper.RuntimeHashSet(mainStructHashkey, name, JsonConvert.SerializeObject(jsonObject));
            //await _redisHelper.RedisDB.HashSetAsync(compHashKey, getTstCompHashSubKey("genmap", name), $"{ordno}♠{caption}");
            _addFormFieldHelper.RuntimeHashSet(compHashKey, getTstCompHashSubKey("genmap", name), $"{ordno}♠{caption}");
            //if (outputKey == "COMPLIST")
            //{
            //    addToCompList("genmap", name, caption, ordno);
            //}

            //return jsonObject;
        }
        private async Task ParseFillgridNode(XElement node)
        {
            string name = GetElementValue(node, "a1");
            string caption = GetElementValue(node, "a4");
            string fgSQLText = string.Empty;
            var a5Childrens = node.Element("a5")?.Elements();
            bool isMultilineSQL = false;
            if (a5Childrens.Count() > 0)
            {
                isMultilineSQL = true;
                fgSQLText = string.Join("\n", node.Element("a5")?.Elements()?.Select(e => e.Value) ?? []);
            }
            else
            {
                isMultilineSQL = false;
                fgSQLText = node.Element("a5")?.Value;
            }

            var a5NodeAttr = new JObject(node.Element("a5").Attributes().Select(attr => new JProperty(attr.Name.LocalName, attr.Value)));

            var jsonObject = new Dictionary<string, object>
                    {
                        {"name", name},
                        {"caption", caption },
                        {"SqlText",  fgSQLText},
                        {"oldSQLText",fgSQLText },
                        {"isMultilineSQL",isMultilineSQL },
                        {"a5NodeAttr",a5NodeAttr },
                        {"fgColumns", ""},
                        {"grpFldInfo",GetAttrValue(node,"a7","gfld")},
                        {"validate", bool.TryParse(GetElementValue(node, "a11"), out bool validate) && validate },
                        {"execOnSave", bool.TryParse(GetElementValue(node, "a13"), out bool execOnSave) && execOnSave},
                        {"firmBind", bool.TryParse(GetElementValue(node, "a14"), out bool firmBind) && firmBind},
                        {"selectOn", GetElementValue(node, "a15")},
                        {"footerString", GetElementValue(node, "a16")},
                        {"valExprsn", GetElementValue(node, "a17")},
                        {"addRows", GetElementValue(node, "a18")},
                        { "autoSelAllRow", bool.TryParse(GetElementValue(node, "a20"), out bool _autoselallrow) && _autoselallrow },
                        {"purpose",GetElementValue(node, "a59")},
                        
                        // Add other genmap-specific mappings
                    };

            string targetDC = GetElementValue(node, "a6");
            jsonObject.Add("targetDC", targetDC);
            if (targetDC != "")
            {
                string targetDCCap = GetAttrValue(node, "a6", "tcap");
                if (targetDCCap == "")
                {
                    //_logHelper.AppendTrace("getting target dc caption using query..");
                    string _targetDCCap = await _addFormFieldHelper.ExecuteQuery(projName, userName, "tstruct", QueryName.GET_DC_CAPTION, new JObject { { "structname", structName }, { "dcname", targetDC } });
                    if (string.IsNullOrEmpty(_targetDCCap))
                    {
                        isXMLParseError = true;
                    }
                    targetDCCap = $"{_targetDCCap}-({targetDC})";
                }
                jsonObject.Add("targetDCCap", targetDCCap);
                jsonObject.Add("targetDCDDN", new { name = targetDC, caption = targetDCCap });

            }
            jsonObject.Add("multiSelect", bool.TryParse(GetElementValue(node, "a8"), out bool _multiselect) && _multiselect);
            jsonObject.Add("autoShow", bool.TryParse(GetElementValue(node, "a9"), out bool _autoshow) && _autoshow);

            string sourceDC = GetElementValue(node, "a10");
            jsonObject.Add("sourceDC", sourceDC);
            if (sourceDC != "")
            {
                string sourceDCCap = GetAttrValue(node, "a10", "scap");
                if (sourceDCCap == "")
                {
                    //_logHelper.AppendTrace("getting source dc caption using query..");
                    string _sourceDCCap = await _addFormFieldHelper.ExecuteQuery(projName, userName, "tstruct", QueryName.GET_DC_CAPTION, new JObject { { "structname", structName }, { "dcname", sourceDC } });
                    if (string.IsNullOrEmpty(_sourceDCCap))
                    {
                        isXMLParseError = true;
                    }
                    sourceDCCap = $"{_sourceDCCap}-({sourceDC})";
                }
                jsonObject.Add("sourceDCCap", sourceDCCap);
                jsonObject.Add("sourceDCDDN", new { name = sourceDC, caption = sourceDCCap });

            }
            jsonObject = await getMapDataForFillgrid(node.Element("a7"), jsonObject);

            string ordno = (++fgCnt).ToString();
            //await _redisHelper.RedisDB.HashSetAsync(mainStructHashkey, name, JsonConvert.SerializeObject(jsonObject));
            _addFormFieldHelper.RuntimeHashSet(mainStructHashkey, name, JsonConvert.SerializeObject(jsonObject));
            //await _redisHelper.RedisDB.HashSetAsync(compHashKey, getTstCompHashSubKey("fillgrid", name), $"{ordno}♠{caption}");
            _addFormFieldHelper.RuntimeHashSet(compHashKey, getTstCompHashSubKey("fillgrid", name), $"{ordno}♠{caption}");
            //if (outputKey == "COMPLIST")
            //{
            //    addToCompList("fillgrid", name, caption, ordno);
            //}

        }

        private async Task ParseBtnNode(XElement node)
        {
            if (scriptList.Count == 0)
            {
                var scriptNode = mainRootElement.Element("scripts");
                if (scriptNode != null)
                {
                    await ParseScriptNode(scriptNode);
                }
            }
            if (node.Name != "action")
            {
                string totalBtnCount = GetAttrValue(node, "", "btn");
                int defBtnWithActionNumber = int.TryParse(totalBtnCount, out int tbcnt) ? tbcnt : node.Elements().Count();
                int grpcnt = 0;
                int btncnt = 0;
                JObject defaultBtnJSON = JObject.Parse(Constants.Tstruct.DEFAULT_BUTTON_JSON_HIDDEN);
                foreach (var element in node.Elements())
                {
                    if (GetAttrValue(element, "", "task") == "tasks")
                    {
                        //await _redisHelper.RedisDB.HashSetAsync(mainStructHashkey, "♠taskbtnnode♠", element.ToString());
                        _addFormFieldHelper.RuntimeHashSet(mainStructHashkey, "♠taskbtnnode♠", element.ToString());
                        //continue;
                    }
                    if (GetAttrValue(element, "", "cat") == "btn")
                    {
                        string name = GetAttrValue(element, "", "key");
                        if (name == "")
                        {
                            name = element.Name.LocalName;
                        }
                        string caption = GetAttrValue(element, "", "caption");
                        if (caption == "")
                        {
                            caption = GetAttrValue(element, "", "hint");
                        }
                        bool.TryParse(GetAttrValue(element, "", "folder"), out bool folder);
                        string defbtnkey = string.Join("", caption.ToLower().Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
                        string task = GetAttrValue(element, "", "task");
                        string btnaction = GetAttrValue(element, "", "action");
                        if ((defaultBtnJSON.ContainsKey(defbtnkey) || defaultBtnJSON.ContainsKey(task)) && btnaction == string.Empty)
                        {
                            defbtnkey = defaultBtnJSON.ContainsKey(defbtnkey) ? defbtnkey : task;
                            defaultBtnJSON[defbtnkey]["visibility"] = true;
                            defaultBtnJSON[defbtnkey]["position"] = GetAttrValue(element, "", "position") == "" ? "header" : GetAttrValue(element, "", "position");
                            defaultBtnJSON[defbtnkey]["parentAttr_oldxml"] = GetAttrValue(element, "", "parent");
                            defaultBtnJSON[defbtnkey]["hint"] = GetAttrValue(element, "", "hint");
                        }
                        else
                        {
                            if (folder)
                            {
                                var jsonObject = new Dictionary<string, object>
                                {
                                    {"name", name},
                                    {"caption", caption },
                                    {"icon", element.Element("icon")?.Element("text")?.Value ?? "" },
                                    {"position", GetAttrValue(element, "", "position") },
                                    {"positionDDN", new {name = GetAttrValue(element, "", "position"), caption = GetAttrValue(element, "", "position")} },
                                    {"visibility", GetAttrValue(element, "", "visible") == "" ? true : bool.Parse(GetAttrValue(element, "", "visible"))},
                                    {"parentAttr_oldxml", GetAttrValue(element, "", "parent")}
                                };
                                //await _redisHelper.RedisDB.HashSetAsync(mainStructHashkey, name, JsonConvert.SerializeObject(jsonObject));
                                _addFormFieldHelper.RuntimeHashSet(mainStructHashkey, name, JsonConvert.SerializeObject(jsonObject));
                                //await _redisHelper.RedisDB.HashSetAsync(compHashKey, getTstCompHashSubKey("btngrp", name), $"{++grpcnt}♠{caption}");
                                _addFormFieldHelper.RuntimeHashSet(compHashKey, getTstCompHashSubKey("btngrp", name), $"{++grpcnt}♠{caption}");
                                if (outputKey == "COMPLIST")
                                {
                                    addToCompList("btngrp", name, caption, (++grpcnt).ToString());
                                }

                                int childcnt = 0;
                                foreach (var child in element.Elements())
                                {
                                    if (child.Name.LocalName.StartsWith("pop"))
                                    {
                                        string childName = GetAttrValue(child, "", "key");
                                        string childCaption = GetAttrValue(child, "", "caption");
                                        var childObj = new Dictionary<string, object>
                                    {
                                        {"name", childName},
                                        {"caption", childCaption },
                                        {"icon", child.Element("icon")?.Element("text")?.Value ?? "" },
                                        {"api", GetAttrValue(child, "", "api") },
                                        {"apiDDN", new {name = GetAttrValue(child, "", "api"), caption = GetAttrValue(child, "", "api")} },
                                        {"position", name },
                                        {"positionDDN", new {name,caption,parentname = name }},
                                        {"visibility", GetAttrValue(child, "", "visible") == "" ? true : bool.Parse(GetAttrValue(child, "", "visible")) }
                                    };

                                        string action = GetAttrValue(child, "", "action");
                                        string script = string.Empty;
                                        string scriptCaption = string.Empty;
                                        if (bool.TryParse(GetAttrValue(child, "", "script"), out bool isscript) && isscript)
                                        {
                                            script = action;
                                            if (scriptList.TryGetValue(action, out var _scriptCaption))
                                            {
                                                scriptCaption = $"{_scriptCaption}-({action})";
                                            }
                                            action = string.Empty;
                                        }

                                        childObj.Add("action", action);
                                        childObj.Add("actionDDN", new { name = action, caption = action });
                                        childObj.Add("script", script);
                                        childObj.Add("scriptDDN", new { name = script, caption = scriptCaption });

                                        //await _redisHelper.RedisDB.HashSetAsync(mainStructHashkey, childName, JsonConvert.SerializeObject(childObj));
                                        _addFormFieldHelper.RuntimeHashSet(mainStructHashkey, childName, JsonConvert.SerializeObject(childObj));
                                        //await _redisHelper.RedisDB.HashSetAsync(compHashKey, getTstCompHashSubKey("udbutton", childName, name), $"{++childcnt}♠{childCaption}");
                                        _addFormFieldHelper.RuntimeHashSet(compHashKey, getTstCompHashSubKey("udbutton", childName, name), $"{++childcnt}♠{childCaption}");
                                        if (outputKey == "COMPLIST")
                                        {
                                            addToCompList("udbutton", name, caption, (++grpcnt).ToString());
                                        }

                                    }



                                }
                            }
                            else
                            {
                                if (int.TryParse(name.Substring(3), out int btnnumber))
                                {
                                    if (btnnumber < 17)
                                    {
                                        name = $"btn{++defBtnWithActionNumber}";
                                    }
                                }
                                var jsonObject = new Dictionary<string, object>
                                {
                                    {"name", name},
                                    {"caption", caption },
                                    {"icon", element.Element("icon")?.Element("text")?.Value ?? "" },
                                    {"api", GetAttrValue(element, "", "api") },
                                    {"apiDDN", new {name = GetAttrValue(element, "", "api"), caption = GetAttrValue(element, "", "api")} },
                                    {"visibility", GetAttrValue(element, "", "visible") == "" ? true : bool.Parse(GetAttrValue(element, "", "visible")) },
                                    {"parentAttr_oldxml", GetAttrValue(element, "", "parent")},
                                    {"hint", GetAttrValue(element, "", "hint")}

                                };
                                var position = GetAttrValue(element, "", "position");
                                string parentname = GetAttrValue(element, "", "parent");
                                if (parentname.ToLower().StartsWith("f_"))
                                {
                                    parentname = "dc" + parentname.Replace("f__", "");
                                    jsonObject.Add("position", parentname);
                                    jsonObject.Add("positionDDN", new { name = parentname, caption = parentname, parentname });
                                }
                                else
                                {
                                    if (position == string.Empty || (position != "Default" && position != "Footer" && !position.StartsWith("dc")))
                                    {
                                        parentname = "NA";
                                        jsonObject.Add("position", "Default");
                                        jsonObject.Add("positionDDN", new { name = "Default", caption = "Default", parentname });
                                    }
                                    else
                                    {
                                        parentname = position.Split('~')?.ElementAtOrDefault(0);
                                        if (parentname == "Default" || parentname == "Footer")
                                        {
                                            parentname = "NA";
                                        }
                                        jsonObject.Add("position", position);
                                        jsonObject.Add("positionDDN", new { name = position, caption = position, parentname });

                                    }
                                }


                                string action = GetAttrValue(element, "", "action");
                                string script = string.Empty;
                                string scriptCaption = string.Empty;
                                if (bool.TryParse(GetAttrValue(element, "", "script"), out bool isscript) && isscript)
                                {
                                    script = action;
                                    if (scriptList.TryGetValue(action, out var _scriptCaption))
                                    {
                                        scriptCaption = $"{_scriptCaption}-({action})";
                                    }

                                    action = string.Empty;
                                }

                                jsonObject.Add("action", action);
                                jsonObject.Add("actionDDN", new { name = action, caption = action });
                                jsonObject.Add("script", script);
                                jsonObject.Add("scriptDDN", new { name = script, caption = scriptCaption });


                                //await _redisHelper.RedisDB.HashSetAsync(mainStructHashkey, name, JsonConvert.SerializeObject(jsonObject));
                                _addFormFieldHelper.RuntimeHashSet(mainStructHashkey, name, JsonConvert.SerializeObject(jsonObject));
                                //await _redisHelper.RedisDB.HashSetAsync(compHashKey, getTstCompHashSubKey("udbutton", name, parentname), $"{++btncnt}♠{caption}");
                                _addFormFieldHelper.RuntimeHashSet(compHashKey, getTstCompHashSubKey("udbutton", name, parentname), $"{++btncnt}♠{caption}");
                            }
                        }


                    }
                    else
                    {
                        var nodeName = element.Name.LocalName;
                        //_redisHelper.RedisDB.HashSetAsync(mainStructHashkey, $"ExtCompsNode♠{nodeName}", element.ToString());
                        _addFormFieldHelper.RuntimeHashSet(mainStructHashkey, $"ExtCompsNode♠{nodeName}", element.ToString());
                    }
                }

                //_redisHelper.RedisDB.HashSetAsync(mainStructHashkey, "♠defbtns♠", JsonConvert.SerializeObject(defaultBtnJSON));
                _addFormFieldHelper.RuntimeHashSet(mainStructHashkey, "♠defbtns♠", JsonConvert.SerializeObject(defaultBtnJSON));
            }

        }
        private async Task ParseScriptNode(XElement node)
        {
            if (node.Name.LocalName == "scripts")
            {
                if (scriptList.Count > 0)
                {
                    return;
                }
                foreach (var element in node.Elements())
                {
                    string name = element.Name.LocalName;
                    string caption = GetAttrValue(element, "", "cap");
                    var _formcontrol = bool.TryParse(GetAttrValue(element, "", "formcontrol"), out bool formControl) && formControl;
                    var _pushtoqueue = bool.TryParse(GetAttrValue(element, "", "pushtoqueue"), out bool pushToQueue) && pushToQueue;
                    var _confirmmsgreq = bool.TryParse(GetAttrValue(element, "", "confreq"), out bool confirmMsgReq) && confirmMsgReq;
                    var active = xmlVno == "11.4" ? bool.TryParse(GetAttrValue(element, "", "active"), out bool _active) && _active : true;

                    var jsonObject = new Dictionary<string, object>
                    {
                        {"name", name},
                        {"caption", caption },
                        {"formControl", _formcontrol },
                        {"pushToQueue", _pushtoqueue },
                        {"queueName", GetAttrValue(element, "", "queuename") },
                        {"confirmMsgReq", _confirmmsgreq },
                        {"confirmMsg", GetAttrValue(element, "", "confmsg") },
                        {"scriptEvent", GetAttrValue(element, "", "apply") },
                        {"appliedOn", GetAttrValue(element, "", "applyon") },
                        {"appliedOnDDN", new {name = GetAttrValue(element, "", "applyon"), caption = GetAttrValue(element, "", "applyon")}},
                        {"active",active }
                    };

                    var script = string.Join("\n", element.Element("r1")?.Element("param1")?.Element("exprset")?.Elements()?.Select(e => e.Value) ?? []);
                    if (formControl)
                    {
                        jsonObject.Add("fcscript", script);
                    }
                    else
                    {
                        jsonObject.Add("script", script);
                    }


                    string ordno = (++scriptCnt).ToString();
                    //await _redisHelper.RedisDB.HashSetAsync(mainStructHashkey, name, JsonConvert.SerializeObject(jsonObject));
                    _addFormFieldHelper.RuntimeHashSet(mainStructHashkey, name, JsonConvert.SerializeObject(jsonObject));
                    //await _redisHelper.RedisDB.HashSetAsync(compHashKey, getTstCompHashSubKey("script", name), $"{ordno}♠{caption}");
                    _addFormFieldHelper.RuntimeHashSet(compHashKey, getTstCompHashSubKey("script", name), $"{ordno}♠{caption}");
                    if (outputKey == "COMPLIST")
                    {
                        addToCompList("script", name, caption, ordno);
                    }
                    scriptList[name] = caption;
                }
            }
            else if (node.Name.LocalName == "actions")
            {

                JArray actionArr = new JArray(
                node.Elements()
                    .Where(child => child.Name.LocalName != "formcontrol")
                    .Select(child => new JObject
                    {
                        ["name"] = child.Name.LocalName,
                        ["caption"] = child.Attribute("cap")?.Value ?? child.Name.LocalName
                    })
                 );
                //var hashEntries = new HashEntry[]
                //{
                //    new HashEntry("♠actionnode♠", node.ToString()),
                //    new HashEntry("♠actionlist♠", actionArr.ToString())
                //};

                //await _redisHelper.RedisDB.HashSetAsync(mainStructHashkey, hashEntries);
                _addFormFieldHelper.RuntimeHashSet(mainStructHashkey, "♠actionnode♠", node.ToString());
                _addFormFieldHelper.RuntimeHashSet(mainStructHashkey, "♠actionlist♠", actionArr);
            }

        }

        private async Task ParseDcNode(XElement node)
        {
            string dcName = GetElementValue(node, "a1");
            string dcCaption = GetElementValue(node, "a2");
            currentDCCaption = dcCaption;
            string dcNo = GetElementValue(node, "a3");
            currentDCNo = dcNo;
            string tableName = GetElementValue(node, "a5");
            bool.TryParse(GetElementValue(node, "a6"), out bool asGrid);
            string dcOrdNo = GetAttrValue(node, "", "ordno") != "" ? GetAttrValue(node, "", "ordno") : (++dcCount).ToString();
            fldCntForcurrDC = 0;
            var jsonObject = new Dictionary<string, object>
            {
                {"name", dcName},
                {"caption", dcCaption},
                {"dcNo", dcNo},
                {"tableName", tableName},
                {"dltOldTbl", "no"},
                {"asGrid", asGrid},
                {"allowEdit", bool.TryParse(GetElementValue(node, "a7"), out bool allowEdit) && allowEdit},
                {"PopUp", bool.TryParse(GetElementValue(node, "a9"), out bool PopUp) && PopUp},
                {"allowEmpty", bool.TryParse(GetElementValue(node, "a17"), out bool allowEmpty) && allowEmpty},
                {"allowAdd", bool.TryParse(GetElementValue(node, "a26"), out bool allowAdd) && allowAdd},
                {"allowDelete", bool.TryParse(GetElementValue(node, "a27"), out bool allowDelete) && allowDelete},
                { "booleanDC", bool.TryParse(GetAttrValue(node,"", "booleandc"),out bool booleanDC) && booleanDC},
                { "defBoolDC", GetAttrValue(node,"", "defaultstate")},
                { "purpose", GetElementValue(node, "a59")}
            };
            //await _redisHelper.RedisDB.HashSetAsync(mainStructHashkey, dcName, JsonConvert.SerializeObject(jsonObject));
            _addFormFieldHelper.RuntimeHashSet(mainStructHashkey, dcName, JsonConvert.SerializeObject(jsonObject));
            _addFormFieldHelper.RuntimeHashSet(compHashKey, getTstCompHashSubKey("dc", dcName), $"{dcOrdNo}♠{dcCaption}♠{asGrid}♠{tableName}");
            if (outputKey == "COMPLIST")
            {
                addToCompList("dc", dcName, dcCaption, dcOrdNo);
            }
            dcList.Add(dcName, jsonObject);
            //return jsonObject;
        }
        private async Task ParseFieldNode(XElement node)
        {
            // Define mappings for 'field' category
            string fldName = GetElementValue(node, "a1");
            string fldCaption = GetElementValue(node, "a2");
            string dcNo = GetAttrValue(node, "", "dcno");
            if (dcNo == string.Empty) dcNo = currentDCNo;
            string dcName = $"dc{dcNo}";
            string dcCaption = currentDCCaption;
            if (dcCaption == string.Empty)
            {
                //_logHelper.AppendTrace("getting field's dc caption using query..");
                dcCaption = node.Attribute("dccap")?.Value ?? await _addFormFieldHelper.ExecuteQuery(projName, userName, "tstruct", QueryName.GET_DC_CAPTION,
                                                               new JObject {
                                                                    { "structname", structName },
                                                                    { "dcname", dcName }
                                                               });
                if (dcCaption == string.Empty)
                {
                    isXMLParseError = true;
                }
            }

            string MOE = GetElementValue(node, "a6");
            //some times calculate field is coming lowecase in xml we need it Pascal/Proper case for axpertstudio.
            if (MOE.ToLower() == "calculate")
            {
                MOE = "Calculate";
            }
            if (MOE.Replace(" ", "").ToLower() == "autogenerate")
            {
                MOE = "autogenerate";
            }
            string sourceType = MOE;
            string dataType = GetElementValue(node, "a3");
            string decimalval = GetElementValue(node, "a5");
            string fldWidth = GetElementValue(node, "a4");
            string fieldtype = GetAttrValue(node, "", "fieldtype");//xmls generated in exe will not contain this attribute, in that case need to evalute fieldtype from ctype.
            if (fieldtype == string.Empty)
            {

                fieldtype = EvaluteFieldType(dataType, fldWidth, decimalval, MOE);
            }
            string ctypeValue = GetAttrValue(node, "a31", "ctype");
            fieldtype = ctypeValue.ToLower() switch
            {
                "check box" => "Check box",
                "check list" => "CheckList",
                "radio group" => "RadioGroup",
                "Memo" => "Memo",
                _ => fieldtype,
            };
            //if (ctypeValue != string.Empty) fieldtype = ctypeValue;


            //if (string.IsNullOrEmpty(fieldtype))
            //{
            //string customtype = GetAttrValue(node, "a31", "ctype");
            //    fieldtype = await EvaluteFieldType(dataType, MOE, customtype, decimalval);
            //}

            //if(GetAttrValue(node, "a31", "ctype") == "Check box")
            //{
            //    fieldtype = "Check box";
            //}
            bool isHidden = bool.TryParse(GetElementValue(node, "a8"), out bool hidden) && hidden;
            bool hideInSearch = false;
            if (new[] { "text", "image", }.Contains(dataType?.ToLower()) || ctypeValue.ToLower() == "memo")
            {
                hideInSearch = true;
            }
            else
            {
                if (isSrchColExists)
                {
                    hideInSearch = !searchColList.Contains(fldName);
                }
                else
                {
                    hideInSearch = isHidden;
                }

            }
            string dbdecimal = GetAttrValue(node, "", "dbdecimal");
            if (dbdecimal == string.Empty)
            {
                dbdecimal = decimalval;
            }
            string masking = GetAttrValue(node, "a48", "masking");
            if (masking == string.Empty)
            {
                masking = "No Mask";

            }
            string maskRoles = GetAttrValue(node, "a48", "maskroles");
            var maskRolesDDN = new List<Dictionary<string, string>>();
            if (!string.IsNullOrWhiteSpace(maskRoles))
            {
                foreach (var role in maskRoles.Split(","))
                {

                    maskRolesDDN.Add(new Dictionary<string, string>
                    {
                        { "name", role },
                        { "caption", role }
                    });

                }
            }

            string customDataType = GetAttrValue(node, "", "customdatatype");
            if (customDataType == string.Empty)
            {
                customDataType = "NA";
            }
            string splDataType = GetElementValue(node, "a69");


            var jsonObject = new Dictionary<string, object>
            {

                {"name", fldName },
                {"oldName", fldName },
                {"caption", fldCaption},
                {"fieldType",fieldtype },
                //{"customDataType",GetAttrValue(node,"","fieldtype") },
                {"dataType",dataType },
                {"dcNo",dcNo },
                {"dcName",dcName },
                {"dcCaption",dcCaption },
                {"dclabel",$"{dcCaption}({dcName})" },
                {"width",fldWidth },
                {"decimal",decimalval },
                {"dbDecimal",dbdecimal },
                //{"dbDecimal",GetAttrValue(node,"","dbdec") },
                //{"MOE",MOE },
                {"hidden", isHidden },
                {"allowEmpty",bool.TryParse(GetElementValue(node, "a9"), out bool allowEmpty) && allowEmpty },
                {"readOnly",bool.TryParse(GetElementValue(node, "a10"), out bool readOnly) && readOnly },
                /*{"",GetElementValue(node,"a12") }*/
                {"setCarry",bool.TryParse(GetElementValue(node, "a11"), out bool setCarry) && setCarry},
                {"saveValue",bool.TryParse(GetElementValue(node, "a12"), out bool saveValue) && saveValue },
                {"expression",GetElementValue(node,"a13") },
                {"validateExp",GetElementValue(node,"a14") },
                {"allowDuplicate",bool.TryParse(GetElementValue(node, "a15"), out bool allowDuplicate) && allowDuplicate },
                {"onlyPositive",bool.TryParse(GetElementValue(node, "a16"), out bool onlyPositive) && onlyPositive },
                {"saveNomralized",bool.TryParse(GetElementValue(node, "a17"), out bool saveNomralized) && saveNomralized },
                //{ "saveNomralized",GetElementValue(node,"a19") },
                { "masking",masking},
                {"maskChar",GetAttrValue(node,"a48","maskchar") },
                {"maskRoles", maskRoles },
                {"maskRolesDDN",maskRolesDDN },
                {"lastCharMask",GetAttrValue(node,"a48", "lastcharmask") },
                {"firstCharMask",GetAttrValue(node,"a48", "firstcharmask") },

                {"pattern",GetElementValue(node,"a49") },
                {"pwdchar",GetElementValue(node,"a50") },
                {"PWAlphaNum",bool.TryParse(GetAttrValue(node, "a50", "alphanumeric"), out bool PWAlphaNum) && PWAlphaNum },
                {"pwdLength",GetAttrValue(node,"a50","minlength") },
                {"pwdExpiry",GetAttrValue(node,"a50","expirydays") },

                {"applyComma",bool.TryParse(GetElementValue(node, "a52"), out bool applyComma) && applyComma },
                {"clientValidation",bool.TryParse(GetElementValue(node, "a55"), out bool clientValidation)  && clientValidation},
                {"purpose",GetElementValue(node,"a59") },
                {"displayDetails",GetElementValue(node,"a65") },
                {"tabstop",bool.TryParse(GetElementValue(node, "a67"), out bool tabstop) ? tabstop : true },
                //{"customDataType",GetElementValue(node,"a69") },
                {"customDataType",customDataType },
                {"customDataTypeDDN",new JObject
                {
                    ["name"] = customDataType,
                    ["caption"] = customDataType,
                    ["datatype"] = dataType,
                    ["width"] = fldWidth,
                    ["dwidth"] = decimalval,
                    ["spldatatype"] = splDataType
                }
                },
                {"splDataType", splDataType },


                {"runtimeFld",bool.TryParse(GetAttrValue(node, "", "runtimefld"), out bool runtimeFld) && runtimeFld},
                {"runtimecriteria",bool.TryParse(GetAttrValue(node, "", "runtimecriteria"), out bool runtimecriteria) && runtimecriteria },
                {"applyRule",bool.TryParse(GetAttrValue(node, "", "applyrule"), out bool applyRule) && applyRule},
                {"ctypeValue",ctypeValue},
                {"radiotype",GetAttrValue(node, "a31", "type")?.ToLower() == "h" ? "horizontal" : GetAttrValue(node, "a31", "type")?.ToLower() == "v" ? "vertical" : "horizontal" },
                {"defTable",GetElementValue(node,"a70") },
                {"displayTotal", GetAttrValue(node, "", "displaytotal") == "true" },
                {"encrypted",GetAttrValue(node, "", "encrypted") == "true" },
                {"hideInSearch", hideInSearch  }// Fallback: only DC1 fields are searchable if SearchColList is missing in both xml and db.

                /*{"patternDDL",GetElementValue(node,"a49") },*///need to be evaluted
               

            // Add other field-specific mappings
        };
            (jsonObject, sourceType) = await getMoreDetailOptions(node, jsonObject, MOE);

            if (sourceType == "select from form" || sourceType == "select from sql")
            {
                selectFldListWithSql.Add(fldName, jsonObject["SqlText"]?.ToString() ?? "");
            }

            var dynamicParams = node.Element("a7")?.Element("a63") != null
                   ? string.Join(",", node.Element("a7").Element("a63").Elements()
                       .Select(e => e.Attribute("cn")?.Value)
                       .Where(value => !string.IsNullOrEmpty(value)))
                   : string.Empty;

            jsonObject.Add("dynamicParams", dynamicParams);
            //await _redisHelper.RedisDB.HashSetAsync(mainStructHashkey, fldName, JsonConvert.SerializeObject(jsonObject));
            _addFormFieldHelper.RuntimeHashSet(mainStructHashkey, fldName, JsonConvert.SerializeObject(jsonObject));
            //await _redisHelper.RedisDB.HashSetAsync(compHashKey, getTstCompHashSubKey("field", fldName, dcName), _addFormFieldHelper.getTstCompHashVal("field", (++fldCntForcurrDC).ToString(), fldCaption, "", "", sourceType, fieldtype));
            _addFormFieldHelper.RuntimeHashSet(compHashKey, getTstCompHashSubKey("field", fldName, dcName), _addFormFieldHelper.getTstCompHashVal("field", (++fldCntForcurrDC).ToString(), fldCaption, "", "", sourceType, fieldtype));

            //jsonObject = await getSQLOptions(node,jsonObject);

            // return jsonObject;
            fieldList.Add(fldName, new Dictionary<string, object>
            {
                {"name", fldName },
                {"caption",fldCaption},
                {"dcname",dcName}
            });
        }

        private async Task<Dictionary<string, object>> getMapDataForFillgrid(XElement node, Dictionary<string, object> jsonObject)
        {
            var fillGridMapDetails = new List<object>();

            foreach (var ele in node.Elements())
            {
                string trgtFieldHdn = ele.Name.LocalName;
                string trgtFieldCaption = string.Empty;
                if (trgtFieldHdn != "")
                {
                    trgtFieldCaption = GetAttrValue(ele, "", "tcap");
                    if (trgtFieldCaption == "")
                    {
                        //_logHelper.AppendTrace("getting target field caption using query..");

                        var _queryResult = await _addFormFieldHelper.ExecuteQueryNew(projName, userName, "tstruct", QueryName.GET_TST_Fld_CAPTION, QueryCategory.general,
                                                                     new JObject {
                                                                        { "structname", structName },
                                                                        {  "fldname", trgtFieldHdn }
                                                                     });
                        if (_queryResult != null && _queryResult.data.Rows.Count > 0)
                        {
                            string _trgtFieldCaption = _queryResult.data.Rows[0]["caption"].ToString();
                            trgtFieldHdn = _queryResult.data.Rows[0]["name"].ToString();
                            trgtFieldCaption = $"{_trgtFieldCaption}-({trgtFieldHdn})";
                        }
                        else
                        {
                            isXMLParseError = true;
                        }

                    }
                }

                object trgtField = new { name = trgtFieldHdn, caption = trgtFieldCaption };
                string srcFldHdn = ele.Value;
                object srcFld = new { name = ele.Value, caption = ele.Value };
                string srcCaption = GetAttrValue(ele, "", "cap");

                fillGridMapDetails.Add(new
                {
                    trgtFieldHdn,
                    trgtField,
                    trgtFieldCaption,
                    srcFld,
                    srcFldHdn,
                    srcCaption
                });
            }

            jsonObject.Add("fillGridMapDetails", fillGridMapDetails);
            return jsonObject;

        }
        private async Task<Dictionary<string, object>> getMapDataForGenmap(XElement node, string targetTst, Dictionary<string, object> jsonObject)
        {
            var mapData = new List<object>();
            var mapGrpFlds = string.Empty;
            var rowCtrlFldList = new HashSet<string>();
            var rowCtrlData = new List<object>();
            foreach (var e in node.Elements())
            {
                string trgtFldHdn = e.Name.LocalName;
                string trgtFldCaption = string.Empty;
                if (trgtFldHdn != "")
                {
                    trgtFldCaption = GetAttrValue(e, "", "tcap");
                    if (trgtFldCaption == "")
                    {
                        //_logHelper.AppendTrace($"getting target field caption for target field \"{trgtFldHdn}\" using query..");
                        var _queryResult = await _addFormFieldHelper.ExecuteQueryNew(projName, userName, "tstruct", QueryName.GET_TST_Fld_CAPTION, QueryCategory.general,
                                             new JObject {
                                                { "structname", targetTst },
                                                {  "fldname", trgtFldHdn }
                                             });
                        if (_queryResult != null && _queryResult.data.Rows.Count > 0)
                        {
                            string _trgtFldCaption = _queryResult.data.Rows[0]["caption"].ToString();
                            trgtFldHdn = _queryResult.data.Rows[0]["name"].ToString();
                            trgtFldCaption = $"{_trgtFldCaption}-({trgtFldHdn})";

                        }
                        else
                        {
                            isXMLParseError = true;
                        }
                    }
                }
                string _source = e.Value.ToString();
                string _sourceType = GetAttrValue(e, "", "type");
                string srcFldHdn = string.Empty;
                string srcFldCaption = string.Empty;
                string srcAsGrid = string.Empty;
                object srcFld = string.Empty;
                object srcVal = string.Empty;
                if (_sourceType == "field")
                {
                    srcFldHdn = _source;
                    srcFldCaption = GetAttrValue(e, "", "scap");
                    srcAsGrid = GetAttrValue(e, "", "sasgrid");
                    if (srcAsGrid == "")
                    {
                        srcAsGrid = GetAttrValue(e, "", "srow") == "0" ? "true" : "false";
                    }

                    if (srcFldCaption == "")
                    {
                        //_logHelper.AppendTrace($"getting field caption for source field \"{srcFldHdn}\" ...");


                        try
                        {
                            if (fieldList.TryGetValue(srcFldHdn, out var innerDict))
                            {
                                srcFldHdn = innerDict["name"]?.ToString() ?? "";
                                srcFldCaption = $"{innerDict["caption"]}-({srcFldHdn})";
                                srcAsGrid = bool.TryParse(dcList[innerDict["dcname"].ToString()]["asGrid"].ToString(), out var asgrid) ? asgrid.ToString() : "false";

                            }
                            else
                            {
                                var srcFldNode = mainRootElement.Elements(srcFldHdn).FirstOrDefault();

                                if (srcFldNode != null)
                                {
                                    var child = mainRootElement.Elements(srcFldHdn)
                                                    .FirstOrDefault(x => (string)x.Attribute("cat") == "field");

                                    var innerNode = child?.Element("a2");
                                    srcFldCaption = $"{innerNode?.Value}-({srcFldHdn})";
                                }
                                else if (srcFldHdn.EndsWith("id"))
                                {
                                    srcFldCaption = srcFldHdn;
                                    string tablename = srcFldHdn.Remove(srcFldHdn.Length - 2);
                                    var dctable = dcList
                                         .FirstOrDefault(x => x.Value.TryGetValue("tableName", out var v) && Equals(tablename, v));
                                    if (dctable.Value != null)
                                    {
                                        srcAsGrid = dctable.Value.GetValueOrDefault("asGrid")?.ToString().ToLower();
                                    }
                                    else
                                    {
                                        isXMLParseError = true;
                                        //_logHelper.AppendTrace($"Genmap \"{jsonObject["name"]?.ToString()}\"source field is invalid.It might have deleted please reselect.");

                                    }
                                }
                                else
                                {
                                    isXMLParseError = true;
                                    //_logHelper.AppendTrace($"Genmap \"{jsonObject["name"]?.ToString()}\"source field is invalid.It might have deleted please reselect.");
                                }
                            }

                        }
                        catch (Exception ex)
                        {
                            var _queryResult = await _addFormFieldHelper.ExecuteQueryNew(projName, userName, "tstruct", QueryName.GET_TST_FLD_CAPTION_N_ASGRID, QueryCategory.general,
                                                                       new JObject {
                                                                        { "structname", structName },
                                                                        { "fieldname", srcFldHdn }
                                                                       });
                            if (_queryResult != null && _queryResult.data.Rows.Count > 0)
                            {
                                string _srcFldCaption = _queryResult.data.Rows[0]["caption"].ToString();
                                srcFldCaption = $"{_srcFldCaption}-({srcFldHdn})";
                                srcAsGrid = _queryResult.data.Rows[0]["asgrid"]?.ToString()?.ToLower() == "f" ? "false" : "true";
                            }
                            else
                            {
                                isXMLParseError = true;
                            }
                        }

                    }
                    srcFld = new
                    {
                        name = srcFldHdn,
                        caption = srcFldCaption,
                        type = _sourceType,
                        isgrid = srcAsGrid.ToLower(),
                    };
                }
                else
                {
                    srcFldHdn = srcFldCaption = _sourceType;
                    srcVal = _source;
                    srcFld = new
                    {
                        name = srcFldHdn,
                        caption = srcFldCaption,
                        type = _sourceType
                    };
                }
                var allMapAttr = new JObject(e.Attributes().Select(attr => new JProperty(attr.Name.LocalName, attr.Value)));
                string trgtRow = GetAttrValue(e, "", "trow");
                mapData.Add(new
                {
                    trgtFldHdn,
                    trgtFldCaption,
                    trgtFld = new
                    {
                        name = trgtFldHdn,
                        caption = trgtFldCaption
                    },
                    srcFldHdn,
                    srcFldCaption,
                    srcFld,
                    trgtRow,
                    srcVal,
                    allMapAttr
                });


                mapGrpFlds = GetAttrValue(e, "", "group");

                string rowCtrlFldHdn = GetAttrValue(e, "", "control");
                if (rowCtrlFldHdn != string.Empty && rowCtrlFldList.Add(rowCtrlFldHdn))//rowctrlFldList will allw only unquie values.for duplicate values it will return false
                {
                    var rowCtrlFldCaption = GetAttrValue(e, "", "controlcap");
                    if (rowCtrlFldCaption == string.Empty)
                    {
                        if (rowCtrlFldHdn.EndsWith("id"))
                        {
                            //_logHelper.AppendTrace($"getting field caption for row control field \"{rowCtrlFldHdn}\" using query..");
                            string tablename = rowCtrlFldHdn.Substring(0, rowCtrlFldHdn.Length - 2);
                            string _rowCtrlFieldCap = await _addFormFieldHelper.ExecuteQuery(projName, userName, "tstruct", QueryName.GET_DCCAPTION_BY_TABLENAME,
                                                     new JObject {
                                                                    { "structname", structName },
                                                                    { "tablename",tablename  }
                                                                 });
                            if (string.IsNullOrEmpty(_rowCtrlFieldCap))
                            {
                                isXMLParseError = true;
                            }
                            rowCtrlFldCaption = $"{_rowCtrlFieldCap}-({rowCtrlFldHdn})";

                        }
                        else
                        {
                            //_logHelper.AppendTrace($"getting field caption for row control field \"{rowCtrlFldHdn}\" using query..");
                            var _queryResult = await _addFormFieldHelper.ExecuteQueryNew(projName, userName, "tstruct", QueryName.GET_TST_Fld_CAPTION, QueryCategory.general,
                                             new JObject {
                                                { "structname", structName },
                                                {  "fldname", rowCtrlFldHdn }
                                             });
                            if (_queryResult != null && _queryResult.data.Rows.Count > 0)
                            {
                                string _rowCtrlFieldCap = _queryResult.data.Rows[0]["caption"].ToString();
                                rowCtrlFldHdn = _queryResult.data.Rows[0]["name"].ToString();
                                rowCtrlFldCaption = $"{_rowCtrlFieldCap}-({rowCtrlFldHdn})";

                            }
                            else
                            {
                                isXMLParseError = true;
                            }
                        }

                    }
                    rowCtrlData.Add(new
                    {
                        trgtRow,
                        rowCtrlFldHdn,
                        rowCtrlFldCaption,
                        rowCtrlFld = new
                        {
                            name = rowCtrlFldHdn,
                            caption = rowCtrlFldCaption
                        }

                    });
                }

            }
            var orderedMapData = mapData.OrderBy(m =>
            {
                var row = ((dynamic)m).trgtRow as string ?? "";
                return row == "1" ? -1 : row == "0" ? int.MaxValue - 1 : int.TryParse(row, out int val) ? val : int.MaxValue;
            }).ToList();

            jsonObject.Add("mapData", orderedMapData);
            //jsonObject.Add("mapData", mapData);
            jsonObject.Add("mapGrpFlds", mapGrpFlds);
            jsonObject.Add("rowCtrlData", rowCtrlData);
            return jsonObject;


        }

        private async Task<(Dictionary<string, object> jsonObject, string sourceType)> getMoreDetailOptions(XElement node, Dictionary<string, object> jsonObject, string MOE)
        {
            //string MOE = GetElementValue(node, "a6");
            //var sqltext = node.Element("a7")?.Element("a22")?.Element("l")?.Value ?? "";
            var sqltext = string.Join("\n", node.Element("a7")?.Element("a22")?.Elements()?.Select(e => e.Value) ?? []);
            jsonObject.Add("SqlText", sqltext);
            jsonObject.Add("oldSQLText", sqltext);
            string sourceType = MOE;
            if (MOE == "Select")
            {
                //data Source
                string dataSource = GetElementValue(node.Element("a7"), "a18");
                if (dataSource != "")
                {
                    string dataSourceCaption = GetAttrValue(node.Element("a7"), "a18", "cfcap");
                    if (dataSourceCaption == "")
                    {
                        //_logHelper.AppendTrace($"getting data source field caption using query..");
                        var _queryResult = await _addFormFieldHelper.ExecuteQueryNew(projName, userName, "tstruct", QueryName.GET_TST_Fld_CAPTION, QueryCategory.general,
                                             new JObject {
                                                { "structname", structName },
                                                {  "fldname", dataSource }
                                             });
                        if (_queryResult != null && _queryResult.data.Rows.Count > 0)
                        {
                            string _dataSourceCaption = _queryResult.data.Rows[0]["caption"].ToString();
                            dataSource = _queryResult.data.Rows[0]["name"].ToString();
                            dataSourceCaption = $"{_dataSourceCaption}-({dataSource})";

                        }
                        else
                        {
                            isXMLParseError = true;
                        }

                    }
                    jsonObject.Add("dataSource", dataSource);
                    jsonObject.Add("dataSourceCaption", dataSourceCaption);
                    jsonObject.Add("dataSourceDDN", new { name = dataSource, caption = dataSourceCaption });
                }
                sourceType = GetAttrValue(node.Element("a7"), "a26", "stype");
                if (sourceType == "tstruct")
                {
                    sourceType = "select from form";
                    jsonObject.Add("source", sourceType);
                    jsonObject = await getFormSourceOptions(node.Element("a7"), jsonObject);
                }
                else if (sourceType == "list")
                {
                    sourceType = "select from list";
                    jsonObject.Add("source", sourceType);
                    var values = node.Element("a7")?.Element("a25")
                                 ?.Elements("l")
                                 ?.Select(e => e.Value)
                                 ?.ToList();
                    jsonObject.Add("defList", string.Join(",", values));
                }
                else if (sourceType == "sql")
                {
                    sourceType = "select from sql";
                    jsonObject.Add("source", sourceType);

                    jsonObject = await getSQLOptions(node.Element("a7"), jsonObject);



                }
                else if (sourceType == "api")
                {
                    sourceType = "select from API";
                    jsonObject.Add("source", sourceType);
                    jsonObject = await getAPIOptions(node.Element("a7").Element("a22"), jsonObject, MOE);
                }


            }
            else
            {
                if (MOE == "Accept")
                {
                    string apiName = GetAttrValue(node, "a6", "apiname");
                    if (apiName != "")
                    {
                        if (GetAttrValue(node, "a6", "fromapi") == "true")
                        {
                            jsonObject.Add("MOE", "From API");
                        }
                        else
                        {
                            jsonObject.Add("MOE", MOE);
                            jsonObject.Add("acceptChoice", "api");
                        }

                        jsonObject.Add("intelligence", bool.TryParse(GetAttrValue(node, "a6", "intelligence"), out bool intelligence) && intelligence);
                        jsonObject.Add("apiNameSql", apiName);
                        jsonObject.Add("apiNameSqlDDN", new { name = apiName, caption = apiName });
                        jsonObject = await getAPIOptions(node.Element("a22"), jsonObject, MOE);
                    }
                    else
                    {
                        string isfillfromapi = GetAttrValue(node, "a6", "fillfromapi");
                        if (isfillfromapi == "true")
                        {
                            jsonObject.Add("MOE", "Fill From API");

                            string fillfromapi_source = GetAttrValue(node, "a6", "fillfromapi_source");
                            if (fillfromapi_source != "")
                            {
                                string fillfromapi_source_cap = GetAttrValue(node, "a6", "fillfromapi_source_cap");
                                if (fillfromapi_source_cap == "")
                                {
                                    //_logHelper.AppendTrace($"getting fill from api source field caption using query..");
                                    var _queryResult = await _addFormFieldHelper.ExecuteQueryNew(projName, userName, "tstruct", QueryName.GET_TST_Fld_CAPTION, QueryCategory.general,
                                             new JObject {
                                                { "structname", structName },
                                                {  "fldname", fillfromapi_source }
                                             });
                                    if (_queryResult != null && _queryResult.data.Rows.Count > 0)
                                    {
                                        string _fillfromapi_source_cap = _queryResult.data.Rows[0]["caption"].ToString();
                                        fillfromapi_source = _queryResult.data.Rows[0]["name"].ToString();
                                        fillfromapi_source_cap = $"{_fillfromapi_source_cap}-({fillfromapi_source})";

                                    }
                                    else
                                    {
                                        isXMLParseError = true;
                                    }

                                }


                                jsonObject.Add("fillFrmApiSrcCap", fillfromapi_source_cap);
                                jsonObject.Add("fillFrmApiSrcDDN", new { name = fillfromapi_source, caption = fillfromapi_source_cap });

                            }
                            jsonObject.Add("fillfromapi_source", fillfromapi_source);

                        }
                        else
                        {
                            jsonObject.Add("MOE", MOE);
                            jsonObject.Add("acceptChoice", "sql");
                        }

                    }
                }
                else
                {
                    if (MOE == "Fill")
                    {
                        jsonObject = await getFillOptions(node.Element("a7"), jsonObject);

                    }
                    jsonObject.Add("MOE", MOE);

                }

            }

            var multiSepa = GetAttrValue(node.Element("a7"), "a22", "mulsep");
            if (multiSepa == "")
            {
                multiSepa = GetAttrValue(node, "a31", "sep");
            }
            jsonObject.Add("separator", multiSepa);
            jsonObject.Add("autoSelect", bool.TryParse(GetElementValue(node.Element("a7"), "a23"), out bool autoSelect) && autoSelect);
            jsonObject.Add("comboBox", GetAttrValue(node.Element("a7"), "a22", "txt") == "true" ? false : true);
            jsonObject.Add("refreshOnSave", bool.TryParse(GetElementValue(node.Element("a7"), "a24"), out bool refreshOnSave) && refreshOnSave);
            jsonObject.Add("suggestive", bool.TryParse(GetElementValue(node.Element("a7"), "a30"), out bool suggestive) && suggestive);
            jsonObject = await getSequenceOptions(node.Element("a7"), jsonObject);
            jsonObject.Add("sqlParam", GetAttrValue(node.Element("a7"), "a22", "param"));
            jsonObject.Add("sqlParamCnt", GetAttrValue(node.Element("a7"), "a22", "pcount"));

            return (jsonObject, sourceType);
        }

        private async Task<Dictionary<string, object>> getFormSourceOptions(XElement node, Dictionary<string, object> jsonObject)
        {
            string DSForm = GetAttrValue(node, "a26", "cap");
            jsonObject.Add("DSForm", DSForm);

            if (DSForm != "")
            {
                string DSFormCaption = GetAttrValue(node, "a26", "tcap");

                if (DSFormCaption == "")
                {
                    //_logHelper.AppendTrace($"getting detail data source form caption using query..");
                    string _DSFormCaption = await _addFormFieldHelper.ExecuteQuery(projName, userName, "tstruct", QueryName.GET_TST_CAPTION,
                                                                new JObject { { "structname", DSForm } });
                    if (string.IsNullOrEmpty(_DSFormCaption))
                    {
                        isXMLParseError = true;
                    }
                    DSFormCaption = $"{_DSFormCaption}-({DSForm})";

                }

                jsonObject.Add("DSFormCaption", DSFormCaption);
                jsonObject.Add("DSFormDDN", new { name = DSForm, caption = DSFormCaption });
            }
            else
            {
                jsonObject.Add("DSFormCaption", "");
                jsonObject.Add("DSFormDDN", "");
            }


            string DSField = GetElementValue(node, "a26");

            if (DSField != "")
            {
                string DSFieldCaption = GetAttrValue(node, "a26", "fcap");

                if (DSFieldCaption == "")
                {
                    //_logHelper.AppendTrace($"getting data source field caption using query..");
                    var _queryResult = await _addFormFieldHelper.ExecuteQueryNew(projName, userName, "tstruct", QueryName.GET_TST_Fld_CAPTION, QueryCategory.general,
                                             new JObject {
                                                { "structname", DSForm },
                                                {  "fldname", DSField }
                                             });
                    if (_queryResult != null && _queryResult.data.Rows.Count > 0)
                    {
                        string _DSFieldCaption = _queryResult.data.Rows[0]["caption"].ToString();
                        DSField = _queryResult.data.Rows[0]["name"].ToString();
                        DSFieldCaption = $"{_DSFieldCaption}-({DSField})";

                    }
                    else
                    {
                        isXMLParseError = true;
                    }

                }
                jsonObject.Add("DSFieldCaption", DSFieldCaption);
                jsonObject.Add("DSFieldDDN", new { name = DSField, caption = DSFieldCaption });
            }
            else
            {
                jsonObject.Add("DSFieldCaption", "");
                jsonObject.Add("DSFieldDDN", "");
            }
            jsonObject.Add("DSField", DSField);
            jsonObject.Add("DSTable", GetAttrValue(node, "a26", "table"));
            //jsonObject.Add("DSTable", GetAttrValue(node, "a26", "table"));

            return jsonObject;

        }

        private async Task<Dictionary<string, object>> getFillOptions(XElement node, Dictionary<string, object> jsonObject)
        {
            string fillMaster = GetElementValue(node, "a27");
            jsonObject.Add("fillMaster", fillMaster);

            if (fillMaster != "")
            {

                string fillMasterCaption = GetAttrValue(node, "a27", "fcaption");
                string fillsqlText = selectFldListWithSql.ContainsKey(fillMaster) ? selectFldListWithSql[fillMaster] : "";

                if (string.IsNullOrEmpty(fillMasterCaption) || string.IsNullOrEmpty(fillsqlText))
                {

                    //_logHelper.AppendTrace($"getting fill master field caption and sql text using query..");
                    var _queryResult = await _addFormFieldHelper.ExecuteQueryNew(projName, userName, "tstruct", QueryName.GET_TST_FLD_CAPTION_N_SQLTEXT, QueryCategory.general,
                                                               new JObject {
                                                                    { "structname", structName },
                                                                    { "fldname", fillMaster }
                                                               });
                    if (_queryResult != null && _queryResult.data.Rows.Count > 0)
                    {
                        string _fillMasterCaption = _queryResult.data.Rows[0]["caption"].ToString();
                        fillMasterCaption = $"{_fillMasterCaption}-({fillMaster})";
                        fillsqlText = _queryResult.data.Rows[0]["sqltext"].ToString().Trim();
                    }
                    else
                    {
                        isXMLParseError = true;
                    }



                }

                jsonObject.Add("fillMasterCaption", fillMasterCaption);
                jsonObject.Add("fillMasterDDN", new { name = fillMaster, caption = fillMasterCaption, sql = fillsqlText });
                jsonObject.Add("masterSqlText", fillsqlText);


            }
            else
            {
                jsonObject.Add("fillMasterCaption", "");
                jsonObject.Add("fillMasterDDN", "");

            }

            string fillSource = GetElementValue(node, "a28");

            jsonObject.Add("fillSource", fillSource);
            jsonObject.Add("fillSourceDDN", new { name = fillSource, caption = fillSource });

            return jsonObject;
        }
        private async Task<Dictionary<string, object>> getSQLOptions(XElement node, Dictionary<string, object> jsonObject)
        {


            string masterForm = GetAttrValue(node, "a26", "mastertstruct");
            jsonObject.Add("masterForm", masterForm);
            if (masterForm != "")
            {
                string masterFormCaption = GetAttrValue(node, "a26", "mtstcap");
                if (masterFormCaption == "")
                {
                    //_logHelper.AppendTrace($"getting master form caption using query..");
                    string _masterFormCaption = await _addFormFieldHelper.ExecuteQuery(projName, userName, "tstruct", QueryName.GET_TST_CAPTION,
                                        new JObject
                                        {
                                            { "structname", masterForm }
                                        });
                    if (string.IsNullOrEmpty(_masterFormCaption))
                    {
                        isXMLParseError = true;
                    }
                    masterFormCaption = $"{_masterFormCaption}-({masterForm})";
                }


                jsonObject.Add("masterFormCaption", masterFormCaption);
                jsonObject.Add("masterFormDDN", new { name = masterForm, caption = masterFormCaption });
                //}

                string masterField = GetAttrValue(node, "a26", "masterfield");
                jsonObject.Add("masterField", masterField);

                if (masterField != "")
                {
                    string masterFieldCaption = GetAttrValue(node, "a26", "mfldcap");
                    if (masterFieldCaption == "")
                    {
                        masterFieldCaption = await _addFormFieldHelper.ExecuteQuery(projName, userName, "tstruct", QueryName.GET_TST_Fld_CAPTION,
                                                new JObject {
                                                    { "structname", structName },
                                                    { "fldname", masterField }
                                                });
                        if (string.IsNullOrEmpty(masterFieldCaption))
                        {
                            isXMLParseError = true;
                        }
                    }



                    jsonObject.Add("masterFieldCaption", masterFieldCaption);
                    jsonObject.Add("masterFieldDDN", new { name = masterField, caption = $"{masterFieldCaption}-({masterField})" });

                }
                else
                {
                    jsonObject.Add("masterFieldCaption", "");
                    jsonObject.Add("masterFieldDDN", "");

                }


            }
            else
            {
                jsonObject.Add("masterFormCaption", "");
                jsonObject.Add("masterFormDDN", "");
            }

            jsonObject.Add("srcTable", GetAttrValue(node, "a26", "table"));
            //jsonObject.Add("tablename", GetAttrValue(node, "a26", "table"));
            string _srcField = GetElementValue(node, "a26");
            jsonObject.Add("srcField", _srcField);
            if (_srcField == string.Empty)
            {
                jsonObject.Add("oldmasterfieldAttr", GetAttrValue(node, "a26", "masterfield"));
            }

            //jsonObject.Add("firstcolvalue", GetElementValue(node, "a26"));
            string srcForm = GetAttrValue(node, "a26", "cap");
            if (srcForm != "")
            {
                jsonObject.Add("srcForm", srcForm);
                string srcformCaption = GetAttrValue(node, "a26", "sfcap");
                if (srcformCaption == string.Empty)
                {
                    //_logHelper.AppendTrace($"getting source form caption using query..");
                    srcformCaption = await _addFormFieldHelper.ExecuteQuery(projName, userName, "tstruct", QueryName.GET_TST_CAPTION,
                                        new JObject {
                                            { "structname", srcForm }});
                    if (string.IsNullOrEmpty(srcformCaption))
                    {
                        isXMLParseError = true;
                    }

                }
                jsonObject.Add("srcformCaption", srcformCaption);
            }
            else
            {
                jsonObject.Add("srcformCaption", "");
            }

            return jsonObject;
        }

        private async Task<Dictionary<string, object>> getAPIOptions(XElement node, Dictionary<string, object> jsonObject, string MOE)
        {


            if (MOE.ToLower() == "select")
            {
                jsonObject.Add("apiNameMO", GetAttrValue(node, "", "apiname"));
                jsonObject.Add("apiNameMODDN", GetAttrValue(node, "", "apiname"));
            }
            jsonObject.Add("apiURL", GetAttrValue(node, "", "url"));
            jsonObject.Add("apiReqStr", GetAttrValue(node, "", "reqstr"));
            jsonObject.Add("apiRespType", GetAttrValue(node, "", "restype"));
            jsonObject.Add("apiRespFormat", GetAttrValue(node, "", "resformat"));
            jsonObject.Add("apiparamstr", GetAttrValue(node, "", "paramstr"));
            jsonObject.Add("mapFieldApi", GetAttrValue(node, "", "mapname"));

            return jsonObject;

        }

        private async Task<Dictionary<string, object>> getSequenceOptions(XElement node, Dictionary<string, object> jsonObject)
        {
            var defSeqTable = new List<object>();
            //var defSeqTable = new JArray();
            string sequence = string.Join("^", node.Element("a41").Elements("l").Select(l =>
            {
                string prefix = GetElementValue(l, "a42");
                string prefixFldHdn = GetElementValue(l, "a53");
                object prefixFld = "";
                if (prefixFldHdn != "")
                    prefixFld = new { name = prefixFldHdn, caption = prefixFldHdn };
                string startNo = GetElementValue(l, "a44");
                var active = bool.TryParse(GetElementValue(l, "a45"), out bool _active) && _active;
                string noOfDigit = GetElementValue(l, "a54");
                //string prefixFldHdn = GetElementValue(l, "a54");
                //var jsonObject = new JObject
                //{
                //    { "prefix",prefix }, { "prefixFld",prefixFld }, { "prefixFldHdn",prefixFldHdn }, { "startNo", startNo}, { "noOfDigit",noOfDigit }

                //};
                defSeqTable.Add(new
                {
                    prefix,
                    prefixFld,
                    prefixFldHdn,
                    active,
                    startNo,
                    noOfDigit

                });
                return $"{prefix}|{prefixFldHdn}|{startNo}|{active}|{noOfDigit}";
            }));
            jsonObject.Add("defSeqTable", defSeqTable);
            jsonObject.Add("sequence", sequence);


            return jsonObject;

        }

        public async Task<(List<Dictionary<string, string>> ddnValues, string values)> getTrackUsers(XElement Element)
        {
            if (Element == null || !Element.Elements().Any())
            {
                return (new List<Dictionary<string, string>>(), "");
            }

            var ddnValues = new List<Dictionary<string, string>>();
            var values = new List<string>();

            foreach (var element in Element.Elements())
            {
                string name = element.Value;

                ddnValues.Add(new Dictionary<string, string>
                {
                    { "name", name },
                    { "caption", name }
                });

                values.Add(name);
            }

            return (ddnValues, string.Join(",", values));
        }

        public async Task<(List<Dictionary<string, string>> ddnValues, string values, string captions)> getTrackFlds(XElement Element)
        {
            if (Element == null || !Element.Elements().Any())
            {
                return (new List<Dictionary<string, string>>(), "", "");
            }

            var ddnValues = new List<Dictionary<string, string>>();
            var values = new List<string>();
            var captions = new List<string>();

            foreach (var element in Element.Elements())
            {
                string name = element.Value;
                string caption = GetAttrValue(element, "", "fcaption");
                if (caption == "")
                {
                    //_logHelper.AppendTrace($"getting field caption for track field \"{name}\" using query..");
                    var _queryResult = await _addFormFieldHelper.ExecuteQueryNew(projName, userName, "tstruct", QueryName.GET_TST_Fld_CAPTION, QueryCategory.general,
                                             new JObject {
                                                { "structname", structName },
                                                {  "fldname", name }
                                             });
                    if (_queryResult != null && _queryResult.data.Rows.Count > 0)
                    {
                        string _caption = _queryResult.data.Rows[0]["caption"].ToString();
                        name = _queryResult.data.Rows[0]["name"].ToString();
                        caption = $"{_caption}-({name})";

                    }
                    else
                    {
                        isXMLParseError = true;
                    }

                }

                ddnValues.Add(new Dictionary<string, string>
                {
                    { "name", name },
                    { "caption", caption }
                });

                values.Add(name);
                captions.Add(caption);
            }

            return (ddnValues, string.Join(",", values), string.Join(",", captions));
        }

        public async Task<Dictionary<string, object>> setSavectrlfld(XElement node, Dictionary<string, object> jsonObject)
        {
            string saveCtrlFld = GetElementValue(node, "a4");

            if (saveCtrlFld != "")
            {
                string savectrlFldCaption = GetAttrValue(node, "a4", "fcaption");
                if (savectrlFldCaption == "")
                {
                    //_logHelper.AppendTrace($"getting save control field caption using query..");
                    var _queryResult = await _addFormFieldHelper.ExecuteQueryNew(projName, userName, "tstruct", QueryName.GET_TST_Fld_CAPTION, QueryCategory.general,
                                             new JObject {
                                                { "structname", structName },
                                                {  "fldname", saveCtrlFld }
                                             });
                    if (_queryResult != null && _queryResult.data.Rows.Count > 0)
                    {
                        string _savectrlFldCaption = _queryResult.data.Rows[0]["caption"].ToString();
                        saveCtrlFld = _queryResult.data.Rows[0]["name"].ToString();
                        savectrlFldCaption = $"{_savectrlFldCaption}-({saveCtrlFld})";

                    }
                    else
                    {
                        isXMLParseError = true;
                    }
                }
                jsonObject.Add("saveCtrlFldsDDN", new { name = saveCtrlFld, caption = savectrlFldCaption });

            }
            else
            {
                jsonObject.Add("saveCtrlFldsDDN", "");

            }
            jsonObject.Add("saveCtrlFld", saveCtrlFld);


            return jsonObject;
        }

        public async Task<Dictionary<string, object>> setDltCtrlFld(XElement node, Dictionary<string, object> jsonObject)
        {
            string dltCtrlFld = GetElementValue(node, "a5");
            if (dltCtrlFld != "")
            {
                string dltCtrlFldCaption = GetAttrValue(node, "a5", "fcaption");
                if (dltCtrlFldCaption == "")
                {
                    //_logHelper.AppendTrace($"getting delete control field caption using query..");

                    var _queryResult = await _addFormFieldHelper.ExecuteQueryNew(projName, userName, "tstruct", QueryName.GET_TST_Fld_CAPTION, QueryCategory.general,
                                             new JObject {
                                                { "structname", structName },
                                                {  "fldname", dltCtrlFld }
                                             });
                    if (_queryResult != null && _queryResult.data.Rows.Count > 0)
                    {
                        string _dltCtrlFldCaption = _queryResult.data.Rows[0]["caption"].ToString();
                        dltCtrlFld = _queryResult.data.Rows[0]["name"].ToString();
                        dltCtrlFldCaption = $"{_dltCtrlFldCaption}-({dltCtrlFld})";

                    }
                    else
                    {
                        isXMLParseError = true;
                    }

                }
                jsonObject.Add("dltCtrlFldsDDN", new { name = dltCtrlFld, caption = dltCtrlFldCaption });

            }
            else
            {
                jsonObject.Add("dltCtrlFldsDDN", "");

            }
            jsonObject.Add("dltCtrlFld", dltCtrlFld);
            return jsonObject;
        }

        public async Task<Dictionary<string, object>> setTrackOptions(XElement node, Dictionary<string, object> jsonObject)
        {
            // bool.TryParse(GetElementValue(node, "a6"), out bool tracking);
            bool tracking = false;
            //if (node.HasElements) tracking = true;// If it has child nodes, return True
            //else tracking = !node.Value.Trim().Equals("False", StringComparison.OrdinalIgnoreCase);
            if (GetAttrValue(node, "", "track") == "true") tracking = true;


            if (tracking)
            {
                jsonObject.Add("tracking", true);
                if (GetAttrValue(node, "", "ausers") == "true")
                {
                    jsonObject.Add("trackby", "All users");
                    jsonObject.Add("trackusrs", "");
                    jsonObject.Add("trackUsrsDDN", new JArray());
                }
                else
                {
                    jsonObject.Add("trackby", "Selected Users");

                    var (trackUsrsDDN, trackUsrs) = await getTrackUsers(node.Element("a21"));

                    jsonObject.Add("trackUsrsDDN", trackUsrsDDN);
                    jsonObject.Add("trackusrs", trackUsrs);

                }
                if (GetAttrValue(node, "", "aflds") == "true")
                {
                    jsonObject.Add("trackfldOption", "All fields");
                    jsonObject.Add("trackFldsDDN", new JArray());
                    jsonObject.Add("trackFlds", "");
                    jsonObject.Add("trackFldCaps", "");
                }
                else
                {
                    jsonObject.Add("trackfldOption", "Selected fields");

                    var (trackFldsDDN, trackFlds, trackFldCaps) = await getTrackFlds(node.Element("a22"));

                    jsonObject.Add("trackFldsDDN", trackFldsDDN);
                    jsonObject.Add("trackFlds", trackFlds);
                    jsonObject.Add("trackFldCaps", trackFldCaps);

                }

            }
            else//tracking is false
            {
                var trackProperties = new Dictionary<string, object>{
                    { "tracking" , false},
                    {"trackby","" },
                    {"trackusrs","" },
                    {"trackUsrsDDN",new JArray() },
                    {"trackfldOption","" },
                    {"trackFlds","" },
                    {"trackFldsDDN",new JArray() }
                };
                jsonObject.Concat(trackProperties);
            }
            return jsonObject;

        }

        private string getTstCompHashSubKey(string comp, string compName, string ParentName = "")
        {

            switch (comp)
            {
                case "dc":
                    return $"{_addFormFieldHelper.GetCompKeyWord(comp)}♠{compName}";
                case "genmap":
                case "mdmap":
                case "fillgrid":
                case "script":
                case "btngrp":
                    return $"{_addFormFieldHelper.GetCompKeyWord(comp)}♠{compName}";
                case "udbutton":
                    return $"{_addFormFieldHelper.GetCompKeyWord(comp)}♠{compName}♠{ParentName}";
                case "field":
                default:
                    return $"{_addFormFieldHelper.GetCompKeyWord(comp)}♠{compName}♠{ParentName}";
            }
        }

        private string GetElementValue(XElement node, string elementName)
        {
            var element = node.Element(elementName);
            return _addFormFieldHelper.StandardizeBoolean(element?.Value ?? "");


        }
        private string GetAttrValue(XElement node, string elementName, string attributName)
        {
            XElement element;

            if (string.IsNullOrEmpty(elementName))
            {
                element = node;
            }
            else
            {
                element = node.Element(elementName);
            }

            return _addFormFieldHelper.StandardizeBoolean(element?.Attribute(attributName)?.Value ?? "");

        }
        private void AddTempNode()
        {
            //mainRootElement.Element("temp").ReplaceWith(new XElement("temp",
            mainRootElement.Add(new XElement("temp",
                new XElement("menupos", "default"),
                new XElement("menulist", ""),
                new XElement("listview", enableListvw),
                new XElement("layout", "Default"),
                new XElement("fcaption", structCaption),
                new XElement("defpage", menuaccess),
                new XElement("includepage", "F"),
                new XElement("changeddc", "")
                ));
        }

        private async Task ParseConfigNode(XElement node)
        {
            //await _redisHelper.RedisDB.HashSetAsync(mainStructHashkey, "♠confignode♠", node.ToString());
            _addFormFieldHelper.RuntimeHashSet(mainStructHashkey, "♠confignode♠", node.ToString());
            // Get  CVALUE node net to  "CONFIGNAME" node whose value is "searchcols"
            var cvalueNode = node.Elements("CONFIGNAME")
            .Where(x => x.Value.Trim() == "searchcols")
            .Select(x => x.ElementsAfterSelf("CVALUE").FirstOrDefault())
            .FirstOrDefault();

            if (cvalueNode != null && !string.IsNullOrWhiteSpace(cvalueNode.Value))
            {
                isSrchColExists = true;
                searchColList = cvalueNode.Value.Trim().Split(',').ToList();
            }
        }



        private string EvaluteFieldType(string dtype, string width, string decimals, string MOE)
        {
            if (MOE.ToLower() == "select")
            {
                return "DropDown";
            }
            else if (MOE.Replace(" ", "").ToLower() == "autogenerate")
            {
                return "Auto Generate";
            }


            if (dtype.ToLower() == "character")
            {
                int _width = int.Parse(width);
                if (_width <= 10)
                {
                    return "Short Text";
                }
                else if (_width > 10 && _width <= 50)
                {
                    return "Simple Text";
                }
                else
                {
                    return "Large Text";
                }
            }
            else if (dtype.ToLower() == "text")
            {
                return "Large Text";
            }
            else if (dtype.ToLower() == "numeric")
            {
                if (decimals == "0")
                {
                    return "Whole Number";

                }
                else
                {
                    return "Decimal Number";
                }
            }
            else if (dtype.ToLower() == "date/time")
            {
                return "Date";
            }
            else if (dtype.ToLower() == "image")
            {
                return "Image";
            }
            else
            {
                return "Simple Text";
            }

        }


    }
}
public class ParsedStructure
{
    public string StructName { get; set; }

    public bool AllowConfig { get; set; }

    public bool EnableCache { get; set; }

    public string MenuAccess { get; set; }

    public string PForm { get; set; }

    public string XmlVno { get; set; }

    public string RootNodeAttr { get; set; }

    public Dictionary<string, object> Configs { get; set; } = new();

    public Dictionary<string, object> Tstructs { get; set; } = new();

    public Dictionary<string, object> Dcs { get; set; } = new();

    public Dictionary<string, object> Fields { get; set; } = new();

    public Dictionary<string, object> Genmaps { get; set; } = new();

    public Dictionary<string, object> Fillgrids { get; set; } = new();

    public Dictionary<string, object> Mdmaps { get; set; } = new();

    public Dictionary<string, object> Scripts { get; set; } = new();

    public Dictionary<string, object> Components { get; set; } = new();
}

//public class StructureRuntimeContext
//{
//    private readonly Dictionary<string, object> _data =
//        new(StringComparer.OrdinalIgnoreCase);

//    public void Set(string key, object value)
//    {
//        _data[key] = value;
//    }

//    public T Get<T>(string key)
//    {
//        if (_data.TryGetValue(key, out var value))
//            return (T)value;

//        return default;
//    }

//    public bool TryGet<T>(string key, out T value)
//    {
//        if (_data.TryGetValue(key, out var obj) && obj is T t)
//        {
//            value = t;
//            return true;
//        }

//        value = default;
//        return false;
//    }
//}