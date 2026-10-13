
using ARM_APIs.Model;
using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMCommon.Model;
using AxParserApp;
using AxStructDef;
using AxStructDef.Helpers;
using AxStructDef.Models;
using AxValidate.Extension;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Buffers;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;




namespace AxValidate.Helpers
{
    public class ValidateHelper
    {



        private readonly IConfiguration _config;

        private readonly AxParser _parser;
        private readonly StructDef _sdef;
        private readonly IARMLogTraceService _traceService;


        private readonly AxStructDefUtils _utils;
        private IDbHelper _dbHelper;
        public ARMCommon.Helpers.Utils _common;




        public ValidateHelper(string dbType, IConfiguration configuration, AxParser parser, StructDef sdef, AxStructDefUtils utils, IDbHelper dbHelper, IARMLogTraceService traceService, Utils common)
        {

            _dbHelper = dbHelper;


            _config = configuration;

            _parser = parser;
            _sdef = sdef;
            _traceService = traceService;


            _utils = utils;
            _common = common;





        }



        //public string FormatNumber(TField tfld, string s)
        //{
        //    int p, decimalPlaces;
        //    string result = s;


        //    if (true)
        //    {
        //        if (string.IsNullOrEmpty(s))
        //            return s;

        //        if (!s.Contains("E", StringComparison.OrdinalIgnoreCase))
        //        {
        //            s = _parser.GetNumber(_utils.RemoveCommas(s));
        //            s = string.Format(
        //                CultureInfo.InvariantCulture,
        //                "{0:F" + tfld.Dec + "}",
        //                _utils.RoundEx(Convert.ToDouble(s), tfld.Dec)
        //            );
        //        }
        //        else
        //        {
        //            s = string.Format(
        //                CultureInfo.InvariantCulture,
        //                "{0:F" + tfld.Dec + "}",
        //                _utils.RoundEx(Convert.ToDouble(s), tfld.Dec)
        //            );
        //        }

        //        decimalPlaces = tfld.Dec;
        //        if (decimalPlaces == 0 && tfld.Dec == tfld.CurDec)
        //            return s;

        //        p = s.IndexOf(_utils.LocDecimalSeparator, StringComparison.Ordinal);
        //        if (p == -1)
        //        {
        //            if (decimalPlaces > 0)
        //                s = s + _utils.LocDecimalSeparator + _utils.Pad("", decimalPlaces, '0');
        //        }
        //        else
        //        {
        //            if (decimalPlaces > 0)
        //                s = s.Substring(0, p + 1) + _utils.Pad(s.Substring(p + 1), decimalPlaces, '0');
        //            else
        //                s = s.Substring(0, p);
        //        }

        //        result = s;
        //    }
        //    else
        //    {
        //        if (string.IsNullOrEmpty(s))
        //        {
        //            s = "0";
        //            decimalPlaces = tfld.Dec;
        //            p = s.IndexOf(_utils.LocDecimalSeparator, StringComparison.Ordinal);

        //            if (p == -1)
        //            {
        //                if (decimalPlaces > 0)
        //                    s = s + _utils.LocDecimalSeparator + _utils.Pad("", decimalPlaces, '0');
        //            }
        //            else
        //            {
        //                if (decimalPlaces > 0)
        //                    s = s.Substring(0, p + 1) + _utils.Pad(s.Substring(p + 1), decimalPlaces, '0');
        //                else
        //                    s = s.Substring(0, p);
        //            }

        //            return s;
        //        }

        //        if (!s.Contains("E", StringComparison.OrdinalIgnoreCase))
        //        {
        //            s = _parser.GetNumber(_utils.RemoveCommas(s));
        //            s = string.Format(
        //                CultureInfo.InvariantCulture,
        //                "{0:F" + tfld.Dec + "}",
        //                _utils.RoundEx(Convert.ToDouble(s), tfld.Dec)
        //            );
        //        }
        //        else
        //        {
        //            s = string.Format(
        //                CultureInfo.InvariantCulture,
        //                "{0:F" + tfld.Dec + "}",
        //                _utils.RoundEx(Convert.ToDouble(s), tfld.Dec)
        //            );
        //        }

        //        decimalPlaces = tfld.Dec;
        //        if (decimalPlaces == 0 && tfld.Dec == tfld.CurDec)
        //            return s;

        //        p = s.IndexOf(_utils.LocDecimalSeparator, StringComparison.Ordinal);
        //        if (p == -1)
        //        {
        //            if (decimalPlaces > 0)
        //                s = s + _utils.LocDecimalSeparator + _utils.Pad("", decimalPlaces, '0');
        //        }
        //        else
        //        {
        //            if (decimalPlaces > 0)
        //                s = s.Substring(0, p + 1) + _utils.Pad(s.Substring(p + 1), decimalPlaces, '0');
        //            else
        //                s = s.Substring(0, p);
        //        }

        //        result = s;
        //    }

        //    return result;
        //}


