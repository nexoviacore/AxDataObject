using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMCommon.Model;
using AxParserApp;
using AxStructDef;
using AxStructDef.Helpers;
using AxStructDef.Models;
using AxValidate.Dto;
using AxValidate.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.Text.RegularExpressions;
using System.Xml;

namespace AxValidate
{
    public class Validate
    {
        public bool validationPassed;
        public SQLResult sqlResult;
        public string LocThousandSeparator;
        public List<string> validateResultList;
        public List<string> errorList;
        private bool onDataLoad;
        private TStringList ParentList { get; set; }
        private static List<string> ErrorLog;
        public string DynamicSql { get; set; }
        public bool dependCall { get; set; }
        private string ErrorStr { get; set; }
        private string WarningMsg { get; set; }
        private bool IsError { get; set; }
        private bool DeleteRowOnError { get; set; }
        private Int64 idValue;
        private static string sName;
        private static dynamic connection;
        private static System.ComponentModel.IComponent owner;
        public AxStructDefUtils _utils;
        private string FieldValue;
        private bool Validation; // placeholder (Delphi had global flags)
        private bool Loading;
        private static IConfiguration _config;
        private bool nonDynamicFilterSQL;
        private static string transid;
        private ValidateHelper _validateHelper;
        private static int LinkSearchCol;
        private static int actualRowNo;
        private static bool forPickList;
        private readonly IConfiguration _configuration;
        private IARMLogTraceService _traceService;
        private Dictionary<string, SQLResult> fillSqlResultDict;
        private string keyName;
        public string MatchErrorStr;
        public string _appName;
        public string _connectionString;
        public string _dbType;
        public StructDef _sdef;
        public AxParser _parser;
        public Utils _common;
        public Dictionary<int, bool> deletedRowsDict = new Dictionary<int, bool>();
        public IARMLogTraceService TraceService
        {
            get { return _traceService; }
            set { _traceService = value; }
        }


        // Constructor
        public Validate(Utils common, IARMLogTraceService traceService)
        {
            // 1. Save dependencies

            //_configuration = configuration;
            _traceService = traceService;
            _utils = new AxStructDefUtils(common);


            // 4. Error and logging setup
            ErrorLog = new List<string>();
            ErrorStr = string.Empty;
            WarningMsg = string.Empty;
            IsError = true;
            DeleteRowOnError = false;
            // 5. Helper objects

            ParentList = new TStringList();
            forPickList = false;
            // Initialize variable
            validateResultList = new List<string>();
            errorList = new List<string>();
            validationPassed = true;
            fillSqlResultDict = new Dictionary<string, SQLResult>();

            // 7. Flags
            Validation = true;
            Loading = false;
            // 8. Misc
            LinkSearchCol = 0;
            actualRowNo = 0;
            LocThousandSeparator = ",";
            nonDynamicFilterSQL = false;
            FieldValue = "";
            onDataLoad = false;
        }

        public async Task<bool> InitValidateAsync(string pName = "")
        {
            bool result = false;
            try
            {
                string pCaption = "";

                XmlDocument xml = null!;

                if (_common._dbHelper == null)
                {
                    await _common.OpenDBConnectionAsync();
                }

                if (_common._dbHelper != null)
                {
                    if (_sdef == null)
                    {
                        _sdef = await StructDef.CreateAsync(pName, _common, _traceService);
                    }

                    if (_parser == null)
                    {
                        _parser = new AxParser(_common, _sdef);
                    }

                    _validateHelper = new ValidateHelper(
                        _dbType,
                        _configuration,
                        _parser,
                        _sdef,
                        _utils,
                        _common._dbHelper,
                        _traceService,
                        _common
                    );

                    //_traceService.LogMessage("StructDef initialized for {StructName}", pName);
                    _traceService.LogMessage("StructDef and Validate initialized for {StructName}", pName);

                }

                if (_sdef != null && _parser != null && _validateHelper != null)
                {
                    result = true;
                }

                return result;
            }
            catch (Exception ex)
            {
                result = false;
                _traceService.LogError($"Error in Validate initialization: {ex.Message}");
                //_traceService.LogError("Error in Validate initialization", ex);
                return result;
            }
        }



        // Method to add error to the global log
        private void LogError(string errorMessage, int rowNo, string fieldValue = "")
        {
            string logEntry =
                $"[Row {rowNo}] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {errorMessage} (FieldValue: {fieldValue})";
            ErrorLog.Add(logEntry);
            //_traceService.LogError(logEntry);
            _traceService.LogError(logEntry);

        }

        public double StrToFloatZ(string s)
        {
            if (!string.IsNullOrWhiteSpace(s))
                return double.Parse(_utils.RemoveCommas(s));
            return 0;
        }

        // Method to retrieve all logged errors
        public List<string> GetErrorLog()
        {
            return ErrorLog;
        }

