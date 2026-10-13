using ARMCommon.Model;
using Microsoft.IdentityModel.Logging;
using Microsoft.VisualBasic.FileIO;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using StackExchange.Redis;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using System.Diagnostics;
using System.DirectoryServices.ActiveDirectory;
using System.Xml.Linq;
using AxStructDef.Models;
using Org.BouncyCastle.Asn1.X500;
using System.Text.RegularExpressions;
using ARMCommon.Helpers;
using System.Configuration;
using ARMCommon.Interface;
using System.Data;
using AxStructUtils.Helper;
using System.Xml;
using ARMCommon.Services;
using System;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace AxStructUtils
{
    public class AxAddFormFieldRepository : IAxAddFormFieldRepository
    {
        private string[] tstCompNameArr = ["props", "dc", "field", "genmap", "mdmap", "fillgrid", "script", "defbtns", "udbutton", "btngrp"];
        public bool isInsert = false;
        public string? ordNo = string.Empty;
        private string mode = string.Empty;
        public string? compName = string.Empty;
        private readonly AddFormFieldHelper _addFormFieldHelper;
        private readonly Utils _common;
        public AppSettingsService _appSettingsService;
        public AppSettingsHelper _appSettingsHelper;
        public AxAddFormFieldRepository(AddFormFieldHelper addFormFieldHelper, Utils common)
        {
            _addFormFieldHelper = addFormFieldHelper;
            _common = common;
            _appSettingsService = new AppSettingsService();
            _appSettingsHelper = new AppSettingsHelper(_appSettingsService);
            _common._appSettingsHelper = _appSettingsHelper;
        }
        public async Task<APIResult> AxAddField(AxAddFormField fieldData)
        {
            bool success = false;
            string compOldName = string.Empty;
            string message = string.Empty;
            string dcName = string.Empty;
            string comp = string.Empty;
            string projName = fieldData.projName;
            string userName = fieldData.userName;
            bool trace = fieldData.trace;
            string axMetamessage = string.Empty;
            mode = fieldData.mode;
            string structName = fieldData.structName;
            string structCaption = fieldData.structCaption;
            string structType = fieldData.structType;
            string propvalJSON = fieldData.fieldJSON;
            try
            {
                string runtimeinfostr = fieldData.runtimeInfo.Replace(@"\", "\\\\");
                var runtimeInfo = JObject.Parse(runtimeinfostr);
                var propvalJSONobj = JObject.Parse(fieldData.fieldJSON);
                string mainHashkey = $"{projName}♠{_addFormFieldHelper.GetStructKeyWord(structType)}♠{structName}";
                string structMode = "";
                string structXmlStr = "";
                comp = "field";
                compName = propvalJSONobj["name"]?.ToString();
                compOldName = compName;
                if (isAxpKeyWord(compName))
                {
                    throw new Exception($"{compName} is a keyword or function name. Please enter a different name.");
                }
                if (!_addFormFieldHelper.KeyExists(mainHashkey))
                {
                    try
                    {
                        structXmlStr = await GetTStructDefFromDb(structName, projName, userName, fieldData.ARMSessionId, trace);

                        _addFormFieldHelper.HashSet(mainHashkey, Constants.Tstruct.Redis.STRUCTXML_SUBKEY, structXmlStr);
                        TstXMLParser _tstXMLParser = new TstXMLParser(projName, userName, _addFormFieldHelper);
                        var _result = await _tstXMLParser.parseXml(structName, structXmlStr);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(ex.Message);
                    }
                    structMode = "edit";
                }
                else
                {
                    structXmlStr = _addFormFieldHelper.HashGet(mainHashkey, Constants.Tstruct.Redis.STRUCTXML_SUBKEY);
                    //in case of very first field save request, the structure will not be saved so struct xml will not available.
                    //in that case the new xml will be created using the metadata from db
                    if (structXmlStr != null && structXmlStr != string.Empty)
                    {
                        structMode = "edit";
                    }
                    else
                    {
                        structMode = "new";
                    }
                }
                if (fieldData.mode == "new" && await _addFormFieldHelper.isDuplicateTstFldName(projName, structName, structType, compName))
                {
                    throw new Exception("Duplicate values are not allowed in field name.");
                }
                string compCaption = propvalJSONobj["caption"]?.ToString();
                string MOE = propvalJSONobj["MOE"]?.ToString() ?? propvalJSONobj["source"].ToString() ?? string.Empty;
                string fieldType = propvalJSONobj["fieldType"]?.ToString();
                findFldOrdNo(propvalJSONobj, projName, structName, structType);

                string defultPropValJSONStr = Constants.Tstruct.Default_FldFormData.GetJson(fieldType.ToLower().Trim().Replace(" ", ""));
                if (defultPropValJSONStr == "")
                {
                    throw new Exception("Invalid Field Type.");
                }
                if (defultPropValJSONStr != null && defultPropValJSONStr != string.Empty)
                {
                    JObject defultpropvalJSONobj = JObject.Parse(defultPropValJSONStr);
                    if (fieldType != "autogenerate")
                    {
                        defultpropvalJSONobj.Remove("autogenStr");
                    }
                    foreach (var property in propvalJSONobj.Properties())
                    {
                        switch (property.Name)
                        {
                            case "dcName":
                                {
                                    defultpropvalJSONobj["dcName"] = property.Value;
                                    if (!propvalJSONobj.ContainsKey("dcNo") || !propvalJSONobj.ContainsKey("dcCaption"))
                                    {
                                        string dcNo = property.Value.ToString().Replace("dc", "");
                                        defultpropvalJSONobj["dcNo"] = dcNo;

                                        string dcCaption = "Header";
                                        if (dcNo == "2")
                                        {
                                            dcCaption = "Details";
                                        }
                                        if (dcNo == "3")
                                        {
                                            dcCaption = "Footer";
                                        }
                                        defultpropvalJSONobj["dcCaption"] = dcCaption;
                                    }
                                    break;
                                }
                            case "DSForm":
                                {
                                    if (propvalJSONobj["source"].ToString().ToLower() == "select from form")
                                    {
                                        defultpropvalJSONobj["DSForm"] = property.Value;
                                        defultpropvalJSONobj["DSFormDDN"] = new JObject { { "name", property.Value }, { "caption", $"{propvalJSONobj["DSFormCaption"]}-({property.Value})" } };
                                    }
                                    break;
                                }
                            case "DSFormCaption":
                                {
                                    if (propvalJSONobj["source"].ToString().ToLower() == "select from form")
                                    {
                                        defultpropvalJSONobj["DSFormCaption"] = $"{property.Value}-({propvalJSONobj["DSForm"]})";
                                    }
                                    break;
                                }
                            case "DSField":
                                {
                                    if (propvalJSONobj["source"].ToString().ToLower() == "select from form")
                                    {
                                        string _dsfield = property.Value.ToString();
                                        defultpropvalJSONobj["DSField"] = _dsfield;

                                        string _dstable = propvalJSONobj["DSTable"].ToString();
                                        bool _saveNomralized = bool.TryParse(propvalJSONobj["saveNomralized"]?.ToString(), out bool __saveNomralized) && __saveNomralized;
                                        defultpropvalJSONobj["DSFieldDDN"] = new JObject { { "name", _dsfield }, { "caption", $"{propvalJSONobj["DSFieldCaption"]}-({_dsfield})" } };

                                        string _sqltext = string.Empty;
                                        if (_dsfield != "" && _dstable != "")
                                        {
                                            _sqltext = $"Select a1.{_dsfield} from {_dstable} a1 order by a1.{_dsfield}";
                                        }
                                        defultpropvalJSONobj["SqlText"] = _sqltext;
                                    }
                                    break;
                                }
                            case "DSFieldCaption":
                                {
                                    if (propvalJSONobj["source"].ToString().ToLower() == "select from form")
                                    {
                                        defultpropvalJSONobj["DSFieldCaption"] = $"{property.Name}-({propvalJSONobj["DSField"]})";
                                    }
                                    break;
                                }
                            case "DSTable":
                                {
                                    if (propvalJSONobj["source"].ToString().ToLower() == "select from form")
                                    {
                                        defultpropvalJSONobj["DSTable"] = property.Value;
                                    }
                                    break;
                                }
                            case "SqlText":
                                {
                                    if (propvalJSONobj["source"]?.ToString()?.ToLower() != "select from form")
                                        defultpropvalJSONobj["SqlText"] = property.Value;
                                    break;
                                }
                            case "source":
                                {
                                    string _source = property.Value.ToString().ToLower();
                                    if (_source == "select from api")
                                    {
                                        defultpropvalJSONobj["source"] = "select from API";
                                    }
                                    else
                                    {
                                        defultpropvalJSONobj["source"] = _source;
                                    }
                                    break;
                                }
                            case "apiNameMO":
                                {
                                    defultpropvalJSONobj["apiNameMO"] = property.Value;

                                    if (property.Value.ToString() != string.Empty)
                                    {
                                        var queryResult = await _addFormFieldHelper.ExecuteQueryNew(projName, userName, "tstruct", QueryName.API_MO_INFO_BY_NAME, QueryCategory.general,
                                                                 new JObject {
                                                                    { "apiname", property.Value }
                                                                 });
                                        if (queryResult.data.Rows.Count > 0)
                                        {
                                            string apiURL = queryResult.data.Rows[0]["url"].ToString();
                                            string apiReqStr = queryResult.data.Rows[0]["reqstr"].ToString();
                                            string apiRespType = queryResult.data.Rows[0]["resptype"].ToString();
                                            string apiRespFormat = queryResult.data.Rows[0]["respformat"].ToString();

                                            defultpropvalJSONobj["apiNameMODDN"] = new JObject {
                                                    { "name", property.Value },
                                                    { "caption", property.Value },
                                                    { "url", apiURL },
                                                    { "reqstr", apiReqStr },
                                                    { "resptype", apiRespType },
                                                    { "respformat", apiRespFormat }
                                                };
                                            defultpropvalJSONobj["apiURL"] = apiURL;
                                            defultpropvalJSONobj["apiReqStr"] = apiReqStr;
                                            defultpropvalJSONobj["apiRespType"] = apiRespType;
                                            defultpropvalJSONobj["apiRespFormat"] = apiRespFormat;
                                        }
                                    }
                                    break;
                                }
                            case "defSrcField":
                                {
                                    if (fieldType.ToString().ToLower() == "table")
                                    {
                                        defultpropvalJSONobj["defSrcField"] = property.Value;
                                        defultpropvalJSONobj["defSrcFieldDDN"] = new JObject { { "name", property.Value }, { "caption", $"{propvalJSONobj["defSrcFieldCaption"]}-({property.Value})" } };
                                    }
                                    break;
                                }
                            case "defSrcFieldCaption":
                                {
                                    if (fieldType.ToString().ToLower() == "table")
                                    {
                                        defultpropvalJSONobj["defSrcFieldCaption"] = $"{property.Value}-({propvalJSONobj["defSrcFieldCaption"]})";
                                    }
                                    break;
                                }
                            case "autogenStr":
                                {
                                    if (fieldType.ToString().ToLower() == "auto generate")
                                    {
                                        defultpropvalJSONobj["sequence"] = property.Value;
                                        string[] autogenArr = property.Value.ToString().Split('|');
                                        JObject autogenObj = new JObject {
                                                        { "prefix", autogenArr[0] },
                                                        { "prefixFldHdn", autogenArr[1] },
                                                        { "prefixFldCaption", autogenArr[1] },
                                                        { "prefixFldDDN", new JObject {
                                                                        { "name", autogenArr[1] },
                                                                        { "caption", autogenArr[1] } }
                                                                    },
                                                        { "startNo", autogenArr[2] },
                                                        { "active", autogenArr[3] },
                                                        { "noOfDigit", autogenArr[3] },
                                                    };
                                        defultpropvalJSONobj["defSeqTable"] = autogenObj;

                                    }


                                    break;
                                }
                            default:
                                {
                                    // Only update fields that exist in json1
                                    if (property.Name == "fldPosition" || property.Name == "position" || defultpropvalJSONobj.ContainsKey(property.Name))
                                    {
                                        defultpropvalJSONobj[property.Name] = property.Value;
                                    }
                                    break;
                                }

                        }
                        defultpropvalJSONobj["runtimeFld"] = true;
                    }
                    defultpropvalJSONobj["saveNomralized"] = false;
                    propvalJSONobj = defultpropvalJSONobj;
                    propvalJSON = JsonConvert.SerializeObject(propvalJSONobj);
                }
                dcName = propvalJSONobj["dcName"]?.ToString() ?? string.Empty;
                if (structType == "tstruct" && !tstCompNameArr.Contains(comp))
                {
                    throw new Exception("Comp field value is not valid in compjson.");
                }
                string? subkey = string.Empty;

                if (structType == "tstruct")
                {
                    string formtype = runtimeInfo["formtype"]?.ToString() ?? "simple";
                    subkey = comp != "props" && comp != "defbtns" ? compName : $"♠{comp}♠";
                    _addFormFieldHelper.HashSet(mainHashkey, subkey, propvalJSON);
                    if (comp != "defbtns")
                        updateTstructCompHash(projName, structType, structName, comp, dcName, structCaption, compCaption, MOE, fieldType, formtype, compOldName);

                    if (comp == "props")
                    {
                        var tracking = bool.TryParse(propvalJSONobj["tracking"]?.ToString(), out bool _tracking) && _tracking;
                        if (mode == "new")
                        {
                            _addFormFieldHelper.HashSet(mainHashkey, "dc1", Constants.Tstruct.DEFAULTDCJSON.Replace("♠dcname♠", "dc1").Replace("♠dcno♠", "1").Replace("♠dccaption♠", structCaption).Replace("♠tablename♠", structName + "1").Replace("♠asgrid♠", "false"));


                            if (formtype != "simple")
                            {
                                _addFormFieldHelper.HashSet(mainHashkey, "dc2", Constants.Tstruct.DEFAULTDCJSON.Replace("♠dcname♠", "dc2").Replace("♠dcno♠", "2").Replace("♠dccaption♠", "Details").Replace("♠tablename♠", structName + "2").Replace("♠asgrid♠", "true"));

                            }
                            if (formtype == "header-details-footer")
                            {
                                _addFormFieldHelper.HashSet(mainHashkey, "dc3", Constants.Tstruct.DEFAULTDCJSON.Replace("♠dcname♠", "dc3").Replace("♠dcno♠", "3").Replace("♠dccaption♠", "Footer").Replace("♠tablename♠", structName + "3").Replace("♠asgrid♠", "false"));

                            }

                            if (tracking)
                            {

                                _addFormFieldHelper.HashSet(mainHashkey, "♠defbtns♠", Constants.Tstruct.DEFAULT_BUTTON_JSON.Replace("♠viewhistory♠", Constants.Tstruct.VIEWHIST_BTN_NODE));
                            }
                            else
                            {
                                _addFormFieldHelper.HashSet(mainHashkey, "♠defbtns♠", Constants.Tstruct.DEFAULT_BUTTON_JSON.Replace("♠viewhistory♠", ""));
                            }

                        }
                        else
                        {

                            var defbtnjson = _addFormFieldHelper.HashGet(mainHashkey, "♠defbtns♠");
                            string defbtnjsonStr = defbtnjson.ToString();
                            if (tracking)
                            {
                                if (defbtnjsonStr.IndexOf("viewhistory") == -1)
                                {
                                    string updatedJson = defbtnjsonStr.Insert(defbtnjsonStr.Length - 1, Constants.Tstruct.VIEWHIST_BTN_NODE);
                                    _addFormFieldHelper.HashSet(mainHashkey, "♠defbtns♠", updatedJson);
                                }

                            }
                            else
                            {
                                if (defbtnjsonStr.IndexOf("viewhistory") != -1)
                                {
                                    var updatedJsonArr = defbtnjsonStr.Split(",\"viewhistory\"");
                                    updatedJsonArr = updatedJsonArr.Take(updatedJsonArr.Length - 1).ToArray();
                                    _addFormFieldHelper.HashSet(mainHashkey, "♠defbtns♠", string.Join(",", updatedJsonArr) + "}");
                                }
                            }
                        }

                    }
                    //when a filed name changes,this condition will remove oldfld name if exists from changedcomps.
                    if (mode == "edit" && comp == "field" && compName != compOldName)
                    {
                        _addFormFieldHelper.HashDelete(mainHashkey, compOldName);
                    }
                }
                else
                {
                    //_redisHelper.RedisDB.StringSet(mainHashkey, propvalJSON);
                }
                if (structType == "tstruct" && comp == "field")
                {
                    TstXMLBuilder _tstXMLBuilder = new TstXMLBuilder(projName, userName, _addFormFieldHelper, true);
                    string newStructXml = string.Empty;
                    if (structMode == "edit")
                    {
                        try
                        {
                            newStructXml = _tstXMLBuilder.AddfieldToExistingXML(structName, propvalJSON, structXmlStr);
                        }
                        catch (Exception ex)
                        {
                            throw new Exception(ex.Message);
                        }
                    }
                    else
                    {
                        AxAddFieldApiResponse<string> createXMLResp = await _tstXMLBuilder.createXml(structName);
                        if (createXMLResp.success)
                        {
                            newStructXml = createXMLResp.data;
                        }
                        else
                        {
                            throw new Exception(createXMLResp.message);
                        }

                    }
                    if (newStructXml != string.Empty)
                    {
                        var servicResult = await WriteDefinitionToDB(projName, userName, structName, structType, newStructXml, trace, fieldData.ARMSessionId);
                        if (servicResult.success)
                        {
                            axMetamessage = await updateMetaDataMasterTable(fieldData, "field", projName);

                            success = servicResult.success;
                            if (servicResult.result == "Structure Created Successfully...!")
                                message = "Field added and Structure Created Successfully...!";
                            else
                                message = servicResult.result;
                        }
                        else
                        {
                            throw new Exception(servicResult.result);
                        }

                    }
                    else
                    {
                        throw new Exception("Error while creating structure xml");
                    }
                }
                success = true;
            }
            catch (Exception ex)
            {
                success = false;
                message = ex.Message.Split('\n', StringSplitOptions.RemoveEmptyEntries)[^1];
            }
            finally
            {
                if (trace)
                {
                    //_logHelper.CreateLog(sessionID, "saveformdataRutimeAPI-axpertStudio-" + structName + "-" + compName, "new");
                    string ScriptsPathFolder = _appSettingsService.GetARMSettings(fieldData.projName).ScriptsPath;
                    _addFormFieldHelper.CreateLog("Exception AxAddField API:" + message, fieldData.ARMSessionId, ScriptsPathFolder, "AxAddFieldAPI-" + structName, "new");
                    if (axMetamessage != "success")
                        _addFormFieldHelper.CreateLog("Error in AxMetaDataMasterTable :" + axMetamessage, fieldData.ARMSessionId, ScriptsPathFolder, "AxAddFieldAPI-" + structName, "");
                }
            }
            var response = new APIResult
            {
                success = success,
                error = message
            };
            return response;
        }

        public async Task<APIResult> AxAddForm(AxAddFormField fieldData)
        {
            bool success = false;
            string compOldName = string.Empty;
            string message = string.Empty;
            string dcName = string.Empty;
            string comp = string.Empty;
            string projName = fieldData.projName;
            string userName = fieldData.userName;
            bool trace = fieldData.trace;
            string axMetamessage = string.Empty;
            mode = fieldData.mode;
            string structName = fieldData.structName;
            string structCaption = fieldData.structCaption;
            string structType = fieldData.structType;
            string propvalJSON = fieldData.propvalJSON;
            try
            {
                string runtimeinfostr = fieldData.runtimeInfo.Replace(@"\", "\\\\");
                var runtimeInfo = JObject.Parse(runtimeinfostr);
                var propvalJSONobj = JObject.Parse(fieldData.propvalJSON);
                var fieldJSONObj = JObject.Parse(fieldData.fieldJSON);
                string mainHashkey = $"{projName}♠{_addFormFieldHelper.GetStructKeyWord(structType)}♠{structName}";
                string structMode = "";
                string structXmlStr = "";

                if (!fieldData.isComp)
                {
                    if (isAxpKeyWord(structName))
                    {
                        throw new Exception($"{structName} is a keyword or function name. Please enter a different name.");
                    }
                    if (IsDuplicateStructName(projName, userName, structName, structType))
                    {
                        throw new Exception("Duplicate Transid is not allowed.");
                    }
                    propvalJSONobj["runtimeFld"] = true;
                    comp = compName = "props";
                }
                if (structType == "tstruct" && !tstCompNameArr.Contains(comp))
                {
                    throw new Exception("Comp field value is not valid in compjson.");
                }
                string? subkey = string.Empty;

                if (structType == "tstruct")
                {
                    string formtype = runtimeInfo["formtype"]?.ToString() ?? "simple";
                    subkey = comp != "props" && comp != "defbtns" ? compName : $"♠{comp}♠";
                    _addFormFieldHelper.HashSet(mainHashkey, subkey, propvalJSON);

                    string compCaption = fieldJSONObj["caption"]?.ToString();
                    string MOE = fieldJSONObj["MOE"]?.ToString() ?? fieldJSONObj["source"].ToString() ?? string.Empty;
                    string fieldType = fieldJSONObj["fieldType"]?.ToString();
                    if (comp != "defbtns")
                        updateTstructCompHash(projName, structType, structName, comp, dcName, structCaption, compCaption, MOE, fieldType, formtype, compOldName);

                    if (comp == "props")
                    {
                        var tracking = bool.TryParse(propvalJSONobj["tracking"]?.ToString(), out bool _tracking) && _tracking;
                        if (mode == "new")
                        {
                            _addFormFieldHelper.HashSet(mainHashkey, "dc1", Constants.Tstruct.DEFAULTDCJSON.Replace("♠dcname♠", "dc1").Replace("♠dcno♠", "1").Replace("♠dccaption♠", structCaption).Replace("♠tablename♠", structName + "1").Replace("♠asgrid♠", "false"));

                            if (formtype != "simple")
                            {
                                _addFormFieldHelper.HashSet(mainHashkey, "dc2", Constants.Tstruct.DEFAULTDCJSON.Replace("♠dcname♠", "dc2").Replace("♠dcno♠", "2").Replace("♠dccaption♠", "Details").Replace("♠tablename♠", structName + "2").Replace("♠asgrid♠", "true"));

                            }
                            if (formtype == "header-details-footer")
                            {
                                _addFormFieldHelper.HashSet(mainHashkey, "dc3", Constants.Tstruct.DEFAULTDCJSON.Replace("♠dcname♠", "dc3").Replace("♠dcno♠", "3").Replace("♠dccaption♠", "Footer").Replace("♠tablename♠", structName + "3").Replace("♠asgrid♠", "false"));

                            }

                            if (tracking)
                            {

                                _addFormFieldHelper.HashSet(mainHashkey, "♠defbtns♠", Constants.Tstruct.DEFAULT_BUTTON_JSON.Replace("♠viewhistory♠", Constants.Tstruct.VIEWHIST_BTN_NODE));
                            }
                            else
                            {
                                _addFormFieldHelper.HashSet(mainHashkey, "♠defbtns♠", Constants.Tstruct.DEFAULT_BUTTON_JSON.Replace("♠viewhistory♠", ""));
                            }

                        }
                        else
                        {

                            var defbtnjson = _addFormFieldHelper.HashGet(mainHashkey, "♠defbtns♠");
                            string defbtnjsonStr = defbtnjson.ToString();
                            if (tracking)
                            {
                                if (defbtnjsonStr.IndexOf("viewhistory") == -1)
                                {
                                    string updatedJson = defbtnjsonStr.Insert(defbtnjsonStr.Length - 1, Constants.Tstruct.VIEWHIST_BTN_NODE);
                                    _addFormFieldHelper.HashSet(mainHashkey, "♠defbtns♠", updatedJson);
                                }

                            }
                            else
                            {
                                if (defbtnjsonStr.IndexOf("viewhistory") != -1)
                                {
                                    var updatedJsonArr = defbtnjsonStr.Split(",\"viewhistory\"");
                                    updatedJsonArr = updatedJsonArr.Take(updatedJsonArr.Length - 1).ToArray();
                                    _addFormFieldHelper.HashSet(mainHashkey, "♠defbtns♠", string.Join(",", updatedJsonArr) + "}");
                                }
                            }
                        }

                    }
                    //when a filed name changes,this condition will remove oldfld name if exists from changedcomps.
                    if (mode == "edit" && comp == "field" && compName != compOldName)
                    {
                        _addFormFieldHelper.HashDelete(mainHashkey, compOldName);
                    }
                }
                else
                {
                    //_redisHelper.RedisDB.StringSet(mainHashkey, propvalJSON);
                }
                if (structType == "tstruct" && comp == "field")
                {
                    TstXMLBuilder _tstXMLBuilder = new TstXMLBuilder(projName, userName, _addFormFieldHelper, true);
                    string newStructXml = string.Empty;
                    if (structMode == "edit")
                    {
                        try
                        {
                            newStructXml = _tstXMLBuilder.AddfieldToExistingXML(structName, propvalJSON, structXmlStr);
                        }
                        catch (Exception ex)
                        {
                            throw new Exception(ex.Message);
                        }
                    }
                    else
                    {
                        AxAddFieldApiResponse<string> createXMLResp = await _tstXMLBuilder.createXml(structName);

                        if (createXMLResp.success)
                        {
                            newStructXml = createXMLResp.data;
                        }
                        else
                        {
                            throw new Exception(createXMLResp.message);
                        }

                    }
                    if (newStructXml != string.Empty)
                    {
                        //var servicResult = await WriteDefinitionToDB(projName, userName, structName, structType, newStructXml, trace, fieldData.ARMSessionId);
                        //if (servicResult.success)
                        //{

                        //}
                        //else
                        //{
                        //    throw new Exception(servicResult.result);
                        //}
                    }
                    else
                    {
                        throw new Exception("Error while creating structure xml");
                    }
                }
                fieldData.isComp = true;

                axMetamessage = await updateMetaDataMasterTable(fieldData, "props", projName);

                AxAddField(fieldData);
                message = "Structure Created Successfully...!";
                success = true;
            }
            catch (Exception ex)
            {
                success = false;
                message = ex.Message.Split('\n', StringSplitOptions.RemoveEmptyEntries)[^1];
            }
            finally
            {
                if (trace)
                {
                    string ScriptsPathFolder = _appSettingsService.GetARMSettings(fieldData.projName).ScriptsPath;
                    _addFormFieldHelper.CreateLog("Exception AxAddForm API:" + message, fieldData.ARMSessionId, ScriptsPathFolder, "AxAddForm-" + structName, "new");
                    if (axMetamessage != "success")
                        _addFormFieldHelper.CreateLog("Error in AxMetaDataMasterTable :" + axMetamessage, fieldData.ARMSessionId, ScriptsPathFolder, "AxAddForm-" + structName, "");
                }
            }
            var response = new APIResult
            {
                success = success,
                error = message
            };
            return response;
        }

        public static readonly string AXPKEYWORDS = "ADD,ALTER,AND,ANY,AS,ASC,AUDIT,BETWEEN,BY,CHAR,CHECK,CLUSTER,COLUMN,COMMENT,COMPRESS,CONNECT,CREATE,CURRENT,DATE,DECIMAL,DELETE,DESC,DISTINCT,DROP,ELSE,EXCLUSIVE,EXISTS,FILE,FLOAT,FOR,FROM,GRANT,GROUP,HAVING,IDENTIFIED,IN,INCREMENT,INDEX,INITIAL,INSERT,INTEGER,INTERSECT,INTO,IS,LEVEL,LIKE,LOCK,LONG,MAXEXTENTS,MINUS,MODE,MODIFY,NOAUDIT,NOCOMPRESS,NOWAIT,NULL,NUMBER,OF,OFFLINE,OPTION,OR,ORDER,PCTFREE,PRIOR,PUBLIC,RAW,RENAME,RESOURCE,REVOKE,ROW,ROWID,ROWLABEL,ROWNUM,ROWS,SELECT,SESSION,SET,SHARE,SIZE,SMALLINT,START,SUCCESSFUL,SYNONYM,SYSDATE,TABLE,TO,TRIGGER,UNION,UNIQUE,USER,VALIDATE,VALUES,VARCHAR,VARCHAR2,VIEW,WHENEVER,WHERE,WITH,UPDATE,FIELD,TAB,DATABASE,ORDER BY,DEFAULT,VALUE,JOINT,PASSWORD,NO,NOT,TO,FIELDCHANGED,GETRCELL,UPPER,LOWER,DTOC,CTOD,RND,ROUND,STUFF,IIF,AMTWORD,VAL,STR,SUBSTR,CONVERT,REVCONVERT,ENCODE,DECODE,HASHCHAR,MANDY,DAYSELAPSED,TIMEELAPSED,ADDTOTIME,ADDTOMONTH,ISEMPTYVALUE,LASTDAYOFMONTH,VALIDENCODEDATE,EVAL,ISEMPTY,ABS,MAKEDATE,DATE,FORMATDATETIME,TIME,DAYOFDATE,MONTHOFDATE,YEAROFDATE,GEN_ID,TOTAL,SUM,GETMAX,GETVALUE,GETROW,GETROWCOUNT,GETCOSTRATE,GETADJUSTEDAMOUNT,SUMTILL,GETCELL,GETINTEGER,POWER,CREATEDBY,CREATEDON,AXPCEIL,AXPFLOOR,DAYS360,DATA,ROWCOUNT";
        public bool isAxpKeyWord(string input)
        {

            var keyArr = AXPKEYWORDS.ToLower().Split(",");
            return keyArr.Contains(input.ToLower());
        }

        public async Task<string> GetTStructDefFromDb(string structName, string proj, string userName, string ARMSessionId, bool trace, Dictionary<string, string> sessInfo = null)
        {
            string ScriptsPathFolder = _appSettingsService.GetARMSettings(proj).ScriptsPath;
            try
            {
                string res = string.Empty;
                string iXml = string.Empty;
                string errfileName = string.Empty;
                if (trace)
                {
                    errfileName = _addFormFieldHelper.CreateLog("Calling webservice to get Structure from DB", ARMSessionId, ScriptsPathFolder, "opentstruct-API-" + structName, "new");
                }
                var isValidSession = await _common.ValidateSession(ARMSessionId);

                string axglobalvars = _common._sessionVars["GLOBALVARS"].ToString();
                string axuservars = _common._sessionVars["USERVARS"].ToString();
                string axPropsXML = await _common.GetAxProps(proj);
                string axAppsXML = await _common.GetAxApps(proj);
                XmlDocument doc = JsonConvert.DeserializeXmlNode(axglobalvars, "globalvars");
                axglobalvars = doc.OuterXml;
                XmlDocument udoc = JsonConvert.DeserializeXmlNode(axuservars, "uservars");
                axuservars = udoc.OuterXml;
                iXml = iXml + "<root axstudio='true' fromprocess='false' axpapp='" + proj + "' axrulescached='F' sessionid='" + ARMSessionId + "' appsessionkey='' username='" + userName + "' trace='" + errfileName + "' axdesign='false' axdesigndefault='false' ><sname>tstructs</sname><name>" + structName + "</name><caption></caption>";
                iXml += axAppsXML + axPropsXML + axglobalvars + axuservars + "</root>";
                //Call service
                string _scriptPath = _appSettingsService.GetARMSettings(proj).scriptsUrlPath;
                //_addFormFieldHelper.GetASBActionClient(_scriptPath);
                res = await _addFormFieldHelper.CallGetStructureWS(_scriptPath, structName, iXml);
                if (res.Contains("<error>") == true)
                {
                    res = res.Replace("<error>", "");
                    res = res.Replace("</error>", "");
                    res = res.Replace("\n", "");
                    throw (new Exception(res));
                }
                else
                {
                    return res;
                }
            }
            catch (Exception ex)
            {
                if (trace)
                {
                    _addFormFieldHelper.CreateLog($"Error fetching structure definition: {ex.Message} transid: {structName}", ARMSessionId, ScriptsPathFolder, $"opentstruct-exception-API-{structName}", "new");
                }
                throw new Exception($"Error fetching structure definition: {ex.Message}");
            }
        }
        private async Task findFldOrdNo(JObject propvalJSONobj, string projName, string structName, string structType)
        {
            string fldPosition = propvalJSONobj["fldPosition"]?.ToString();
            string fldrefrence = propvalJSONobj["position"]?.ToString();
            string compHashKey = $"{projName}♠{_addFormFieldHelper.GetStructKeyWord(structType)}♠{structName}♠comps";
            if (fldPosition != null && fldPosition != "" && fldrefrence != null && fldrefrence != string.Empty)
            {

                string pattern = @"\((.*?)\)";
                Match match = Regex.Match(fldrefrence, pattern);
                if (match.Success)
                {
                    fldrefrence = match.Groups[1].Value; // Extracted string inside ()
                }
                var fieldcomp = _addFormFieldHelper.HashScan(compHashKey, $"axfield♠{fldrefrence}♠*").FirstOrDefault();
                if (fieldcomp != default && int.TryParse(fieldcomp.Value?.ToString()?.Split('♠').FirstOrDefault(), out int _compOrdNo))
                {
                    if (fldPosition == "After field")
                    {
                        ordNo = (_compOrdNo + 1).ToString();
                    }
                    else
                    {
                        ordNo = (_compOrdNo - 1).ToString();
                    }

                    isInsert = true;
                }
                else
                {
                    throw new Exception($"Exception in updateTstructCompHash : Failed to parse order numbers for key: {fieldcomp.Value}");
                }
            }
            else
            {
                if (mode == "new")
                {
                    var fieldList = _addFormFieldHelper.HashScan(compHashKey, "axfield♠*").ToList();
                    ordNo = (fieldList.Count + 1).ToString();
                    isInsert = false;
                }
                else
                {
                    var fldHash = _addFormFieldHelper.HashScan(compHashKey, $"axfield♠{compName}♠*").FirstOrDefault();
                    if (fldHash != default)
                    {
                        var hashVal = fldHash.Value?.ToString();
                        if (!string.IsNullOrEmpty(hashVal))
                        {
                            var splitval = hashVal.Split('♠');
                            ordNo = splitval.FirstOrDefault();
                        }
                    }
                    else
                    {
                        throw new Exception("Exception while getting order no.");
                    }
                }
            }
        }
        public bool IsDuplicateStructName(string projName, string userName, string structName, string structType)
        {
            string mainHashkey = $"{projName}♠{_addFormFieldHelper.GetStructKeyWord(structType)}♠{structName}";
            if (_addFormFieldHelper.KeyExists(mainHashkey))
                return true;
            else
            {
                if (structType == "tstruct")
                {
                    try
                    {
                        Dictionary<string, string> configcon = _appSettingsHelper.GetDBConfigurations(projName);
                        string connectionString = configcon["ConnectionString"];
                        string dbType = configcon["DBType"];
                        string sql = CustomSQL_AddField.GetSqlQuery(dbType, "checkduptstruct");
                        IDbHelper dbHelper = DBHelper.CreateDbHelper(dbType);
                        ScalarResult result = dbHelper.ExecuteScalarAsync(sql, connectionString, new[] { "structname", "structtype" }, new[] { DbType.String, DbType.String }, new object[] { structName, structType }).GetAwaiter().GetResult();
                        return Convert.ToInt32(result.result) == 1;
                    }
                    catch
                    {
                        return true;
                    }
                }
                else
                    return false;
            }
            return false;
        }

        public async Task<dynamic> WriteDefinitionToDB(string projName, string userName, string structName, string structType, string strXML, bool trace, string ARMSessionId)
        {
            string result;
            string ScriptsPathFolder = _appSettingsService.GetARMSettings(projName).ScriptsPath;
            bool success = false;
            string axapps;
            string axprops;
            try
            {
                var isValidSession = await _common.ValidateSession(ARMSessionId);

                string axglobalvars = _common._sessionVars["GLOBALVARS"].ToString();
                string axuservars = _common._sessionVars["USERVARS"].ToString();
                string axPropsXML = await _common.GetAxProps(projName);
                string axAppsXML = await _common.GetAxApps(projName);
                XmlDocument doc = JsonConvert.DeserializeXmlNode(axglobalvars, "globalvars");
                axglobalvars = doc.OuterXml;
                XmlDocument udoc = JsonConvert.DeserializeXmlNode(axuservars, "uservars");
                axuservars = udoc.OuterXml;

                string inpXML = "<root ";
                if (structType == "iv")
                {
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(strXML);
                    string ppagecreate = xmlDoc.SelectNodes("//root/temp/defpage")[0].InnerText;
                    inpXML += $"defaultpage='{ppagecreate.ToUpper()}' ";
                }

                string traceLogPath = string.Empty;
                if (trace)
                    traceLogPath = _addFormFieldHelper.CreateLog($"Trace log for {structName}", ARMSessionId, ScriptsPathFolder, $"WriteDefinitionToDB-API-{structName}", "new");
                string _scriptPath = _appSettingsService.GetARMSettings(projName).scriptsUrlPath;
                inpXML += "scriptpath='" + _scriptPath + "'  axpapp='" + projName + "' trace='" + traceLogPath + "' sessionid='" + ARMSessionId + "' appsessionkey='' transid='" + structName + "' stype='" + structType.Substring(0, 1) + "' username='" + userName + "'>";
                inpXML += axAppsXML;
                inpXML += axPropsXML + axglobalvars + axuservars + "</root>";
                //_addFormFieldHelper.GetASBDefineClient(_scriptPath);
                result = await _addFormFieldHelper.CallWriteDefinitionWS(_scriptPath, inpXML, strXML);
                if (result.StartsWith("Exception"))
                {
                    success = false;
                    result = result.Split(':').ElementAtOrDefault(1);
                }
                else if (result.StartsWith("{"))
                {
                    result = result.Split("#$#")[0];
                    result = result.Split("♠")[0];
                    JObject jsonResult = JObject.Parse(result.Split("#$#")[0]);
                    string message = (string)jsonResult["message"]?[0]?["msg"];
                    if (message == "Structure Created Successfully...!")
                    {
                        success = true;
                        result = message;
                    }
                    else
                    {
                        success = false;
                    }
                }
                else
                {
                    success = false;
                }
            }
            catch (Exception ex)
            {
                if (trace)
                {
                    _addFormFieldHelper.CreateLog($"Error in WriteDefinition WS: {ex.Message} transid: {structName}", ARMSessionId, ScriptsPathFolder, $"WriteDefinitionToDB-exception-API-{structName}", "new");
                }
                success = false;
                result = $"Error in WriteDefinition WS: {ex.Message}";
            }
            return new { success, result };
        }

        private async Task<string> updateMetaDataMasterTable(AxAddFormField param, string comp, string projName)
        {
            try
            {
                Dictionary<string, string> configcon = _appSettingsHelper.GetDBConfigurations(projName);
                string connectionString = configcon["ConnectionString"];
                string dbType = configcon["DBType"];
                IDbHelper dbHelper = DBHelper.CreateDbHelper(dbType);
                string sql;
                string[] paramNames;
                DbType[] paramTypes;
                object[] paramValues;
                if ((comp == "props" || string.IsNullOrEmpty(comp)) && param.mode == "new")
                {
                    sql = CustomSQL_AddField.GetSqlQuery(dbType, "insert_metadatamaster");
                    paramNames = new[] { "structtype", "structname", "structcaption", "structstatus", "createdby", "updatedby" };
                    paramTypes = new[] { DbType.String, DbType.String, DbType.String, DbType.String, DbType.String, DbType.String };
                    paramValues = new object[] { param.structType, param.structName, param.structCaption, "ready", param.userName, param.userName };
                }
                else
                {
                    sql = CustomSQL_AddField.GetSqlQuery(dbType, "update_metadatamaster");
                    paramNames = new[] { "structname", "structcaption", "updatedby", "structtype" };
                    paramTypes = new[] { DbType.String, DbType.String, DbType.String, DbType.String };
                    paramValues = new object[] { param.structName, param.structCaption, param.userName, param.structType };
                }
                NonQueryResult result = await dbHelper.ExecuteNonQueryAsync(sql, connectionString, paramNames, paramTypes, paramValues);
                if (result.count > 0)
                    return "success";
                else
                {
                    return result.error;
                }
            }
            catch (Exception ex)
            {
                if (comp == "props" || string.IsNullOrEmpty(comp))
                    throw new Exception("Error while inserting in axmetadatamaster table. ex:" + ex.Message);
                else
                    throw new Exception("Error while update in axmetadatamaster table. ex:" + ex.Message);
            }
        }
        private async void updateTstructCompHash(string projName, string structType, string structName, string comp, string dcName, string structCaption, string compCaption, string MOE, string fieldType, string formtype, string compOldName)
        {
            string Haskey = $"{projName}♠{_addFormFieldHelper.GetStructKeyWord(structType)}♠{structName}♠comps";
            string parent = "";
            if (comp == "field")
            {
                parent = dcName;
            }
            else if (comp == "udbutton")
            {
                parent = "";// btnParent;
            }
            string? subkey = _addFormFieldHelper.getTstCompHashSubKey(comp, compName, parent);
            string val = _addFormFieldHelper.getTstCompHashVal(comp, ordNo, compCaption, "false", "", MOE, fieldType);

            if (isInsert)
            {
                IEnumerable<HashEntry> compEntries = null;

                try
                {
                    if (comp == "dc")
                    {
                        compEntries = _addFormFieldHelper.HashScanNew(Haskey, "axdc♠*");
                    }
                    else if (comp == "field")
                    {
                        compEntries = _addFormFieldHelper.HashScanNew(Haskey, "axfield♠*" + dcName);
                    }

                    foreach (HashEntry entry in compEntries)
                    {
                        var key = entry.Name.ToString();
                        var valueArr = entry.Value.ToString().Split('♠');
                        if (int.TryParse(valueArr.FirstOrDefault(), out int compOrdNo) && int.TryParse(ordNo, out int ordNoInt))
                        {
                            if (compOrdNo >= ordNoInt)
                            {
                                valueArr[0] = (compOrdNo + 1).ToString();
                                _addFormFieldHelper.HashSet(Haskey, key, string.Join("♠", valueArr));
                            }
                        }
                        else
                        {
                            throw new Exception($"Exception in updateTstructCompHash : Failed to parse order numbers for key: {key}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Exception in updateTstructCompHash : " + ex.Message, ex);
                }

            }

            if (comp == "props")
            {
                if (mode == "new")
                {
                    List<HashEntry> hashEntries = new List<HashEntry>
                                    {
                                        new HashEntry("axdc♠dc1", $"1♠{structCaption}♠false♠{structName}1")
                                    };

                    // Add conditionally
                    if (formtype != "simple")
                    {
                        hashEntries.Add(new HashEntry("axdc♠dc2", $"2♠Details♠true♠{structName}2"));
                    }
                    if (formtype == "header-details-footer")
                    {
                        hashEntries.Add(new HashEntry("axdc♠dc3", $"3♠Footer♠false♠{structName}3"));
                    }
                    _addFormFieldHelper.HashSetNew(Haskey, hashEntries.ToArray());
                }

            }
            else
            {

                _addFormFieldHelper.HashSet(Haskey, subkey, val);

            }

            //Delete the old subkey when a tstruct field name is changed.
            if (structType == "tstruct" && comp == "field" && mode == "edit" && compName != compOldName)
            {
                string? oldSubkey = _addFormFieldHelper.getTstCompHashSubKey(comp, compOldName, dcName);
                _addFormFieldHelper.HashDelete(Haskey, oldSubkey);
            }
        }
    }
}