        public string FormatNumber(TField tfld, string s)
        {
            int p, decimalPlaces;
            string result = s;

            if (string.IsNullOrEmpty(s))
                return s;

            // Validate if the string is actually a number before conversion
            if (!IsNumeric(s))
            {
                return "0"; // Return original string if it's not numeric
            }

            try
            {
                if (!s.Contains("E", StringComparison.OrdinalIgnoreCase))
                {
                    s = _parser.GetNumber(_utils.RemoveCommas(s));
                    // Add double.TryParse for safety
                    if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out double parsedValue))
                    {
                        s = string.Format(
                            CultureInfo.InvariantCulture,
                            "{0:F" + tfld.Dec + "}",
                            _utils.RoundEx(parsedValue, tfld.Dec)
                        );
                    }
                }
                else
                {
                    if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out double parsedValue))
                    {
                        s = string.Format(
                            CultureInfo.InvariantCulture,
                            "{0:F" + tfld.Dec + "}",
                            _utils.RoundEx(parsedValue, tfld.Dec)
                        );
                    }
                }

                decimalPlaces = tfld.Dec;
                if (decimalPlaces == 0 && tfld.Dec == tfld.CurDec)
                    return s;

                p = s.IndexOf(_utils.LocDecimalSeparator, StringComparison.Ordinal);
                if (p == -1)
                {
                    if (decimalPlaces > 0)
                        s = s + _utils.LocDecimalSeparator + _utils.Pad("", decimalPlaces, '0');
                }
                else
                {
                    if (decimalPlaces > 0)
                        s = s.Substring(0, p + 1) + _utils.Pad(s.Substring(p + 1), decimalPlaces, '0');
                    else
                        s = s.Substring(0, p);
                }

                result = s;
            }
            catch (Exception ex)
            {
                // Log the error and return original string
                System.Diagnostics.Debug.WriteLine($"FormatNumber error: {ex.Message}");
                return result;
            }

            return result;
        }

        // Helper method to check if string is numeric
        private bool IsNumeric(string value)
        {
            return double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out _);
        }

        public string TaxCompute(TField fd, int rno, SQLResult sqlResult)
        {
            //if (fd.QSelect.CDS.IsEmpty) return "";
            if (sqlResult == null || sqlResult.data == null) return "";

            // Prefer view (filtered) if present, otherwise use the raw data table
            DataTable table = (sqlResult.view != null && sqlResult.view.Count > 0)
                        ? sqlResult.view.ToTable()
                        : sqlResult.data;


            if (table == null || table.Rows.Count == 0) return "";


            // emulate original logic: get first column value from first row and extract fcodes 
            string firstColValue = table.Columns.Count > 0 ? (table.Rows[0][0]?.ToString() ?? "") : "";
            var taxcodes = new List<string>();
            var amtlist = new List<string>();
            string fcodes = _utils.GetNthString(firstColValue, 2, ' ').Trim();


            foreach (DataRow row in table.Rows)
            {
                string taxcode = table.Columns.Contains("taxcode") ? (row["taxcode"]?.ToString() ?? "") : "";
                string basic = table.Columns.Contains("basicamount") ? (row["basicamount"]?.ToString() ?? "0") : "0";
                string rate = table.Columns.Contains("rate") ? (row["rate"]?.ToString() ?? "0") : "0";
                string formula = table.Columns.Contains("formula") ? (row["formula"]?.ToString() ?? "") : "";

                _parser.RegisterVar("basicamount", "n", basic);
                _parser.RegisterVar("rate", "n", rate);

                double evaluated = 0;
                if (!string.IsNullOrWhiteSpace(formula))
                {
                    try
                    {
                        if (!_parser.Evaluate(formula))
                            throw new InvalidOperationException("Tax formula evaluation failed.");

                        if (!double.TryParse(_parser.Value?.ToString() ?? "0", out evaluated))
                            throw new InvalidOperationException("Tax formula returned a non-numeric result.");
                    }
                    catch (Exception exception)
                    {
                        throw new InvalidOperationException("Tax formula evaluation failed.", exception);
                    }
                }

                amtlist.Add(evaluated.ToString());
                if (!string.IsNullOrEmpty(taxcode))
                    _parser.RegisterVar(taxcode, "n", evaluated.ToString());
                taxcodes.Add(taxcode);
            }

            double rslt = 0;
            var tcodesSb = new StringBuilder();

            for (int i = 0; i < amtlist.Count; i++)
            {
                rslt += _utils.StrToFloatZ(amtlist[i]);
                tcodesSb.Append($"{taxcodes[i]},{amtlist[i]},");

            }

            string result = rslt.ToString();
            string tcodes = tcodesSb.Length > 0 ? tcodesSb.ToString().TrimEnd(',') : "";


            if (!string.IsNullOrEmpty(fcodes))
            {
                _sdef.SubmitValue(fcodes, rno, tcodes, "", 0, 0, 0);
            }

            return result;


        }

        public void FilterCDS(TField tfld, SQLResult sqlResult, int rowNo)
        {
            string fs = "";
            for (int i = 0; i < tfld.FilterCols.Count; i++)
            {
                var kv = tfld.FilterCols.ElementAt(i);
                string cn = kv.Key;
                string ftype = kv.Value;
                string opr;
                string fn = GetFieldName(cn, out opr);
                string fv = GetValue(fn, rowNo);

                if (opr == "c_like")
                    fv = $"%{fv.ToLower()}%";
                else if (opr == "s_like")
                    fv = $"{fv.ToLower()}%";
                else if (opr == "e_like")
                    fv = $"%{fv.ToLower()}";
                else if (opr == "lower")
                    fv = fv.ToLower();
                else if (opr == "upper")
                    fv = fv.ToUpper();
                else if (opr.Trim() == "in")
                    fv = $"({fv})";

                if (opr.Contains("like"))
                    opr = $" {opr.Substring(2, 4)} ";
                else if (opr == "upper" || opr == "lower")
                    opr = "=";

                if (ftype == "n")
                {
                    if (string.IsNullOrEmpty(fv))
                        fv = "0";
                }
                else if (opr.Trim() != "in")
                {
                    if (ftype == "d" && string.IsNullOrEmpty(fv))
                        fv = $"\"{_utils.DummyDate}\"";
                    fv = $"\"{fv}\"";
                }

                string cs = $"{cn}{opr}{fv}";
                fs = string.IsNullOrEmpty(fs) ? cs : $"{fs} and {cs}";
            }



            DataRow[] rows = sqlResult.data.Select(fs);

            if (rows.Length > 0)
            {
                sqlResult.row = rows[0];

            }


        }

        private string GetFieldName(string qcName, out string fopr)
        {
            string result = qcName;
            fopr = "=";

            for (int i = 0; i < _utils.LDSKeys.Count; i++)
            {
                string kv = _utils.LDSKeys.GetName(i);
                if (qcName.Substring(0, Math.Min(kv.Length, qcName.Length)).ToLower() == kv)
                {
                    result = qcName.Substring(kv.Length);
                    fopr = _utils.LDSKeys.GetValueFromIndex(i);
                    break;
                }
            }

            return result;
        }


        public bool IsSysVar(string s)
        {
            string vstr = ";username;recordid;finyrst;finyred;afinyrst;afinyred;usergroup;";
            vstr += "usergroupno;activerow;_maincurr;rolename;responsibilities;";
            vstr += "siteno;wf_applevel;wf_status;wf_maxlevel;axp_language;";
            vstr += "axp_timezone;axp_service;axp_apipath;axp_appschema;axp_devschema;";

            return vstr.Contains($";{s.ToLower()};");
        }

        public string GetValue(string fieldName, int rowNo)
        {
            string result;

            if (IsSysVar(fieldName))
            {
                result = _parser.GetVarValue(fieldName);
                //result = result.Trim(); 
                return result;
            }

            if (fieldName.ToLower() == "comp_name")
            {
                result = _sdef.CompanyName;
                return result;
            }

            result = _parser.GetVarValue(fieldName);



            if (rowNo == -1 && _parser.LastVarType.ToLower() == "c")
            {
                result = $"\'{result}\'";
            }

            return result;
        }

        public double StrToFloatZ(string s)
        {
            if (!string.IsNullOrWhiteSpace(s))
                return double.Parse(RemoveCommas(s));
            return 0;
        }




        //public async Task<string> PrepareSelectedField(TField xfld, int rowNo, string ErrorStr, double IdValue,  bool dependCall, string DynamicSql, Xds xQuery, int actualRowNo, bool forPickList, bool nonDynamicFilterSQL)
        //{
        //    string result = string.Empty;
        //    ErrorStr = string.Empty;

        //    bool validatePickList;


        //    // fld = xfld;

        //    object fvalue = null;
        //    string dcno = string.Empty;
        //    IdValue = 0;
        //    string visibleDCs = _sdef.VisibleDCs;

        //    // --- Mode: txtSelection ---
        //    if (xfld.TxtSelection)
        //    {
        //        fvalue = GetValue(xfld.FieldName, rowNo);

        //        if (string.IsNullOrEmpty(fvalue.ToString()) && !string.IsNullOrEmpty(xfld.Cexp))
        //        {
        //            // if (_parser.EvalPrepared(xfld.Exprn))
        //            if (_parser.Evaluate(xfld.Cexp)) fvalue = _parser.Value;
        //        }

        //        if (!string.IsNullOrEmpty(fvalue.ToString()))
        //        {

        //            IdValue = 0;
        //            dcno = "dc" + xfld.FrameNo;
        //            validatePickList = await ValidatePickList(xfld, fvalue.ToString(), rowNo, ErrorStr, actualRowNo, forPickList, nonDynamicFilterSQL, DynamicSql);

        //            if (dependCall || visibleDCs.Contains(dcno))
        //            {
        //                if (validatePickList)
        //                {
        //                    fvalue = Query.CDS.Fields[xfld.Searchcol].AsString;
        //                    IdValue = xfld.SourceKey ? Query.CDS.Fields[0].AsFloat : 0;
        //                }
        //                else
        //                {
        //                    fvalue = string.Empty;
        //                    IdValue = 0;
        //                }
        //            }
        //            else if (xfld.SourceKey && validatePickList)
        //            {
        //                fvalue = Query.CDS.Fields[xfld.Searchcol].AsString;
        //                IdValue = Query.CDS.Fields[0].AsFloat;
        //            }

        //            if (xfld.DataType == "n")
        //                fvalue = FormatNumber(xfld, fvalue.ToString());


        //            _sdef.SubmitValue(xfld.FieldName, rowNo, fvalue.ToString(), "", IdValue, 0, 0, "");
        //            _parser.RegisterVar(xfld.FieldName, xfld.DataType[0].ToString(), fvalue.ToString());
        //        }
        //    }
        //    // --- Mode: accept ---
        //    else if (xfld.ModeofEntry == "accept")
        //    {
        //        fvalue = GetValue(xfld.FieldName, rowNo);

        //        if (xfld.DataType == "n" && !string.IsNullOrEmpty(fvalue.ToString()) && StrToFloatZ(fvalue.ToString()) == 0)
        //            fvalue = string.Empty;

        //        if (string.IsNullOrEmpty(fvalue.ToString()) && !string.IsNullOrEmpty(xfld.Cexp))
        //        {
        //            // if (_parser.EvalPrepared(xfld.Exprn))
        //            if (_parser.Evaluate(xfld.Cexp))
        //            {
        //                fvalue = _parser.Value;
        //                if (xfld.DataType == "n") fvalue = FormatNumber(xfld, fvalue.ToString());

        //                _sdef.SubmitValue(xfld.FieldName, rowNo, fvalue.ToString(), "", 0, 0, 0);
        //                _parser.RegisterVar(xfld.FieldName, xfld.DataType[0].ToString(), fvalue.ToString());
        //            }
        //        }

        //        if (xfld.QSelect != null)
        //        {
        //            DynamicSql = xfld.DynamicParams ? xfld.SQL.ToString() : "";
        //            SQLResult sqlResult = await QueryOpen(xfld, rowNo, "0", fvalue.ToString(), forPickList, nonDynamicFilterSQL, DynamicSql);
        //            if (xQuery != xfld.QSelect) xfld.QSelect = xQuery;

        //            switch (xfld.DataType)
        //            {
        //                case "c":
        //                case "n":
        //                case "t":
        //                    fvalue = sqlResult.rowView[0].ToString();
        //                    break;
        //                case "d":
        //                    fvalue = !string.IsNullOrEmpty(sqlResult.rowView[0].ToString())
        //                        ? Convert.ToDateTime(sqlResult.rowView[0]).ToString()
        //                        : sqlResult.rowView[0].ToString();
        //                    break;
        //            }

        //            if (xfld.DataType == "n") fvalue = FormatNumber(xfld, fvalue.ToString());


        //            _sdef.SubmitValue(xfld.FieldName, rowNo, fvalue.ToString(), "", 0, 0, 0);
        //            _parser.RegisterVar(xfld.FieldName, xfld.DataType[0].ToString(), fvalue.ToString());
        //        }
        //    }
        //    // --- Mode: select (from list or not) ---
        //    else if (xfld.ModeofEntry == "select" && xfld.FromList && xfld.Exprn > -1)
        //    {
        //        fvalue = GetValue(xfld.FieldName, rowNo);
        //        // if (string.IsNullOrEmpty(fvalue.ToString()) && _parser.EvalpPrepared(xfld.Exprn))
        //        if (string.IsNullOrEmpty(fvalue.ToString()) && _parser.Evaluate(xfld.Cexp))
        //        {
        //            fvalue = _parser.Value;
        //            if (_sdef.GetFieldIndex(fvalue.ToString().Trim()) > -1) fvalue = string.Empty;

        //            if (xfld.DataType == "n") fvalue = FormatNumber(xfld, fvalue.ToString());
        //            // 

        //            _sdef.SubmitValue(xfld.FieldName, rowNo, fvalue.ToString(), "", 0, 0, 0);
        //            _parser.RegisterVar(xfld.FieldName, xfld.DataType[0].ToString(), fvalue.ToString());
        //        }
        //    }
        //    else if (xfld.ModeofEntry == "select" && !xfld.FromList)
        //    {
        //        IdValue = 0;
        //        fvalue = GetValue(xfld.FieldName, rowNo);

        //        if (!string.IsNullOrEmpty(xfld.Cexp) || xfld.Autoselect || !string.IsNullOrEmpty(xfld.ComponentType))
        //        {
        //            // if (_parser.EvalPrepared(xfld.Exprn))
        //            if (_parser.Evaluate(xfld.Cexp)) fvalue = _parser.Value;

        //            if (!string.IsNullOrEmpty(fvalue.ToString()) || xfld.Autoselect || !string.IsNullOrEmpty(xfld.ComponentType))
        //            {
        //                dcno = "dc" + xfld.FrameNo;

        //                if (xfld.QSelect != null && (dependCall || visibleDCs.Contains(dcno)))
        //                {
        //                    DynamicSql = xfld.DynamicParams ? xfld.SQLText : "";
        //                    SQLResult sqlResult = await QueryOpen(xfld, rowNo, "0", fvalue.ToString(), forPickList, nonDynamicFilterSQL, DynamicSql);
        //                    // if (xQuery != xfld.QSelect) xfld.QSelect = xQuery;

        //                    if (sqlResult.view.Count == 1 && xfld.Autoselect)
        //                    {
        //                        fvalue = sqlResult.rowView[xfld.Searchcol].ToString();
        //                        if (xfld.SourceKey) IdValue = Convert.ToDouble(sqlResult.rowView[0]);
        //                    }

        //                    // if (!xfld.QSelect.CDS.Locate(xfld.QSelect.CDS.Fields[xfld.Searchcol].FieldName, fvalue, true))
        //                    // {
        //                    //     fvalue = string.Empty;
        //                    //     IdValue = 0;
        //                    // }
        //                    // else if (xfld.SourceKey) IdValue = xfld.QSelect.CDS.Fields[0].AsFloat;

        //                    if (!xfld.QSelect.CDS.Locate(sqlResult.rowView[xfld.Searchcol], fvalue, true))
        //                    {
        //                        fvalue = string.Empty;
        //                        IdValue = 0;
        //                    }
        //                    else if (xfld.SourceKey) IdValue = Convert.ToDouble(sqlResult.rowView[0]);
        //                }

        //                if (xfld.DataType == "n") fvalue = FormatNumber(xfld, fvalue.ToString());
        //                _sdef.SubmitValue(xfld.FieldName, rowNo, fvalue.ToString(), "", IdValue, 0, 0);
        //                _parser.RegisterVar(xfld.FieldName, xfld.DataType[0].ToString(), fvalue.ToString());
        //            }
        //        }
        //    }
        //    // --- Mode: autogenerate ---
        //    else if (xfld.ModeofEntry == "autogenerate")
        //    {
        //        fvalue = _sdef.GetFieldValue(xfld.FieldName, rowNo);
        //        _sdef.SubmitValue(xfld.FieldName, rowNo, fvalue.ToString(), "", 0, 0, 0);
        //        _parser.RegisterVar(xfld.FieldName, xfld.DataType[0].ToString(), fvalue.ToString());
        //    }
        //    // --- Mode: calculate ---
        //    else if (xfld.ModeofEntry == "calculate" && !string.IsNullOrEmpty(xfld.Cexp))
        //    {
        //        // if (_parser.EvalPrepared(xfld.Exprn))
        //        if (_parser.Evaluate(xfld.Cexp))
        //        {
        //            fvalue = _parser.Value;
        //            if (xfld.DataType == "n") fvalue = FormatNumber(xfld, fvalue.ToString());
        //            _sdef.SubmitValue(xfld.FieldName, rowNo, fvalue.ToString(), "", 0, 0, 0);
        //            _parser.RegisterVar(xfld.FieldName, xfld.DataType[0].ToString(), fvalue.ToString());
        //        }
        //    }

        //    _traceService.LogMessage($"Field Value - {fvalue}");
        //    result = $"{fvalue}`{rowNo}`{IdValue}";
        //    if (!string.IsNullOrEmpty(ErrorStr))
        //        _traceService.LogError(ErrorStr);

        //    return result;
        //}


        //private (string[], DbType[], object[]) ExtractParametersFromSql(string sqlText, int rowNo, bool forPickList)
        //{
        //    List<string> paramNames = new List<string>();
        //    List<DbType> paramTypes = new List<DbType>();
        //    List<object> paramValues = new List<object>();

        //    // Regex pattern to find standard database parameters (e.g., :deptid, @deptid)
        //    // This handles the colon (Oracle) and at-sign (SQL Server, PostgreSQL) prefixes.
        //    Regex paramPattern = new Regex(@"[:\@](\w+)");
        //    MatchCollection matches = paramPattern.Matches(sqlText);

        //    foreach (Match match in matches)
        //    {
        //        // Extract the pure parameter name (e.g., "deptid" from ":deptid")
        //        string paramName = match.Groups[1].Value;

        //        // Get the parameter value from your application's context
        //        string paramValue = GetValue(paramName, rowNo);

        //        // Determine the parameter's DbType based on your application's logic
        //        string fieldType = GetFieldType(paramName, forPickList).ToLower();
        //        DbType paramType = ConvertFieldTypeToDbType(fieldType);

        //        paramNames.Add(paramName);
        //        paramTypes.Add(paramType);

        //        if (paramType == DbType.Int64)
        //        {
        //            paramValues.Add(Convert.ToInt64(paramValue)); 

        //        } else if (paramType == DbType.Double)
        //        {
        //            paramValues.Add(Convert.ToDouble(paramValue));

        //        } else
        //        {
        //            paramValues.Add(paramValue); 
        //        }

        //        // If Required we need to handle this for other types also later

        //    }

        //    return (paramNames.ToArray(), paramTypes.ToArray(), paramValues.ToArray());
        //}


        //private (string[], DbType[], object[]) ExtractParametersFromSql(string sqlText, int rowNo, bool forPickList)
        //{
        //    List<string> paramNames = new List<string>();
        //    List<DbType> paramTypes = new List<DbType>();
        //    List<object> paramValues = new List<object>();

        //    // Regex pattern to find standard database parameters (e.g., :deptid, @deptid)
        //    Regex paramPattern = new Regex(@"[:\@](\w+)");
        //    MatchCollection matches = paramPattern.Matches(sqlText);

        //    foreach (Match match in matches)
        //    {
        //        // Extract the pure parameter name (e.g., "deptid" from ":deptid")
        //        string paramName = match.Groups[1].Value;

        //        // Get the parameter value from your application's context
        //        string paramValue = GetValue(paramName, rowNo);

        //        // Determine the parameter's DbType based on your application's logic
        //        string fieldType = GetFieldType(paramName, forPickList).ToLower();
        //        DbType paramType = ConvertFieldTypeToDbType(fieldType);

        //        paramNames.Add(paramName);
        //        paramTypes.Add(paramType);

        //        // SAFE CONVERSION WITH VALIDATION
        //        if (paramType == DbType.Int64)
        //        {
        //            if (long.TryParse(paramValue, out long longValue))
        //            {
        //                paramValues.Add(longValue);
        //            }
        //            else
        //            {
        //                // Handle conversion failure - use default or log error
        //                paramValues.Add(0L); // Default value
        //                                     // Alternatively, you might want to throw a more specific exception:
        //                                     // throw new FormatException($"Cannot convert '{paramValue}' to Int64 for parameter '{paramName}'");
        //            }
        //        }
        //        else if (paramType == DbType.Double)
        //        {
        //            if (double.TryParse(paramValue, NumberStyles.Any, CultureInfo.InvariantCulture, out double doubleValue))
        //            {
        //                paramValues.Add(doubleValue);
        //            }
        //            else
        //            {
        //                // Handle conversion failure
        //                paramValues.Add(0.0); // Default value
        //            }
        //        }
        //        else if (paramType == DbType.Int32)
        //        {
        //            if (int.TryParse(paramValue, out int intValue))
        //            {
        //                paramValues.Add(intValue);
        //            }
        //            else
        //            {
        //                paramValues.Add(0); // Default value
        //            }
        //        }
        //        else if (paramType == DbType.Decimal)
        //        {
        //            if (decimal.TryParse(paramValue, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal decimalValue))
        //            {
        //                paramValues.Add(decimalValue);
        //            }
        //            else
        //            {
        //                paramValues.Add(0m); // Default value
        //            }
        //        }
        //        else
        //        {
        //            // For string types (DbType.String, DbType.AnsiString, etc.), use as-is
        //            paramValues.Add(paramValue ?? string.Empty);
        //        }
        //    }

        //    return (paramNames.ToArray(), paramTypes.ToArray(), paramValues.ToArray());
        //}

        private DbType ConvertFieldTypeToDbTypeOld(string fieldType)
        {


            // Handle both single-character codes and full type names for backward compatibility
            switch (fieldType?.ToLower())
            {
                // Character/String types
                case "c":
                case "char":
                case "string":
                case "varchar":
                case "text":
                case "nvarchar":
                case "nchar":
                case "clob":
                case "nclob":
                    return DbType.String;

                // Numeric types
                case "n":
                case "int":
                case "integer":
                case "number":
                case "smallint":
                    return DbType.Int64;

                case "l":
                case "bigint":
                case "long":
                    return DbType.Int64;
                case "f":
                case "float":
                case "real":
                    return DbType.Double;
                case "d":
                case "decimal":
                case "numeric":
                case "money":
                    return DbType.Decimal;
                case "double":
                    return DbType.Double;

                // Date/Time types
                case "t":
                case "date":
                case "datetime":
                case "timestamp":
                case "datetime2":
                case "time":
                    return DbType.DateTime;

                // Boolean types
                case "b":
                case "boolean":
                case "bool":
                case "bit":
                    return DbType.Boolean;

                // Binary types
                case "y":
                case "blob":
                case "bfile":
                case "varbinary":
                case "image":
                case "binary":
                    return DbType.Binary;

                // Special types
                case "g":
                case "guid":
                case "uniqueidentifier":
                    return DbType.Guid;

                // Default case - assume string
                default:
                    return DbType.String;
            }
        }

        private DbType ConvertFieldTypeToDbType(string fieldType)
        {


            // Handle both single-character codes and full type names for backward compatibility
            switch (fieldType?.ToLower())
            {
                // Character/String types
                case "c":
                    //case "char":
                    //case "string":
                    //case "varchar":
                    //case "text":
                    //case "nvarchar":
                    //case "nchar":
                    //case "clob":
                    //case "nclob":
                    return DbType.String;

                // Numeric types
                case "n":
                    //case "int":
                    //case "integer":
                    //case "number":
                    //case "smallint":
                    return DbType.Double;

                //case "l":
                //case "bigint":
                //case "long":
                //    return DbType.Int64;
                //case "f":
                //case "float":
                //case "real":
                //    return DbType.Double;
                //case "d":
                //case "decimal":
                //case "numeric":
                //case "money":
                //    return DbType.Decimal;
                //case "double":
                //    return DbType.Double;

                // Date/Time types
                case "d":
                    //case "date":
                    //case "datetime":
                    //case "timestamp":
                    //case "datetime2":
                    //case "time":
                    return DbType.DateTime;

                // Boolean types
                //case "b":
                //case "boolean":
                //case "bool":
                //case "bit":
                //    return DbType.Boolean;

                // Binary types
                //case "y":
                //case "blob":
                //case "bfile":
                //case "varbinary":
                //case "image":
                //case "binary":
                //    return DbType.Binary;

                // Special types
                //case "g":
                //case "guid":
                //case "uniqueidentifier":
                //    return DbType.Guid;

                // Default case - assume string
                default:
                    return DbType.String;
            }
        }
        //private string ConvertToParameterizedSql(string sqlText, string dbType)
        //{
        //    // The input SQL is already parameterized. We just need to ensure the
        //    // parameter prefix matches the target database.
        //    Regex paramPattern = new Regex(@"[:\@](\w+)");

        //    string parameterizedSql = paramPattern.Replace(sqlText, match =>
        //    {
        //        string pureParamName = match.Groups[1].Value; // e.g., "deptid"

        //        return dbType.ToLower() switch
        //        {
        //            "oracle" => $":{pureParamName}", // Oracle uses :param
        //            "postgres" or "postgresql" or "postgre" or "mssql" or "sqlserver" => $"@{pureParamName}", // PostgreSQL & MSSQL use @param
        //            _ => $"@{pureParamName}" // Default to @param
        //        };
        //    });

        //    return parameterizedSql;
        //}



        //public async Task<SQLResult> QueryOpen(TField xfld, int RowNo, string FName, string FValue, bool forPickList, bool NonDynamicFilterSQL, string DynamicSQL)
        //{

        //    //var connectionString = _config.GetConnectionString("WebApiDatabase");
        //    string dbType = _dbHelper.GetDBType();

        //    Console.WriteLine($"Database Type: {dbType}");
        //    _traceService.LogMessage($"Database Type {dbType}");

        //    //IDbHelper dbHelper = DBHelper.CreateDbHelper(dbType);

        //    string s1 = xfld.SQLText;

        //    // Handle dynamic SQL starting with colon
        //    if (s1.StartsWith(":"))
        //    {
        //        s1 = s1.Substring(1);
        //        string dSql = GetValue(s1, 1);
        //        xfld.SQLText = GetValue(s1, 1);
        //    }

        //    // Extract parameters from SQL text
        //    var (paramNames, paramTypes, paramValues) = ExtractParametersFromSql(xfld.SQLText, RowNo, forPickList);

        //    // Convert to parameterized SQL based on database type
        //    string finalSql = ConvertToParameterizedSql(xfld.SQLText, dbType);

        //    // Determine SQL type and execute accordingly
        //    SQLResult result = new SQLResult();
        //    string sqlLower = finalSql.Trim().ToLower();

        //    if (sqlLower.StartsWith("select"))
        //    {
        //        // For SELECT queries
        //        result = await _dbHelper.ExecuteSQLAsync(finalSql, paramNames, paramTypes, paramValues);

        //    }
        //    else if (sqlLower.StartsWith("insert") || sqlLower.StartsWith("update") || sqlLower.StartsWith("delete") || sqlLower.StartsWith("merge"))
        //    {
        //        // For DML queries (INSERT, UPDATE, DELETE, MERGE)
        //       NonQueryResult nonQueryResult = await _dbHelper.ExecuteNonQueryAsync(finalSql, paramNames, paramTypes, paramValues);

        //        // Convert NonQueryResult to SQLResult
        //        result.count = nonQueryResult.count;
        //        result.error = nonQueryResult.error;
        //        result.data = new DataTable(); // Empty data table for non-select queries

        //        if (!string.IsNullOrEmpty(nonQueryResult.error))
        //        {
        //            _traceService.LogError($"NonQuery Error: {nonQueryResult.error}");
        //        }
        //        else
        //        {
        //            _traceService.LogMessage($"DML Operation Successful: {result.count} rows affected");
        //        }
        //    }
        //    else
        //    {
        //        // For other SQL statements (stored procedures, etc.)
        //        result = await _dbHelper.ExecuteSQLAsync(finalSql, paramNames, paramTypes, paramValues);
        //    }

        //    return result;
        //}

        public async Task<SQLResult> QueryOpen(TField xfld, int RowNo, string FName, string FValue, bool forPickList, bool NonDynamicFilterSQL, string DynamicSQL, string sqlText)
        {


            string dbType = _dbHelper.GetDBType();
            string s1;
            Console.WriteLine($"Database Type: {dbType}");
            _traceService.LogMessage($"Database Type {dbType}");

            if (string.IsNullOrEmpty(sqlText))
            {
                s1 = xfld.SQLText;

            }
            else
            {
                s1 = sqlText;
            }



            // Handle dynamic SQL starting with colon
            if (s1.StartsWith(":"))
            {
                s1 = s1.Substring(1);
                string dSql = GetValue(s1, 1);
                xfld.SQLText = GetValue(s1, 1);
            }


            string fldSql;

            if (!string.IsNullOrEmpty(DynamicSQL.Trim()))
            {
                fldSql = FillCompanyParams(RowNo, FName, FValue, DynamicSQL, forPickList);

            }
            else
            {
                if (string.IsNullOrEmpty(sqlText))
                {
                    fldSql = xfld.SQLText;

                }
                else
                {
                    fldSql = sqlText;
                }


            }



            // FIRST: Process dynamic filters and extract parameters
            //var (processedSql, paramNames, paramTypes, paramValues) = ProcessSqlWithDynamicFilters(xfld.SQLText, RowNo, forPickList, dbType);
            var (processedSql, paramNames, paramTypes, paramValues) = ProcessSqlWithDynamicFilters(fldSql, RowNo, forPickList, dbType);

            // SECOND: Convert to parameterized SQL based on database type
            string finalSql = ConvertToParameterizedSql(processedSql, dbType);

            // Determine SQL type and execute accordingly
            SQLResult result = new SQLResult();
            string sqlLower = finalSql.Trim().ToLower();

            if (sqlLower.StartsWith("select"))
            {
                result = await _dbHelper.ExecuteSQLAsync(finalSql, paramNames, paramTypes, paramValues);
            }
            else if (sqlLower.StartsWith("insert") || sqlLower.StartsWith("update") || sqlLower.StartsWith("delete") || sqlLower.StartsWith("merge"))
            {
                NonQueryResult nonQueryResult = await _dbHelper.ExecuteNonQueryAsync(finalSql, paramNames, paramTypes, paramValues);
                result.count = nonQueryResult.count;
                result.error = nonQueryResult.error;
                result.data = new DataTable();

                if (!string.IsNullOrEmpty(nonQueryResult.error))
                {
                    _traceService.LogError($"NonQuery Error: {nonQueryResult.error}");
                }
                else
                {
                    _traceService.LogMessage($"DML Operation Successful: {result.count} rows affected");
                }
            }
            else
            {
                result = await _dbHelper.ExecuteSQLAsync(finalSql, paramNames, paramTypes, paramValues);
            }

            return result;
        }

        public bool IsDouble(string bvalue)
        {
            if (string.IsNullOrWhiteSpace(bvalue))
                return false;

            // Try parse as a number
            if (double.TryParse(bvalue, out _))
            {
                // Return true only if it contains a decimal point
                return bvalue.Contains('.');
            }

            return false;
        }

        private (string processedSql, string[] paramNames, DbType[] paramTypes, object[] paramValues)
            ProcessSqlWithDynamicFilters(string sqlText, int rowNo, bool forPickList, string dbType)
        {
            List<string> allParamNames = new List<string>();
            List<DbType> allParamTypes = new List<DbType>();
            List<object> allParamValues = new List<object>();

            // Process dynamic filters first
            string processedSql = ProcessDynamicFilters(sqlText, allParamNames, allParamTypes, allParamValues, rowNo, forPickList, dbType);

            // Then extract regular parameters from the processed SQL
            //Regex paramPattern = new Regex(@"[:\@](\w+)");
            // Create a regular expression to find SQL parameters in a query.
            // Matches parameters that start with ':' or '@' — e.g., :param or @param
            // BUT ignores SQL type casts like '::text' by using a negative lookbehind.
            //
            // Explanation of the pattern:
            //   (?<!:)   → Negative lookbehind: ensures the ':' or '@' is NOT preceded by another ':'
            //   ([:@])   → Matches a single ':' or '@' character (parameter prefix)
            //   (\w+)    → Captures one or more word characters (letters, digits, or underscores) as the parameter name
            //
            Regex paramPattern = new Regex(@"(?<!:)([:@])(\w+)");
            MatchCollection matches = paramPattern.Matches(processedSql);

            foreach (Match match in matches)
            {
                string paramName = match.Groups[2].Value;
                //string paramName = match.Value;

                // Skip if already added from dynamic filter
                if (allParamNames.Contains(paramName))
                    continue;

                string paramValue = GetValue(paramName, rowNo);
                string fieldType = GetFieldType(paramName, forPickList).ToLower();
                DbType paramType;



                if (fieldType == "n")
                {
                    if (string.IsNullOrEmpty(paramValue)) paramValue = "0";
                    if (IsDouble(paramValue))
                    {
                        paramType = DbType.Double;
                    }
                    else
                    {
                        paramType = DbType.Int64;
                    }

                }
                else
                {
                    paramType = ConvertFieldTypeToDbType(fieldType);
                }

                allParamNames.Add(paramName);
                allParamTypes.Add(paramType);

                // Safe conversion
                object convertedValue = ConvertParameterValue(paramValue, paramType, paramName);
                allParamValues.Add(convertedValue);
            }

            return (processedSql, allParamNames.ToArray(), allParamTypes.ToArray(), allParamValues.ToArray());
        }

        private string ProcessDynamicFilters(string sqlText, List<string> paramNames, List<DbType> paramTypes, List<object> paramValues, int rowNo, bool forPickList, string dbType)
        {
            string processedSql = sqlText;

            // Find all dynamic filters
            var dynamicFilterMatches = Regex.Matches(sqlText, @"{dynamicfilter\s+([^}]+)}");

            foreach (Match match in dynamicFilterMatches)
            {
                string fullMatch = match.Groups[0].Value;
                string filterContent = match.Groups[1].Value;

                // Format: a.itemdesc,a.itemcode~ProductName,ProductCode
                var parts = filterContent.Split('~');
                if (parts.Length != 2)
                {
                    processedSql = processedSql.Replace(fullMatch, "");
                    continue;
                }

                string columnNames = parts[0].Trim();
                string parameterNames = parts[1].Trim();

                var columns = columnNames.Split(',').Select(c => c.Trim()).ToArray();
                var parameters = parameterNames.Split(',').Select(p => p.Trim()).ToArray();

                if (columns.Length != parameters.Length)
                {
                    processedSql = processedSql.Replace(fullMatch, "");
                    continue;
                }

                // Build conditions and add parameters
                var conditions = new List<string>();
                for (int i = 0; i < columns.Length; i++)
                {
                    if (!string.IsNullOrEmpty(parameters[i]))
                    {
                        string paramName = parameters[i];
                        string paramValue = GetValue(paramName, rowNo);

                        // Only add filter if parameter value is not empty
                        if (!string.IsNullOrEmpty(paramValue))
                        {
                            // Choose the appropriate parameter prefix based on database type
                            string paramPrefix = dbType.ToLower() switch
                            {
                                "oracle" => ":",
                                _ => "@" // PostgreSQL, SQL Server, etc.
                            };

                            conditions.Add($"LOWER({columns[i]}) LIKE LOWER({paramPrefix}{paramName})");

                            // Add to parameters list with wildcards
                            paramNames.Add(paramName);
                            paramTypes.Add(DbType.String);
                            paramValues.Add($"%{paramValue}%"); // Add wildcards for LIKE search
                        }
                    }
                }

                string whereClause = conditions.Count > 0 ? string.Join(" OR ", conditions) : "";
                string replacement = !string.IsNullOrEmpty(whereClause) ? $"AND ({whereClause})" : "";

                processedSql = processedSql.Replace(fullMatch, replacement);
            }

            return processedSql;
        }

        //private string ConvertToParameterizedSql(string sqlText, string dbType)
        //{
        //    // This method now only handles standard parameters, dynamic filters are already processed
        //    Regex paramPattern = new Regex(@"[:\@](\w+)");

        //    string parameterizedSql = paramPattern.Replace(sqlText, match =>
        //    {
        //        string pureParamName = match.Groups[1].Value;

        //        return dbType.ToLower() switch
        //        {
        //            "oracle" => $":{pureParamName}",
        //            "postgres" or "postgresql" or "postgre" or "mssql" or "sqlserver" => $"@{pureParamName}",
        //            _ => $"@{pureParamName}"
        //        };
        //    });

        //    return parameterizedSql;
        //}

        private string ConvertToParameterizedSql(string sqlText, string dbType)
        {
            // Match parameters starting with a single ':' or '@' — but not '::'
            Regex paramPattern = new Regex(@"(?<!:)([:@])(\w+)", RegexOptions.Compiled);

            string parameterizedSql = paramPattern.Replace(sqlText, match =>
            {
                string pureParamName = match.Groups[2].Value; // parameter name
                string prefix = match.Groups[1].Value;        // ':' or '@'

                return dbType.ToLower() switch
                {
                    "oracle" => $":{pureParamName}",
                    "postgres" or "postgresql" or "postgre" or "mssql" or "sqlserver" => $"@{pureParamName}",
                    _ => $"@{pureParamName}"
                };
            });

            return parameterizedSql;
        }

        // Helper method for safe parameter conversion (from previous solution)
        // Note: We need to handle Decimal

        private object ConvertParameterValue(string value, DbType dbType, string paramName)
        {
            if (string.IsNullOrEmpty(value) || value == "0")
                return GetDefaultValueForType(dbType);

            switch (dbType)
            {
                case DbType.Int64:
                    if (long.TryParse(value, out long longResult))
                        return longResult;
                    throw new FormatException($"Cannot convert '{value}' to Int64 for parameter '{paramName}'");

                case DbType.Int32:
                    if (int.TryParse(value, out int intResult))
                        return intResult;
                    throw new FormatException($"Cannot convert '{value}' to Int32 for parameter '{paramName}'");
                // Double.TryParse converts a string to a double;
                // it removes trailing zeros (e.g., 0.00 becomes 0),
                // so use formatting like .ToString("F2") to preserve two decimal places when displaying.
                case DbType.Double:
                    //if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double doubleResult))
                    return value;
                    throw new FormatException($"Cannot convert '{value}' to Double for parameter '{paramName}'");

                case DbType.Decimal:
                    if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal decimalResult))
                        return decimalResult;
                    throw new FormatException($"Cannot convert '{value}' to Decimal for parameter '{paramName}'");

                case DbType.DateTime:
                    //if (DateTime.TryParse(value, out DateTime dateResult))
                    //    return dateResult;
                    string inputValue = value?.Trim();

                    string[] dateTimeFormats =
                    {
                        $"{_common.ApiDateFormat} {_common.ApiTimeFormat}",
                        _common.ApiDateFormat
                    };

                    if (DateTime.TryParseExact(
                            inputValue,
                            dateTimeFormats,
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.None,
                            out DateTime dateResult))
                    {
                        return dateResult;
                    }
                    throw new FormatException($"Cannot convert '{value}' to DateTime for parameter '{paramName}'");

                case DbType.Boolean:
                    if (bool.TryParse(value, out bool boolResult))
                        return boolResult;
                    throw new FormatException($"Cannot convert '{value}' to Boolean for parameter '{paramName}'");

                default:
                    return value ?? string.Empty;
            }
        }

        //private object ConvertParameterValue(string value, DbType dbType, string paramName)
        //{
        //    if (string.IsNullOrEmpty(value))
        //        return GetDefaultValueForType(dbType);

        //    try
        //    {
        //        switch (dbType)
        //        {
        //            case DbType.Int64:
        //                return long.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out long longResult)
        //                    ? longResult : GetDefaultValueForType(dbType);

        //            case DbType.Int32:
        //                return int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out int intResult)
        //                    ? intResult : GetDefaultValueForType(dbType);

        //            case DbType.Double:
        //                string doubleValue = value.Replace(",", ".");
        //                return double.TryParse(doubleValue, NumberStyles.Any, CultureInfo.InvariantCulture, out double doubleResult)
        //                    ? doubleResult : GetDefaultValueForType(dbType);

        //            case DbType.Decimal:
        //                string decimalValue = value.Replace(",", ".");
        //                return decimal.TryParse(decimalValue, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal decimalResult)
        //                    ? decimalResult : GetDefaultValueForType(dbType);


        //            case DbType.DateTime:
        //                return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dateResult)
        //                    ? dateResult : GetDefaultValueForType(dbType);

        //            case DbType.Boolean:
        //                if (value == "1") return true;
        //                if (value == "0") return false;
        //                return bool.TryParse(value, out bool boolResult) ? boolResult : false;

        //            default:
        //                return value ?? string.Empty;
        //        }
        //    }
        //    catch
        //    {
        //        _logger.LogWarning($"Conversion failed for parameter '{paramName}'. Using default value.");
        //        return GetDefaultValueForType(dbType);
        //    }
        //}



        private object GetDefaultValueForType(DbType dbType)
        {
            switch (dbType)
            {
                case DbType.Int64: return 0L;
                case DbType.Int32: return 0;
                case DbType.Double: return 0.0;
                case DbType.Decimal: return 0m;
                case DbType.DateTime: return DateTime.MinValue;
                case DbType.Boolean: return false;
                default: return string.Empty;
            }
        }



        public string FindAndReplace(string s, string findWhat, string replaceWith)
        {
            string result = s;
            int p = s.IndexOf(findWhat, StringComparison.Ordinal);

            if (p == -1) // No match found
                return result;

            result = "";
            while (p >= 0)
            {
                result += s.Substring(0, p) + replaceWith;
                s = s.Substring(p + findWhat.Length);
                p = s.IndexOf(findWhat, StringComparison.Ordinal);
            }
            result += s;

            return result;

        }


        public string RemoveExtraSpaces(string pString)
        {
            string str = pString.Trim();
            int i = 0;
            while (i < str.Length)
            {
                if (str[i] == ' ' && i + 1 < str.Length && str[i + 1] == ' ')
                {
                    str = str.Remove(i, 1);
                    continue;
                }
                i++;
            }
            return str;
        }

        public string RemoveCommas(string s)
        {
            while (true)
            {
                int k = s.IndexOf(_utils.LocThousandSeparator);
                if (k == -1) break;
                s = s.Remove(k, 1);
            }
            return s;
        }

        public string GetQuotedStr(string DelimitedStr, string Delimiter)
        {
            string result = "";
            if (string.IsNullOrEmpty(Delimiter)) Delimiter = ",";

            int i = 1;
            string s = _utils.GetNthString(DelimitedStr, i, Delimiter);
            while (!string.IsNullOrEmpty(s))
            {
                result += "," + QuoteString(s);
                i++;
                s = _utils.GetNthString(DelimitedStr, i, Delimiter);
            }

            if (!string.IsNullOrEmpty(result))
            {
                result = result.Substring(1); // Remove leading comma
            }

            return result;
        }





        public string FillCompanyParams(int RowNo, string fieldName, string FValue, string DynamicSQL, bool ForPickList)
        {

            string S = DynamicSQL;

            if (string.IsNullOrEmpty(S)) return "";

            S = FindAndReplace(S, "\r\n", " ");
            S = FindAndReplace(S, "\n", " ");
            S = RemoveExtraSpaces(S);
            //S = ChangeSQL(S, FValue, _sdef.GetField(fieldName).Pickfields, 1); 

            while (true)
            {
                int p1 = S.IndexOf('{');
                if (p1 == -1) break;

                int p2 = S.IndexOf('}');
                if (p2 == -1)
                    throw new Exception("Invalid SQL");

                string CName = S.Substring(p1 + 1, p2 - p1 - 1).Trim();
                string CValue = "";
                string LastDataType = "";

                bool isMultiValue = CName.EndsWith("*");
                TField fld1 = new TField();
                if (isMultiValue)
                {
                    CName = CName.Substring(0, CName.Length - 1);


                    fld1 = _sdef.GetField(CName);
                    if (fld1 != null)
                    {
                        if (isMultiValue && _sdef.Frames[fld1.FrameNo - 1].AsGrid)
                        {
                            int rc = _sdef.GetRowCount(fld1.FrameNo);
                            CValue = "";
                            for (int i = 1; i <= rc; i++)
                            {
                                string val = _sdef.GetFieldValue(CName, i);
                                if (i == 1)
                                    LastDataType = fld1.DataType.ToUpper();

                                if (LastDataType == "N")
                                {
                                    val = string.IsNullOrEmpty(val) ? "0" : RemoveCommas(val);
                                    CValue += "," + val;
                                }
                                else
                                {
                                    if (!fld1.UsedQuotedStr)
                                    {
                                        val = FindAndReplace(val, "'", "''");
                                        val = FindAndReplace(val, "\"", "\"\"");
                                    }
                                    if (fld1.ComponentType == "cl" || fld1.Cl_multiselect)
                                        CValue += "," + GetQuotedStr(val, fld1.Separator);
                                    else
                                        CValue += "," + QuoteString(val);
                                }
                            }
                            CValue = CValue.Length > 0 ? CValue.Substring(1) : "";
                            if (string.IsNullOrEmpty(CValue))
                            {
                                CValue = fld1.DataType == "n" ? "0" : QuoteString(CValue);
                            }
                        }
                        else
                        {
                            CValue = GetValue(CName, RowNo);
                            if (string.IsNullOrEmpty(CValue))
                            {
                                CValue = fld1.DataType == "n" ? "0" : QuoteString(CValue);
                            }
                            else
                            {
                                if (fld1.ComponentType == "cl" || fld1.Cl_multiselect)
                                {
                                    if (fld1.DataType == "c")
                                        CValue = _utils.GetQuotedStr(CValue, fld1.Separator);
                                }
                                else
                                {
                                    if (!fld1.UsedQuotedStr)
                                    {
                                        CValue = _utils.FindAndReplace(CValue, "'", "''");
                                        CValue = _utils.FindAndReplace(CValue, "\"", "\"\"");
                                    }
                                    if (fld1.DataType == "c")
                                        CValue = QuoteString(CValue);
                                }
                            }
                        }
                    }
                    else
                    {
                        CValue = GetValue(CName, RowNo);
                        LastDataType = GetFieldType(CName, ForPickList).ToUpper();
                        CValue = LastDataType == "N" ? (string.IsNullOrEmpty(CValue) ? "0" : CValue) : QuoteString(CValue);
                    }
                }
                else
                {
                    if (CName.ToLower().IndexOf("dynamicfilter") == 0)
                    {
                        // TxtSelection = false and PickList = false
                        CValue = "";

                    }
                    else
                    {
                        //fld1 = _sdef.GetField(CName);

                        CValue = GetValue(CName, RowNo);
                        LastDataType = GetFieldType(CName, ForPickList).ToUpper();
                        CValue = LastDataType == "N" ? (string.IsNullOrEmpty(CValue) ? "0" : CValue) : QuoteString(CValue);



                    }



                }


                _traceService.LogMessage($"{CName} = {CValue}");
                S = S.Remove(p1, p2 - p1 + 1);
                S = S.Insert(p1, CValue);
            }

            S = FillCompositeParams(S);

            return S;


        }

        public string FillCompositeParams(string S)
        {
            int p1 = S.IndexOf("(:");
            while (p1 >= 0)
            {
                int p2 = S.IndexOf(")", p1 + 1);
                if (p2 == -1) break; // Safety check to avoid infinite loop if no closing parenthesis

                string CName = S.Substring(p1 + 2, p2 - p1 - 2);
                string CValue = "(" + GetValue(CName, -1) + ")";
                if (CValue == "()")
                {
                    CValue = "('')";
                }

                _traceService.LogMessage($"{CName} = {CValue}");
                S = S.Remove(p1, p2 - p1 + 1);
                S = S.Insert(p1, CValue);

                p1 = S.IndexOf("(:");
            }

            return S;
        }


        public async Task<SQLResult> ValidatePickList(TField fd, string value, int rowno, string ErrorStr, int actualRowNo, bool forPickList, bool nonDynamicFilterSQL)
        {
            string sqlText;
            SQLResult result;
            // If the field is not a selection, exit immediately
            if (!fd.TxtSelection)
            {
                return null; // Assuming exit from Pascal maps to returning false
            }



            // Recreate and re-open the query component to avoid the "Outof present range" bug
            _traceService.LogMessage("Query component recreating and opening again");

            sqlText = ChangeSQL(fd.SQLText, value, fd.Pickfields, 1);

            // Set dynamic SQL if needed
            string dynamicSql = fd.DynamicParams ? sqlText : string.Empty;

            // Handle pagination
            int tpgno = _utils.Pagination_PageNo;
            _utils.Pagination_PageNo = 0;

            // Open the query
            result = await QueryOpen(fd, 1, fd.FieldName, value, forPickList, nonDynamicFilterSQL, dynamicSql, "");

            DataRow row = null;
            if (result.data != null && result.data.Rows.Count > 0)
            {
                row = result.data.Rows[0];
            }

            // Filter the dataset if there are filter columns
            if (fd.FilterCols.Count > 0)
            {

                FilterCDS(fd, result, rowno);
            }

            // Restore pagination
            _utils.Pagination_PageNo = tpgno;

            _traceService.LogMessage("Query opened");



            //result = sqlResult == null || sqlResult.data.Rows.Count == 0;

            if (result == null || result.data.Rows.Count == 0)
            {
                result = null;
                // Set the error message if the value is invalid
                ErrorStr = $"Invalid selection \"{value}\" in {GetFldPrompt(fd, rowno, actualRowNo)}";
            }
            else
            {
                // Clear the error and store data if the value is valid
                ErrorStr = string.Empty;
                var idValue = Convert.ToInt64(0);
                if (fd.SourceKey && row != null)
                {
                    // Assuming the first field is the ID
                    idValue = Convert.ToInt64(row[0].ToString());

                }

                _sdef.SubmitValue(fd.FieldName, rowno, value, "", idValue, 0, 0);
                //result = true; 
            }

            _traceService.LogMessage("Query component recreating and opening completed");

            return result;
        }

        // Placeholder methods - implement as needed
        public bool IsNumeric(string s, string schar)
        {
            bool result = true;
            s = s.Trim().ToLower();

            for (int i = 0; i < s.Length; i++)
            {
                string c = s.Substring(i, 1);
                if (schar.Contains(c))
                {
                    continue;
                }
                else
                {
                    result = false;
                    break;
                }
            }

            return result;
        }





        public async Task<bool> RapidCheckDuplicate(TField fld, int rowNo, double IdValue, string FieldValue)
        {
            bool result = true;
            string fname = fld.FieldName;

            if (fld.NoDuplicate && !fld.AsGrid)
            {
                if ((fld.ModeofEntry == "autogenerate" &&
                    ((_utils.ServiceName != "submitdata" && _utils.ServiceName != "rapidsave") && //TODO: This Condition needs to be checked
                    (!_sdef.NoAutoGenStr.Contains($";{fname};"))) ||
                    string.IsNullOrEmpty(fld.Tablename)))
                {
                    return false;
                }



                string tName = $"{_sdef.CompanyName}{fld.Tablename}";
                string idField = $"{fld.Tablename}ID";
                string sqlText = fld.SQLText;

                if (fld.DataType == "c")
                {
                    if (IdValue == 0)
                    {

                        sqlText = $"Select {fld.FieldName} from {tName} where {_utils.SQLLower}({fld.FieldName}) = :{fld.FieldName}";

                    }
                    else
                    {
                        sqlText = $"Select {fld.FieldName} from {tName} where {_utils.SQLLower}({fld.FieldName}) = {IdValue}";
                    }

                }
                else if (fld.DataType == "n")
                {
                    //fcheck.CDS.CommandText = $"Select {fld.FieldName} from {tName} where {fld.FieldName} = :S";
                    sqlText = $"Select {fld.FieldName} from {tName} where {fld.FieldName} = :{fld.FieldName}";
                    //fcheck.AssignParam(0, FieldValue, "n");
                }
                else if (fld.DataType == "d")
                {
                    //if (string.IsNullOrEmpty(FieldValue))
                    //{
                    //    sqlText = $"Select {fld.FieldName} from {tName} where {fld.FieldName} = :{fld.FieldName}";

                    //} 
                    //else

                    sqlText = $"Select {fld.FieldName} from {tName} where {fld.FieldName} = :{fld.FieldName}";





                }

                if (_sdef.LastSavedRecordid != 0)
                {

                    sqlText = sqlText + " and " + idField + " <> " + _sdef.GetRecordId(fld, rowNo).ToString();
                }

                var tempSqlResult = await QueryOpen(fld, rowNo, fld.FieldName, fld.FieldValue, false, true, "", sqlText);

                _traceService.LogDataTable(tempSqlResult.data);
                if (tempSqlResult != null && tempSqlResult.data != null && tempSqlResult.data.Rows.Count > 0)
                {
                    result = true;
                }
                else
                {
                    result = false;
                }



                return result;
            }
            return false;
        }





        public string GetFldPrompt(TField fld, int rowNo, int ActualRowNo)
        {
            string result = "";
            string fieldCaption = fld.Caption;

            if (string.IsNullOrEmpty(fieldCaption))
            {
                result = fld.Hint;
            }
            else
            {
                if (fieldCaption.Contains("~"))
                {
                    fieldCaption = _utils.FindAndReplace(fieldCaption, "~", " ");
                }
                result = fieldCaption;
            }

            if (fld.AsGrid)
            {
                if (_sdef.Frames[fld.FrameNo - 1].Popup)
                {
                    result = $"{result} Parent Row {_parser.GetVarValue("activeprow")}, Popgrid Row {ActualRowNo} ";
                }
                else
                {
                    result = $"{result} Row {rowNo} ";
                }
            }

            return result;
        }

        public void GetParentValue(int frmno, int rowno, string pFields, TStringList ParentList)
        {
            ParentList.Clear();
            int i = 1;
            string pField = _utils.GetNthString(pFields, i).Trim();

            while (!string.IsNullOrEmpty(pField))
            {
                string fname = $"sub{frmno}_{pField}";
                dynamic tfld = _sdef.GetField(pField);
                if (tfld != null)
                    pField = tfld.FieldName;

                string val = _sdef.GetFieldValue(fname, rowno);
                if (tfld?.DataType == "n" && string.IsNullOrWhiteSpace(val))
                    val = "0";

                ParentList.Add($"{pField}={val}");
                _traceService.LogMessage($"Parent Values = {pField}={val}");

                i++;
                pField = _utils.GetNthString(pFields, i).Trim();
            }
        }


        public void GetParentActiveRow(int rowNo, int popIndex, string ErrorStr, TStringList ParentList)
        {

            dynamic popGrid = _sdef.PopGrids[popIndex];
            string s = popGrid.Parent;
            int pfrmNo = int.Parse(s.Substring(2, 3));
            GetParentValue(popGrid.FrameNo, rowNo, popGrid.ParentField, ParentList);
            int rc = _sdef.GetRowCount(pfrmNo);
            _traceService.LogMessage($"Parent Row count = {rc}");
            bool validRow = false;
            for (int i = 1; i <= rc; i++)
            {

                for (int j = 0; j < ParentList.Count; j++)
                {
                    dynamic tfld = _sdef.GetField(ParentList[i]);
                    if (tfld.DataType == "n")
                    {
                        string fieldValue = _sdef.GetFieldValue(ParentList[j], i);
                        if (string.IsNullOrEmpty(fieldValue))
                        {
                            validRow = false;
                            break;
                        }
                        else
                        {
                            double sdval = _utils.StrToFloatZ(fieldValue);
                            double pval = string.IsNullOrEmpty(ParentList.GetValueFromIndex(j))
                                ? 0
                                : _utils.StrToFloatZ(ParentList.GetValueFromIndex(j));
                            if (sdval != pval)
                            {
                                validRow = false;
                                break;
                            }
                            else
                            {
                                validRow = true;
                            }
                        }
                    }
                    else
                    {
                        if (_sdef.GetFieldValue(ParentList.GetName(j), i).ToLower() != ParentList.GetValueFromIndex(j).ToLower())
                        {
                            validRow = false;
                            break;
                        }
                        else
                        {
                            validRow = true;
                        }
                    }
                }

                if (validRow)
                {
                    RegRow(pfrmNo, i, ParentList);
                    _parser.RegisterVar("activeprow", "n", i.ToString());
                    _traceService.LogMessage($"Parent Row = {i}");
                    return;
                }
            }

            if (!validRow)
            {
                ErrorStr = "Active parentrow is not found.";
            }
        }

        public void RegRow(int frameNo, int rowNo, TStringList ParentList)
        {

            bool rowFound = false;

            dynamic fm = _sdef.Frames[frameNo - 1];
            _parser.RegisterVar("activerow", "n", rowNo.ToString());
            int prow = int.Parse(_parser.GetVarValue("activeprow").ToString());

            for (int i = fm.StartIndex; i < fm.StartIndex + fm.FieldCount; i++)
            {
                TField fd = _sdef.Flds[i];

                for (int j = 0; j < fd.DataRows.Count; j++)
                {
                    int k = int.Parse(fd.DataRows[j]);
                    if (_sdef.FieldList[k].RowNo == rowNo)
                    {
                        string value = _sdef.FieldList[k].Value;
                        _parser.RegisterVar(_sdef.FieldList[k].FieldName, _sdef.FieldList[k].DataType[0].ToString(), value);
                        rowFound = true;
                        break;
                    }
                }
                if (!rowFound)
                    _parser.RegisterVar(fd.FieldName, fd.DataType[0].ToString(), "");
            }

            rowFound = false;
            if (fm.Popup)
            {
                _parser.RegisterVar("activeprow", "n", prow.ToString());
                fm = (TFrame)_sdef.Frames[
             _sdef.PopGrids[fm.PopIndex].ParentFrameNo - 1
          ];
                for (int i = fm.StartIndex; i < fm.StartIndex + fm.FieldCount; i++)
                {
                    TField fd = _sdef.Flds[i];
                    for (int j = 0; j < fd.DataRows.Count; j++)
                    {
                        int k = int.Parse(fd.DataRows[j]);
                        if (_sdef.FieldList[k].RowNo == prow)
                        {
                            string value = _sdef.FieldList[k].Value;
                            _parser.RegisterVar(_sdef.FieldList[k].FieldName, _sdef.FieldList[k].DataType[0].ToString(), value);
                            rowFound = true;
                            break;
                        }
                    }
                    if (!rowFound)
                        _parser.RegisterVar(fd.FieldName, fd.DataType[0].ToString(), "");
                }
            }
        }


        public void SetUsedQuotedStrProp(TField xfld)
        {
            if (!_utils.UsedQuotedStr) return;
            xfld.UsedQuotedStr = true;
            _utils.UsedQuotedStr = false;
        }

        public bool RapidDuplicateInGrid(int rowNo, TField fld, string FieldValue)
        {
            bool result = true;
            string fname = fld.FieldName.ToLower().Trim();

            for (int i = 0; i < fld.FieldValList.Count; i++)
            {
                if (int.Parse(fld.FieldValList.GetName(i)) == rowNo) continue;
                if (fld.FieldValList.GetValueFromIndex(i).Trim().ToLower() == FieldValue.Trim().ToLower())
                {
                    result = false;
                    break;
                }
            }

            return result;
        }

        public string GetErrorStr(string m, int rowNo, int actualRowNo, TField fld, bool DeleteRowOnError)
        {
            if (m.StartsWith("--"))
            {
                if (!DeleteRowOnError)
                    DeleteRowOnError = true;
                return $"{GetFldPrompt(fld, rowNo, actualRowNo)} {m.Substring(2)}";
            }
            return $"{GetFldPrompt(fld, rowNo, actualRowNo)} {m}";
        }

        public void SubmitValue(TField fld, int rowNo, string fldValue, string oldFldValue, Int64 idVal, Int64 oldIdVal, Int64 recId)
        {
            if (fld != null)
            {
                _sdef.SetFieldValue(fld, rowNo, fldValue, oldFldValue, idVal, oldIdVal, recId);
            }
            RegisterToParser(fld, fldValue);
        }

        private void RegisterToParser(TField fld, string fldValue)
        {
            if (fld != null)
            {
                _parser.RegisterVar(fld.FieldName, fld.DataType[0].ToString(), fldValue);


            }
        }

        public string ChangeSQL(string sqlText, string searchValue, List<string> searchFldList, int searchIdx)
        {
            if (string.IsNullOrEmpty(sqlText))
                return sqlText;

            // Exit if "dynamicfilter" is not found
            if (sqlText.ToLower().IndexOf("dynamicfilter") == -1)
                return sqlText;

            string result = sqlText;
            int l = sqlText.Length;

            // Process SearchValue if it contains both '"' and ':'
            if (searchValue.Contains("\"") && searchValue.Contains(":"))
                searchValue = MakeProperValue(searchValue);

            // Register variable for SQL parameterization
            _parser.RegisterVar("axp_dynamicfilter", "c", searchValue);

            int k = -1;
            int i = sqlText.ToLower().IndexOf("{dynamicfilter");
            string sStr = i >= 0 ? sqlText.Substring(0, i) : sqlText;

            while (true)
            {
                i = result.ToLower().IndexOf("{dynamicfilter");
                if (i == -1)
                    break;

                string tail = result.Substring(i);
                int j = tail.IndexOf('}');
                if (j == -1)
                    break; // No closing brace, exit to avoid infinite loop

                k++;
                // Get the field name from searchFldList
                string searchField = _utils.GetNthString(searchFldList[k], searchIdx);
                string replaceWith = $"{_utils.SQLLower}({searchField}) = {_utils.SQLLower}(:axp_dynamicfilter)";

                // Determine if we need "where" or "and"
                string s = sStr.ToLower().Contains("where") ? $" and {replaceWith}" : $" where {replaceWith}";

                // Update sStr for the next iteration
                sStr = result.Substring(i + j);

                // Replace {dynamicfilter...} with the new clause
                result = result.Remove(i, j + 1).Insert(i, s);

                // Update sStr for the next iteration
                i = sStr.ToLower().IndexOf("{dynamicfilter");
                if (i >= 0)
                    sStr = sStr.Substring(0, i);
            }

            return result;
        }

        public async Task<bool> RapidValidatePickList(TField fd, string value, int rowNo, bool fieldEmpty, SQLResult sqlResult, string DynamicSql, string ErrorStr, string FieldValue, Int64 IdValue, int actualRowNo, bool ForPickList)
        {
            bool result;
            if (!fd.TxtSelection)
                return true;

            DataRow row = null;

            if (sqlResult.data != null && sqlResult.data.Rows.Count > 0)
            {
                row = sqlResult.data.Rows[0];

            }



            string commandText;

            commandText = ChangeSQL(fd.SQLText, value, fd.Pickfields, 1);

            if (fd.DynamicParams)
                DynamicSql = commandText;
            else
                DynamicSql = "";

            int tpgno = _utils.Pagination_PageNo;
            _utils.Pagination_PageNo = 0;
            await RapidQueryOpen(fd, sqlResult, 1, fd.FieldName, value, DynamicSql, ForPickList, rowNo);
            _utils.Pagination_PageNo = tpgno;
            _traceService.LogMessage("Query opened");

            result = sqlResult?.data?.Rows?.Count > 0;

            if (!result)
            {
                if (value != "Invalid item")
                    ErrorStr = $"\"{value}\" is not a valid {GetFldPrompt(fd, rowNo, actualRowNo)}";

                if (value != "Invalid item")
                {
                    FieldValue = "Invalid item";
                    fd.EnterFieldProc?.Invoke(rowNo, fieldEmpty);
                }
                else if (value == "Invalid item")
                {
                    _sdef.SetFieldValue(fd, rowNo, FieldValue, "", IdValue, 0, 0);
                }
            }
            else
            {
                if (fd.SourceKey)
                    IdValue = Convert.ToInt64(row[0]);
                _sdef.SetFieldValue(fd, rowNo, value, "", IdValue, 0, 0);
            }

            _traceService.LogMessage("Query component recreating and opening completed");
            return result;
        }

        public bool CheckWidth(TField fld, string FieldValue)
        {
            bool result = true;
            if (fld.ComponentType == "cl") return result;

            int w = fld.Width;
            int d = fld.Dec;
            string t = fld.DataType;
            string s = FieldValue;

            if (t == "c")
            {
                result = s.Trim().Length <= w;
            }
            else if (t == "n")
            {
                s = _utils.RemoveCommas(s).Trim();
                s = FormatNumber(fld, s);
                if (!string.IsNullOrEmpty(s))
                    s = Math.Abs(double.Parse(s)).ToString();

                int l = s.Length;
                int p = s.IndexOf(_utils.LocDecimalSeparator);

                if (d == 0)
                {
                    result = l <= w;
                    if (p != -1) result = false;
                }
                else
                {
                    if (p == -1) p = l;
                    result = (p - 1) <= w;
                    if (result)
                        result = (l - p - 1) <= d;
                }
            }

            return result;
        }

        private DbType ConvertStringToDbType(string typeString)
        {
            return typeString.ToLower() switch
            {
                "n" or "number" or "numeric" => DbType.Decimal,
                "s" or "string" or "varchar" => DbType.String,
                "d" or "date" or "datetime" => DbType.DateTime,
                "i" or "int" or "integer" => DbType.Int32,
                "b" or "bool" or "boolean" => DbType.Boolean,
                _ => DbType.String // Default fallback
            };
        }

        //public async void RapidQueryOpen(TField fld, SQLResult sqlResult, int rowNo, string fName, string fValue, string DynamicSQL, bool ForPickList, int RowNo)
        //{
        //    string paramData = "";
        //    string dbType = _dbHelper.GetDBType();

        //    var (paramNames, paramTypes, paramValues) = ExtractParametersFromSql(fld.SQLText, RowNo, ForPickList);

        //    RapidFillCompanyParams(rowNo, fName, fValue, DynamicSQL, ForPickList);
        //    string finalSql = ConvertToParameterizedSql(fld.SQLText, dbType);

        //    if (paramNames.Length == 0)
        //    {
        //        if (sqlResult.data != null)
        //        {
        //            if (fld.SQLText.ToLower().StartsWith("select"))
        //            {
        //                if (fValue == "~")
        //                {
        //                    sqlResult = await _dbHelper.ExecuteSQLAsync(finalSql, paramNames, paramTypes, paramValues);
        //                    _traceService.LogMessage($"{_utils.AxpLogStr} SQL CommandText : {finalSql}");
        //                    _traceService.LogMessage($"{_utils.AxpLogStr} ParamData : {paramData}");
        //                    return;
        //                }

        //                sqlResult = await _dbHelper.ExecuteSQLAsync(finalSql, paramNames, paramTypes, paramValues);
        //                _traceService.LogMessage($"{_utils.AxpLogStr} SQL CommandText : {finalSql}");
        //                _traceService.LogMessage($"{_utils.AxpLogStr} ParamData : {paramData}");
        //            }
        //            else
        //            {
        //                await _dbHelper.ExecuteNonQueryAsync(finalSql, paramNames, paramTypes, paramValues);
        //            }

        //            paramData = "**";
        //            if (!fld.QParamvalues.Contains(paramData))
        //                fld.QParamvalues.Add(paramData);
        //        }
        //    }
        //    else
        //    {
        //        // Process parameters from extracted data
        //        for (int i = 0; i < paramNames.Length; i++)
        //        {
        //            string paramName = paramNames[i];
        //            bool idParam = false;
        //            if (paramName.StartsWith("id__"))
        //            {
        //                idParam = true;
        //                paramName = paramName.Substring(4);
        //            }

        //            string paramValue = RapidGetValue(paramName, rowNo);
        //            string t = idParam ? "n" : GetFieldType(paramName, ForPickList).ToLower();

        //            DbType convertedDbType = ConvertStringToDbType(t);

        //            // Update the parameter values
        //            paramValues[i] = paramValue;
        //            paramTypes[i] = convertedDbType;

        //            fld.QParamnames.Add(paramName);
        //            paramData += "!@$" + paramValue + "!@$";
        //        }

        //        if (!string.IsNullOrEmpty(paramData))
        //        {
        //            if (!fld.QParamvalues.Contains(paramData))
        //                fld.QParamvalues.Add(paramData);
        //        }

        //        // Execute query with parameters
        //        if (fld.SQLText.Trim().ToLower().StartsWith("select"))
        //        {
        //            if (fValue == "~")
        //            {
        //                sqlResult = await _dbHelper.ExecuteSQLAsync(finalSql, paramNames, paramTypes, paramValues);
        //                _traceService.LogMessage($"{_utils.AxpLogStr} SQL CommandText : {finalSql}");
        //                _traceService.LogMessage($"{_utils.AxpLogStr} ParamData : {paramData}");
        //                return;
        //            }

        //            if (_utils.Pagination_PageNo == 1)
        //            {
        //                string originalSql = finalSql;
        //                string paginatedSql = _utils.ChangeSqlForPagination(originalSql, true);
        //                if (!string.Equals(originalSql, paginatedSql, StringComparison.OrdinalIgnoreCase))
        //                {
        //                     sqlResult = await _dbHelper.ExecuteSQLAsync(paginatedSql, paramNames, paramTypes, paramValues);
        //                    _traceService.LogMessage($"{_utils.AxpLogStr} SQL CommandText : {paginatedSql}");
        //                    _traceService.LogMessage($"{_utils.AxpLogStr} ParamData : {paramData}");
        //                }
        //                finalSql = originalSql;
        //            }

        //            finalSql = _utils.ChangeSqlForPagination(finalSql, false);
        //            sqlResult = await _dbHelper.ExecuteSQLAsync(finalSql, paramNames, paramTypes, paramValues);
        //            _traceService.LogMessage($"{_utils.AxpLogStr} SQL CommandText : {finalSql}");
        //            _traceService.LogMessage($"{_utils.AxpLogStr} ParamData : {paramData}");
        //        }
        //        else
        //        {
        //            await _dbHelper.ExecuteNonQueryAsync(finalSql, paramNames, paramTypes, paramValues);
        //        }
        //    }
        //}

        public async Task RapidQueryOpen(TField fld, SQLResult sqlResult, int rowNo, string fName, string fValue, string DynamicSQL, bool ForPickList, int RowNo)
        {
            string paramData = "";
            string dbType = _dbHelper.GetDBType();

            // Use the new ProcessSqlWithDynamicFilters method instead of ExtractParametersFromSql
            var (processedSql, paramNames, paramTypes, paramValues) = ProcessSqlWithDynamicFilters(fld.SQLText, RowNo, ForPickList, dbType);

            RapidFillCompanyParams(rowNo, fName, fValue, DynamicSQL, ForPickList);

            // Convert to parameterized SQL (dynamic filters are already processed)
            string finalSql = ConvertToParameterizedSql(processedSql, dbType);

            if (paramNames.Length == 0)
            {
                if (sqlResult.data != null)
                {
                    if (processedSql.ToLower().StartsWith("select"))
                    {
                        if (fValue == "~")
                        {
                            sqlResult = await _dbHelper.ExecuteSQLAsync(finalSql, paramNames, paramTypes, paramValues);
                            _traceService.LogDataTable(sqlResult.data);
                            _traceService.LogMessage($"{_utils.AxpLogStr} SQL CommandText : {finalSql}");
                            _traceService.LogMessage($"{_utils.AxpLogStr} ParamData : {paramData}");
                            return;
                        }

                        sqlResult = await _dbHelper.ExecuteSQLAsync(finalSql, paramNames, paramTypes, paramValues);
                        _traceService.LogMessage($"{_utils.AxpLogStr} SQL CommandText : {finalSql}");
                        _traceService.LogMessage($"{_utils.AxpLogStr} ParamData : {paramData}");
                    }
                    else
                    {
                        await _dbHelper.ExecuteNonQueryAsync(finalSql, paramNames, paramTypes, paramValues);
                    }

                    paramData = "**";
                    if (!fld.QParamvalues.Contains(paramData))
                        fld.QParamvalues.Add(paramData);
                }
            }
            else
            {
                // Process parameters from extracted data
                for (int i = 0; i < paramNames.Length; i++)
                {
                    string paramName = paramNames[i];
                    bool idParam = false;
                    if (paramName.StartsWith("id__"))
                    {
                        idParam = true;
                        paramName = paramName.Substring(4);
                    }

                    string paramValue = RapidGetValue(paramName, rowNo);
                    string t = idParam ? "n" : GetFieldType(paramName, ForPickList).ToLower();

                    DbType convertedDbType = ConvertStringToDbType(t);

                    // Update the parameter values with safe conversion
                    object safeValue = ConvertParameterValue(paramValue, convertedDbType, paramName);
                    paramValues[i] = safeValue;
                    paramTypes[i] = convertedDbType;

                    fld.QParamnames.Add(paramName);
                    paramData += "!@$" + safeValue.ToString() + "!@$";
                }

                if (!string.IsNullOrEmpty(paramData))
                {
                    if (!fld.QParamvalues.Contains(paramData))
                        fld.QParamvalues.Add(paramData);
                }

                // Execute query with parameters
                if (processedSql.Trim().ToLower().StartsWith("select"))
                {
                    if (fValue == "~")
                    {
                        sqlResult = await _dbHelper.ExecuteSQLAsync(finalSql, paramNames, paramTypes, paramValues);

                        _traceService.LogMessage($"{_utils.AxpLogStr} SQL CommandText : {finalSql}");
                        _traceService.LogMessage($"{_utils.AxpLogStr} ParamData : {paramData}");
                        _traceService.LogDataTable(sqlResult.data);
                        return;
                    }

                    if (_utils.Pagination_PageNo == 1)
                    {
                        string originalSql = finalSql;
                        string paginatedSql = _utils.ChangeSqlForPagination(originalSql, true);
                        if (!string.Equals(originalSql, paginatedSql, StringComparison.OrdinalIgnoreCase))
                        {
                            sqlResult = await _dbHelper.ExecuteSQLAsync(paginatedSql, paramNames, paramTypes, paramValues);
                            _traceService.LogMessage($"{_utils.AxpLogStr} SQL CommandText : {paginatedSql}");
                            _traceService.LogMessage($"{_utils.AxpLogStr} ParamData : {paramData}");
                            _traceService.LogDataTable(sqlResult.data);
                        }
                        finalSql = originalSql;
                    }

                    finalSql = _utils.ChangeSqlForPagination(finalSql, false);
                    sqlResult = await _dbHelper.ExecuteSQLAsync(finalSql, paramNames, paramTypes, paramValues);
                    _traceService.LogMessage($"{_utils.AxpLogStr} SQL CommandText : {finalSql}");
                    _traceService.LogMessage($"{_utils.AxpLogStr} ParamData : {paramData}");
                    _traceService.LogDataTable(sqlResult.data);
                }
                else
                {
                    await _dbHelper.ExecuteNonQueryAsync(finalSql, paramNames, paramTypes, paramValues);
                }
            }
        }



        public string MakeProperValue(string str)
        {
            if (string.IsNullOrEmpty(str))
                return str;

            int j = 0;
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] == '"')
                    j++;
            }

            if (j % 2 != 0) // Odd number of quotes
            {
                int i = str.Length - 1;
                while (i >= 0)
                {
                    if (str[i] == '"')
                        break;
                    if (str[i] == ':')
                    {
                        str = str.Substring(0, i) + ":" + str.Substring(i);
                        break;
                    }
                    i--;
                }
            }

            return str;
        }

        public void RapidFillCompanyParams(int RowNo, string fieldName, string Fvalue, string DynamicSQL, bool ForPickList)
        {
            string S = DynamicSQL;
            if (string.IsNullOrEmpty(S))
                return;

            while (true)
            {
                int p1 = S.IndexOf('{');
                if (p1 == -1)
                    break;

                int p2 = S.IndexOf('}');
                if (p2 == -1)
                    throw new Exception("Invalid SQL");

                string CName = S.Substring(p1 + 1, p2 - p1 - 1).Trim();
                string CValue = string.Empty;
                string LastDataType = string.Empty;
                TField fld1 = _sdef.GetField(CName);

                if (CName.EndsWith("*"))
                {
                    CName = CName.Substring(0, CName.Length - 1);


                    if (fld1 != null && fld1.AsGrid)
                    {
                        int rc = fld1.FieldValList.Count;
                        for (int i = 1; i <= rc; i++)
                        {
                            string val = _sdef.GetFieldValue(fld1, i);
                            if (i == 1)
                                LastDataType = fld1.DataType.ToUpper();

                            if (LastDataType == "N")
                            {
                                val = string.IsNullOrEmpty(val) ? "0" : _utils.RemoveCommas(val);
                                CValue += "," + val;
                            }
                            else
                            {
                                CValue += "," + QuoteString(val);
                            }
                        }
                        CValue = CValue.Length > 0 ? CValue.Substring(1) : string.Empty;

                        if (string.IsNullOrEmpty(CValue))
                        {
                            CValue = fld1.DataType.ToLower() == "n" ? "0" : QuoteString(CValue);
                        }
                    }
                    else
                    {
                        CValue = GetValue(CName, RowNo);
                        if (string.IsNullOrEmpty(CValue))
                        {
                            CValue = fld1.DataType.ToLower() == "n" ? "0" : QuoteString(CValue);
                        }
                    }
                }
                else
                {
                    CValue = RapidGetValue(CName, RowNo);
                    LastDataType = GetFieldType(CName, ForPickList).ToUpper();

                    if (LastDataType == "N")
                    {
                        CValue = string.IsNullOrEmpty(CValue) ? "0" : _utils.RemoveCommas(CValue);
                    }
                }

                _traceService.LogMessage($"{CName} = {CValue}");
                S = S.Remove(p1, p2 - p1 + 1);
                S = S.Insert(p1, CValue);
            }

            S = RapidFillCompositeParams(S);




        }


        public string RapidGetValue(string fieldName, int RowNo)
        {
            string result = string.Empty;

            if (_utils.IsSysVar(fieldName))
            {
                result = _parser.GetVarValue(fieldName);
                return result;
            }

            result = _parser.GetVarValue(fieldName);
            if (RowNo == -1 && _parser.LastVarType.ToLower() == "c")
            {
                result = QuoteString(result);
            }

            return result;
        }

        public string GetFieldType(string fname, bool ForPickList)
        {
            string result = "c";

            if (ForPickList)
            {
                _parser.GetVarValue(fname);
                result = _parser.LastVarType;
            }
            else
            {
                TField fld1 = _sdef.GetField(fname);
                if (fld1 != null)
                {
                    result = fld1.DataType;
                }
                else
                {
                    _parser.GetVarValue(fname);
                    result = _parser.LastVarType;
                }
            }

            return result;
        }

        private string QuoteString(string value) => $"'{value.Replace("'", "''")}'";

        public string RapidFillCompositeParams(string S)
        {
            int p1 = S.IndexOf("(:");
            while (p1 > -1)
            {
                int p2 = S.IndexOf(")", p1 + 1);
                if (p2 == -1)
                    break; // No closing parenthesis found, exit loop to avoid infinite loop

                string CName = S.Substring(p1 + 2, p2 - p1 - 2);
                string CValue = $"({RapidGetValue(CName, -1)})";
                if (CValue == "()")
                    CValue = "(' ')";

                _traceService.LogMessage($"{CName} = {CValue}");
                S = S.Remove(p1, p2 - p1 + 1);
                S = S.Insert(p1, CValue);
                p1 = S.IndexOf("(:");
            }

            return S;
        }

        public bool IsDataTableActive(DataTable table)
        {
            return table != null && table.Columns.Count > 0;
        }

        // Locate Data
        public int GetIndexOfColumn(SQLResult sqlResult, string result, TField xfld, int linkSearchCol = -1)
        {
            try
            {
                string columnName = sqlResult.data.Columns[xfld.Searchcol].ColumnName;

                if (linkSearchCol > -1)
                    columnName = sqlResult.data.Columns[linkSearchCol].ColumnName;

                DataView dv = sqlResult.data.DefaultView;
                dv.Sort = columnName;  // Sort needed for binary search

                int viewIndex = dv.Find(result);

                if (viewIndex >= 0)
                {
                    DataRow foundRow = dv[viewIndex].Row;
                    int originalIndex = sqlResult.data.Rows.IndexOf(foundRow);
                    return originalIndex;
                }
            }
            catch { }

            return -1;  // Not found
        }
        // Apply Filter
        public int GetIndexOfColumnAndRefresh(SQLResult sqlResult, string result, TField xfld)
        {
            string columnName = sqlResult.data.Columns[xfld.Searchcol].ColumnName;

            DataView dv = sqlResult.data.DefaultView;
            dv.Sort = columnName;  // Sort needed for binary search

            int viewIndex = dv.Find(result);

            if (viewIndex >= 0)
            {
                DataRow foundRow = dv[viewIndex].Row;
                int originalIndex = sqlResult.data.Rows.IndexOf(foundRow);
                return originalIndex;
            }

            return -1;  // Not found
        }


    }



}