        public async Task<string> RefreshFieldAsync(TField xfld, int rowNo)
        {
            string result = string.Empty;
            string v = string.Empty;
            string fldActualValue = string.Empty;
            sqlResult = null;
            idValue = 0;

            bool fieldEmpty = false;
            int k;
            DataRow row = null;
            DynamicSql = "";

            result = "";
            //_traceService.LogMessage(
            //    "Refresh Field - " + xfld.FieldName + " Row No " + rowNo.ToString()
            //);
            _traceService.LogMessage(
                "Refresh Field - " + xfld.FieldName + " Row No " + rowNo.ToString()
            );
            v = "";

            //k = _sdef.GetIndex(xfld.FieldName, xfld.Tablename, rowNo);
            k = _sdef.GetFieldIndex(xfld.FieldName, rowNo);

            if (k > -1)
            {
                if (v == "")
                    v = ((FieldRec)_sdef.FieldList[k]).Value;
                if (xfld.DataType == "n")
                {
                    if (!_validateHelper.IsNumeric(v, "-0123456789."))
                    {
                        ErrorStr = $"{v} is invalid number.";
                        LogError(ErrorStr, rowNo, v);
                        throw new Exception(ErrorStr);
                    }
                    else if (StrToFloatZ(v) == 0)
                    {
                        fieldEmpty = true;
                    }
                }
                else if (v == "")
                    fieldEmpty = true;
                else
                    fieldEmpty = false;
            }
            else
                fieldEmpty = true;

            result = v;
            fldActualValue = v;

            if (_sdef.Frames[xfld.FrameNo - 1].Popup)
            {
                _validateHelper.GetParentActiveRow(
                    rowNo,
                    _sdef.Frames[xfld.FrameNo - 1].PopIndex,
                    ErrorStr,
                    ParentList
                );
                _parser.RegisterVar("activerow", "n", rowNo.ToString());
            }
            else
            {
                _parser.RegisterVar("activerow", "n", rowNo.ToString());
            }

            if (xfld.FromList)
            {
                if (!string.IsNullOrEmpty(xfld.fromListValues))
                {
                    sqlResult = new SQLResult();

                    //if (sqlResult != null)
                    {
                        sqlResult.data = _utils.AddCsvValuesToDataTable(
                            sqlResult?.data,
                            xfld.fromListValues
                        );
                    }

                    if (sqlResult != null && sqlResult.data != null)
                    {
                        result = _sdef.GetFieldValue(xfld, rowNo);
                        if (!string.IsNullOrEmpty(xfld.Cexp) && fieldEmpty)
                        {
                            //_traceService.LogMessage("Evaluating - " + xfld.Cexp);
                            _traceService.LogMessage("Evaluating - " + xfld.Cexp);

                            if (!_parser.Evaluate(xfld.Cexp))
                                throw new InvalidOperationException($"Unable to evaluate expression for {xfld.FieldName}.");

                            result = _parser.Value;
                            result = result.Trim('"'); // Note: Parser returns quoted string this needs to be fixed in Parser
                        }

                        if (!xfld.Cl_multiselect) // This Condition (Cl_multiselect) is added to avoid multiselect field refresh
                        {

                            if (sqlResult.data != null && sqlResult.data.Rows.Count > 0)
                            {
                                row = sqlResult.data.Rows[0];
                            }

                            if (sqlResult.data.Rows.Count == 1 && xfld.Autoselect)
                            {
                                result = _utils.GetRowValue(row, sqlResult.data.Columns[0].ColumnName,
                                xfld.CustType);
                            }

                            int index = _validateHelper.GetIndexOfColumn(sqlResult, result, xfld);
                            if (index < 0)
                            {
                                //result = "";
                                result = fldActualValue;
                                idValue = 0;
                            }

                        }

                        if (xfld.DataType == "n")
                            result = _validateHelper.FormatNumber(xfld, result);
                        _sdef.SubmitValue(xfld.FieldName, rowNo, result, "", idValue, 0, 0);
                        _parser.RegisterVar(xfld.FieldName, xfld.DataType[0].ToString(), result);
                    }
                }
            }
            //else
            //{

            //}
            if (xfld.SQL != null && xfld.SQLText.Trim() != "" && (xfld.ModeofEntry == "select" || xfld.ModeofEntry == "accept"))
            {
                if (xfld.DynamicParams)
                    DynamicSql = xfld.SQLText;
                else
                    DynamicSql = "";
                if (xfld.TxtSelection)
                {
                    if (result == "" && !string.IsNullOrEmpty(xfld.Cexp))
                    {
                        _traceService.LogMessage("Evaluating - " + xfld.Cexp);

                        if (!_parser.Evaluate(xfld.Cexp))
                            throw new InvalidOperationException($"Unable to evaluate expression for {xfld.FieldName}.");

                        result = _parser.Value.ToString();
                    }
                    if (result != "")
                    {
                        sqlResult = await _validateHelper.ValidatePickList(
                                xfld,
                                result,
                                rowNo,
                                ErrorStr,
                                actualRowNo,
                                forPickList,
                                nonDynamicFilterSQL
                            );
                        bool bValidatePickList = sqlResult != null && sqlResult.data != null;
                        if (!bValidatePickList)
                        {
                            result = "";
                            idValue = 0;
                        }
                        _sdef.SubmitValue(xfld.FieldName, rowNo, result, "", idValue, 0, 0);
                        _parser.RegisterVar(xfld.FieldName, xfld.DataType[0].ToString(), result);
                    }
                    keyName = $"{xfld.FieldName} + {rowNo}";

                    if (!fillSqlResultDict.ContainsKey(keyName))
                    {
                        fillSqlResultDict.Add(keyName, sqlResult);

                    }
                }
                else if (xfld.ModeofEntry == "accept")
                {
                    if (xfld.DynamicParams)
                        DynamicSql = xfld.SQLText;
                    else
                        DynamicSql = "";
                    sqlResult = await _validateHelper.QueryOpen(
                        xfld,
                        rowNo,
                        xfld.FieldName,
                        v,
                        forPickList,
                        nonDynamicFilterSQL,
                        DynamicSql,
                        ""
                    );
                    // Adding the sql Result fill Sql Dictionary

                    if (xfld.FilterCols.Count > 0)
                        _validateHelper.FilterCDS(xfld, sqlResult, rowNo);

                    string acceptFldValue = "";

                    if (sqlResult.data != null && sqlResult.data.Rows.Count > 0)
                    {
                        acceptFldValue = _utils.GetRowValue(
                            sqlResult.data.Rows[0],
                            sqlResult.data.Columns[0].ColumnName,
                            xfld.CustType
                        );
                    }

                    //if (sqlResult != null) fillSqlResultDict.Add(xfld.FieldName, sqlResult);

                    
                    result = acceptFldValue;

                    // Editable accept field: its SQL gives a suggested default, not a list. When the SQL gives no value,
                    // keep the value already submitted (payload/loaded) instead of blanking it, as typed on the web form
                    if (xfld.Suggestive && string.IsNullOrEmpty(acceptFldValue) && !xfld.Readonly && !string.IsNullOrEmpty(fldActualValue))
                    {
                        _traceService.LogMessage($"Accept field {xfld.FieldName}: SQL returned no value; keeping submitted value");
                        result = fldActualValue;
                    }

                    //if (result != "" && result.Substring(0, 2) == "++")
                    if (
                        !string.IsNullOrEmpty(result)
                        && result.Length >= 2
                        && result.Substring(0, 2) == "++"
                    )
                        result = _validateHelper.TaxCompute(xfld, rowNo, sqlResult);

                    if (xfld.DataType == "n")
                        result = _validateHelper.FormatNumber(xfld, result);
                    _sdef.SubmitValue(xfld.FieldName, rowNo, result, "", 0, 0, 0);
                    _parser.RegisterVar(xfld.FieldName, xfld.DataType[0].ToString(), result);
                }
                else
                {
                    if (xfld.DynamicParams)
                        DynamicSql = xfld.SQLText;
                    else
                        DynamicSql = "";
                    sqlResult = await _validateHelper.QueryOpen(
                        xfld,
                        rowNo,
                        xfld.FieldName,
                        v,
                        forPickList,
                        nonDynamicFilterSQL,
                        DynamicSql,
                        ""
                    );


                    result = _sdef.GetFieldValue(xfld, rowNo);

                    //if (result == "" && xfld.Exprn > -1)
                    if (result == "" && !string.IsNullOrEmpty(xfld.Cexp))
                        {
                        _traceService.LogMessage("Evaluating - " + xfld.Cexp);
                        if (!_parser.Evaluate(xfld.Cexp))
                            throw new InvalidOperationException($"Unable to evaluate expression for {xfld.FieldName}.");

                        result = _parser.Value.ToString();

                    }
                    if (!xfld.Cl_multiselect) // This Condition (Cl_multiselect) is added to avoid multiselect field refresh
                    {
                        // if required we need to handle refresh for multiselect but note in delphi multiselect refresh is not handled
                        if (sqlResult.data != null && sqlResult.data.Rows.Count > 0)
                        {
                            row = sqlResult.data.Rows[0];
                        }

                        if (xfld.FilterCols.Count > 0)
                            _validateHelper.FilterCDS(xfld, sqlResult, rowNo);
                        if (sqlResult.data.Rows.Count == 1 && xfld.Autoselect)
                        {
                            result = _utils.GetRowValue(
                                row,
                                sqlResult.data.Columns[xfld.Searchcol].ColumnName,
                                //sqlResult.data.Columns[_validateHelper.GetSearchCol(sqlResult.data, xfld.Searchcol)].ColumnName,
                                xfld.CustType
                            );
                        }

                        if (xfld.SourceKey && row != null)
                            idValue = Convert.ToInt64(row[0].ToString());

                        int index = _validateHelper.GetIndexOfColumn(sqlResult, result, xfld);
                        if (index < 0)
                        {
                            //result = "";
                            result = fldActualValue;
                            idValue = 0;
                        }
                        else // if (index >= 0)
                        {
                            if (xfld.SourceKey)
                            {
                                //if (index < sqlResult.data.Rows.Count)
                                {
                                    row = sqlResult.data.Rows[index];
                                }
                                idValue = Convert.ToInt64(row[0].ToString());
                            }
                        }
                    }

                    //else if (xfld.SourceKey) idValue = Convert.ToInt64(row[0].ToString());
                    if (xfld.DataType == "n")
                        result = _validateHelper.FormatNumber(xfld, result);
                    _sdef.SubmitValue(xfld.FieldName, rowNo, result, "", idValue, 0, 0);
                    _parser.RegisterVar(xfld.FieldName, xfld.DataType[0].ToString(), result);

                    keyName = $"{xfld.FieldName} + {rowNo}";

                    if (!fillSqlResultDict.ContainsKey(keyName))
                    {
                        fillSqlResultDict.Add(keyName, sqlResult);
                    }
                }
            }
            else if (xfld.ModeofEntry == "fill")
            {
                //if (xfld.Suggestive)
                //{
                //    _traceService.LogMessage("Skipping fill field in EnterField : " + xfld.FieldName + " - " + FieldValue);
                //}
                //else
                //{

                //}
                _traceService.LogMessage("Finding linkfield");

                //int j = _sdef.GetIndex(xfld.LinkField, xfld.Tablename, rowNo);
                int j = _sdef.GetFieldIndex(xfld.LinkField);
                if (j == -1)
                {
                    ErrorStr =
                        "Unable to find LinkField for "
                        + _validateHelper.GetFldPrompt(xfld, rowNo, actualRowNo);
                    return result;
                }
                _traceService.LogMessage("Linkfield found and assigning Query from link field");

                //if (sqlResult.data == null)
                //{
                //    var linkFld = _sdef.GetField(xfld.LinkField);

                //    if (linkFld.DynamicParams) DynamicSql = linkFld.SQLText; else DynamicSql = "";
                //    sqlResult = await _validateHelper.QueryOpen(linkFld, rowNo, "0", _sdef.GetFieldValue(linkFld, rowNo), forPickList, nonDynamicFilterSQL, DynamicSql);

                //}



                keyName = $"{xfld.LinkField} + {rowNo}";

                if (fillSqlResultDict.TryGetValue(keyName, out sqlResult!)) { }
                else
                {
                    if (sqlResult != null && sqlResult.data != null)
                        sqlResult.data = null;
                    _traceService.LogMessage(
                        $"Key {xfld.LinkField}  not found in fillSqlResultDict during RefreshFieldAsync."
                    );
                }

                if (sqlResult?.data != null && sqlResult.data.Rows.Count > 0)
                {
                    row = sqlResult.data.Rows[0];
                }

                var linkFld = _sdef.GetField(xfld.LinkField);

                if (linkFld.DynamicParams)
                {
                    DynamicSql = string.Join("", linkFld.SQL);
                }
                else
                    DynamicSql = "";




                if (_sdef.Flds[j].TxtSelection && row == null)
                {
                    v = _validateHelper.GetValue(xfld.LinkField, rowNo);
                    LinkSearchCol = ((TField)_sdef.Flds[j]).Searchcol;
                    //sqlResult = await _validateHelper.QueryOpen(
                    //    linkFld,
                    //    rowNo,
                    //    linkFld.FieldName,
                    //    v,
                    //    forPickList,
                    //    nonDynamicFilterSQL,
                    //    DynamicSql,
                    //    ""
                    //);
                    //if (sqlResult?.data != null && sqlResult.data.Rows.Count > 0)
                    //{
                    //    row = sqlResult.data.Rows[0];
                    //}

                    //if (
                    //    _validateHelper.IsDataTableActive(sqlResult.data)
                    //    || (
                    //        _validateHelper.IsDataTableActive(sqlResult.data)
                    //        && row[LinkSearchCol].ToString().ToLower() != v.ToLower()
                    //    )
                    //)
                    //{
                    sqlResult = await _validateHelper.ValidatePickList(
                            _sdef.Flds[j],
                            v,
                            rowNo,
                            ErrorStr,
                            actualRowNo,
                            forPickList,
                            nonDynamicFilterSQL
                        );
                    bool bValidatePickList = sqlResult != null && sqlResult.data != null;
                    if (bValidatePickList)
                    {
                        if (sqlResult.data.Rows.Count > 0)
                        {
                            row = sqlResult.data.Rows[0];
                            result = _utils.GetRowValue(row, xfld.SourceField,
                                xfld.CustType);
                        }
                        else
                            result = "";


                    }
                    else
                        result = "";
                    ////}
                    //else
                    //    result = _utils.GetRowValue(row, xfld.SourceField);
                }
                else
                {
                    _traceService.LogMessage("Query assigned");
                    //if (sqlResult.data != null)
                    //{
                    LinkSearchCol = _sdef.Flds[j].Searchcol;
                    if (_sdef.Flds[j].DynamicParams)
                    {
                        DynamicSql = string.Join("", _sdef.Flds[j].SQL);
                    }
                    else
                        DynamicSql = "";
                    //var linkFld = _sdef.GetField(xfld.LinkField);
                    if (sqlResult == null || sqlResult.data == null)
                    {
                        // Query executed successfully and returned at least one row (DataTable is "active")
                        //sqlResult = await _validateHelper.QueryOpen(xfld, rowNo, "0", _sdef.GetFieldValue(xfld.LinkField, rowNo), forPickList, nonDynamicFilterSQL, DynamicSql);
                        sqlResult = await _validateHelper.QueryOpen(
                            linkFld,
                            rowNo,
                            linkFld.FieldName,
                            _sdef.GetFieldValue(linkFld, rowNo),
                            forPickList,
                            nonDynamicFilterSQL,
                            DynamicSql,
                            ""
                        );
                    }

                    if (sqlResult.data != null)
                    {
                        if (sqlResult.data.Rows.Count > 0)
                        {
                            row = sqlResult.data.Rows[0];
                        }

                        if (_sdef.Flds[j].FilterCols.Count > 0)
                            _validateHelper.FilterCDS(_sdef.Flds[j], sqlResult, rowNo);

                        Int64 linkFldIdValue = 0;

                        if (linkFld.SourceKey && row != null)
                            linkFldIdValue = Convert.ToInt64(row[0].ToString());

                        int linkFldIndex = _validateHelper.GetIndexOfColumn(
                            sqlResult,
                            _sdef.GetFieldValue(linkFld, rowNo),
                            linkFld
                        );
                        if (linkFldIndex < 0)
                        {
                            //result = "";
                            linkFldIdValue = 0;
                        }
                        else
                        //if (linkFldIndex >= 0)
                        {
                            // TODO: Need to implement setIdValue for assigning idValue for the field
                            // NOTE: AxGet not registered Idvalue
                            //if xfld.SourceKey then idValue := xfld.QSelect.CDS.Fields[0].AsFloat;
                            if (linkFld.SourceKey)
                            {
                                //if (linkFldIndex < sqlResult.data.Rows.Count)
                                {
                                    row = sqlResult.data.Rows[linkFldIndex];
                                }
                                linkFldIdValue = Convert.ToInt64(row[0].ToString());
                            }
                            _sdef.SubmitValue(
                                linkFld.FieldName,
                                rowNo,
                                _sdef.GetFieldValue(linkFld, rowNo),
                                linkFldIdValue
                            );
                        }

                        string linkFieldResult = _validateHelper.GetValue(xfld.LinkField, rowNo);
                        if (linkFieldResult != "")
                        {
                            //if (result.ToLower() != _utils.GetRowValue(row, sqlResult.data.Columns[LinkSearchCol].ColumnName).ToString().ToLower())
                            //{

                            //    int index = _validateHelper.GetIndexOfColumn(sqlResult, result, xfld);

                            //    if (index >= 0) row = sqlResult.data.Rows[index];
                            //    else row = sqlResult.data.Rows[0];

                            //}

                            if (
                                row != null
                                && LinkSearchCol >= 0
                                && LinkSearchCol < sqlResult.data.Columns.Count
                            )
                            {
                                var value = _utils
                                    .GetRowValue(
                                        row,
                                        xfld.SourceField,
                                        xfld.CustType
                                    )
                                    .ToString();
                                if (
                                    !string.Equals(
                                        linkFieldResult,
                                        value,
                                        StringComparison.OrdinalIgnoreCase
                                    )
                                )
                                {
                                    int index = _validateHelper.GetIndexOfColumn(
                                        sqlResult,
                                        linkFieldResult,
                                        xfld, LinkSearchCol
                                    );

                                    if (index >= 0 && index < sqlResult.data.Rows.Count)
                                        row = sqlResult.data.Rows[index];
                                    else if (sqlResult.data.Rows.Count > 0)
                                        row = sqlResult.data.Rows[0];
                                    else
                                    {
                                        _traceService.LogMessage(
                                            $"No rows available in sqlResult.data for field {xfld.FieldName}."
                                        );
                                        row = null;
                                    }
                                }
                            }

                            //if (_utils.GetRowValue(row, sqlResult.data.Columns[LinkSearchCol].ColumnName).ToString().ToLower() != result.ToLower())
                            //    result = "";
                            //else

                            //    result = _utils.GetRowValue(row, xfld.SourceField).ToString();
                            if (
                                row != null
                                && LinkSearchCol >= 0
                                && LinkSearchCol < sqlResult.data.Columns.Count
                            )
                            {
                                var colName = sqlResult.data.Columns[LinkSearchCol].ColumnName;
                                var cellValue = _utils.GetRowValue(row, colName,
                                linkFld.CustType)?.ToString() ?? "";
                                //Note: This condition may not be required as we already found row in previous condition
                                if (
                                    !string.Equals(
                                        cellValue,
                                        linkFieldResult,
                                        StringComparison.OrdinalIgnoreCase
                                    )
                                )
                                {
                                    //result = "";
                                    result = fldActualValue;

                                }
                                else
                                {
                                    var sourceColName = xfld.SourceField;
                                    if (sqlResult.data.Columns.Contains(sourceColName))
                                        result =
                                            _utils.GetRowValue(row, sourceColName,
                                xfld.CustType)?.ToString()
                                            ?? "";
                                    else
                                    {
                                        _traceService.LogMessage(
                                            $"Column '{sourceColName}' not found in DataTable."
                                        );
                                        //result = "";
                                        result = fldActualValue;

                                    }
                                }
                            }
                        }
                    }
                    if (xfld.DataType == "n")
                        result = _validateHelper.FormatNumber(xfld, result);
                }
                _sdef.SubmitValue(xfld.FieldName, rowNo, result, "", 0, 0, 0);
                _parser.RegisterVar(xfld.FieldName, xfld.DataType[0].ToString(), result);
                keyName = $"{xfld.LinkField} + {rowNo}";
                if (!fillSqlResultDict.ContainsKey(keyName) && sqlResult != null)
                {
                    fillSqlResultDict.Add(keyName, sqlResult);
                }
            }
            else if (xfld.ModeofEntry == "autogenerate")
            {
                if (!onDataLoad)
                {
                    result = await _sdef.GetLastNo(xfld.FieldName, false);
                    //await _sdef.UpdateLastNo(xfld.FieldName);  // Note: Added for Testing purpose
                }
                else
                {
                    if (xfld.OldFieldValue != xfld.FieldValue)
                    {
                        ErrorStr = "Autogenerate fields can't be updated/cleared.";
                        _traceService.LogMessage(
                                        $"Validation failed for field {xfld.FieldName}: {ErrorStr}"
                                    );
                        result = ErrorStr;
                        validationPassed = false;
                        validateResultList.Add($"Validation Fail: Field '{xfld.FieldName}' (Row No: {rowNo}) with value '{xfld.FieldValue}'; {ErrorStr}");
                    }

                }

                _sdef.SubmitValue(xfld.FieldName, rowNo, result, "", 0, 0, 0);
                _parser.RegisterVar(xfld.FieldName, xfld.DataType[0].ToString(), result);
            }
            else if (!string.IsNullOrEmpty(xfld.Cexp))
            {
                if (xfld.ModeofEntry == "calculate" || fieldEmpty)
                {
                    _traceService.LogMessage("Evaluating - " + xfld.Cexp);

                    if (!_parser.Evaluate(xfld.Cexp))
                        throw new InvalidOperationException($"Unable to evaluate expression for {xfld.FieldName}.");

                    result = _parser.Value.ToString();
                    result = result.Trim('"'); // Note: This Needs to be handled in parser
                    _validateHelper.SetUsedQuotedStrProp(xfld);
                    if (xfld.DataType != "n")
                    {
                        _sdef.SubmitValue(xfld.FieldName, rowNo, result, "", 0, 0, 0);
                        _parser.RegisterVar(xfld.FieldName, xfld.DataType[0].ToString(), result);
                    }
                    else
                    {
                        if (result != "") // and condition commented out after discussion with sab
                        {
                            result = _validateHelper.FormatNumber(xfld, result);
                            _sdef.SubmitValue(xfld.FieldName, rowNo, result, "", 0, 0, 0);
                            _parser.RegisterVar(
                                xfld.FieldName,
                                xfld.DataType[0].ToString(),
                                result
                            );
                        }
                    }
                    if (xfld.ModeofEntry == "accept")
                    {
                        k = _sdef.GetFieldIndex(xfld.FieldName, rowNo);
                        //k = _sdef.GetIndex(xfld.FieldName, xfld.Tablename, rowNo);

                        if (k > -1)
                            ((FieldRec)_sdef.FieldList[k]).AutoValue = true;
                    }
                }
            }
            else
            {
                if (xfld.DataType == "n")
                {
                    result = _validateHelper.FormatNumber(xfld, result);
                    _sdef.SubmitValue(xfld.FieldName, rowNo, result, "", 0, 0, 0);
                }
                _parser.RegisterVar(xfld.FieldName, xfld.DataType[0].ToString(), result);
            }
            _traceService.LogMessage("Refresh Field Value - " + result);
            //if (!xfld.SaveValue || xfld.Hidden || string.IsNullOrEmpty(result)) // Non saved fields or Hidden fields
            //{
            //    result = fldActualValue;

            //}

            /*
                Note added on 09/12/2025
 
                A condition was previously introduced during the development of Axis (AxGet / AxPut).
                The RefreshFieldAsync triggered for all fields during new data and modified data operations.
                During load-data, it triggered refresh only for calculate and non-save fields.
 
                This control was added earlier to handle some issues, but it is no longer required.
                Hence, it is being commented out now (09/12/2025).

            */
            //if (xfld.SaveValue) //  saved fields   // TODO: This need to be verified
            //{
            //    result = fldActualValue;
            //}
            return result;
        }

        public bool CheckMatch(string matchString, string value)
        {
            MatchErrorStr = "";
            bool result = true;
            matchString = matchString.ToLower();

            if (matchString == "isemail")
            {
                if (!_utils.CheckAxpEmail(value))
                {
                    MatchErrorStr = "Invalid email address";
                    result = false;
                }
            }
            else if (matchString == "isalpha")
            {
                if (!_utils.CheckAlpha(value))
                {
                    MatchErrorStr = "Only characters are allowed";
                    result = false;
                }
            }
            else if (matchString == "isnumeric")
            {
                if (!_utils.CheckIsNumeric(value))
                {
                    MatchErrorStr = "Only numbers are allowed";
                    result = false;
                }
            }
            else if (matchString == "isalphanumeric")
            {
                if (!_utils.CheckAlphaNumeric(value))
                {
                    MatchErrorStr = "Only alphanumeric characters are allowed";
                    result = false;
                }
            }
            else if (matchString == "isphone")
            {
                if (!_utils.CheckIsPhone(value))
                {
                    MatchErrorStr = "Please enter a valid PhoneNumber";
                    result = false;
                }
            }
            else if (matchString == "initcaps")
            {
                if (!_utils.CheckIsInit(value))
                {
                    MatchErrorStr = "First character of each word should be in Caps";
                    result = false;
                }
            }
            else if (matchString == "caps")
            {
                if (!_utils.CheckIsCaps(value))
                {
                    MatchErrorStr = "Only Uppercase characters are allowed";
                    result = false;
                }
            }
            else if (matchString == "small")
            {
                if (!_utils.CheckIsSmall(value))
                {
                    MatchErrorStr = "Only Lowercase characters are allowed";
                    result = false;
                }
            }
            else if (matchString == "24h format" || matchString == "12h format")
            {
                if (!_utils.CheckIsValidTime(value, matchString))
                {
                    MatchErrorStr = "Invalid time format";
                    result = false;
                }
            }
            else
            {
                try
                {
                    // Use Regex as equivalent to Delphi's TMask
                    Regex regex = new Regex(matchString, RegexOptions.None);
                    if (!regex.IsMatch(value))
                        result = false;
                }
                catch (ArgumentException)
                {
                    // Handle invalid regex pattern
                    MatchErrorStr = "Invalid match pattern";
                    result = false;
                }
            }

            return result;
        }

        public async Task<bool> RapidValidateData(TField fld, int rowNo, bool fieldEmpty)
        {
            bool result = true;
            bool deleteRowOnError = false;
            string warningMsg = "";
            string fieldValue = FieldValue;

            try
            {
                if (!Validation || Loading || fld.FieldName.ToLower().StartsWith("axpfile_"))
                    return result;



                //if (fld.Cl_multiselect) // This Condition (Cl_multiselect) is added to avoid multiselect field validations
                //return result;

                if (fld.DataType[0] == 'n')
                {
                    if (!_validateHelper.IsNumeric(fieldValue, "-0123456789."))
                    {
                        ErrorStr = $"{fieldValue} is invalid number.";
                        LogError(ErrorStr, rowNo, fieldValue);
                        fieldValue = "0";
                        _sdef.SubmitValue(fld.FieldName, rowNo, fieldValue, "", idValue, 0, 0);
                        return false;
                    }
                }

                if (
                    !string.IsNullOrEmpty(fld.Pattern)
                    && !string.IsNullOrEmpty(fieldValue)
                    && !CheckMatch(fld.Pattern, fieldValue)
                )
                {
                    ErrorStr = string.IsNullOrEmpty(MatchErrorStr)
                        ? $"The value in {_validateHelper.GetFldPrompt(fld, rowNo, actualRowNo)} should be in this format {fld.Pattern}"
                        : $"{MatchErrorStr} in {_validateHelper.GetFldPrompt(fld, rowNo, actualRowNo)}";
                    LogError(ErrorStr, rowNo, fieldValue);
                    return false;
                }

                if (!fld.Cvalexp.IsNullOrEmpty())
                {
                    _traceService.LogMessage($"Evaluating - {fld.Cvalexp}");
                    if (!_parser.Evaluate(fld.Cvalexp))
                    {
                        ErrorStr =
                            $"Unable to evaluate validation expression for {_validateHelper.GetFldPrompt(fld, rowNo, actualRowNo)}.";
                        LogError(ErrorStr, rowNo);
                        return false;
                    }

                    string m = _parser.Value.ToString();
                    if (m.ToLower() != "t" && !string.IsNullOrEmpty(m))
                    {
                        if (m[0] != '_')
                            result = false;
                        else
                        {
                            m = m.Substring(1);
                            warningMsg =
                                $"{_validateHelper.GetFldPrompt(fld, rowNo, actualRowNo)} {m}";
                            LogError($"Warning: {warningMsg}", rowNo, fieldValue);
                        }

                        if (m.ToLower() == "f")
                        {
                            ErrorStr =
                                $"Invalid input in {_validateHelper.GetFldPrompt(fld, rowNo, actualRowNo)}";
                            LogError(ErrorStr, rowNo, fieldValue);
                        }
                        else if (!result)
                        {
                            if (m.StartsWith("--"))
                            {
                                ErrorStr =
                                    $"{_validateHelper.GetFldPrompt(fld, rowNo, actualRowNo)}{m.Substring(2)}";
                                deleteRowOnError = true;
                            }
                            else
                                ErrorStr =
                                    $"{_validateHelper.GetFldPrompt(fld, rowNo, actualRowNo)}{m}";
                            LogError(ErrorStr, rowNo, fieldValue);
                            return result;
                        }
                    }
                }

                if (fld.NoDuplicate)
                {
                    if (fld.AsGrid)
                    {
                        result =
                            string.IsNullOrEmpty(fieldValue)
                            || _validateHelper.RapidDuplicateInGrid(rowNo, fld, fieldValue);
                        if (!result)
                        {
                            string m = _parser.GetVarValue($"OnDuplicate_{fld.FieldName}");
                            ErrorStr = !string.IsNullOrEmpty(m)
                                ? _validateHelper.GetErrorStr(
                                    m,
                                    rowNo,
                                    actualRowNo,
                                    fld,
                                    deleteRowOnError
                                )
                                : $"Duplicate values are not allowed in {_validateHelper.GetFldPrompt(fld, rowNo, actualRowNo)}";
                            LogError(ErrorStr, rowNo, fieldValue);
                            return result;
                        }
                    }
                    else
                    {
                        result = !await _validateHelper.RapidCheckDuplicate(
                            fld,
                            rowNo,
                            idValue,
                            fieldValue
                        );
                        if (!result)
                        {
                            ErrorStr =
                                $"Duplicate values are not allowed in {_validateHelper.GetFldPrompt(fld, rowNo, actualRowNo)}";
                            LogError(ErrorStr, rowNo, fieldValue);
                            return result;
                        }
                    }
                }

                if (!fld.Empty)
                {
                    string dt = fld.DataType.ToLower();
                    if (
                        (dt == "n" && StrToFloatZ(fieldValue) == 0)
                        || _utils.IsEmptyValue(fieldValue, dt) == "T"
                    )
                    {
                        result = false;
                        if (fld.AsGrid)
                        {
                            string m = _parser.GetVarValue($"OnEmpty_{fld.FieldName}");
                            if (!string.IsNullOrEmpty(m))
                            {
                                ErrorStr = _validateHelper.GetErrorStr(
                                    m,
                                    rowNo,
                                    actualRowNo,
                                    fld,
                                    deleteRowOnError
                                );
                                LogError(ErrorStr, rowNo, fieldValue);
                                return result;
                            }
                        }
                        ErrorStr =
                            $"{_validateHelper.GetFldPrompt(fld, rowNo, actualRowNo)} cannot be left empty.";
                        LogError(ErrorStr, rowNo, fieldValue);
                        return result;
                    }
                }

                if (fld.OnlyPositive)
                {
                    if (fld.DataType == "n" && StrToFloatZ(fieldValue) < 0)
                    {
                        result = false;
                        string m = _parser.GetVarValue($"OnNegative_{fld.FieldName}");
                        ErrorStr = !string.IsNullOrEmpty(m)
                            ? _validateHelper.GetErrorStr(
                                m,
                                rowNo,
                                actualRowNo,
                                fld,
                                deleteRowOnError
                            )
                            : $"{_validateHelper.GetFldPrompt(fld, rowNo, actualRowNo)} cannot be negative";
                        LogError(ErrorStr, rowNo, fieldValue);
                        return result;
                    }
                }

                if (
                    !string.IsNullOrEmpty(fld.Tablename)
                    && !_validateHelper.CheckWidth(fld, fieldValue)
                )
                {
                    result = false;
                    ErrorStr =
                        $"{_validateHelper.GetFldPrompt(fld, rowNo, actualRowNo)} : value exceeds defined width. Actual Data : {fieldValue}";
                    LogError(ErrorStr, rowNo, fieldValue);
                    fieldValue = fieldValue.Substring(0, Math.Min(fieldValue.Length, fld.Width));
                    _sdef.SubmitValue(fld.FieldName, rowNo, fieldValue, "", idValue, 0, 0);
                    return result;
                }

                if (fld.TxtSelection)
                {
                    if (sqlResult != null
                        && sqlResult.data != null
                        && sqlResult.data.Rows.Count > 0
                        && !string.IsNullOrEmpty(fieldValue)
                    )
                    {
                        var res = await _validateHelper.RapidValidatePickList(
                                fld,
                                fieldValue,
                                rowNo,
                                fieldEmpty,
                                sqlResult,
                                DynamicSql,
                                ErrorStr,
                                FieldValue,
                                idValue,
                                actualRowNo,
                                forPickList
                            );

                        if (!res)
                        {
                            LogError(ErrorStr, rowNo, fieldValue);
                            return false;
                        }
                    }
                }
                else if (fld.ModeofEntry == "select")
                {
                    if (
                        sqlResult != null
                        && _validateHelper.IsDataTableActive(sqlResult.data)
                        && !string.IsNullOrEmpty(fieldValue)
                    )
                    {
                        if (fld.CommaSelection || fld.Cl_multiselect)
                        {
                            int j = 1;
                            string fValue = _utils.GetNthString(fieldValue, j, fld.Separator);
                            bool bInvalidData = false;
                            List<string> invalidData = [];

                            while (!string.IsNullOrEmpty(fValue))
                            {
                                int index = _validateHelper.GetIndexOfColumn(
                                    sqlResult,
                                    fValue,
                                    fld
                                );
                                DataRow row;
                                if (index >= 0)
                                    row = sqlResult.data.Rows[index];
                                else
                                    row = sqlResult.data.Rows.Count > 0 ? sqlResult.data.Rows[0] : null;

                                // No rows: the value is not in the list (reported as invalid below)
                                string fldValue = row == null ? "" : _utils.GetRowValue(
                                    row,
                                    sqlResult.data.Columns[fld.Searchcol].ColumnName,
                                //sqlResult.data.Columns[_validateHelper.GetSearchCol(sqlResult.data, fld.Searchcol)].ColumnName,

                                fld.CustType
                                );
                                if (fldValue.ToLower() != fValue.ToLower())
                                {
                                    invalidData.Add(fValue);
                                }
                                j++;
                                fValue = _utils.GetNthString(fieldValue, j, fld.Separator);
                            }
                            bInvalidData = invalidData.Count > 0;

                            if (bInvalidData)
                            {
                                result = false;
                                string invData = string.Join(",", invalidData);
                                ErrorStr =
                                    $"\"{invData}\" is not a valid {_validateHelper.GetFldPrompt(fld, rowNo, actualRowNo)}";
                                LogError(ErrorStr, rowNo, invData);
                                fieldValue = "Invalid item";

                                _validateHelper.SubmitValue(
                                    fld,
                                    rowNo,
                                    fieldValue,
                                    "",
                                    idValue,
                                    0,
                                    0
                                );
                                return result;
                            }
                        }
                        else
                        {
                            int index = _validateHelper.GetIndexOfColumn(
                                sqlResult,
                                fieldValue,
                                fld
                            );
                            DataRow row;
                            if (index >= 0)
                                row = sqlResult.data.Rows[index];
                            else
                                row = sqlResult.data.Rows.Count > 0 ? sqlResult.data.Rows[0] : null;

                            // No rows: the value is not in the list (reported as invalid below)
                            string fldValue = row == null ? "" : _utils.GetRowValue(
                                row,
                                sqlResult.data.Columns[fld.Searchcol].ColumnName,
                                //sqlResult.data.Columns[_validateHelper.GetSearchCol(sqlResult.data, fld.Searchcol)].ColumnName,

                                fld.CustType
                            );

                            if (fldValue.ToLower() != fieldValue.ToLower())
                            {
                                result = false;
                                ErrorStr =
                                    $"\"{fieldValue}\" is not a valid {_validateHelper.GetFldPrompt(fld, rowNo, actualRowNo)}";
                                LogError(ErrorStr, rowNo, fieldValue);

                                fieldValue = "Invalid item";
                                _validateHelper.SubmitValue(
                                    fld,
                                    rowNo,
                                    fieldValue,
                                    "",
                                    idValue,
                                    0,
                                    0
                                );
                                return result;
                            }
                        }
                    }
                    else if (fld.FromList) // FromList may not be needed, as we handled everything in the if block itself.
                    {
                        if (
                            sqlResult != null
                            && _validateHelper.IsDataTableActive(sqlResult.data)
                            && !string.IsNullOrEmpty(fieldValue)
                        )
                        {
                            if (fld.CommaSelection)
                            {
                                int j = 1;
                                string fValue = _utils.GetNthString(fieldValue, j, fld.Separator);

                                while (!string.IsNullOrEmpty(fValue))
                                {
                                    int index = _validateHelper.GetIndexOfColumn(
                                        sqlResult,
                                        fValue,
                                        fld
                                    );
                                    DataRow row;
                                    if (index >= 0)
                                        row = sqlResult.data.Rows[index];
                                    else
                                        row = sqlResult.data.Rows[0];

                                    string fldValue = _utils.GetRowValue(
                                        row,
                                        sqlResult.data.Columns[0].ColumnName,
                                        fld.CustType
                                    );
                                    if (fldValue.ToLower() != fValue.ToLower())
                                    {
                                        result = false;
                                        ErrorStr =
                                            $"\"{fValue}\" is not a valid {_validateHelper.GetFldPrompt(fld, rowNo, actualRowNo)}";
                                        LogError(ErrorStr, rowNo, fValue);
                                        fieldValue = "Invalid item";

                                        _validateHelper.SubmitValue(
                                            fld,
                                            rowNo,
                                            fieldValue,
                                            "",
                                            idValue,
                                            0,
                                            0
                                        );
                                        return result;
                                    }
                                    j++;
                                    fValue = _utils.GetNthString(fieldValue, j, fld.Separator);
                                }
                            }
                            else
                            {
                                int index = _validateHelper.GetIndexOfColumn(
                                    sqlResult,
                                    fieldValue,
                                    fld
                                );
                                DataRow row;
                                if (index >= 0)
                                    row = sqlResult.data.Rows[index];
                                else
                                    row = sqlResult.data.Rows[0];
                                string fldValue = _utils.GetRowValue(
                                    row,
                                    sqlResult.data.Columns[0].ColumnName,
                                    fld.CustType
                                );
                                if (fldValue.ToLower() != fieldValue.ToLower())
                                {
                                    result = false;
                                    ErrorStr =
                                        $"\"{fieldValue}\" is not a valid {_validateHelper.GetFldPrompt(fld, rowNo, actualRowNo)}";
                                    LogError(ErrorStr, rowNo, fieldValue);
                                    fieldValue = "Invalid item";
                                    _validateHelper.SubmitValue(
                                        fld,
                                        rowNo,
                                        fieldValue,
                                        "",
                                        idValue,
                                        0,
                                        0
                                    );
                                    return result;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log any unexpected exceptions
                ErrorStr = $"Unexpected error in RapidValidateData: {ex.Message}";
                LogError(
                    $"Exception: {ex.Message} | StackTrace: {ex.StackTrace}",
                    rowNo,
                    fieldValue
                );
                result = false;
            }

            return result;
        }

        public async Task<string> SubmitValueOnLoad(TStructRequestDto dataJson)
        {
            try
            {
                // Add null checks for critical dependencies
                if (_sdef == null)
                    throw new InvalidOperationException("StructDef not initialized");
                if (_parser == null)
                    throw new InvalidOperationException("Parser not initialized");

                // Parse recordid - now it might be from keyvalue or recordid field
                //Int64 recordId = 0;
                //if (!string.IsNullOrEmpty(dataJson.Keyvalue) && Int64.TryParse(dataJson.Keyvalue, out var keyVal))
                //{
                //    recordId = keyVal;
                //}
                //else if (!string.IsNullOrEmpty(dataJson.Recordid) && Int64.TryParse(dataJson.Recordid, out var recId))
                //{
                //    recordId = recId;
                //}

                int processedFields = 0;

                // Check for submitdata instead of data.data (NEW PAYLOAD STRUCTURE)
                if (dataJson.Submitdata == null)
                {
                    _traceService.LogMessage("Invalid data structure: missing submitdata");
                    return "Invalid data structure: missing submitdata";
                }

                _traceService.LogMessage(
                    $"Starting to process {dataJson.Submitdata.Count} data containers"
                );

                onDataLoad = true;

                // OUTER LOOP: Iterate through Data Containers (dc1, dc2, dc3, etc.)
                // Each DC represents a different data frame/table in the structure
                foreach (var dc in dataJson.Submitdata)
                {
                    string dcName = dc.Key; // Gets "dc1", "dc2", etc.

                    // Skip if the key doesn't start with "dc" (data container)
                    if (!dcName.StartsWith("dc", StringComparison.OrdinalIgnoreCase))
                        continue;

                    // Extract frame number from dcName (dc1 -> 1, dc2 -> 2, etc.)
                    if (!int.TryParse(dcName.Substring(2), out int frameNo))
                        continue;

                    int dcNo = frameNo;

                    TFrame frame = _sdef.Frames[dcNo - 1];

                    _traceService.LogMessage(
                        $"Processing Data Container: {dcName} (Frame {frameNo})"
                    );

                    // MIDDLE LOOP: Iterate through Rows within each Data Container (row1, row2, row3, etc.)
                    // Each row represents a record/entry within that data container
                    foreach (var row in dc.Value)
                    {
                        string rowName = row.Key; // Gets "row1", "row2", "row3", etc.
                        Int64 recordId = 0;
                        string rowAction = "";

                        // Skip if the key doesn't start with "row"
                        if (!rowName.StartsWith("row", StringComparison.OrdinalIgnoreCase))
                            continue;

                        // Extract row number from rowName (row1 -> 1, row2 -> 2, etc.)
                        if (!int.TryParse(rowName.Substring(3), out int rowNo))
                            continue;

                        _traceService.LogMessage(
                            $"Processing Row: {rowName} (Row Number {rowNo}) in {dcName}"
                        );

                        // INNER LOOP: Process all fields within this specific row
                        // Each field represents a column/attribute of the current row
                        foreach (var field in row.Value)
                        {
                            string fieldName = field.Key; // Field name like "cname", "jdate", "axrow_action", etc.
                            string fieldValue = field.Value ?? ""; // Field value
                            //bool gridRowActionFromSdef = false;
                            //bool nonGridRowActinFromSdef = false;

                            string rowKeyName = fieldName.ToLowerInvariant();

                            switch (rowKeyName)
                            {
                                case "axp_recid":
                                    if (Int64.TryParse(fieldValue, out var recId))
                                    {
                                        recordId = recId;
                                        _sdef.SubmitRecordId(dcNo, rowNo, recordId);
                                    }
                                    else
                                    {
                                        _traceService.LogMessage(
                                            $"Invalid axp_recid value '{fieldValue}' in {dcName}->{rowName}"
                                        );
                                    }
                                    continue;

                                case "axrow_action":
                                    rowAction = fieldValue;

                                    if (rowAction.ToLower() == "delete")
                                        deletedRowsDict[frameNo] = true;

                                    if (frame != null)
                                    {
                                        if (frame.AsGrid)
                                        {
                                            _sdef.SetGridRowAction(dcNo, rowNo, rowAction);
                                        }
                                        else
                                        {
                                            _sdef.SetNonGridRowAction(frame.TableName, rowAction);
                                        }
                                    }
                                    continue;


                                default:
                                    break;
                            }

                            _traceService.LogMessage(
                                $"Processing Field: '{fieldName}' with Value: '{fieldValue}' in {dcName}->{rowName}"
                            );

                            try
                            {
                                // Get field definition from struct
                                TField fp = _sdef.GetField(fieldName);
                                if (fp == null)
                                {
                                    _traceService.LogMessage(
                                        $"Field {fieldName} not found in struct definition"
                                    );
                                    continue;
                                }


                                string varName = fieldName;
                                string varType = fp.DataType;

                                string fldIdVal = "";
                                if (fp.SourceKey && row.Value.ContainsKey(fieldName + "_sourceid") && !string.IsNullOrEmpty(row.Value[fieldName + "_sourceid"]))
                                {
                                    fldIdVal = row.Value[fieldName + "_sourceid"].ToString();
                                }

                                Int64 idVal = 0;
                                Int64.TryParse(fldIdVal, out idVal);

                                var rowActionValue = _sdef.GetRowAction(dcNo, rowNo);

                                if (rowActionValue.ToLower() == "delete")
                                {
                                    if (varType == "n")
                                    {
                                        fieldValue = "0";
                                    }
                                    else
                                    {
                                        fieldValue = string.Empty;
                                    }
                                    _sdef.SubmitValueOnLoad(fieldName, rowNo, fieldValue);

                                    // Register variable in parser
                                    _parser.RegisterVar(varName, varType, fieldValue);

                                    continue;
                                }
                                //// TODO: Remove This block of code after testing

                                //if (gridRowActionFromSdef || nonGridRowActinFromSdef)
                                //{
                                //    if (gridRowActionFromSdef)
                                //    {
                                //        string rowActionFromStructDef = _sdef.GetRowAction(dcNo, rowNo);

                                //        Console.WriteLine("Row Action from structDef: " + rowActionFromStructDef);

                                //        string gridRowChangesFromSdef = _sdef.GetGridRowChanges(dcNo, rowNo);

                                //        Console.WriteLine("Row Action from structDef: " + gridRowChangesFromSdef);

                                //    }

                                //    string ngChanges = _sdef.GetNGChanges(frame.TableName);

                                //    Console.WriteLine("Row Action from structDef: " + ngChanges);

                                //}

                                // Submit the field value to the struct definition
                                //_sdef.SubmitValue(
                                //    fieldName,
                                //    rowNo,
                                //    fieldValue,
                                //    "",
                                //    0,
                                //    0,
                                //    recordId
                                //);

                                _sdef.SubmitValueOnLoad(fieldName, rowNo, fieldValue, idVal, true);

                                // Register variable in parser

                                //string varName = fieldName;
                                //string varType = fp.DataType;
                                _parser.RegisterVar(varName, varType, fieldValue);

                                processedFields++;
                                _traceService.LogMessage(
                                    $"Successfully processed field '{fieldName}' = '{fieldValue}'"
                                );
                            }
                            catch (Exception fieldEx)
                            {
                                _traceService.LogError(
                                    $"Error processing field {fieldName} in {dcName}->{rowName}; Exception: {fieldEx.Message}"
                                );
                                // Optionally add to results list if you have one
                                validateResultList.Add($"Error processing field {fieldName}: {fieldEx.Message}");
                            }
                        }
                        // End of INNER LOOP (Fields within a row)

                        _traceService.LogMessage(
                            $"Completed processing all fields for {dcName}->{rowName}"
                        );
                    }
                    // End of MIDDLE LOOP (Rows within a data container)

                    _traceService.LogMessage($"Completed processing all rows for {dcName}");
                }
                // End of OUTER LOOP (Data containers)

                _traceService.LogMessage(
                    $"Processed {processedFields} fields total across all data containers and rows"
                );

                // Return success message with field count
                return processedFields > 0
                    ? $"Successfully Submitted {processedFields} fields. Details: {string.Join("; " + Environment.NewLine, validateResultList)}"
                    : "No values were submitted";
            }
            catch (Exception ex)
            {
                _traceService.LogError($"Error in Submitting Fields to StructDef; Error: {ex.Message}");
                validateResultList.Add($"Error in Submitting Fields to StructDef: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }

        //public async Task<string> ValidateData()
        //{
        //    try
        //    {
        //        //_traceService.LogMessage($"Calling RefreshField for row {rowNo}");
        //        //_traceService.LogMessage("Before RefreshField for row {RowNo}", rowNo);
        //        string refreshResult = await RefreshField();
        //        //_traceService.LogMessage("After RefreshField for row {RowNo}", rowNo);

        //        if (!string.IsNullOrEmpty(refreshResult))
        //        {
        //            _traceService.LogMessage($"RefreshField completed for row {refreshResult}");
        //            //results.Add($"Refresh for row {refreshResult}");
        //        }
        //        else
        //        {
        //            //_traceService.LogMessage($"RefreshField completed successfully for row {rowNo}");
        //            //results.Add($"Refresh for row Success");
        //        }

        //        //_sdef.InsertedRowNoInfo(frameNo, rowNo);
        //    }
        //    catch (Exception refreshEx)
        //    {
        //        _traceService.LogError(refreshEx, $"Error in RefreshField for row");
        //        //results.Add($"Refresh error for row {refreshEx.Message}");
        //    }

        //}

        public async Task<string> SubmitValue(TStructRequestDto dataJson)
        {
            try
            {
                // Add null checks for critical dependencies
                if (_sdef == null)
                    throw new InvalidOperationException("StructDef not initialized");
                if (_parser == null)
                    throw new InvalidOperationException("Parser not initialized");

                // Parse recordid - now it might be from keyvalue or recordid field
                //Int64 recordId = 0;
                //if (!string.IsNullOrEmpty(dataJson.Keyvalue) && Int64.TryParse(dataJson.Keyvalue, out var keyVal))
                //{
                //    recordId = keyVal;
                //}
                //else if (!string.IsNullOrEmpty(dataJson.Recordid) && Int64.TryParse(dataJson.Recordid, out var recId))
                //{
                //    recordId = recId;
                //}

                int processedFields = 0;
                int erroredFields = 0;

                // Check for submitdata instead of data.data (NEW PAYLOAD STRUCTURE)
                if (dataJson.Submitdata == null)
                {
                    _traceService.LogMessage("Invalid data structure: missing submitdata");
                    return "Invalid data structure: missing submitdata";
                }

                _traceService.LogMessage(
                    $"Starting to process {dataJson.Submitdata.Count} data containers"
                );

                // OUTER LOOP: Iterate through Data Containers (dc1, dc2, dc3, etc.)
                // Each DC represents a different data frame/table in the structure
                foreach (var dc in dataJson.Submitdata)
                {
                    string dcName = dc.Key; // Gets "dc1", "dc2", etc.

                    // Skip if the key doesn't start with "dc" (data container)
                    if (!dcName.StartsWith("dc", StringComparison.OrdinalIgnoreCase))
                        continue;

                    // Extract frame number from dcName (dc1 -> 1, dc2 -> 2, etc.)
                    if (!int.TryParse(dcName.Substring(2), out int frameNo))
                        continue;

                    int dcNo = frameNo;

                    TFrame frame = _sdef.Frames[dcNo - 1];

                    _traceService.LogMessage(
                        $"Processing Data Container: {dcName} (Frame {frameNo})"
                    );

                    // MIDDLE LOOP: Iterate through Rows within each Data Container (row1, row2, row3, etc.)
                    // Each row represents a record/entry within that data container
                    foreach (var row in dc.Value)
                    {
                        string rowName = row.Key; // Gets "row1", "row2", "row3", etc.
                        Int64 recordId = 0;
                        string rowAction = "";

                        // Skip if the key doesn't start with "row"
                        if (!rowName.StartsWith("row", StringComparison.OrdinalIgnoreCase))
                            continue;

                        // Extract row number from rowName (row1 -> 1, row2 -> 2, etc.)
                        int rowNo = 0;
                        if (onDataLoad)
                        {
                            if (!row.Value.ContainsKey("axrow_action"))
                            {
                                _traceService.LogMessage($"axrow_action column is not found in Frame {dcNo}, Row {row.Key}.");
                                throw new Exception($"axrow_action column is not found in Frame {dcNo}, Row {row.Key}.");
                            }

                            if (!row.Value.ContainsKey("axp_recid"))
                            {
                                _traceService.LogMessage($"axp_recid column is not found in Frame {dcNo}, Row {row.Key}.");
                                throw new Exception($"axp_recid column is not found in Frame {dcNo}, Row {row.Key}.");
                            }

                            Int64 tempRecordId = 0;

                            if (row.Value.ContainsKey("axrow_action"))
                            {
                                var tempRowAction = row.Value["axrow_action"].ToLower();
                                if (tempRowAction == "edit" || tempRowAction == "delete")
                                {

                                    if (row.Value.ContainsKey("axp_recid"))
                                    {
                                        tempRecordId = Convert.ToInt64(row.Value["axp_recid"]);

                                        var tempRowNo = _sdef.Frames[dcNo - 1].RecordIdDict.FirstOrDefault(x => x.Value == tempRecordId).Key;

                                        if (tempRowNo > 0)
                                        {
                                            rowNo = tempRowNo;
                                        }
                                        else
                                        {
                                            _traceService.LogMessage(
                                                $"RecordId {tempRecordId} not found in Frame {dcNo}, skipping row."
                                            );

                                            throw new Exception($"{tempRecordId} RecordId is not found in DC {dcNo}");
                                        }
                                    }
                                }
                                else if (tempRowAction == "create")
                                {
                                    if (frame.AsGrid)
                                        rowNo = _sdef.GetRowCount(dcNo) + 1;
                                    else
                                    {
                                        if (_sdef.GetRowCount(dcNo) == 0)
                                            rowNo = 1;
                                        else
                                        {
                                            _traceService.LogMessage(
                                                $"Cannot create new row in Non-grid DC {dcNo} as already a row exists."
                                            );

                                            throw new Exception($"Cannot create new row in Non-grid DC {dcNo} as already a row exists");
                                        }
                                    }
                                }
                            }

                        }
                        else
                        {
                            if (frame.AsGrid)
                                rowNo = _sdef.GetRowCount(dcNo) + 1;
                            else
                            {
                                if (_sdef.GetRowCount(dcNo) == 0)
                                    rowNo = 1;
                                else
                                {
                                    _traceService.LogMessage(
                                        $"Cannot create new row in Non-grid DC {dcNo} as already a row exists."
                                    );

                                    throw new Exception($"Cannot create new row in Non-grid DC {dcNo} as already a row exists");
                                }
                            }
                        }


                        _traceService.LogMessage(
                            $"Processing Row: {rowName} (Row Number {rowNo}) in {dcName}"
                        );

                        // INNER LOOP: Process all fields within this specific row
                        // Each field represents a column/attribute of the current row
                        foreach (var field in row.Value)
                        {
                            string fieldName = field.Key; // Field name like "cname", "jdate", "axrow_action", etc.
                            string fieldValue = field.Value ?? ""; // Field value
                            //bool gridRowActionFromSdef = false;
                            //bool nonGridRowActinFromSdef = false;

                            string rowKeyName = fieldName.ToLowerInvariant();

                            switch (rowKeyName)
                            {
                                case "axp_recid":
                                    if (Int64.TryParse(fieldValue, out var recId))
                                    {
                                        recordId = recId;
                                        _sdef.SubmitRecordId(dcNo, rowNo, recordId);
                                    }
                                    else
                                    {
                                        _traceService.LogMessage(
                                            $"Invalid axp_recid value '{fieldValue}' in {dcName}->{rowName}"
                                        );
                                    }
                                    continue;

                                case "axrow_action":
                                    rowAction = fieldValue;
                                    if (rowAction.ToLower() == "delete")
                                        deletedRowsDict[frameNo] = true;

                                    if (frame != null)
                                    {
                                        if (frame.AsGrid)
                                        {
                                            _sdef.SetGridRowAction(dcNo, rowNo, rowAction);
                                        }
                                        else
                                        {
                                            _sdef.SetNonGridRowAction(frame.TableName, rowAction);
                                        }
                                    }

                                    continue;

                                default:
                                    break;
                            }

                            _traceService.LogMessage(
                                $"Processing Field: '{fieldName}' with Value: '{fieldValue}' in {dcName}->{rowName}"
                            );

                            try
                            {
                                // Get field definition from struct
                                TField fp = _sdef.GetField(fieldName);
                                if (fp == null)
                                {
                                    _traceService.LogMessage(
                                        $"Field {fieldName} not found in struct definition"
                                    );

                                    throw new Exception($"Field {fieldName} not found in struct definition");
                                    //continue;
                                }

                                string varName = fieldName;
                                string varType = fp.DataType;

                                var rowActionValue = _sdef.GetRowAction(dcNo, rowNo);

                                if (rowActionValue.ToLower() == "delete")
                                {
                                    if (varType == "n")
                                    {
                                        fieldValue = "0";
                                    }
                                    else
                                    {
                                        fieldValue = string.Empty;
                                    }
                                    _sdef.SubmitValue(fieldName, rowNo, fieldValue);
                                    // Register variable in parser
                                    _parser.RegisterVar(varName, varType, fieldValue);

                                    continue;
                                }



                                // Submit the field value to the struct definition
                                _sdef.SubmitValue(fieldName, rowNo, fieldValue, "", 0, 0, recordId, true);

                                // Register variable in parser

                                //string varName = fieldName;
                                //string varType = fp.DataType;
                                _parser.RegisterVar(varName, varType, fieldValue);

                                processedFields++;
                                _traceService.LogMessage(
                                    $"Successfully processed field '{fieldName}' = '{fieldValue}'"
                                );
                            }
                            catch (Exception fieldEx)
                            {
                                erroredFields++;

                                _traceService.LogError(

                                    $"Error processing field {fieldName} in {dcName}->{rowName}; Error: {fieldEx.Message}"
                                );
                                // Optionally add to results list if you have one
                                validateResultList.Add($"Error processing field {fieldName}: {fieldEx.Message}");
                                errorList.Add($"Error submitting field {fieldName} with value '{fieldValue}': {fieldEx.Message}");
                            }
                        }
                        // End of INNER LOOP (Fields within a row)

                        _traceService.LogMessage(
                        $"Completed processing all fields for {dcName}->{rowName}"
                    );
                    }
                    // End of MIDDLE LOOP (Rows within a data container)

                    _traceService.LogMessage($"Completed processing all rows for {dcName}");
                }
                // End of OUTER LOOP (Data containers)

                // After processing all fields in the row, then do the refresh
                //try
                //{
                //    //_traceService.LogMessage($"Calling RefreshField for row {rowNo}");
                //    //_traceService.LogMessage("Before RefreshField for row {RowNo}", rowNo);
                //    string refreshResult = await RefreshField();
                //    //_traceService.LogMessage("After RefreshField for row {RowNo}", rowNo);

                //    if (!string.IsNullOrEmpty(refreshResult))
                //    {
                //        _traceService.LogMessage($"RefreshField completed for row {refreshResult}");
                //        //results.Add($"Refresh for row {refreshResult}");
                //    }
                //    else
                //    {
                //        //_traceService.LogMessage($"RefreshField completed successfully for row {rowNo}");
                //        //results.Add($"Refresh for row Success");
                //    }

                //    //_sdef.InsertedRowNoInfo(frameNo, rowNo);
                //}
                //catch (Exception refreshEx)
                //{
                //    _traceService.LogError(refreshEx, $"Error in RefreshField for row");
                //    //results.Add($"Refresh error for row {refreshEx.Message}");
                //}

                _traceService.LogMessage(
                    $"Processed {processedFields} fields total across all data containers and rows"
                );


                if (erroredFields > 0)
                    return $"{string.Join("; " + Environment.NewLine, errorList)}";
                else
                    return processedFields > 0
                        ? $"Successfully Submitted {processedFields} fields. Details: {string.Join("; " + Environment.NewLine, validateResultList)}"
                        : "No values were submitted";
            }
            catch (Exception ex)
            {
                _traceService.LogError($"Error in Submitting Fields to StructDef; Error: {ex.Message}");
                validateResultList.Add($"Error in Submitting Fields to StructDef: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }

        //public async Task<string> SubmitValueToSdef(TStructRequestDto dataJson)
        //{
        //    try
        //    {
        //        // Add null checks for critical dependencies
        //        if (_sdef == null) throw new InvalidOperationException("StructDef not initialized");
        //        if (_parser == null) throw new InvalidOperationException("Parser not initialized");

        //        Int64 recordId = Int64.TryParse(dataJson.Recordid, out var recId) ? recId : 0;

        //        int processedFields = 0;

        //        if (dataJson.Data?.Data == null)
        //        {
        //            _traceService.LogMessage("Invalid data structure: missing data.data");
        //            return "Invalid data structure: missing data";
        //        }

        //        _traceService.LogMessage($"Starting to process {dataJson.Data.Data.Count} data containers");

        //        foreach (var dc in dataJson.Data.Data)
        //        {
        //            string dcName = dc.Key;
        //            if (!dcName.StartsWith("dc", StringComparison.OrdinalIgnoreCase))
        //                continue;

        //            if (!int.TryParse(dcName.Substring(2), out int frameNo))
        //                continue;

        //            _traceService.LogMessage($"Processing frame {frameNo}");

        //            foreach (var row in dc.Value)
        //            {
        //                string rowName = row.Key;
        //                if (!rowName.StartsWith("row", StringComparison.OrdinalIgnoreCase))
        //                    continue;

        //                if (!int.TryParse(rowName.Substring(3), out int rowNo))
        //                    continue;

        //                _traceService.LogMessage($"Processing row {rowNo} in frame {frameNo}");

        //                // Process all fields in this row first
        //                foreach (var field in row.Value)
        //                {
        //                    string fieldName = field.Key;
        //                    string fieldValue = field.Value ?? "";

        //                    _traceService.LogMessage($"Processing field {fieldName} with value: {fieldValue}");

        //                    try
        //                    {
        //                        TField fp = _sdef.GetField(fieldName);
        //                        if (fp == null)
        //                        {
        //                            _traceService.LogMessage($"Field {fieldName} not found in struct definition");
        //                            continue;
        //                        }

        //                        _sdef.SubmitValue(
        //                            fieldName,
        //                            rowNo,
        //                            fieldValue,
        //                            "",
        //                            0,
        //                            0,
        //                            recordId
        //                        );

        //                        //string varName = $"dc{frameNo}_row{rowNo}_{fieldName
        //                        string varName = fieldName;

        //                        //string varType = Int64.TryParse(fieldValue, out _) ? "n" : "s";
        //                        string varType = fp.DataType;
        //                        _parser.RegisterVar(varName, varType, fieldValue);

        //                        processedFields++;
        //                        //results.Add($"Field {fieldName} value '{fieldValue}' has been submitted.");

        //                        _traceService.LogMessage($"Field {fieldName} value '{fieldValue}' has been submitted.");
        //                    }
        //                    catch (Exception fieldEx)
        //                    {
        //                        _traceService.LogError(fieldEx, $"Error processing field {fieldName}");

        //                        //results.Add($"Error processing field {fieldName}: {fieldEx.Message}");
        //                    }
        //                }

        //            }
        //        }

        //        // After processing all fields in the row, then do the refresh
        //        //try
        //        //{
        //        //    //_traceService.LogMessage($"Calling RefreshField for row {rowNo}");
        //        //    //_traceService.LogMessage("Before RefreshField for row {RowNo}", rowNo);
        //        //    string refreshResult = await RefreshField();
        //        //    //_traceService.LogMessage("After RefreshField for row {RowNo}", rowNo);

        //        //    if (!string.IsNullOrEmpty(refreshResult))
        //        //    {
        //        //        _traceService.LogMessage($"RefreshField completed for row {refreshResult}");
        //        //        //results.Add($"Refresh for row {refreshResult}");
        //        //    }
        //        //    else
        //        //    {
        //        //        //_traceService.LogMessage($"RefreshField completed successfully for row {rowNo}");
        //        //        //results.Add($"Refresh for row Success");
        //        //    }

        //        //    //_sdef.InsertedRowNoInfo(frameNo, rowNo);
        //        //}
        //        //catch (Exception refreshEx)
        //        //{
        //        //    _traceService.LogError(refreshEx, $"Error in RefreshField for row");
        //        //    //results.Add($"Refresh error for row {refreshEx.Message}");
        //        //}

        //        _traceService.LogMessage($"Processed {processedFields} fields total");
        //        //return processedFields > 0
        //        //    ? $"Successfully processed {processedFields} fields. Details: {string.Join("; ", results)}"
        //        //    : "No values were submitted";
        //        return processedFields > 0
        //  ? $"Successfully Submitted {processedFields} fields. Details: {string.Join("; " + Environment.NewLine, validateResultList)}"
        //    : "No values were submitted";
        //    }
        //    catch (Exception ex)
        //    {
        //        _traceService.LogError(ex, "Error in Submitting Fields to StructDef");
        //        validateResultList.Add($"Error in Submitting Fields to StructDef: {ex.Message}");
        //        return $"Error: {ex.Message}";
        //    }
        //}

        // SubmitValuetoStructDef function

        //public async Task<string> SubmitValueToStructDef(TStructRequestDto dataJson)
        //{
        //    try
        //    {
        //        // Add null checks for critical dependencies
        //        if (_sdef == null) throw new InvalidOperationException("StructDef not initialized");
        //        if (_parser == null) throw new InvalidOperationException("Parser not initialized");
        //        // if (_storeData == null) throw new InvalidOperationException("StoreData not initialized");

        //        Int64 recordId = Int64.TryParse(dataJson.Recordid, out var recId) ? recId : 0;

        //        int processedFields = 0;

        //        if (dataJson.Data?.Data == null)
        //        {
        //            _traceService.LogMessage("Invalid data structure: missing data.data");
        //            return "Invalid data structure: missing data";
        //        }

        //        _traceService.LogMessage($"Starting to process {dataJson.Data.Data.Count} data containers");

        //        foreach (var dc in dataJson.Data.Data)
        //        {
        //            string dcName = dc.Key;
        //            if (!dcName.StartsWith("dc", StringComparison.OrdinalIgnoreCase))
        //                continue;

        //            if (!int.TryParse(dcName.Substring(2), out int frameNo))
        //                continue;

        //            _traceService.LogMessage($"Processing frame {frameNo}");

        //            foreach (var row in dc.Value)
        //            {
        //                string rowName = row.Key;
        //                if (!rowName.StartsWith("row", StringComparison.OrdinalIgnoreCase))
        //                    continue;

        //                if (!int.TryParse(rowName.Substring(3), out int rowNo))
        //                    continue;

        //                _traceService.LogMessage($"Processing row {rowNo} in frame {frameNo}");

        //                // Process all fields in this row first
        //                foreach (var field in row.Value)
        //                {
        //                    string fieldName = field.Key;
        //                    string fieldValue = field.Value ?? "";

        //                    _traceService.LogMessage($"Processing field {fieldName} with value: {fieldValue}");

        //                    try
        //                    {
        //                        TField fp = _sdef.GetField(fieldName);
        //                        if (fp == null)
        //                        {
        //                            _traceService.LogMessage($"Field {fieldName} not found in struct definition");
        //                            continue;
        //                        }

        //                        _sdef.SubmitValue(
        //                            fieldName,
        //                            rowNo,
        //                            fieldValue,
        //                            "",
        //                            0,
        //                            0,
        //                            recordId

        //                        );

        //                        //string varName = $"dc{frameNo}_row{rowNo}_{fieldName
        //                        string varName = fieldName;

        //                        //string varType = Int64.TryParse(fieldValue, out _) ? "n" : "s";
        //                        string varType = fp.DataType;
        //                        _parser.RegisterVar(varName, varType, fieldValue);

        //                        processedFields++;
        //                        //results.Add($"Field {fieldName} value '{fieldValue}' has been submitted.");

        //                        _traceService.LogMessage($"Field {fieldName} value '{fieldValue}' has been submitted.");
        //                    }
        //                    catch (Exception fieldEx)
        //                    {
        //                        _traceService.LogError(fieldEx, $"Error processing field {fieldName}");
        //                        //results.Add($"Error processing field {fieldName}: {fieldEx.Message}");
        //                    }
        //                }

        //            }
        //        }

        //        // After processing all fields in the row, then do the refresh
        //        try
        //        {
        //            //_traceService.LogMessage($"Calling RefreshField for row {rowNo}");
        //            //_traceService.LogMessage("Before RefreshField for row {RowNo}", rowNo);
        //            string refreshResult = await RefreshField();
        //            //_traceService.LogMessage("After RefreshField for row {RowNo}", rowNo);

        //            if (!string.IsNullOrEmpty(refreshResult))
        //            {
        //                _traceService.LogMessage($"RefreshField completed for row {refreshResult}");
        //                //results.Add($"Refresh for row {refreshResult}");
        //            }
        //            else
        //            {
        //                //_traceService.LogMessage($"RefreshField completed successfully for row {rowNo}");
        //                //results.Add($"Refresh for row Success");
        //            }

        //            //_sdef.InsertedRowNoInfo(frameNo, rowNo);
        //        }
        //        catch (Exception refreshEx)
        //        {
        //            _traceService.LogError(refreshEx, $"Error in RefreshField for row");
        //            //results.Add($"Refresh error for row {refreshEx.Message}");
        //        }

        //        _traceService.LogMessage($"Processed {processedFields} fields total");
        //        //return processedFields > 0
        //        //    ? $"Successfully processed {processedFields} fields. Details: {string.Join("; ", results)}"
        //        //    : "No values were submitted";
        //        return processedFields > 0
        //  ? $"Successfully processed {processedFields} fields. Details: {string.Join("; " + Environment.NewLine, validateResultList)}"
        //    : "No values were submitted";
        //    }
        //    catch (Exception ex)
        //    {
        //        _traceService.LogError(ex, "Error in SubmitValueToStructDef");
        //        return $"Error: {ex.Message}";
        //    }
        //}

        public async Task<string> RefreshField()
        {
            string result = string.Empty;
            int fieldCount = 0;
            string processField = "";
            int processedFields = 0;
            string fieldResult = string.Empty;

            _traceService.LogMessage($"RefreshField started for row with {_sdef.Flds?.Count} fields");

            // Check if Flds is null or empty
            if (_sdef.Flds == null || _sdef.Flds.Count == 0)
            {
                _traceService.LogMessage("No fields found in StructDef.Flds");
                return "No fields to refresh";
            }

            for (int fidx = 0; fidx < _sdef.Frames.Count; fidx++)
            {
                try
                {
                    var rowNo = 1;
                    int totalRowCount = _sdef.GetRowCount(fidx + 1); //rowcount

                    for (int idx = 1; idx <= totalRowCount; idx++)
                    {
                        rowNo = idx;

                        int startFldIdx = _sdef.Frames[fidx].StartIndex;
                        int endFldIdx =
                            _sdef.Frames[fidx].StartIndex + _sdef.Frames[fidx].FieldCount;
                        for (int fldIdx = startFldIdx; fldIdx <= endFldIdx - 1; fldIdx++)
                        {
                            try
                            {
                                processedFields++;
                                fieldCount++;
                                TField field = _sdef.Flds[fldIdx];
                                processField = field.FieldName;
                                _traceService.LogMessage(
                            $"Processing field {fieldCount}: {field.FieldName}"
                        );

                                // Call RefreshFieldAsync for the current field
                                _traceService.LogMessage(
                                    $"Calling RefreshFieldAsync for {field.FieldName}"
                                );

                                // Assigning the current Value
                                fieldResult = _sdef.GetFieldValue(field, rowNo);

                                fieldResult = await RefreshFieldAsync(field, rowNo);
                                FieldValue = fieldResult;

                                _traceService.LogMessage(
                                    $"RefreshFieldAsync completed for {field.FieldName}: '{fieldResult}'"
                                );

                                if (!string.IsNullOrEmpty(fieldResult))
                                {
                                    result = fieldResult;
                                }

                                // Perform validation for the current field
                                _traceService.LogMessage(
                                    $"Calling RapidValidateData for {field.FieldName}"
                                );
                                bool validationResult = await RapidValidateData(
                                    field,
                                    rowNo,
                                    string.IsNullOrEmpty(fieldResult)
                                );

                                _traceService.LogMessage(
                                    $"RapidValidateData result for {field.FieldName}: {validationResult}"
                                );

                                if (!validationResult)
                                {
                                    _traceService.LogMessage(
                                        $"Validation failed for field {field.FieldName}: {ErrorStr}"
                                    );
                                    result = ErrorStr;
                                    validationPassed = false;
                                    validateResultList.Add($"Validation Fail: Field '{field.FieldName}' (Row No: {rowNo}) with value '{fieldResult}'; {ErrorStr}");
                                }
                                else
                                {
                                    _traceService.LogMessage($"Validation Success: Field '{field.FieldName}' (Row No: {rowNo}) with value '{fieldResult}' is Valid.");
                                }
                            }
                            catch (Exception ex)
                            {
                                _traceService.LogMessage(
                                    $"Validation failed for field {processField}: {ErrorStr}"
                                );
                                result = ErrorStr;
                                validationPassed = false;
                                validateResultList.Add($"Validation Fail: Field '{processField}' (Row No: {rowNo}) with value '{fieldResult}'; {ErrorStr}");
                                _traceService.LogError($"Error processing field {processField}: Error: {ex.Message}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    validationPassed = false;
                    _traceService.LogError($"Error processing field {processField} Error: {ex.Message}");
                    result = $"Error processing {processField}: {ex.Message}";
                    break;
                }
            }

            _traceService.LogMessage(
                $"RefreshField completed for row  Processed {fieldCount} fields. Final result: {result}"
            );
            return result;
        }

        public async Task<string> RefreshFieldsOnLoad()
        {
            string result = string.Empty;

            string processField = "";
            string fieldResult = string.Empty;
            int processedFields = 0;

            _traceService.LogMessage(
                $"RefreshFieldOnLoad started for row with {_sdef.Flds?.Count} fields"
            );

            // Check if Flds is null or empty
            if (_sdef.Flds == null || _sdef.Flds.Count == 0)
            {
                _traceService.LogMessage("No fields found in StructDef.Flds");
                return "No fields to refresh";
            }

            onDataLoad = true;
            for (int fidx = 0; fidx < _sdef.Frames.Count; fidx++)
            {
                _traceService.LogMessage($"DC Name: DC{_sdef.Frames[fidx].FrameNo}");

                try
                {
                    var rowNo = 1;
                    int totalRowCount = _sdef.GetRowCount(fidx + 1); //rowcount

                    for (int idx = 1; idx <= totalRowCount; idx++)
                    {
                        rowNo = idx;

                        int startFldIdx = _sdef.Frames[fidx].StartIndex;
                        int endFldIdx =
                            _sdef.Frames[fidx].StartIndex + _sdef.Frames[fidx].FieldCount;
                        RegRow(_sdef.Frames[fidx].FrameNo, idx);

                        for (int fldIdx = startFldIdx; fldIdx <= endFldIdx - 1; fldIdx++)
                        {
                            TField field = _sdef.Flds[fldIdx];
                            _traceService.LogMessage($"Field Name:  {field.FieldName}");

                            if (!field.SaveValue)
                            {

                                try
                                {
                                    processedFields++;

                                    processField = field.FieldName;
                                    _traceService.LogMessage(
                                        $"Processing field {processedFields}: {field.FieldName}"
                                    );

                                    // Call RefreshFieldAsync for the current field
                                    _traceService.LogMessage(
                                        $"Calling RefreshFieldAsync for {field.FieldName}"
                                    );

                                    // Assigning the current Value
                                    fieldResult = _sdef.GetFieldValue(field, rowNo);

                                    if (
                                        processField.Length >= 3
                                        && processField.Substring(0, 3).ToLower() == "old"
                                    )
                                    {
                                        fieldResult = _sdef.GetFieldValue(
                                            processField.Substring(3),
                                            rowNo
                                        );
                                        //axprovider.dbm.gf.DoDebug.Msg($"{processField} row = {rowNo} Value = {fieldResult}");
                                        _sdef.SetFieldValue(
                                            field,
                                            rowNo,
                                            fieldResult,
                                            fieldResult,
                                            0,
                                            0,
                                            0
                                        );
                                        _parser.RegisterVar(
                                            processField,
                                            field.DataType[0].ToString(),
                                            fieldResult
                                        );
                                    }
                                    else
                                    {
                                        fieldResult = await RefreshFieldAsync(field, rowNo);
                                    }

                                    FieldValue = fieldResult;

                                    _traceService.LogMessage(
                                        $"RefreshFieldAsync (Onload) completed for {field.FieldName}: '{fieldResult}'"
                                    );

                                    if (!string.IsNullOrEmpty(fieldResult))
                                    {
                                        result = fieldResult;
                                    }

                                    // Perform validation for the current field
                                    _traceService.LogMessage(
                                        $"Calling RapidValidateData for {field.FieldName}"
                                    );
                                    bool validationResult = await RapidValidateData(
                                        field,
                                        rowNo,
                                        string.IsNullOrEmpty(fieldResult)
                                    );

                                    _traceService.LogMessage(
                                        $"RapidValidateData result for {field.FieldName}: {validationResult}"
                                    );

                                    if (!validationResult)
                                    {
                                        _traceService.LogMessage(
                                            $"Validation failed for field {field.FieldName}: {ErrorStr}"
                                        );
                                        result = ErrorStr;
                                        validationPassed = false;
                                        validateResultList.Add($"Validation Fail: Field '{field.FieldName}' (Row No: {rowNo}) with value '{fieldResult}'; {ErrorStr}");
                                    }
                                    else
                                    {
                                        _traceService.LogMessage($"Validation Success: Field '{field.FieldName}' (Row No: {rowNo}) with value '{fieldResult}' is Valid.");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    validationPassed = false;
                                    validateResultList.Add($"Validation Fail: Field '{processField}' (Row No: {rowNo}) with value '{fieldResult}'; {ex.Message}");
                                    _traceService.LogMessage(
                                        $"Validation failed for field {processField}: {ex.Message}"
                                    );
                                    //result = $"Successfully processed {processedFields} fields. Details: {string.Join("; " + Environment.NewLine, validateResultList)}";

                                    _traceService.LogError($"Error processing field {processField}: {ex.Message}{Environment.NewLine}{ex.StackTrace}");
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    validationPassed = false;
                    validateResultList.Add($"Error processing field {processField}");
                    _traceService.LogError($"Error processing field {processField} Error: {ex.Message}");
                    //result = $"Successfully processed {processedFields} fields. Details: {string.Join("; " + Environment.NewLine, validateResultList)}";
                    return result;
                }
                finally
                {
                    onDataLoad = false;
                }
            }

            _traceService.LogMessage(
                $"RefreshField completed for row  Processed {processedFields} fields. Final result: {result}"
            );
            return processedFields > 0
                ? $"Successfully processed {processedFields} fields. Details: {string.Join("; " + Environment.NewLine, validateResultList)}"
                : "No values were submitted";
        }

        public async Task<OperationResult> ValidateData()
        {
            OperationResult result = new OperationResult();
            string processField = "";
            string fieldResult = string.Empty;
            int processedFields = 0;
            _traceService.LogMessage($"ValidateData started for row with {_sdef.Flds?.Count} fields");

            // Check if Flds is null or empty
            if (_sdef.Flds == null || _sdef.Flds.Count == 0)
            {
                _traceService.LogMessage("No fields found in StructDef.Flds");
                result.Result = "Fail";
                result.Message = "No fields to refresh";
                return result;
            }

            for (int fidx = 0; fidx < _sdef.Frames.Count; fidx++)
            {
                try
                {
                    var rowNo = 1;
                    int totalRowCount = 1;

                    // We only retrieve and process row counts for grid fields. 
                    // For non-grid fields, we assign rowNo = 1 and refresh all non-grid DCs. 
                    // This behavior is by design: non-grid fields can be processed through expressions alone, 
                    // while grid fields require at least one row to process correctly.
                    if (_sdef.Frames[fidx].AsGrid)
                    {
                        totalRowCount = _sdef.GetRowCount(fidx + 1); //rowcount
                    }

                    for (int idx = 1; idx <= totalRowCount; idx++)
                    {
                        rowNo = idx;

                        //if (_sdef.Frames[fidx].AsGrid)
                            RegRow(_sdef.Frames[fidx].FrameNo, rowNo);

                        int startFldIdx = _sdef.Frames[fidx].StartIndex;
                        int endFldIdx =
                            _sdef.Frames[fidx].StartIndex + _sdef.Frames[fidx].FieldCount;
                        for (int fldIdx = startFldIdx; fldIdx <= endFldIdx - 1; fldIdx++)
                        {
                            TField field = _sdef.Flds[fldIdx];

                            try
                            {
                                processedFields++;

                                processField = field.FieldName;
                                _traceService.LogMessage(
                                    $"Processing field {processedFields}: {field.FieldName}"
                                );

                                // Call RefreshFieldAsync for the current field
                                _traceService.LogMessage(
                                    $"Calling RefreshFieldAsync for {field.FieldName}"
                                );

                                // Assigning the current Value
                                fieldResult = _sdef.GetFieldValue(field, rowNo);

                                fieldResult = await RefreshFieldAsync(field, rowNo);

                                FieldValue = fieldResult;

                                _traceService.LogMessage(
                                    $"RefreshFieldAsync completed for {field.FieldName}: '{fieldResult}'"
                                );

                                //if (!string.IsNullOrEmpty(fieldResult))
                                //{
                                //    result.Message = fieldResult;
                                //}

                                // Perform validation for the current field
                                _traceService.LogMessage(
                                    $"Calling RapidValidateData for {field.FieldName}"
                                );
                                bool validationResult = await RapidValidateData(
                                    field,
                                    rowNo,
                                    string.IsNullOrEmpty(fieldResult)
                                );

                                _traceService.LogMessage(
                                    $"RapidValidateData result for {field.FieldName}: {validationResult}"
                                );

                                if (!validationResult)
                                {
                                    _traceService.LogMessage(
                                        $"Validation failed for field {field.FieldName}: {ErrorStr}"
                                    );
                                    validateResultList.Add($"Validation Fail: Field '{field.FieldName}' (Row No: {rowNo}) with value '{fieldResult}'; {ErrorStr}");

                                    result.Message = $"Validation Fail: Field '{field.FieldName}' (Row No: {rowNo}) with value '{fieldResult}'; {ErrorStr}";
                                    validationPassed = false;
                                }
                                else
                                {
                                    _traceService.LogMessage($"Validation Success: Field '{field.FieldName}' (Row No: {rowNo}) with value '{fieldResult}' is Valid.");
                                    //result.Message = string.Join(
                                    //    "; " + Environment.NewLine,
                                    //    validateResultList
                                    //);
                                }
                            }
                            catch (Exception ex)
                            {
                                validationPassed = false;
                                validateResultList.Add($"Validation Fail: Field '{processField}' (Row No: {rowNo}) with value '{fieldResult}'; {ex.Message}");
                                _traceService.LogMessage(
                                    $"Validation failed for field {processField}: {ex.Message}"
                                );
                                _traceService.LogError($"Error processing field {processField}: {ex.Message}{Environment.NewLine}{ex.StackTrace}");

                                result.Message = $"Validation Fail: Field '{processField}' (Row No: {rowNo}) with value '{fieldResult}'; {ex.Message}";

                                //result = $"Successfully processed {processedFields} fields. Details: {string.Join("; " + Environment.NewLine, validateResultList)}";
                            }
                        }
                    }


                }
                catch (Exception ex)
                {
                    validationPassed = false;
                    validateResultList.Add($"Error processing field {processField}");
                    _traceService.LogError($"Error processing field {processField}: Error: {ex.Message}");
                    result.Message = $"Error processing field {processField}";
                    //result = $"Successfully processed {processedFields} fields. Details: {string.Join("; " + Environment.NewLine, validateResultList)}";
                }
            }

            _traceService.LogMessage(
                $"RefreshField completed for row  Processed {processedFields} fields. Final result: {result}"
            );

            if (processedFields > 0)
            {


                if (!validationPassed)
                {
                    result.Result = "Fail";
                }
                else
                {
                    result.Result = "Success";
                }

                result.Message = string.Join(";", validateResultList);

                return result;
            }
            else
            {
                result.Result = "Success";
                result.Message = "No values were submitted";
                return result;
            }
        }

        public async Task<
            Dictionary<string, Dictionary<string, Dictionary<string, string>>>
        > GetTstructDataForAxGet()
        {
            Dictionary<string, Dictionary<string, Dictionary<string, string>>> tstData =
                new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();
            for (int fidx = 0; fidx < _sdef.Frames.Count; fidx++)
            {
                var rowNo = 1;
                int totalRowCount = _sdef.GetRowCount(fidx + 1);

                Dictionary<string, Dictionary<string, string>> dcRowsData =
                    new Dictionary<string, Dictionary<string, string>>();
                for (int idx = 1; idx <= totalRowCount; idx++)
                {
                    rowNo = idx;

                    int startFldIdx = _sdef.Frames[fidx].StartIndex;
                    int endFldIdx = _sdef.Frames[fidx].StartIndex + _sdef.Frames[fidx].FieldCount;
                    Dictionary<string, string> fieldData = new Dictionary<string, string>();
                    for (int fldIdx = startFldIdx; fldIdx <= endFldIdx - 1; fldIdx++)
                    {
                        TField field = _sdef.Flds[fldIdx];
                        fieldData[field.FieldName] = _sdef.GetFieldValue(field, rowNo);
                    }
                    dcRowsData[$"row{rowNo}"] = fieldData;
                }

                tstData[$"dc{fidx + 1}"] = dcRowsData;
            }

            return tstData;
        }


        public void PrepareExps()
        {
            for (int i = 0; i < _sdef.Flds.Count; i++)
            {
                var fld = _sdef.Flds[i];

                if (fld.DataType.ToLower() == "n")
                    _parser.RegisterVar(fld.FieldName, fld.DataType.ToString(), "0");

                if (fld.Cexp != "")
                {
                    if (fld.DataType.ToLower() != "n")
                        _parser.RegisterVar(fld.FieldName, fld.DataType.ToString(), "");
                }

            }
        }

        /// <summary>
        /// Registers the field values of the given row of a frame as parser variables (Delphi TValidate.RegRow),
        /// so expressions, link fields and SQL params evaluated for that row read this row's values.
        /// Fields with no value in this row are registered as empty.
        /// </summary>
        public void RegRow(int frameNo, int rowNo)
        {
            if (frameNo < 1 || frameNo > _sdef.Frames.Count)
                return;

            TFrame frame = _sdef.Frames[frameNo - 1];
            _parser.RegisterVar("activerow", "n", rowNo.ToString());

            for (int i = frame.StartIndex; i < frame.StartIndex + frame.FieldCount; i++)
            {
                TField fld = _sdef.Flds[i];

                // Delphi: fd.DataRows -> FieldList entry of this field for this row
                int k = _sdef.GetFieldIndex(fld.FieldName, rowNo);
                if (k > -1)
                {
                    FieldRec rec = _sdef.FieldList[k];
                    _parser.RegisterVar(rec.FieldName, fld.DataType, rec.Value ?? "");
                }
                else
                {
                    _parser.RegisterVar(fld.FieldName, fld.DataType, "");
                }
            }
        }
    }
}
