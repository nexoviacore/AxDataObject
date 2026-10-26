using ARMCommon.Interface;
// Please use StructDef from New AxStructDef
using AxStructDef;
using AxStructDef.Models;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace AxParserApp

{

    public class AxParser : IAxParser
    {
        public List<string> VarList = new List<string>();
        public List<string> ValueList = new List<string>();
        public List<string> VarTypeList = new List<string>();
        public string VarTypeStr = string.Empty;
        public string ZeroValue = string.Empty;
        public string LastVarType = string.Empty;
        private List<string> exprset;


        public string Value = "";
        private string expressiontype = "";
        public List<string> ExpList = new List<string>();
        public List<string> ExpTypeList = new List<string>();
        public int ExpIndex = new int();
        private string CallType = "p";
        public List<object> FncList = new List<object>();
        private string bracket = string.Empty;
        private string expression = string.Empty;
        private int StrCount = new int();
        private int ExpStrPos = new int();
        private HashSet<char> DeLimiters = new HashSet<char> { ' ', '(', ')', ',' };
        private HashSet<char> Operators = new HashSet<char> { '+', '-', '*', '/', '>', '<', '=', '#', '&', '|', '$' };
        private HashSet<char> Numbers = new HashSet<char> { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '-', '.' };
        public List<string> FunList = new List<string>();
        public List<string> FunTypeList = new List<string>();

        public List<string> RegVarFldList = new List<string>();
        public List<string> funclist = new List<string>();
        public List<string> funcTypelist = new List<string>();
        public List<string> Parameters = new List<string>();
        public List<char> enc64List = new List<char>();
        public int[] dec64List = new int[128];
        public string dtCh = "/";
        public DateTime currentDate;
        public int cYear;
        public int minYear;
        public int maxYear;
        public ARMCommon.Helpers.Utils _common;


        public StructDef _structDef;
        private bool WorkOnStructdef = true;

        public AxParser(ARMCommon.Helpers.Utils common, StructDef structDef)
        {
            var result = "";

            _structDef = structDef;
            if (_structDef == null)
            {
                _structDef = new StructDef();
            }

            string transid = _structDef.Struct.TransId;


            _common = common;
            currentDate = DateTime.Now;
            cYear = currentDate.Year;
            minYear = cYear - 100;
            maxYear = 9999;

            InitBase64();

            funclist.AddRange(new string[] {"abs", "addtodate", "addtomonth", "amtword",
            "checkemail", "chkuniqchar", "cmonthyear", "convertmd5", "ctod", "curramtword",
            "date", "datediff", "dayofdate", "dayselapsed", "daysinmonth", "decode", "dtoc",
            "encode", "evaluatepower", "extractnum",
            "findandreplace", "findcharpos", "formataccamount", "formatamount", "formatval",
            "getallvalues", "getinteger", "getlength", "getmax", "getmin", "getmod", "getnthstring", "getrow", "getrowcount", "getseperatorcount", "getsubtotal", "getold", "getvalue", "getvarvalue",
            "iif", "infixtopostfix", "inputprecedence", "insertcommas", "isempty", "isemptyvalue", "isoperator", "isvarempty",
            "lastdayofmonth", "leftpad", "length", "lower", "lowercase",
            "makedate", "mods", "monthofdate",
            "pad", "postfixeval", "postfixtoinfix",
            "regvar", "removecommas", "rightpad", "rnd", "round", "roundoff",
            "setfont", "stackprecedence", "str", "stuff", "substr", "sum", "sumtill",
            "time", "timeelapsed", "trim", "trimall", "triminbetwnspaces", "total",
            "upper", "uppercase",
            "val", "validencodedate",
            "yearofdate",
            "floor", "ceil", "networkdays", "days360", "axpfloor", "axpceil",
            "axhidecontrols", "axunhidecontrols", "axenablecontrols", "axdisablecontrols", "setvalue", "axvalueexits", "axaddrow", "loadform", "loadformanddata", "loadiview", "loadpage", "openpage", "setfieldcaption", "setdccaption", "doformdesign", "canceltransaction", "getaxvalue", "stringpos", "axdccollapse", "axdcexpand", "getdcstate", "axallowempty", "axreadonly", "axmask", "axnomask", "axdisablegridcell", "axenablegridcell", "setfieldfontproperty", "firesql", "sqlget", "sqlregvar", "fieldchanged"
        });

            funcTypelist.AddRange(new string[] {"n", "d", "d", "c","c", "c", "c", "s", "d", "c",
            "d", "n", "n", "n", "n", "s", "c",
            "s", "n", "s",
            "s", "n", "n", "n", "n",
            "s", "n", "n", "n", "n", "n", "s", "n", "n", "n", "n", "s", "s", "s",
            "v", "s", "n", "n", "c", "c", "s", "s",
            "d", "s", "n", "c", "s",
            "d", "n", "n",
            "s", "n", "s",
            "s", "s", "s", "n", "n", "n",
            "s", "n", "c", "n", "c", "n", "n",
            "s", "n", "s", "n", "s", "n",
            "c", "s",
            "n", "d",
            "n",
            "n", "n", "n", "n", "n", "n",
            "s", "s", "s", "s", "s", "s", "s", "s", "s", "s", "s", "s", "s", "s", "s", "s", "s", "s", "s", "s", "s", "s", "s", "s", "s", "s", "s", "s","s","s","n", "s"
        });


        }
        public string CallFunction(string functionname, string[] _params)
        {
            string result = string.Empty;

            // Convert "~e~" placeholder values to empty strings.
            // Process a maximum of 20 parameters (As per Delphi code), or all available parameters if the array has fewer than 20.
            for (int i = 0; i < Math.Min(20, _params.Length); i++)
            {
                if (_params[i] == "~e~")
                    _params[i] = string.Empty;
            }

            if (functionname == "length" || functionname == "getlength")
                result = Length(_params[0]).ToString();
            else if (functionname == "substr")
                result = SubStr(_params[0], _params[1], _params[2]);
            else if (functionname == "abs")
                result = PAbs(_params[0]).ToString();
            else if (functionname == "iif")
            {
                var IfFunVal = IfFun(_params[0], _params[1], _params[2]);
                result = int.TryParse(IfFunVal.ToString(), out _) ? IfFunVal : IfFunVal;
                //string IfFunVal;
                //bool boolVal;
                //// Check the first element
                //if (_params[0].ToString() == "0")
                //    boolVal = false;
                //else
                //    boolVal = true;

                //// Assign S based on V
                //if (boolVal)
                //    IfFunVal = _params[1].ToString();

                //else
                //    IfFunVal = _params[2].ToString();

                //result = IfFunVal;
            }
            else if (functionname == "findcharpos")
                result = FindCharPos(_params[0], _params[1]).ToString();
            else if (functionname == "chkuniqchar")
                result = ChkUniqChar(_params[0]).ToString();
            else if (functionname == "trimall" || functionname == "trim")
                result = TrimAll(_params[0]);
            else if (functionname == "extractnum")
                result = ExtractNum(_params[0]).ToString();
            else if (functionname == "mods")
                result = Mods(_params[0], _params[1]).ToString();
            else if (functionname == "lastdayofmonth")
                result = LastDayOfMonth(_params[0]);
            else if (functionname == "daysinmonth")
                result = DaysInMonth(_params[0], _params[1]).ToString();
            else if (functionname == "validencodedate")
                result = ValidEncodeDate(_params[0], _params[1], _params[2]).ToString();
            else if (functionname == "getinteger")
                result = GetInteger(_params[0]).ToString();
            else if (functionname == "addtomonth")
                result = AddToMonth(_params[0], _params[1]);
            else if (functionname == "addtodate")
                result = AddToDate(_params[0], _params[1]);
            else if (functionname == "timeelapsed")
                result = TimeElapsed(_params[0], _params[1]);
            else if (functionname == "dayselapsed")
                result = DaysElapsed(_params[0], _params[1]).ToString();
            else if (functionname == "mandy")
                result = MandY(_params[0]);
            else if (functionname == "findandreplace")
                result = FindAndReplace(_params[0], _params[1], _params[2]);
            else if (functionname == "removecommas")
                result = RemoveCommas(_params[0]);
            else if (functionname == "getnthstring")
                result = GetNthString(_params[0], _params[1]);
            else if (functionname == "roundoff" || functionname == "round")
                result = RoundOff(_params[0], _params[1], true).ToString();
            else if (functionname == "insertcomas")
                result = InsertCommas(_params[0]);
            else if (functionname == "leftpad")
                result = LeftPad(_params[0], _params[1], _params[2]);
            else if (functionname == "rightpad")
                result = RightPad(_params[0], _params[1], _params[2]);
            else if (functionname == "pad")
                result = Pad(_params[0], _params[1], _params[2]);
            else if (functionname == "getseperatorcount")
                result = GetSeperatorCount(_params[0], _params[1]);
            else if (functionname == "val")
                result = Val(_params[0]);
            else if (functionname == "uppercase" || functionname == "upper")
                result = UpperCase(_params[0]);
            else if (functionname == "lowercase" || functionname == "lower")
                result = LowerCase(_params[0]);
            else if (functionname == "isvarempty")
                result = IsVarEmpty(_params[0]);
            else if (functionname == "getvarvalue")
                result = GetVarValue(_params[0]);
            else if (functionname == "amtword")
                result = Amt_Word(_params[0]);
            else if (functionname == "evaluatepower")
                result = EvaluatePower(_params[0], _params[1]).ToString();
            else if (functionname == "infixtopostfix")
                result = InfixToPostfix(_params[0]);
            else if (functionname == "postfixtoinfix")
                result = PostfixToInfix(_params[0]);
            else if (functionname == "postfixeval")
                result = PostfixEval(_params[0]).ToString();
            else if (functionname == "inputprecedence")
                result = InputPrecedence(_params[0]).ToString();
            else if (functionname == "stackprecedence")
                result = StackPrecedence(_params[0]).ToString();
            else if (functionname == "isoperator")
                result = ISOperator(_params[0]).ToString();
            else if (functionname == "encode")
                result = Encode(_params[0]);
            else if (functionname == "decode")
                result = Decode(_params[0]);
            else if (functionname == "dtoc")
                result = DTOC(_params[0]);
            else if (functionname == "ctod")
                result = CTOD(_params[0]);
            else if (functionname == "cmonthyear")
                result = CMonthYear(_params[0]);
            else if (functionname == "stuff")
                result = Stuff(_params[0], _params[1], _params[2]);
            else if (functionname == "rnd")
                result = Rnd(_params[0], _params[1]).ToString();
            else if (functionname == "isemptyvalue")
                result = IsEmptyValue(_params[0], _params[1]);
            else if (functionname == "formatval")
                result = FormatVal(_params[0], _params[1]);
            else if (functionname == "formataccamount")
                result = FormatAccAmount(_params[0], _params[1], _params[2], _params[3], _params[4]);
            else if (functionname == "formatamount")
                result = FormatAmount(_params[0], _params[1], _params[2], _params[3], _params[4], _params[5]);
            else if (functionname == "triminbetwnspaces")
                result = TrimInBetweenSpaces(_params[0]);
            else if (functionname == "date")
                result = TodaysDate();
            else if (functionname == "time")
                result = Time();
            else if (functionname == "isempty")
                result = ISEMPTY(_params[0]);
            else if (functionname == "datediff")
                result = DateDiff(_params[0], _params[1]).ToString();
            else if (functionname == "checkemail")
                result = CheckEmail(_params[0]);
            else if (functionname == "convertmd5")
                result = MD5Hash(_params[0]);
            else if (functionname == "total")
                result = Total(_params[0]).ToString();
            else if (functionname == "curramtword")
                result = CurrAmtWord(_params[0], _params[1], _params[2], _params[3], _params[4]);
            else if (functionname == "str")
                result = Str(_params[0]);
            else if (functionname == "getvalue")
                result = GetValue(_params[0], _params[1]);
            else if (functionname == "makedate")
                result = MakeDate(_params[0], _params[1], _params[2]);
            else if (functionname == "getallvalues")
                result = GetAllValues(_params[0], _params[1]);
            else if (functionname == "dayofdate")
                result = DayOfDate(_params[0]).ToString();
            else if (functionname == "yearofdate")
                result = YearOfDate(_params[0]).ToString();
            else if (functionname == "monthofdate")
                result = MonthOfDate(_params[0]).ToString();
            else if (functionname == "getmin")
                result = GetMin(_params[0]).ToString();
            else if (functionname == "getmax")
                result = GetMax(_params[0]).ToString();
            else if (functionname == "getrowcount")
                result = GetRowCount(_params[0]).ToString();
            else if (functionname == "sumtill")
                result = SumTill(_params[0], _params[1], _params[2], _params[3]).ToString();
            else if (functionname == "sum")
                result = Sum(_params[0], _params[1], _params[2]).ToString();
            else if (functionname == "regvar")
                result = RegisterVar(_params[0], _params[1], _params[2]).ToString();
            else if (functionname == "getrow")
                result = GetRow(_params[0], _params[1]).ToString();
            else if (functionname == "getsubtotal")
                result = GetSubTotal(_params[0], _params[1]).ToString();
            else if (functionname == "getmod")
                result = GetMod(_params[0], _params[1]).ToString();
            else if (functionname == "getold")
                result = GetOld(_params[0], _params[1]);
            else if (functionname == "ceil" || functionname == "axpceil")
                result = AxCeil(_params[0]).ToString();
            else if (functionname == "floor" || functionname == "axpfloor")
                result = AxFloor(_params[0]).ToString();
            else if (functionname == "networkdays")
                result = NetworkDays(_params[0], _params[1], _params[2]).ToString();
            else if (functionname == "days360")
                result = Days360(_params[0], _params[1], _params[2]).ToString();
            else if (functionname == "stringpos")
                result = GetStringPos(_params[0], _params[1], _params[2]).ToString();
            else if (functionname == "firesql")
                result = FireSql(_params[0], _params[1]).GetAwaiter().GetResult(); //To-Do Null check in _param[0]
            else if (functionname == "sqlregvar")
                result = SQLRegVar(_params[0], _params[1]).ToString();
            else if (functionname == "sqlget")
                result = SQLGet(_params[0], _params[1]).ToString();
            else if (functionname == "fieldchanged")
                result = FieldChanged(_params[0], _params[1]).ToString();
            return result;
        }


        private int Length(string str)
        {
            int length;
            length = str.ToString().Length;
            return length;
        }
        private string SubStr(string str, object start, object len)
        {
            int startInt = Convert.ToInt32(start);
            int lenInt = Convert.ToInt32(len);
            if (startInt != 0)
                startInt -= 1;
            if (!string.IsNullOrEmpty(str))
            {
                if (startInt < 0) startInt = 0;
                if (startInt >= str.Length) return string.Empty;
                if (startInt + lenInt > str.Length)
                    lenInt = str.Length - startInt;

                return str.Substring(startInt, lenInt);
            }
            return string.Empty;
        }
        private int PAbs(object param)
        {
            int x = Convert.ToInt32(param);
            int abs = Math.Abs(x);
            return abs;
        }
        // private string IfFun(object val1, object val2, object val3)
        // {
        //     string result = "";
        //     if (val1 != null)
        //     {
        //         if (val2 != null && !double.TryParse(val2.ToString(), out _))
        //         {
        //             if (val2.ToString() == "01/01/1900" || val2.ToString() == "")
        //                 val2 = "";
        //             result = "\"" + val2 + "\"";
        //         }
        //         else
        //         {
        //             result = val2?.ToString() ?? "";
        //         }
        //     }
        //     else
        //     {
        //         if (val3 != null && !double.TryParse(val3.ToString(), out _))
        //         {
        //             if (val3.ToString() == "01/01/1900" || val3.ToString() == "")
        //                 val3 = "";
        //             result = "\"" + val3 + "\"";
        //         }
        //         else
        //         {
        //             result = val3?.ToString() ?? "";
        //         }
        //     }
        //     return result;
        // }

        // IFfun


        private string IfFun(object val1, object val2, object val3)
        {
            string result = "";
            if (val1 != null && val1.ToString() != "0")
            {
                if (val2 != null)
                {
                    if (val2.ToString() == "01/01/1900")
                        val2 = "";
                    result = val2.ToString();
                }
                else
                {
                    result = val2?.ToString() ?? "";
                }
            }
            else
            {
                if (val3 != null)
                {
                    if (val3.ToString() == "01/01/1900")
                        val3 = "";
                    result = val3.ToString();
                }
                else
                {
                    result = val3?.ToString() ?? "";
                }
            }
            return result;
        }



        private int FindCharPos(string param1, string param2)
        {
            var pos = param1.IndexOf(param2);
            if (pos == -1)
            {
                //showAlertDialog("error", 2033, "client");
                string error = "Error: Your string doesnot have the character";
                return pos;
            }
            else
                return pos;
        }
        private bool ChkUniqChar(string str)
        {
            int l = str.Length;
            int count = 0;
            for (int i = 0; i < l; i++)
            {
                for (int j = i + 1; j < l; j++)
                {
                    if (str[i] == str[j])
                    {
                        count++;
                    }
                }
            }
            return count > 0;
        }
        private string TrimAll(string sString)
        {
            while (sString.StartsWith(" "))
            {
                sString = sString.Substring(1, sString.Length - 1);
            }
            while (sString.EndsWith(" "))
            {
                sString = sString.Substring(0, sString.Length - 1);
            }
            return sString;
        }
        private int ExtractNum(string str)
        {
            string digitsOnly = Regex.Replace(str, @"\D", "");
            return int.TryParse(digitsOnly, out int number) ? number : 0;
        }
        private int Mods(string _v1, string _v2)
        {
            int v1 = Convert.ToInt32(_v1);
            int v2 = Convert.ToInt32(_v2);
            int x = v1 % v2;
            return x;
        }
        private string LastDayOfMonth(string dateStr)
        {
            try
            {
                if (string.IsNullOrEmpty(dateStr))
                    return string.Empty;

                string[] dtStr = dateStr.Split('/');
                string day, month, year;

                if (dateStr.Contains("dd/mm/yyyy"))
                {
                    day = dtStr[0];
                    month = dtStr[1];
                    year = dtStr[2];
                }
                else
                {
                    day = dtStr[1];
                    month = dtStr[0];
                    year = dtStr[2];
                }

                DateTime inputDate = DateTime.ParseExact(
                    $"{month}/{day}/{year}",
                    "MM/dd/yyyy",
                    CultureInfo.InvariantCulture
                );
                //DateTime lastDayOfMonth = new DateTime(
                //    inputDate.Year,
                //    inputDate.Month
                //    //DaysInMonth(inputDate.Year, inputDate.Month)
                //);

                return string.Empty;// ConstructDate(lastDayOfMonth);
            }
            catch
            {
                return string.Empty;
            }
        }
        private string ConstructDate(DateTime dateObj)
        {
            if (string.Equals(_common.ApiDateFormat, "dd/mm/yyyy", StringComparison.OrdinalIgnoreCase))
            {
                return dateObj.Day.ToString("00") + "/" +
                       dateObj.Month.ToString("00") + "/" +
                       dateObj.Year.ToString("0000");
            }
            else
            {
                return dateObj.Month.ToString("00") + "/" +
                       dateObj.Day.ToString("00") + "/" +
                       dateObj.Year.ToString("0000");
            }
        }
        private int DaysInMonth(string _year, string _month)
        {
            int month = Convert.ToInt32(_month);
            int year = Convert.ToInt32(_year);
            if (month == 1 || month == 3 || month == 5 || month == 7 || month == 8 || month == 10 || month == 12)
                return 31;
            else if (month == 4 || month == 6 || month == 9 || month == 11)
                return 30;
            else
                return IsLeapYear(year) ? 29 : 28;
        }
        private bool IsLeapYear(int yr)
        {
            return DateTime.IsLeapYear(yr);
        }
        private DateTime ValidEncodeDate(string y, string m, string d)
        {
            int year = Convert.ToInt32(y);
            int month = Convert.ToInt32(m);
            int day = Convert.ToInt32(d);
            return new DateTime(year, month, day);
        }
        private int GetInteger(object real)
        {
            if (real == null)
                return 0;
            if (int.TryParse(real.ToString(), out int x))
                return x;
            if (double.TryParse(real.ToString(), out double dbl))
                return (int)dbl;
            return 0;
        }
        private string AddToMonth(string dateTime, string _i)
        {
            int i = Convert.ToInt32(_i);
            if (!string.IsNullOrWhiteSpace(dateTime) && dateTime != "0")
            {
                DateTime? newDate = GetDateObj(dateTime);
                if (newDate.HasValue)
                {
                    DateTime updatedDate = newDate.Value.AddMonths(i);
                    return ConstructDate(updatedDate);
                }
            }
            return "0";
        }


        // private DateTime? GetDateObj(string dateStr)
        // {

        //     if (!string.IsNullOrWhiteSpace(dateStr) && dateStr.Contains("/"))
        //     {
        //         var dtStr = dateStr.Split('/');
        //         string day, month, year;

        //         if (_common.ApiDateFormat.ToLower() == "dd/mm/yyyy")
        //         {
        //             day = dtStr[0];
        //             month = dtStr[1];
        //             year = dtStr[2];
        //         }
        //         else
        //         {
        //             day = dtStr[1];
        //             month = dtStr[0];
        //             year = dtStr[2];
        //         }

        //         if (!int.TryParse(day, out int d) ||
        //             !int.TryParse(month, out int m) ||
        //             !int.TryParse(year, out int y))
        //         {
        //             return null;
        //         }

        //         try
        //         {
        //             return new DateTime(y, m, d);
        //         }
        //         catch
        //         {
        //             return null; // Invalid date
        //         }
        //     }
        //     return null;
        // }


        private DateTime? GetDateObj(string dateStr)
        {

            if (!string.IsNullOrWhiteSpace(dateStr) && dateStr.Contains("/"))
            {
                var dtStr = dateStr.Split('/');
                string day, month, year;

                if (_common.ApiDateFormat.ToLower() == "dd/mm/yyyy")
                {
                    day = dtStr[0];
                    month = dtStr[1];
                    year = dtStr[2];
                }
                else
                {
                    day = dtStr[1];
                    month = dtStr[0];
                    year = dtStr[2];
                }

                if (!int.TryParse(day, out int d) ||
                    !int.TryParse(month, out int m) ||
                    !int.TryParse(year, out int y))
                {
                    return null;
                }

                try
                {
                    return new DateTime(y, m, d);
                }
                catch
                {
                    return null; // Invalid date
                }
            }
            return null;
        }


        private string AddToDate(string dateTime, string _daysToAdd)
        {
            int daysToAdd = Convert.ToInt32(_daysToAdd);
            if (!string.IsNullOrWhiteSpace(dateTime))
            {
                DateTime? newDate = GetDateObj(dateTime);
                if (newDate.HasValue)
                {
                    DateTime updatedDate = newDate.Value.AddDays(daysToAdd);
                    return updatedDate.ToString(_common.ApiDateFormat);
                }
            }
            return "0";
        }
        private string TimeElapsed(string d1, string d2)
        {
            if (!string.IsNullOrWhiteSpace(d1) && !string.IsNullOrWhiteSpace(d2))
            {
                DateTime? date1 = GetDateObj(d1);
                DateTime? date2 = GetDateObj(d2);

                if (date1 != null && date2 != null)
                {
                    TimeSpan ts = date2.Value - date1.Value;

                    long totalSeconds = (long)ts.TotalSeconds;

                    long days = totalSeconds / (24 * 60 * 60);
                    totalSeconds -= days * (24 * 60 * 60);

                    long hours = totalSeconds / (60 * 60);
                    totalSeconds -= hours * (60 * 60);

                    long minutes = totalSeconds / 60;
                    totalSeconds -= minutes * 60;

                    long seconds = totalSeconds;

                    return $"{days} day{(days != 1 ? "s" : "")}, " +
                           $"{hours} hour{(hours != 1 ? "s" : "")}, " +
                           $"{minutes} minute{(minutes != 1 ? "s" : "")}, " +
                           $"{seconds} second{(seconds != 1 ? "s" : "")}";
                }
            }
            return string.Empty;
        }
        private int DaysElapsed(string d1, string d2)
        {
            try
            {
                if (d1 == "01/01/1900") d1 = "";
                if (d2 == "01/01/1900") d2 = "";
                if (!string.IsNullOrWhiteSpace(d1) && !string.IsNullOrWhiteSpace(d2))
                {
                    DateTime? date1 = GetDateObj(d1);
                    DateTime? date2 = GetDateObj(d2);
                    if (date1 != null && date2 != null)
                    {
                        DateTime startDate = new DateTime(date1.Value.Year, date1.Value.Month, date1.Value.Day, 0, 0, 0, DateTimeKind.Utc);
                        DateTime endDate = new DateTime(date2.Value.Year, date2.Value.Month, date2.Value.Day, 0, 0, 0, DateTimeKind.Utc);
                        return Math.Abs((int)(endDate - startDate).TotalDays);
                    }
                }
                return 0;
            }
            catch
            {
                return 0;
            }
        }
        private string MandY(string dateTime)
        {
            if (!string.IsNullOrWhiteSpace(dateTime) && dateTime != "0")
            {
                DateTime? dateObj = GetDateObj(dateTime);
                if (dateObj != null)
                {
                    string monthStr = PrefixZero(dateObj.Value.Month).ToString();
                    string yearStr = dateObj.Value.Year.ToString();
                    return yearStr + monthStr;
                }
            }
            return "";
        }
        private string PrefixZero(int dayOrMonth)
        {
            return dayOrMonth.ToString("D2");
        }
        private string FindAndReplace(string strText, string findstr, string repstr)
        {
            return Regex.Replace(strText, findstr, repstr, RegexOptions.IgnoreCase);
        }
        private string RemoveCommas(object aNum)
        {
            if (aNum == null || string.IsNullOrEmpty(aNum.ToString()))
                return "0";
            return aNum.ToString().Replace(",", "");
        }
        private string GetNthString(string str, string _pos)
        {
            int pos = Convert.ToInt32(_pos);
            if (string.IsNullOrEmpty(str) || pos < 0 || pos >= str.Length)
                return string.Empty;

            return str.Substring(pos);
        }
        private double RoundOff(string num, string _d, bool isFromEval = false)
        {
            int d = Convert.ToInt32(_d);
            double numVal = double.Parse(num, CultureInfo.InvariantCulture);

            if (!isFromEval)
                numVal = double.Parse(num, CultureInfo.InvariantCulture);

            string strNum = numVal.ToString("G17", CultureInfo.InvariantCulture).Trim();
            int l = strNum.Length;
            int p = strNum.IndexOf('.');

            if (p > -1)
                strNum = strNum.Substring(p + 1, l - (p + 1));

            int w = strNum.Length;

            if (numVal > 0)
            {
                if (w >= 10 && w < 15)
                {
                    if (w == 10)
                        numVal += 0.000000001;
                    else if (w == 11)
                        numVal += 0.0000000001;
                    else if (w == 12)
                        numVal += 0.00000000001;
                    else if (w == 13)
                        numVal += 0.000000000001;
                    else if (w == 14)
                        numVal += 0.0000000000001;
                }
                else if (w == 15)
                {
                    numVal += 0.00000000000001;
                }
            }

            if (numVal == 0)
                return 0.0;

            double f1 = Math.Pow(10, w);
            double f2 = Math.Pow(10, w - d);
            numVal = numVal * f1;
            double d1 = Math.Floor(numVal / f2);
            double fact = numVal - (d1 * f2);

            if (fact >= f2 / 2)
                numVal += f2 - fact;
            else
                numVal -= fact;

            return numVal / f1;
        }
        private string InsertCommas(string amount)
        {
            if (!double.TryParse(amount, NumberStyles.Any, CultureInfo.InvariantCulture, out double i))
            {
                i = 0.00;
            }

            string minus = "";
            if (i < 0) minus = "-";

            i = Math.Abs(i);

            // Equivalent to parseInt((i + .005) * 100) in JS
            i = (int)((i + 0.005) * 100);
            i = i / 100;

            string s = i.ToString(CultureInfo.InvariantCulture);

            if (!s.Contains(".")) s += ".00";
            if (s.IndexOf('.') == s.Length - 2) s += "0";

            s = minus + s;

            return CommaFormatted(s);
        }
        private string CommaFormatted(string amount)
        {
            string delimiter = ",";

            if (!amount.Contains("."))
                amount += ".";

            string[] a = amount.Split('.');
            string d = (a.Length > 1) ? a[1] : "";
            if (!int.TryParse(a[0], out int i))
                return "";

            string minus = "";
            if (double.TryParse(amount, NumberStyles.Any, CultureInfo.InvariantCulture, out double num) && num < 0)
                minus = "-";

            i = Math.Abs(i);
            string n = i.ToString();

            List<string> arr = new List<string>();

            try
            {
                if (Parameters.Count > 0 && Parameters.Contains("millions~T"))
                {
                    while (n.Length > 3)
                    {
                        string nn = n.Substring(n.Length - 3);
                        arr.Insert(0, nn);
                        n = n.Substring(0, n.Length - 3);
                    }
                }
                else
                {
                    if (n.Length > 3)
                    {
                        string nn = n.Substring(n.Length - 3);
                        arr.Insert(0, nn);
                        n = n.Substring(0, n.Length - 3);
                    }
                    while (n.Length > 2)
                    {
                        string nn = n.Substring(n.Length - 2);
                        arr.Insert(0, nn);
                        n = n.Substring(0, n.Length - 2);
                    }
                }
            }
            catch
            {
                while (n.Length > 3)
                {
                    string nn = n.Substring(n.Length - 3);
                    arr.Insert(0, nn);
                    n = n.Substring(0, n.Length - 3);
                }
            }

            if (n.Length > 0) arr.Insert(0, n);

            n = string.Join(delimiter, arr);

            if (string.IsNullOrEmpty(d)) amount = n;
            else amount = n + "." + d;

            amount = minus + amount;

            return amount;
        }
        private string LeftPad(object text, string _size, string ch = "0")
        {
            int size = Convert.ToInt32(_size);
            string outStr = text?.ToString() ?? "";
            if (string.IsNullOrEmpty(ch))
                ch = "0";
            while (outStr.Length < size)
            {
                if (outStr.Length == size)
                    outStr += ch;
                else
                    outStr = ch + outStr;
            }
            return outStr;
        }
        private string RightPad(object text, string _size, string ch = "0")
        {
            int size = Convert.ToInt32(_size);
            string outStr = text?.ToString() ?? "";
            if (string.IsNullOrEmpty(ch))
                ch = "0";
            while (outStr.Length < size)
            {
                if (outStr.Length == size)
                    outStr = ch + outStr;
                else
                    outStr += ch;
            }
            return outStr;
        }
        private string Pad(object text, string _size, string ch = "0")
        {
            int size = Convert.ToInt32(_size);
            string outStr = Convert.ToString(text) ?? "";
            if (string.IsNullOrEmpty(ch)) ch = "0";

            while (outStr.Length < size)
            {
                outStr = ch + outStr;
                if (outStr.Length < size)
                    outStr += ch;
            }

            return outStr;
        }
        private string GetSeperatorCount(object srcstr, object separator)
        {
            string str = srcstr?.ToString() ?? "";
            string sep = separator?.ToString() ?? "";
            string[] strArray = str.Split(new string[] { sep }, StringSplitOptions.None);
            return (strArray.Length - 1).ToString();
        }
        private string Val(object fldValue)
        {
            string num = fldValue?.ToString() ?? "";
            if (num.Contains("{") || num.Contains("}"))
            {
                string x = Regex.Replace(num, @"^\{?([^\}])", "");
                string[] parts = x.Split(')');
                num = parts.Length > 0 ? parts[0] : "";
            }
            num = RemoveCommas(num);
            Match match = Regex.Match(num, @"^[+-]?\d*\.?\d+");
            if (match.Success)
                return match.Value;
            return "";
        }
        private string Str(object param, string rowno)
        {
            if (!double.TryParse(param?.ToString(), out _))
            {
                string fldnm = (param?.ToString() ?? "") + rowno;
                ArrayList list = new ArrayList();
                int index;
                if (int.TryParse(fldnm, out index) && index >= 0 && index < list.Count)
                {
                    string fresult = list[index]?.ToString() ?? "";
                    if (!string.IsNullOrEmpty(fresult))
                        return fresult;
                }
            }
            return null;
        }
        private string UpperCase(object strParam)
        {
            string str = strParam.ToString();
            return str.ToUpper();
        }
        private string LowerCase(object strParam)
        {
            string str = strParam.ToString();
            return str.ToLower();
        }
        private string IsVarEmpty(object strParam)
        {
            string str = strParam.ToString();
            if (str == "")
                return "true";
            else
                return "false";
        }
        private string GetVarValue(object param)
        {
            return param.ToString();
        }
        private string Amt_Word(string num)
        {
            string inWords = string.Empty;
            if (num != null)
            {
                if (num.Length == 0)
                {
                    return "true";
                }
                else
                {
                    inWords = ToWords(num);
                }
            }
            return inWords;
        }
        private string ToWords(string s)
        {
            string finalWord1 = ValidateAmount(s);
            string finalWord2 = string.Empty;

            string val = s;
            int idx = val.IndexOf('.');
            if (idx != -1)
            {
                string decimalPart = val.Substring(idx + 1);
                if (decimalPart.Length == 0 || decimalPart == "00")
                {
                    finalWord2 = "Zero paisa Only";
                }
                else
                {
                    finalWord2 = ValidateAmount(decimalPart) + " paisa Only";
                }
                if (string.IsNullOrEmpty(finalWord1))
                    return finalWord2;
                else
                    return "Rupees " + finalWord1 + " and " + finalWord2;
            }
            else
            {
                return "Rupees " + finalWord1 + " Only";
            }
        }
        private string ValidateAmount(string s)
        {
            if (!long.TryParse(Math.Floor(Convert.ToDecimal(s)).ToString(), out long junkVal))
                return string.Empty;

            if (junkVal < 0)
            {
                return string.Empty;
            }

            if (junkVal == 0)
                return string.Empty;

            char[] numReversed = junkVal.ToString().ToCharArray();
            Array.Reverse(numReversed);
            char[] actnumber = numReversed;

            if (actnumber.Length > 9)
            {
                return string.Empty;
            }

            string[] iWords = { "Zero", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine" };
            string[] ePlace = { "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen" };
            string[] tensPlace = { "dummy", "Ten", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety" };

            string[] inWords = new string[actnumber.Length];
            int j = 0;

            for (int i = 0; i < actnumber.Length; i++)
            {
                int digit = actnumber[i] - '0';
                switch (i)
                {
                    case 0:
                        if (digit == 0 || (i + 1 < actnumber.Length && actnumber[i + 1] == '1'))
                            inWords[j] = "";
                        else
                            inWords[j] = iWords[digit];
                        break;

                    case 1:
                        inWords[j] = TensComplication(digit, actnumber[i - 1] - '0', ePlace, tensPlace);
                        break;

                    case 2:
                        if (digit == 0)
                            inWords[j] = "";
                        else if ((i - 1 >= 0 && actnumber[i - 1] != '0') && (i - 2 >= 0 && actnumber[i - 2] != '0'))
                            inWords[j] = iWords[digit] + " Hundred and";
                        else
                            inWords[j] = iWords[digit] + " Hundred";
                        break;

                    case 3:
                        if (digit == 0 || (i + 1 < actnumber.Length && actnumber[i + 1] == '1'))
                            inWords[j] = "";
                        else
                            inWords[j] = iWords[digit];
                        if ((i + 1 < actnumber.Length && actnumber[i + 1] != '0') || digit > 0)
                            inWords[j] += " Thousand";
                        break;

                    case 4:
                        inWords[j] = TensComplication(digit, actnumber[i - 1] - '0', ePlace, tensPlace);
                        break;

                    case 5:
                        if (digit == 0 || (i + 1 < actnumber.Length && actnumber[i + 1] == '1'))
                            inWords[j] = "";
                        else
                            inWords[j] = iWords[digit];
                        if ((i + 1 < actnumber.Length && actnumber[i + 1] != '0') || digit > 0)
                            inWords[j] += " Lakh";
                        break;

                    case 6:
                        inWords[j] = TensComplication(digit, actnumber[i - 1] - '0', ePlace, tensPlace);
                        break;

                    case 7:
                        if (digit == 0 || (i + 1 < actnumber.Length && actnumber[i + 1] == '1'))
                            inWords[j] = "";
                        else
                            inWords[j] = iWords[digit];
                        if ((i + 1 < actnumber.Length && actnumber[i + 1] != '0') || digit > 0)
                            inWords[j] += " Crore";
                        break;

                    case 8:
                        inWords[j] = TensComplication(digit, actnumber[i - 1] - '0', ePlace, tensPlace);
                        break;
                }
                j++;
            }

            Array.Reverse(inWords);

            string finalWord = "";
            foreach (var word in inWords)
            {
                if (!string.IsNullOrWhiteSpace(word))
                    finalWord += " " + word;
            }

            return finalWord.Trim();
        }
        private string TensComplication(int digit, int previousDigit, string[] ePlace, string[] tensPlace)
        {
            if (digit == 0)
                return "";
            else if (digit == 1)
                return ePlace[previousDigit];
            else
                return tensPlace[digit];
        }
        private double EvaluatePower(string @base, string strParam)
        {
            if (double.TryParse(@base, out double baseVal) &&
                double.TryParse(strParam, out double power))
            {
                return Math.Pow(baseVal, power);
            }
            else
            {
                return 0;
            }
        }
        private void push_stack(List<string> stackArr, string ele)
        {
            stackArr.Add(ele);
        }
        private string pop_stack(List<string> stackArr)
        {
            string temp = stackArr[stackArr.Count - 1];
            stackArr.RemoveAt(stackArr.Count - 1);
            return temp;
        }
        private string TopStack(List<string> stackArr)
        {
            return stackArr[stackArr.Count - 1];
        }
        private bool IsEmpty(List<string> stackArr)
        {
            return stackArr.Count == 0;
        }
        private bool IsOperator(string who)
        {
            return who == "+" || who == "-" || who == "*" || who == "/" || who == "^";
        }
        private bool IsOperand(string who)
        {
            return !IsOperator(who) && who != "(" && who != ")";
        }
        private int Prcd(string who)
        {
            if (who == "^") return 5;
            if (who == "*" || who == "/") return 4;
            if (who == "+" || who == "-") return 3;
            if (who == "(") return 2;
            if (who == ")") return 1;
            return 0;
        }
        private string InfixToPostfix(string infixStr)
        {
            List<string> postfixStr = new List<string>();
            List<string> stackArr = new List<string>();

            char[] infixChars = infixStr.ToCharArray();
            foreach (char c in infixChars)
            {
                string token = c.ToString();

                if (IsOperand(token))
                {
                    postfixStr.Add(token);
                }
                else if (IsOperator(token))
                {
                    if (token != "^")
                    {
                        while (!IsEmpty(stackArr) && Prcd(token) <= Prcd(TopStack(stackArr)))
                        {
                            postfixStr.Add(pop_stack(stackArr));
                        }
                    }
                    else
                    {
                        while (!IsEmpty(stackArr) && Prcd(token) < Prcd(TopStack(stackArr)))
                        {
                            postfixStr.Add(pop_stack(stackArr));
                        }
                    }
                    push_stack(stackArr, token);
                }
                else if (token == "(")
                {
                    push_stack(stackArr, token);
                }
                else if (token == ")")
                {
                    while (TopStack(stackArr) != "(")
                    {
                        postfixStr.Add(pop_stack(stackArr));
                    }
                    pop_stack(stackArr); // remove "("
                }
            }

            while (!IsEmpty(stackArr))
            {
                if (TopStack(stackArr) == "(")
                    pop_stack(stackArr);
                else
                    postfixStr.Add(pop_stack(stackArr));
            }

            StringBuilder returnVal = new StringBuilder();
            foreach (var item in postfixStr)
            {
                returnVal.Append(item);
            }

            return returnVal.ToString();
        }
        private string PostfixToInfix(string postfixStr)
        {
            List<string> stackArr = new List<string>();
            char[] tokens = postfixStr.ToCharArray();
            foreach (char c in tokens)
            {
                string token = c.ToString();
                if (IsOperand(token))
                {
                    push_stack(stackArr, token);
                }
                else
                {
                    string temp = TopStack(stackArr);
                    pop_stack(stackArr);
                    string pushVal = TopStack(stackArr) + token + temp;
                    pop_stack(stackArr);
                    push_stack(stackArr, pushVal);
                }
            }
            return TopStack(stackArr);
        }
        private double PostfixEval(string postfixStr)
        {
            List<string> stackArr = new List<string>();
            char[] tokens = postfixStr.ToCharArray();
            foreach (char c in tokens)
            {
                string token = c.ToString();
                if (IsOperand(token))
                {
                    push_stack(stackArr, token);
                }
                else
                {
                    double temp = double.Parse(TopStack(stackArr));
                    pop_stack(stackArr);

                    double left = double.Parse(TopStack(stackArr));
                    pop_stack(stackArr);

                    double pushVal = PostfixSubEval(left, temp, token);

                    push_stack(stackArr, pushVal.ToString());
                }
            }
            return double.Parse(TopStack(stackArr));
        }
        private double PostfixSubEval(double num1, double num2, string sym)
        {
            double returnVal = 0;
            if (sym == "+")
                returnVal = num1 + num2;
            else if (sym == "-")
                returnVal = num1 - num2;
            else if (sym == "*")
                returnVal = num1 * num2;
            else if (sym == "/")
                returnVal = num1 / num2;
            else if (sym == "^")
                returnVal = Math.Pow(num1, num2);
            return returnVal;
        }
        private int InputPrecedence(string precedence)
        {
            int result = 11;
            if (precedence == ")") return 0;
            else if (precedence == "|") return 1;
            else if (precedence == "&") return 3;
            else if (precedence == "+" || precedence == "-") return 5;
            else if (precedence == "*" || precedence == "/") return 7;
            else if (precedence == "^" || precedence == ">" || precedence == "<" || precedence == "#" || precedence == "$") return 10;
            else if (precedence == "(") return 13;
            return result;
        }
        private int StackPrecedence(string precedence)
        {
            int result = 12;

            if (precedence == "(") return 0;
            else if (precedence == "|") return 2;
            else if (precedence == "&") return 4;
            else if (precedence == "+" || precedence == "-") return 6;
            else if (precedence == "*" || precedence == "/") return 8;
            else if (precedence == "^" || precedence == ">" || precedence == "=" || precedence == "<" || precedence == "#" || precedence == "$") return 9;

            return result;
        }
        private bool ISOperator(string op)
        {
            return op == "+" || op == "-" || op == "*" || op == "/" ||
                   op == "<" || op == ">" || op == "=" || op == "#" ||
                   op == "&" || op == "|" || op == "$";
        }
        private void InitBase64()
        {
            enc64List.Clear();
            dec64List = new int[128];
            // A-Z
            for (int i = 0; i < 26; i++) enc64List.Add((char)(65 + i));
            // a-z
            for (int i = 0; i < 26; i++) enc64List.Add((char)(97 + i));
            // 0-9
            for (int i = 0; i < 10; i++) enc64List.Add((char)(48 + i));
            // + /
            enc64List.Add('+');
            enc64List.Add('/');

            for (int i = 0; i < 128; i++) dec64List[i] = -1;
            for (int i = 0; i < 64; i++) dec64List[enc64List[i]] = i;
        }
        private string Encode(string input)
        {
            StringBuilder output = new StringBuilder();
            int ptr = 0;
            int end = 0;
            char[] chars = input.ToCharArray();

            while (end == 0)
            {
                int c = (ptr < chars.Length) ? chars[ptr++] : ((end = 1) == 1 ? 0 : 0);
                int d = (ptr < chars.Length) ? chars[ptr++] : ((end += 1) == 1 ? 0 : 0);
                int e = (ptr < chars.Length) ? chars[ptr++] : ((end += 1) == 1 ? 0 : 0);

                char u = enc64List[c >> 2];
                char v = enc64List[((c & 0x03) << 4) | (d >> 4)];
                char w = enc64List[((d & 0x0F) << 2) | (e >> 6)];
                char x = enc64List[e & 0x3F];

                if (end >= 1) x = '=';
                if (end == 2) w = '=';

                if (end < 3) output.Append(u).Append(v).Append(w).Append(x);
            }
            string result = output.ToString();
            StringBuilder formatted = new StringBuilder();
            int lineLength = 76;
            for (int i = 0; i < result.Length; i += lineLength)
            {
                formatted.AppendLine(result.Substring(i, Math.Min(lineLength, result.Length - i)));
            }
            return formatted.ToString().TrimEnd();
        }
        private string Decode(string input)
        {
            StringBuilder output = new StringBuilder();
            char[] chars = input.Replace("\n", "").Replace("\r", "").ToCharArray();
            int c = 0, d = 0, e = 0, f, iVal, n = 0, ptr = 0;

            while (ptr < chars.Length)
            {
                f = chars[ptr++];
                if (f >= 0 && f < 128)
                {
                    iVal = dec64List[f];
                    if (iVal != -1)
                    {
                        if (n % 4 == 0)
                        {
                            c = iVal << 2;
                        }
                        else if (n % 4 == 1)
                        {
                            c |= (iVal >> 4);
                            d = (iVal & 0x0F) << 4;
                        }
                        else if (n % 4 == 2)
                        {
                            d |= (iVal >> 2);
                            e = (iVal & 0x03) << 6;
                        }
                        else
                        {
                            e |= iVal;
                        }

                        n++;
                        if (n % 4 == 0)
                        {
                            output.Append((char)c).Append((char)d).Append((char)e);
                        }
                    }
                }
            }

            if (n % 4 == 3) output.Append((char)c).Append((char)d);
            else if (n % 4 == 2) output.Append((char)c);

            return output.ToString();
        }
        // private string DTOC(string dateTimeParam)
        // {
        //     if (!string.IsNullOrWhiteSpace(dateTimeParam) &&
        //         dateTimeParam != "01/01/1900")
        //     {
        //         var dd = GetDateObj(dateTimeParam);
        //         if (dd.HasValue)
        //         {
        //             var mon = PrefixZero(dd.Value.Month);
        //             var year = dd.Value.Year.ToString();
        //             var date = PrefixZero(dd.Value.Day);

        //             if (_common.ApiDateFormat.ToLower() == "dd/mm/yyyy")
        //                 return $"{date}/{mon}/{year}";
        //             else
        //                 return $"{mon}/{date}/{year}";
        //         }
        //     }
        //     return string.Empty;
        // }


        // DTOC 
        private string DTOC(string dateTimeParam)
        {
            if (!string.IsNullOrWhiteSpace(dateTimeParam) &&
                dateTimeParam != "01/01/1900")
            {
                var dd = GetDateObj(dateTimeParam);
                if (dd.HasValue)
                {
                    var mon = PrefixZero(dd.Value.Month);
                    var year = dd.Value.Year.ToString();
                    var date = PrefixZero(dd.Value.Day);

                    if (_common.ApiDateFormat.ToLower() == "dd/mm/yyyy")
                        return $"{date}/{mon}/{year}";
                    else
                        return $"{mon}/{date}/{year}";
                }
            }
            return string.Empty;
        }

        private string CTOD(string strDateParam)
        {
            return DTOC(strDateParam);
        }
        private string CMonthYear(string dateTimeParam)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(dateTimeParam) && dateTimeParam != "0")
                {
                    var dd = GetDateObj(dateTimeParam);
                    if (dd.HasValue)
                    {
                        string[] monthNames = { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };
                        int mon = dd.Value.Month - 1;
                        string monthName = monthNames[mon];
                        int year = dd.Value.Year;
                        return $"{monthName} {year}";
                    }
                }
                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
        private string Stuff(string str1, string str2, string _pos)
        {
            int pos = Int32.Parse(_pos);
            if (str1 == null)
                return "";
            string str = str1.ToString();
            if (IsEmptyValue(str, str1.ToString()) == "T")
            {
                return "";
            }
            if (pos < 0 || pos > str.Length)
                return "";
            string lstr = str.Substring(0, pos);
            string rstr = str.Substring(pos);

            str = lstr + str2 + rstr;
            return str;
        }
        private double Rnd(string strReal, string strInt)
        {
            if (!double.TryParse(strReal, out double nval))
                return 0;

            if (!int.TryParse(strInt, out int x) || x == 0)
                return 0;

            int m = (int)(nval * 100);
            int h = x / 2;
            int d = m - x * (m / x);
            int amt;

            if (d >= h)
                amt = m + x - d;
            else
                amt = m - d;

            double result = amt / 100.0;

            return double.IsNaN(result) ? 0 : result;
        }
        private string IsEmptyValue(object val, string datatype)
        {
            string result = "F";

            string strVal = val?.ToString() ?? string.Empty;

            if (strVal == "01/01/1900")
                strVal = string.Empty;

            if (string.IsNullOrEmpty(strVal))
            {
                result = "T";
            }
            else if (datatype == "object" && !double.TryParse(strVal, out _))
            {
                result = "T";
            }
            else if (datatype == "d" && (strVal == "dd/mm/yyyy" || strVal == "01/01/1900"))
            {
                result = "T";
            }
            else if (datatype == "n" && double.TryParse(strVal, out double num) && num == 0)
            {
                result = "T";
            }
            return result;
        }
        private string FormatVal(string amount, string _declen)
        {
            int declen = Int32.Parse(_declen);
            if (declen <= 0) declen = 2;

            if (!double.TryParse(amount, out double i)) i = 0.0;

            string minus = "";
            if (i < 0) minus = "-";

            i = Math.Abs(i);

            double factor = Math.Pow(10, declen);
            i = Math.Truncate((i + 0.005) * factor) / factor;

            string s = i.ToString($"F{declen}");
            s = minus + s;

            return CommaFormatted(s);
        }
        private string FormatAccAmount(string amountStr, string declenStr, string MillionRep, string Malie, string Rep)
        {
            if (!double.TryParse(amountStr, out double amount))
                amount = 0;
            if (!int.TryParse(declenStr, out int declen))
                declen = 2;
            double amt = amount < 0 ? -amount : amount;

            string alie = (Malie ?? "").ToLower().Trim();
            string rp = (Rep ?? "").ToLower().Trim();

            double factor = declen > 0 ? Math.Pow(10, declen) : Math.Pow(10, 2);

            double i = amt;
            string minus = "";
            if (amount < 0) minus = "-";

            i = Math.Abs(i);

            i = Math.Truncate((i + 0.005) * factor) / factor;

            string s = i.ToString($"F{declen}");
            s = minus + s;

            string result;
            if (MillionRep == "true")
                result = CommaFormatted(s);
            else
                result = s;

            if (alie == "a" || alie == "e")
            {
                if (amount < 0 && rp == "br")
                    result = "(" + result + ")";
                else if (amount < 0)
                    result = result + " " + Rep;
            }
            else if (alie == "l" || alie == "i")
            {
                if (amount > 0 && rp == "br")
                    result = "(" + result + ")";
                else if (amount > 0)
                    result = result + " " + Rep;
            }
            return result;
        }
        private string FormatAmount(string amountStr, string declenStr, string ApplyComma, string MillionRep, string NRep, string PRep)
        {
            if (!double.TryParse(amountStr, out double amount))
                amount = 0;
            if (!int.TryParse(declenStr, out int declen))
                declen = 2;

            double amt = amount < 0 ? -amount : amount;
            double factor = declen > 0 ? Math.Pow(10, declen) : Math.Pow(10, 2);

            double i = amt;
            string minus = "";
            if (amount < 0) minus = "-";

            i = Math.Abs(i);
            i = Math.Truncate((i + 0.005) * factor) / factor;

            string s = i.ToString($"F{declen}");
            s = minus + s;

            string Result;

            if (MillionRep == "true" || MillionRep == "T")
            {
                Result = CommaFormatted(s);
            }
            else
            {
                if (!string.IsNullOrEmpty(ApplyComma) && ApplyComma.ToLower() == "t")
                {
                    Result = CommaFormatted(s);
                }
                else
                    Result = s;
            }
            if (NRep == "br" || PRep == "br")
            {
                if (amount < 0)
                    Result = "(" + Result + ")";
            }
            else
            {
                if (amount < 0)
                    Result = Result + " " + NRep;
                else
                    Result = Result + " " + PRep;
            }
            Result = TrimAll(Result);
            return Result;
        }
        private string TrimInBetweenSpaces(string input)
        {
            if (input == null)
                return "";
            return Regex.Replace(input, @"\s+", "");
        }
        private string TodaysDate()
        {
            DateTime today = DateTime.Today;
            string day = PrefixZero(today.Day);
            string month = PrefixZero(today.Month);
            string year = today.Year.ToString();
            bool isDayFirstFormat = string.Equals(_common.ApiDateFormat, "dd/MM/yyyy", StringComparison.OrdinalIgnoreCase);

            string sdt = isDayFirstFormat
                ? $"{day}/{month}/{year}"
                : $"{month}/{day}/{year}";

            return sdt;
        }
        private string Time()
        {
            DateTime dt = DateTime.Now;
            return dt.ToString("HH:mm:ss");
        }
        private string ISEMPTY(string fldVar)
        {
            if (string.IsNullOrEmpty(fldVar) || fldVar == "dd/mm/yyyy" || fldVar == "01/01/1900")
                return "T";
            else
                return "F";
        }
        private int DateDiff(string dt1, string dt2)
        {
            return DaysElapsed(dt1, dt2);
        }
        private string CheckEmail(string email)
        {
            string emailReg = @"^[a-zA-Z0-9._-]+@[a-zA-Z0-9._-]+\.[a-zA-Z]{2,4}$";
            bool output = Regex.IsMatch(email, emailReg);
            if (!output)
            {
                output = false;
            }
            return output.ToString();
        }
        private string MD5Hash(string text)
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            md5.ComputeHash(ASCIIEncoding.ASCII.GetBytes(text));
            byte[] result = md5.Hash;
            StringBuilder strBuilder = new StringBuilder();
            for (int i = 0; i < result.Length; i++)
            {
                strBuilder.Append(result[i].ToString("x2"));
            }
            return strBuilder.ToString();
        }
        //public static double Total(string fldName)
        //{
        //    ArrayList values = new ArrayList();
        //    double sum = 0;
        //    int dcno = GetDcNo(fldName);

        //    if (dcno > 0)
        //    {
        //        int rcount = GetDcRowCount(dcno);

        //        for (int nt = 1; nt <= rcount; nt++)
        //        {
        //            int rowNo = GetClientRowNo(nt, dcno);
        //            string expFldname = fldName + rowNo + "F" + dcno;

        //            if (values.Contains(expFldname))
        //            {
        //                string valStr = values[values.IndexOf(expFldname)].ToString();

        //                if (!string.IsNullOrEmpty(valStr) && valStr != "***")
        //                {
        //                    double val = 0;

        //                    if (valStr.Length > 3 && valStr.Contains(","))
        //                        val = RemoveCommas(valStr);
        //                    else
        //                        val = double.Parse(valStr);

        //                    sum += val;
        //                }
        //            }
        //        }
        //    }

        //    return sum;
        //}
        private string NumberInMillionWord(double n)
        {
            string str = n.ToString().Replace(",", "").Replace(" ", "");
            if (n == 0) return "zero";

            string[] units = { "", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine",
                           "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen",
                           "Seventeen", "Eighteen", "Nineteen" };

            string[] tens = { "", "", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety" };

            string[] scales = { "", "Thousand", "Million", "Billion", "Trillion", "Quadrillion", "Quintillion",
                            "Sextillion", "Septillion", "Octillion", "Nonillion", "Decillion", "Undecillion",
                            "Duodecillion", "Tredecillion", "Quatttuor-decillion", "Quindecillion", "Sexdecillion",
                            "Septen-decillion", "Octodecillion", "Novemdecillion", "Vigintillion", "Centillion" };

            List<string> chunks = new List<string>();
            int start = str.Length;

            while (start > 0)
            {
                int end = start;
                start = Math.Max(0, start - 3);
                chunks.Add(str.Substring(start, end - start));
            }

            if (chunks.Count > scales.Length) return "";

            List<string> words = new List<string>();
            string andWord = "and";

            for (int i = 0; i < chunks.Count; i++)
            {
                int chunkValue = int.Parse(chunks[i]);
                if (chunkValue == 0) continue;

                int[] ints = chunks[i].PadLeft(3, '0').Reverse().Select(c => int.Parse(c.ToString())).ToArray();

                if (ints[1] == 1)
                {
                    ints[0] += 10; // handle teens
                }

                // Add scale word
                if (!string.IsNullOrEmpty(scales[i])) words.Add(scales[i]);

                // Add unit word
                if (!string.IsNullOrEmpty(units[ints[0]])) words.Add(units[ints[0]]);

                // Add tens word
                if (!string.IsNullOrEmpty(tens[ints[1]])) words.Add(tens[ints[1]]);

                // Add 'and' if needed
                if (ints[0] != 0 || ints[1] != 0)
                {
                    if (ints[2] != 0 || (i + 1) > chunks.Count) words.Add(andWord);
                }

                // Add hundreds word
                if (ints[2] != 0) words.Add(units[ints[2]] + " hundred");
            }
            words.Reverse();
            return string.Join(" ", words).Trim();
        }
        private string ValidateAmountNew(string amount)
        {
            if (!long.TryParse(Math.Floor(Convert.ToDecimal(amount)).ToString(), out long junkVal))
                return string.Empty;
            if (junkVal < 0)
            {
                return string.Empty;
            }

            if (junkVal == 0)
                return string.Empty;

            char[] numReversed = junkVal.ToString("0").ToCharArray();
            Array.Reverse(numReversed);

            if (numReversed.Length > 19)
            {
                return null;
            }

            string[] iWords = { "Zero", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine" };
            string[] ePlace = { "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen" };
            string[] tensPlace = { "dummy", "Ten", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety" };

            int iWordsLength = numReversed.Length;
            string[] inWords = new string[iWordsLength];
            string finalWord = "";

            for (int i = 0; i < iWordsLength; i++)
            {
                int currentDigit = numReversed[i] - '0';
                int nextDigit = (i + 1 < iWordsLength) ? numReversed[i + 1] - '0' : 0;

                switch (i)
                {
                    case 0:
                        inWords[i] = (currentDigit == 0 || nextDigit == 1) ? "" : iWords[currentDigit];
                        break;
                    case 1:
                        inWords[i] = TensComplication(currentDigit, numReversed[i - 1] - '0', tensPlace, ePlace);
                        break;
                    case 2:
                        if (currentDigit == 0)
                            inWords[i] = "";
                        else if ((numReversed[i - 1] - '0') != 0 && (numReversed[i - 2] - '0') != 0)
                            inWords[i] = iWords[currentDigit] + " Hundred and";
                        else
                            inWords[i] = iWords[currentDigit] + " Hundred";
                        break;
                    case 3:
                        inWords[i] = (currentDigit == 0 || nextDigit == 1) ? "" : iWords[currentDigit];
                        if (nextDigit != 0 || currentDigit > 0)
                            inWords[i] += " Thousand";
                        break;
                    case 4:
                        inWords[i] = TensComplication(currentDigit, numReversed[i - 1] - '0', tensPlace, ePlace);
                        break;
                    case 5:
                        inWords[i] = (currentDigit == 0 || nextDigit == 1) ? "" : iWords[currentDigit];
                        if (nextDigit != 0 || currentDigit > 0)
                            inWords[i] += " Lakh";
                        break;
                    case 6:
                        inWords[i] = TensComplication(currentDigit, numReversed[i - 1] - '0', tensPlace, ePlace);
                        break;
                    case 7:
                        inWords[i] = (currentDigit == 0 || nextDigit == 1) ? "" : iWords[currentDigit];
                        if (nextDigit != 0 || currentDigit > 0)
                            inWords[i] += " Crore";
                        break;
                    case 8:
                        inWords[i] = TensComplication(currentDigit, numReversed[i - 1] - '0', tensPlace, ePlace);
                        break;
                    case 9:
                        inWords[i] = (currentDigit == 0 || nextDigit == 1) ? "" : iWords[currentDigit];
                        if (nextDigit != 0 || currentDigit > 0)
                            inWords[i] += " Arab";
                        break;
                    // Extend for higher denominations if needed
                    default:
                        inWords[i] = "";
                        break;
                }
            }

            Array.Reverse(inWords);
            foreach (var word in inWords)
            {
                if (!string.IsNullOrEmpty(word))
                    finalWord += " " + word;
            }

            return finalWord.Trim();
        }
        private string CurtoWords(double s, string millionRep = "f")
        {
            if (millionRep == "f")
            {
                string finalWord1 = ValidateAmountNew(s.ToString());
                string finalWord2 = "";

                string valStr = s.ToString();
                int idx = valStr.IndexOf('.');
                if (idx != -1)
                {
                    string decimals = valStr.Substring(idx + 1);
                    if (string.IsNullOrEmpty(decimals) || decimals == "00")
                    {
                        finalWord2 = "Zero paisa";
                    }
                    else
                    {
                        finalWord2 = ValidateAmountNew(decimals) + " paisa";
                    }

                    if (string.IsNullOrEmpty(finalWord1))
                        return finalWord2;
                    else
                        return finalWord1 + " and " + finalWord2;
                }
                else
                {
                    return finalWord1;
                }
            }
            else
            {
                return " " + NumberInMillionWord(s);
            }
        }
        private string CurrAmtWord(string val1, string val2, string val3, string val4, string _val5)
        {
            if (string.IsNullOrEmpty(val1)) return "";

            int val5 = Int32.Parse(_val5);

            string[] valSplit = val1.Split('.');
            string amt = CurtoWords(double.Parse(valSplit[0]), val4);

            if (valSplit.Length > 1)
            {
                string decimals = valSplit[1];
                while (decimals.Length < val5)
                    decimals += "0";

                string amtDec = CurtoWords(double.Parse(decimals), val4);

                if (!string.IsNullOrEmpty(amt))
                    return $"{val2}{amt} and {val3} {amtDec} only";
                else
                    return $"{val3} {amtDec} only";
            }
            else
            {
                return $"{val2}{amt} only";
            }
        }

        //public string GetGridCellValue(string fldname, string ActiveRow, bool isActual)
        //{
        //    int dcno = GetDcNo(fldname);              // Your helper method
        //    string fldIndx = GetFieldIndex(fldname);  // Your helper method
        //    string fldType = FDataType.ContainsKey(fldIndx) ? FDataType[fldIndx] : "";

        //    if (!isActual)
        //        ActiveRow = GetClientRowNo(ActiveRow, dcno);

        //    string newrow = GetRowNoHelper(ActiveRow);

        //    if (!IsDcGrid(dcno) && ActiveRow == "1")
        //        newrow = "//}000";

        //    string newFldName = fldname + newrow + "F" + dcno;
        //    string newFldVal = "";

        //    if (!string.IsNullOrEmpty(newrow))
        //    {
        //        if (fieldValues.ContainsKey(newFldName))
        //        {
        //            newFldVal = fieldValues[newFldName];

        //            if (fldType.ToLower() == "numeric")
        //                newFldVal = RemoveCommas(newFldVal);
        //        }
        //    }

        //    if (string.IsNullOrEmpty(newFldVal) && fldType.ToLower() == "numeric")
        //        newFldVal = "0";

        //    if (fldType.ToLower() == "numeric")
        //        newFldVal = "n" + newFldVal;
        //    else
        //        newFldVal = "s" + newFldVal;

        //    return newFldVal;

        public string MakeDate(string _dd, string _mon, string _yy)
        {
            int dd = int.Parse(_dd);
            int mon = int.Parse(_mon);
            int yy = int.Parse(_yy);

            string day = PrefixZero(dd);
            string month = PrefixZero(mon);
            string resultDate;

            if (_common.ApiDateFormat.ToLower() == "dd/mm/yyyy")
                resultDate = $"{day}/{month}/{yy}";
            else
                resultDate = $"{month}/{day}/{yy}";

            return resultDate;
        }

        //public string GetAllValues(string fld, int F, Dictionary<string, string> fieldValues)
        //{
        //    string roles = "";
        //    string roles1 = "";
        //    int count = 0;
        //    int dcno = GetDcNo(fld);

        //    int rcount = int.Parse(GetDcRowCount(dcno));
        //    for (int nt = 0; nt <= rcount; nt++)
        //    {
        //        string newprevrow = GetClientRowNo(nt, dcno);
        //        string expFldname = fld + newprevrow + "F" + dcno;

        //        if (fieldValues.ContainsKey(expFldname))
        //        {
        //            string val = fieldValues[expFldname];
        //            if (count == 0)
        //            {
        //                roles = val;
        //                if (!string.IsNullOrEmpty(roles) && roles != "***")
        //                {
        //                    string[] rl = roles.Split(new string[] { ",," }, StringSplitOptions.None);
        //                    roles = rl[0];
        //                    count++;
        //                }
        //            }
        //            else
        //            {
        //                roles1 = val;
        //                if (!string.IsNullOrEmpty(roles1) && roles1 != "***")
        //                {
        //                    string[] rl = roles1.Split(new string[] { ",," }, StringSplitOptions.None);
        //                    roles += "," + rl[0];
        //                }
        //            }
        //        }
        //    }

        //    return roles;
        //}

        public int MonthOfDate(string dt)
        {
            return ReturnDMYOfDate(dt, "month");
        }
        public int YearOfDate(string dt)
        {
            return ReturnDMYOfDate(dt, "year");
        }
        public int DayOfDate(string dt)
        {
            return ReturnDMYOfDate(dt, "day");
        }
        public int ReturnDMYOfDate(string dt, string src)
        {
            if (IsValidDate(dt) == 0)
            {
                var arrdt = dt.Split(dtCh);
                int result = 0;
                if (_common.ApiDateFormat.ToLower() == "dd/mm/yyyy")
                {
                    if (src == "day") result = int.Parse(arrdt[0]);
                    else if (src == "month") result = int.Parse(arrdt[1]);
                    else if (src == "year") result = int.Parse(arrdt[2]);
                }
                else
                {
                    if (src == "day") result = int.Parse(arrdt[1]);
                    else if (src == "month") result = int.Parse(arrdt[0]);
                    else if (src == "year") result = int.Parse(arrdt[2]);
                }
                return result;
            }
            else
            {
                return 0;
            }
        }
        public int IsValidDate(string dtStr)
        {
            if (string.IsNullOrEmpty(dtStr) || dtStr.ToLower() == _common.ApiDateFormat.ToLower())
                return -1;

            int[] daysInMonth = DaysArray();
            int pos1 = dtStr.IndexOf(dtCh);
            int pos2 = dtStr.IndexOf(dtCh, pos1 + 1);

            string strDay = "";
            string strMonth = "";
            string strYear = "";

            if (_common.ApiDateFormat.ToLower() == "mm/dd/yyyy")
            {
                strMonth = dtStr.Substring(0, pos1);
                strDay = dtStr.Substring(pos1 + 1, pos2 - pos1 - 1);
            }
            else
            {
                strDay = dtStr.Substring(0, pos1);
                strMonth = dtStr.Substring(pos1 + 1, pos2 - pos1 - 1);
            }
            strYear = dtStr.Substring(pos2 + 1);

            strDay = strDay.TrimStart('0');
            strMonth = strMonth.TrimStart('0');
            strYear = strYear.TrimStart('0');

            if (!int.TryParse(strDay, out int day) || !int.TryParse(strMonth, out int month) || !int.TryParse(strYear, out int year))
                return 5;

            if (pos1 == -1 || pos2 == -1) return 1;
            if (month < 1 || month > 12) return 2;
            if (day < 1 || day > 31 || (month == 2 && day > DaysInFebruary(year)) || day > daysInMonth[month]) return 3;
            if (strYear.Length != 4 || year == 0 || year < minYear || year > maxYear) return 4;

            string stripped = StripCharsInBag(dtStr, dtCh.ToString());
            if (!IsInteger(stripped)) return 5;

            return 0;
        }
        private bool IsInteger(string s)
        {
            return s.All(char.IsDigit);
        }
        private string StripCharsInBag(string s, string bag)
        {
            return new string(s.Where(c => !bag.Contains(c)).ToArray());
        }
        private int DaysInFebruary(int year)
        {
            return DateTime.IsLeapYear(year) ? 29 : 28;
        }
        private int[] DaysArray()
        {
            int[] days = new int[13]; // 1-based index
            for (int i = 1; i <= 12; i++)
            {
                if (i == 4 || i == 6 || i == 9 || i == 11)
                    days[i] = 30;
                else if (i == 2)
                    days[i] = 29;
                else
                    days[i] = 31;
            }
            return days;
        }

        //public int GetMinValue(string fldname, int dcno, int rowCount, Func<int, string> getValueFunc)
        //{
        //    var arrValues = GetFieldValues(fldname, dcno, rowCount, getValueFunc)
        //                    .Select(v => double.TryParse(v, out var d) ? d : double.MaxValue)
        //                    .ToList();

        //    double minValue = arrValues.Min();

        //    // Return the 1-based index of first occurrence
        //    for (int j = 0; j < arrValues.Count; j++)
        //    {
        //        if (arrValues[j] == minValue)
        //        {
        //            return j + 1; // same as JS index logic
        //        }
        //    }
        //    return 0;
        //}

        //public int GetMaxValue(string fldname, int dcno, int rowCount, Func<int, string> getValueFunc)
        //{
        //    var arrValues = GetFieldValues(fldname, dcno, rowCount, getValueFunc)
        //                    .Select(v => double.TryParse(v, out var d) ? d : double.MinValue)
        //                    .ToList();

        //    double maxValue = arrValues.Max();

        //    // Return the 1-based index of first occurrence
        //    for (int j = 0; j < arrValues.Count; j++)
        //    {
        //        if (arrValues[j] == maxValue)
        //        {
        //            return j + 1; // same as JS index logic
        //        }
        //    }
        //    return 0;
        //}

        public string RegisterRegVariables(string varName, string varType, string varValue)
        {
            bool isAlreadyFound = false;
            if (!string.IsNullOrEmpty(varName))
            {
                for (int x = 0; x < VarList.Count; x++)
                {
                    if (VarList[x] == varName)
                    {
                        ValueList[x] = varValue;
                        isAlreadyFound = true;
                        break;
                    }
                }
                if (!isAlreadyFound)
                {
                    VarList.Add(varName);
                    ValueList.Add(varValue);
                    VarTypeList.Add(varType);
                    VarTypeStr += varType;
                }
                bool isext = false;
                for (int i = 0; i < RegVarFldList.Count; i++)
                {
                    if (RegVarFldList[i].StartsWith(varName + ":"))
                    {
                        RegVarFldList[i] = $"{varName}:{varType}:{varValue}";
                        isext = true;
                        break;
                    }
                }
                if (!isext)
                {
                    RegVarFldList.Add($"{varName}:{varType}:{varValue}");
                }
            }
            return "";
        }
        private int GetMod(string fld1, string fld2)
        {
            if (!int.TryParse(fld2, out int num2) || num2 == 0)
                return 0;

            if (!int.TryParse(fld1, out int num1))
                return 0;

            return num1 % num2;
        }
        private double AxCeil(string n)
        {
            if (double.TryParse(n, out double num))
                return Math.Ceiling(num);
            else
                return 0;
        }
        private double AxFloor(string n)
        {
            if (double.TryParse(n, out double num))
                return Math.Floor(num);
            else
                return 0;
        }
        private int NetworkDays(string start, string end, string holidays)
        {
            DateTime? stDate = GetDateObj(start);
            DateTime? enDate = GetDateObj(end);

            if (!stDate.HasValue || !enDate.HasValue)
                return -1;

            if (stDate.Value > enDate.Value)
                return -1;

            int days = 0;
            DateTime currentDate = stDate.Value;

            while (currentDate <= enDate.Value)
            {
                if (currentDate.DayOfWeek != DayOfWeek.Saturday && currentDate.DayOfWeek != DayOfWeek.Sunday)
                    days++;

                currentDate = currentDate.AddDays(1);
            }
            int _holidays = 0;
            if (holidays == null || holidays == "")
                _holidays = 0;
            else
                _holidays = int.Parse(holidays);
            return days - _holidays;
        }
        private int Days360(string start, string end, string _method)
        {
            DateTime? stDate = GetDateObj(start);
            DateTime? enDate = GetDateObj(end);

            if (!stDate.HasValue || !enDate.HasValue)
                return 0;

            DateTime sDate = stDate.Value;
            DateTime eDate = enDate.Value;
            bool method = (_method == null || _method == "") ? false : (_method == "true" ? true : false);
            if (method) // US method
            {
                if (sDate.Day == 31) sDate = sDate.AddDays(-1);
                if (eDate.Day == 31) eDate = eDate.AddDays(-1);
            }
            else // European method
            {
                DateTime lastDateOfStartMonth = new DateTime(sDate.Year, sDate.Month, DateTime.DaysInMonth(sDate.Year, sDate.Month));
                if (sDate.Day == lastDateOfStartMonth.Day)
                    sDate = new DateTime(sDate.Year, sDate.Month, 30);

                DateTime lastDateOfEndMonth = new DateTime(eDate.Year, eDate.Month, DateTime.DaysInMonth(eDate.Year, eDate.Month));
                if (eDate.Day == lastDateOfEndMonth.Day && sDate.Day < 30)
                    eDate = new DateTime(eDate.Year, eDate.Month, 30);
            }

            int result = (((eDate.Year - sDate.Year) * 12) + (eDate.Month - sDate.Month)) * 30 + (eDate.Day - sDate.Day);
            return result;
        }
        private int GetStringPos(string subStr, string str, string separator)
        {
            if (string.IsNullOrEmpty(str))
                return -1;
            if (string.IsNullOrEmpty(separator))
                separator = ",";
            string[] strArray = str.Split(new string[] { separator }, StringSplitOptions.None);
            for (int i = 0; i < strArray.Length; i++)
            {
                if (strArray[i] == subStr)
                    return i;
            }
            return -1;
        }

        public string CheckSpecialCharInGlobalVars(string str)
        {
            return str.Replace(";bkslh", "\\");
        }

        public string RevCheckSpecialCharInGlobalVars(string str)
        {
            return str.Replace("\\", ";bkslh");
        }

        public bool IsDelimiter(char who)
        {
            return who == ' ' || who == '(' || who == ')' || who == ',';
        }

        public void RegisterVariables(string varName, string varType, string varValue)
        {
            if (string.IsNullOrEmpty(varName))
                return;

            int index = VarList.IndexOf(varName);

            if (index >= 0)
            {
                ValueList[index] = varValue;
                VarTypeList.Add(varType);
                VarTypeStr += varType;
            }
            else
            {
                VarList.Add(varName);
                ValueList.Add(varValue);
                VarTypeList.Add(varType);
                VarTypeStr += varType;
            }
        }


        #region Eval functions from javascript
        ////Eval functions
        //public object Evaluate(string a, string b, string Expstr, string exprtype, string fromIview)
        //{
        //    if (!string.IsNullOrEmpty(Expstr))
        //        Expstr = Expstr.Replace('♦', ',');

        //    expressiontype = exprtype;
        //    prerow = b;
        //    int i = 0;
        //    string tem = "";
        //    varlist = new List<string>();
        //    varvaluelist = new List<string>();
        //    wordlist = new List<string>();
        //    //int w = 0;
        //    //expFldname = a;
        //    //int fIndx = a.LastIndexOf("F");
        //    //if (fIndx != -1)
        //    //    rowno = a.Substring(fIndx - 3);
        //    //else
        //    //    rowno = "";
        //    //int curActiveRowNo = 0;
        //    //if (!string.IsNullOrEmpty(AxActiveRowNo))
        //    //    curActiveRowNo = int.Parse(AxActiveRowNo);
        //    //int activerowno = curActiveRowNo == 0 ? int.Parse(a.Substring(fIndx - 3, 3)) : curActiveRowNo;
        //    //rno = int.Parse(rowno.Substring(0, 3));
        //    //frmno = rowno.Substring(rowno.Length - 1, 1);
        //    //Expstr = CheckEval(Expstr, b);
        //    //string expression = Expstr;
        //    //if (Parameters.Count > 1)
        //    //{
        //    //    foreach (var param in Parameters)
        //    //    {
        //    //        var list = param.Split('~');
        //    //        varlist.Add(list[0]);
        //    //        valuelist.Add(CheckSpecialCharInGlobalVars(list[1]));
        //    //        w++;
        //    //    }
        //    //}
        //    //varlist.Add("activerow");
        //    //valuelist.Add(activerowno.ToString());

        //    string expression = Expstr;
        //    expression = TrimAll(expression) + " ";
        //    var ch = expression.ToCharArray();
        //    string wordstring = "";
        //    int bcount = 0;
        //    string brackets = "";
        //    bool constword = false;
        //    string sw = string.Empty;
        //    for (int j = 0; j < ch.Length; j++)
        //    {
        //        if (ch[j] == '{')
        //        {
        //            if (bcount > 0)
        //            {
        //                wordstring += ch[j];
        //            }
        //            else
        //            {
        //                constword = true;
        //                wordstring = "";
        //            }
        //            bcount++;
        //            continue;
        //        }

        //        if (ch[j] == '}')
        //        {
        //            if (bcount == 1)
        //            {
        //                constword = false;
        //                sw = "c" + wordstring;
        //                wordlist[i] = sw;
        //                i++;
        //                bcount = 0;
        //                wordstring = "";
        //            }
        //            else
        //            {
        //                bcount--;
        //                wordstring += ch[j];
        //            }
        //            continue;
        //        }

        //        if (constword)
        //        {
        //            wordstring += ch[j];
        //            continue;
        //        }
        //        string t;

        //        if (IsDelimiter(ch[j]) || ISOperator(ch[j].ToString()))
        //        {
        //            bool chrstate = false;
        //            tem = wordstring;
        //            tem = tem.ToLower();

        //            if (!string.IsNullOrEmpty(tem) && !double.TryParse(tem, out _))
        //            {
        //                // Try to find the variable in varlist
        //                int varIndex = varlist.FindIndex(v => v.Equals(wordstring, StringComparison.OrdinalIgnoreCase));
        //                if (varIndex != -1)
        //                {
        //                    chrstate = true;
        //                    string elevalue = varvaluelist[varIndex];
        //                    string vartype = vartypelist[varIndex];

        //                    if (vartype == "Date/Time")
        //                    {
        //                        sw = "d" + elevalue;
        //                    }
        //                    else if (vartype == "Character" || vartype == "Text")
        //                    {
        //                        sw = "s" + elevalue;
        //                    }
        //                    else if (vartype == "Numeric")
        //                    {
        //                        if (string.IsNullOrEmpty(elevalue)) elevalue = "0";
        //                        sw = "n" + elevalue;
        //                        if (sw.Length == 1)
        //                            sw = "n0";
        //                    }
        //                    else
        //                    {
        //                        // Default to string if type is unknown
        //                        sw = "s" + elevalue;
        //                    }
        //                    wordlist[i] = sw;
        //                    i++;
        //                }
        //            }

        //            // Function list check (funclist must be defined elsewhere)
        //            for (int r = 0; r < funclist.Count; r++)
        //            {
        //                if (wordstring.Equals(funclist[r], StringComparison.OrdinalIgnoreCase))
        //                {
        //                    chrstate = true;
        //                    sw = "f" + funclist[r];
        //                    wordlist[i] = sw;
        //                    i++;
        //                    break;
        //                }
        //            }

        //            if (!chrstate)
        //            {
        //                if (!string.IsNullOrEmpty(wordstring))
        //                {
        //                    if (!double.TryParse(wordstring, out _))
        //                        t = "s";
        //                    else
        //                        t = "n";

        //                    wordlist[i] = t + wordstring;
        //                    i++;
        //                }
        //            }
        //            wordstring = "";
        //        }
        //        else if (ch[j] == '(')
        //        {
        //            if (wordlist.Count > 0)
        //            {
        //                var fchar = wordlist[wordlist.Count - 1];
        //                fchar = fchar.Substring(0, 1);
        //                if (fchar == "f")
        //                    brackets += "f";
        //                else
        //                    brackets += "(";
        //            }
        //            else
        //            {
        //                brackets += "(";
        //            }
        //            wordlist[i] = "b(";
        //            i++;
        //            continue;
        //        }
        //        else if (ch[j] == ')')
        //        {
        //            if (brackets[brackets.Length - 1] == '(')
        //            {
        //                wordlist[i] = "b)";
        //                i++;
        //            }
        //            else
        //            {
        //                wordlist[i] = "e";
        //                i++;
        //            }
        //            brackets = brackets.Substring(0, brackets.Length - 1);
        //            continue;
        //        }
        //        else if (ISOperator(ch[j].ToString()))
        //        {
        //            wordlist[i] = "o" + ch[j];
        //            i++;
        //            continue;
        //        }
        //        else if (ch[j] == ' ')
        //        {
        //            continue;
        //        }
        //        else if (ch[j] == ',')
        //        {
        //            wordlist[i] = "l,";
        //            i++;
        //            wordstring = "";
        //            continue;
        //        }
        //        wordstring += ch[j];
        //    }

        //    bool fun = true;
        //    foreach (var word in wordlist)
        //    {
        //        if (word == "sFIELDCHANGED" || word.ToLower() == "sfieldchanged")
        //            fun = false;
        //    }
        //    if (fun)
        //    {
        //        EvalFun();
        //        var s = GetExprString();
        //        if (string.IsNullOrEmpty(s))
        //            return "";
        //        try
        //        {
        //            object res = null;
        //            if (!string.IsNullOrEmpty(s) && !double.TryParse(s, out _) && s.Contains("\\"))
        //            {
        //                s = s.Replace("\\", ";bkslh");
        //                res = EvaluateExpression(s); // You need to implement a safe expression evaluator
        //                res = res.ToString().Replace(";bkslh", "\\");
        //            }
        //            else
        //            {
        //                res = EvaluateExpression(s); // Implement this
        //            }
        //            if (res.ToString() == "Infinity") res = 0;
        //            if (res != null && res.ToString().Contains("♠♥"))
        //                res = res.ToString().Replace("♠♥", "\"");
        //            return res;
        //        }
        //        catch
        //        {
        //            try
        //            {
        //                s = s.Replace("\\", ";bkslh");
        //                var res = EvaluateExpression("'" + s + "'"); // Implement this
        //                res = res.ToString().Replace(";bkslh", "\\");
        //                return res;
        //            }
        //            catch
        //            {
        //                return "";
        //            }
        //        }
        //    }
        //    else
        //    {
        //        return true;
        //    }
        //}
        //public void EvalFun()
        //{
        //    List<object> @params = new List<object>();
        //    for (int i = wordlist.Count - 1; i >= 0; i--)
        //    {
        //        string ch = wordlist[i];
        //        string strch = ch;
        //        char firstchar = ch.Length > 0 ? ch[0] : '\0';

        //        string EvalString = "", EvalType = "", ExtEvalString = "", EvalResult = "";
        //        int ParamIndx = 0;
        //        string bracketClose = "", bracketOpen = "";

        //        if (firstchar == 'f')
        //        {
        //            wordlist[i + 1] = null;
        //            int j = i + 2;
        //            string exprEvalFun = "";

        //            string functionname = strch.Substring(1);
        //            functionname = functionname.ToLower();

        //            while (wordlist[j] != "e")
        //            {
        //                string value = "";
        //                if (!string.IsNullOrEmpty(wordlist[j]))
        //                {
        //                    ch = wordlist[j];
        //                    strch = ch;
        //                    firstchar = ch[0];
        //                    value = strch.Substring(1);

        //                    if (wordlist[j] == "b(")
        //                    {
        //                        EvalString += "(";
        //                        bracketOpen += "(";
        //                        EvalType = "";
        //                    }
        //                    else if (wordlist[j] == "b)")
        //                    {
        //                        EvalString += ")";
        //                        bracketClose += ")";
        //                    }
        //                    else if (wordlist[j] == "o<" || wordlist[j] == "o>" || wordlist[j] == "o=" || wordlist[j] == "o#")
        //                    {
        //                        char op = ch[1];
        //                        if (op == '=') op = '=';
        //                        else if (op == '#') op = '!';
        //                        EvalString += (op == '!' ? "!=" : (op == '=' ? "==" : op.ToString()));
        //                    }
        //                    else if (wordlist[j] == "o+" || wordlist[j] == "o-" || wordlist[j] == "o*" || wordlist[j] == "o/")
        //                    {
        //                        char op = ch[1];
        //                        if (EvalString != "")
        //                            EvalString += op;
        //                        else if (ExtEvalString != "")
        //                            ExtEvalString += op;
        //                    }
        //                    else if (firstchar == 'o')
        //                    {
        //                        char op = ch[1];
        //                        ExtEvalString += EvalString + op + op;
        //                        bracketClose = "";
        //                        bracketOpen = "";
        //                        EvalString = "";
        //                        EvalType = "";
        //                    }
        //                    else if (wordlist[j] == "l,")
        //                    {
        //                        var evalRes = GetEvalResult(EvalString, EvalType)?.ToString();
        //                        EvalResult = evalRes ?? "";

        //                        if (EvalResult != "")
        //                        {
        //                            if (EvalType == "d" && !EvalResult.StartsWith("\""))
        //                            {
        //                                if (ExtEvalString.Contains("&&") || ExtEvalString.Contains("||"))
        //                                    ExtEvalString += EvalString;
        //                                else
        //                                    ExtEvalString += "\"" + EvalResult + "\"";
        //                            }
        //                            else if (EvalType == "s" && !EvalResult.StartsWith("\""))
        //                                ExtEvalString += "\"" + EvalResult + "\"";
        //                            else
        //                                ExtEvalString += EvalResult;

        //                            EvalResult = GetEvalResult(ExtEvalString, EvalType)?.ToString();
        //                        }
        //                        else if (EvalResult != null)
        //                        {
        //                            if (EvalType == "d" && (ExtEvalString.Contains("&&") || ExtEvalString.Contains("||")))
        //                                ExtEvalString += EvalString;
        //                            else
        //                                ExtEvalString += EvalResult;

        //                            EvalResult = GetEvalResult(ExtEvalString, EvalType)?.ToString();
        //                        }

        //                        SetParam(@params, ParamIndx, EvalResult);
        //                        ParamIndx++;
        //                        bracketClose = bracketOpen = EvalResult = EvalType = EvalString = ExtEvalString = "";
        //                    }
        //                    else
        //                    {
        //                        if (firstchar == 'n')
        //                        {
        //                            if (string.IsNullOrEmpty(value)) value = "0";
        //                            EvalString += value;
        //                            EvalType = "n";
        //                        }
        //                        else if (firstchar == 'd')
        //                        {
        //                            EvalType = "d";
        //                            if (value == "") value = "01/01/1900";
        //                            EvalString += "\"" + value + "\"";
        //                        }
        //                        else if (firstchar == 's' || firstchar == 'c')
        //                        {
        //                            if ((value.Contains("/") && value.Split('/').Length == 3) || EvalType == "d")
        //                            {
        //                                value = value.Replace("\"", "");
        //                                var dtlenth = value.Split('/');
        //                                if (dtlenth[0].Length == 2 && dtlenth[1].Length == 2 && dtlenth[2].Length == 4)
        //                                {
        //                                    EvalType = "d";
        //                                    EvalString += "\"" + value + "\"";
        //                                }
        //                                else if (!value.StartsWith("\""))
        //                                {
        //                                    EvalType = "s";
        //                                    EvalString += "\"" + value + "\"";
        //                                }
        //                                else
        //                                {
        //                                    EvalType = "s";
        //                                    EvalString += value;
        //                                }
        //                            }
        //                            else if (!value.StartsWith("\""))
        //                            {
        //                                EvalType = "s";
        //                                EvalString += "\"" + value + "\"";
        //                            }
        //                            else
        //                            {
        //                                EvalType = "s";
        //                                EvalString += value;
        //                            }
        //                        }

        //                        SetParam(@params, ParamIndx, EvalString);
        //                    }
        //                }

        //                wordlist[j] = null; // delete
        //                j++;

        //                if (wordlist[j] == "e")
        //                {
        //                    try
        //                    {
        //                        if (EvalString != "")
        //                            SetParam(@params, ParamIndx, EvaluateExpression(EvalString));
        //                        else if (ExtEvalString != "")
        //                            SetParam(@params, ParamIndx, EvaluateExpression(ExtEvalString));
        //                    }
        //                    catch
        //                    {
        //                        SetParam(@params, ParamIndx, EvalString);
        //                    }

        //                    var val = @params[ParamIndx]?.ToString();
        //                    if (val == "Infinity" || val == "NaN")
        //                        SetParam(@params, ParamIndx, 0);
        //                }
        //            }

        //            wordlist[j] = null; // delete
        //            var paramArray = @params.Select(p => p?.ToString()).ToArray();
        //            wordlist[i] = CallFunction(functionname, paramArray)?.ToString();
        //            @params = new List<object>();
        //            ParamIndx = 0;
        //        }
        //    }
        //}
        //public void SetParam(List<object> list, int index, object value)
        //{
        //    if (index < list.Count)
        //        list[index] = value;
        //    else
        //    {
        //        while (list.Count < index)
        //            list.Add(null);
        //        list.Add(value);
        //    }
        //}
        //public string GetEvalResult(string EvalString, string EvalType)
        //{
        //    string Result = "";

        //    if (EvalType == "d" && !string.IsNullOrEmpty(EvalString) && EvalString.Split('/').Length >= 3)
        //    {
        //        if (EvalString.Contains("&&") || EvalString.Contains("||"))
        //        {
        //            var regex = new Regex("\"([^\"]+)\"\\s*(>|<|==|!=)\\s*\"([^\"]+)\"");
        //            string _evalString = regex.Replace(EvalString, m =>
        //            {
        //                try
        //                {
        //                    string Date1 = m.Groups[1].Value.Trim();
        //                    string Date2 = m.Groups[3].Value.Trim();
        //                    string Operator = m.Groups[2].Value == "==" ? "=" : m.Groups[2].Value;
        //                    bool res = CompareDate(Date1, Date2, Operator);
        //                    return res ? "true" : "false";
        //                }
        //                catch
        //                {
        //                    return m.Value;
        //                }
        //            });

        //            Result = EvaluateExpression(_evalString);
        //        }
        //        else if (EvalString.Contains("<") || EvalString.Contains(">") || EvalString.Contains("==") ||
        //                 EvalString.Contains("!=") || EvalString.Contains("-"))
        //        {
        //            string op = EvalString.Contains("<") ? "<" :
        //                        EvalString.Contains(">") ? ">" :
        //                        EvalString.Contains("==") ? "==" :
        //                        EvalString.Contains("!=") ? "!=" : "-";

        //            var parts = EvalString.Split(new string[] { op }, StringSplitOptions.None);
        //            string Date1 = parts[0].Replace("\"", "");
        //            string Date2 = parts[1].Replace("\"", "");

        //            if (op == "-")
        //                Result = SubtractDate(Date1, Date2, op).ToString();
        //            else
        //                Result = CompareDate(Date1, Date2, op).ToString();
        //        }
        //        else
        //            Result = EvaluateExpression(EvalString).ToString();
        //    }
        //    else
        //    {
        //        try
        //        {
        //            Result = EvaluateExpression(EvalString).ToString();
        //        }
        //        catch
        //        {
        //            Result = EvalString;
        //        }
        //    }

        //    if (Result == "Infinity" || Result == "NaN")
        //        Result = "0";
        //    if (Result == null)
        //        Result = EvalString;

        //    return Result;
        //}
        //public bool CompareDate(string dt1, string dt2, string op)
        //{
        //    dt1 = ReplaceSpecialCharInDate(dt1);
        //    dt2 = ReplaceSpecialCharInDate(dt2);

        //    if (op == "!=")
        //    {
        //        if (string.IsNullOrEmpty(dt1)) dt1 = "01/01/1900";
        //        if (string.IsNullOrEmpty(dt2)) dt2 = "01/01/1900";
        //        return dt1 != dt2;
        //    }

        //    if (!string.IsNullOrEmpty(dt1) && !string.IsNullOrEmpty(dt2))
        //    {
        //        DateTime? startdt1 = GetDateObj(dt1);
        //        DateTime? enddt2 = GetDateObj(dt2);

        //        if (startdt1.HasValue && enddt2.HasValue)
        //        {
        //            if (op == "<")
        //                return startdt1.Value < enddt2.Value;
        //            else if (op == ">")
        //                return startdt1.Value > enddt2.Value;
        //            else if (op == "=")
        //                return startdt1.Value.Date == enddt2.Value.Date;
        //        }
        //    }

        //    return false;
        //}
        //public double SubtractDate(string dt1, string dt2, string op)
        //{
        //    dt1 = ReplaceSpecialCharInDate(dt1);
        //    dt2 = ReplaceSpecialCharInDate(dt2);
        //    double days = 0;

        //    if (!string.IsNullOrEmpty(dt1) && !string.IsNullOrEmpty(dt2))
        //    {
        //        DateTime? startdt1 = GetDateObj(dt1);
        //        DateTime? enddt2 = GetDateObj(dt2);

        //        if (startdt1.HasValue && enddt2.HasValue)
        //        {
        //            days = (startdt1.Value - enddt2.Value).TotalDays;
        //        }
        //    }

        //    return days;
        //}
        //public string ReplaceSpecialCharInDate(string dt)
        //{
        //    if (dt.Contains("("))
        //        dt = dt.Replace("(", "");
        //    if (dt.Contains(")"))
        //        dt = dt.Replace(")", "");
        //    return dt;
        //}
        //private string GetExprString()
        //{
        //    string s = string.Empty;
        //    string variable = string.Empty;

        //    for (int i = 0; i < wordlist.Count; i++)
        //    {
        //        if (!string.IsNullOrEmpty(wordlist[i]))
        //        {
        //            string ch = wordlist[i];
        //            string strch = ch;
        //            char firstchar = ch[0];

        //            if (firstchar == 'e')
        //            {
        //                variable = ")";
        //            }
        //            else if (firstchar == 's' || firstchar == 'c')
        //            {
        //                if (strch.Length > 1 && strch[1] != '"')
        //                    variable = "\"" + strch.Substring(1) + "\"";
        //                else if (strch.Length > 1 && strch.Substring(1) == "")
        //                    variable = "\"" + strch.Substring(1) + "\"";
        //                else
        //                    variable = strch.Substring(1);
        //            }
        //            else if (firstchar == 'd')
        //            {
        //                variable = "\"" + strch.Substring(1) + "\"";
        //            }
        //            else
        //            {
        //                if (firstchar == 'n')
        //                {
        //                    variable = strch.Substring(1);
        //                    if (int.TryParse(variable, out int num) && num < 0)
        //                    {
        //                        variable = "(" + variable + ")";
        //                    }
        //                }
        //                else
        //                {
        //                    variable = strch.Substring(1);
        //                }
        //            }

        //            s += variable;
        //        }
        //    }

        //    return s;
        //}

        #endregion



        // Delphi code converted to c#
        private List<string> cpf = new List<string>();
        private List<string> StkList = new List<string>();
        private List<string> StkTypeList = new List<string>();
        private List<string> NewExprTypeList = new List<string>();
        private bool DynamicCompute { get; set; }
        public string Compute(string value1, string value2, string op, string expressionType)
        {
            //string ExpressionType = expressionType[0].ToString();

            if (expressionType.Trim().ToLower() == "s")
                expressionType = "c";

            if (value1 == "~e~") value1 = "";
            if (value2 == "~e~") value2 = "";

            // ---------- NUMERIC ----------
            if (expressionType.ToLower() == "n")
            {
                if (string.IsNullOrWhiteSpace(value1) || value1.Trim().ToLower() == "null") value1 = "0";
                if (string.IsNullOrWhiteSpace(value2) || value2.Trim().ToLower() == "null") value2 = "0";

                double v1 = double.TryParse(value1, out var tmp1) ? tmp1 : 0;
                double v2 = double.TryParse(value2, out var tmp2) ? tmp2 : 0;

                switch (op)
                {
                    case "+": return (v1 + v2).ToString(CultureInfo.InvariantCulture);
                    case "-": return (v1 - v2).ToString(CultureInfo.InvariantCulture);
                    case "*": return (v1 * v2).ToString(CultureInfo.InvariantCulture);
                    case "/": return v2 == 0 ? "0" : (v1 / v2).ToString(CultureInfo.InvariantCulture);
                    case ">": return v1 > v2 ? "1" : "0";
                    case "<": return v1 < v2 ? "1" : "0";
                    case "=": return v1 == v2 ? "1" : "0";
                    case "#": return v1 == v2 ? "0" : "1";
                    case "&": return (value1 == "1" && value2 == "1") ? "1" : "0";
                    case "|": return (value1 == "1" || value2 == "1") ? "1" : "0";
                }
            }

            // ---------- CHARACTER / STRING ----------
            if (expressionType.ToLower() == "c")
            {
                if (value1.Trim().ToLower() == "null") value1 = "";
                if (value2.Trim().ToLower() == "null") value2 = "";

                switch (op)
                {
                    case "+": return value1 + value2;
                    case "=": return value1 == value2 ? "1" : "0";
                    case "#": return value1 == value2 ? "0" : "1";
                    case "&": return (value1 == "1" && value2 == "1") ? "1" : "0";
                    case ">": return (string.Compare(value1, value2, StringComparison.Ordinal) > 0) ? "1" : "0";
                    case "<": return (string.Compare(value1, value2, StringComparison.Ordinal) < 0) ? "1" : "0";
                    case "$": return value2.Contains(value1) ? "1" : "0";
                    case "|": return (value1 == "1" || value2 == "1") ? "1" : "0";
                }
            }

            // ---------- DATE ----------
            if (expressionType.ToLower() == "d")
            {
                if (string.IsNullOrWhiteSpace(value1) || value1.Trim().ToLower() == "null")
                    value1 = "01/01/1900";
                if (string.IsNullOrWhiteSpace(value2) || value2.Trim().ToLower() == "null")
                    value2 = "01/01/1900";

                DateTime d1 = DateTime.TryParseExact(value1, _common.ApiDateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var t1)
                    ? t1 : DateTime.ParseExact("01/01/1900", _common.ApiDateFormat, CultureInfo.InvariantCulture);

                DateTime d2 = DateTime.TryParseExact(value2, _common.ApiDateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var t2)
                    ? t2 : DateTime.ParseExact("01/01/1900", _common.ApiDateFormat, CultureInfo.InvariantCulture);

                switch (op)
                {
                    case "+": return (d1.AddDays(d2.Day)).ToOADate().ToString(CultureInfo.InvariantCulture);
                    case "-": return (d1 - d2).TotalDays.ToString(CultureInfo.InvariantCulture);
                    case ">": return d1 > d2 ? "1" : "0";
                    case "<": return d1 < d2 ? "1" : "0";
                    case "=": return d1 == d2 ? "1" : "0";
                    case "#": return d1 == d2 ? "0" : "1";
                    case "&": return (value1 == "1" && value2 == "1") ? "1" : "0";
                    case "|": return (value1 == "1" || value2 == "1") ? "1" : "0";
                }
            }

            // ---------- TEXT ----------
            if (expressionType.ToLower() == "t")
            {
                if (value1.Trim().ToLower() == "null") value1 = "";
                if (value2.Trim().ToLower() == "null") value2 = "";

                switch (op)
                {
                    case "+": return value1 + value2;
                    case "=": return value1 == value2 ? "1" : "0";
                    case "#": return value1 == value2 ? "0" : "1";
                    case "$": return value2.Contains(value1) ? "1" : "0";
                    case "|": return (value1 == "1" || value2 == "1") ? "1" : "0";
                }
            }

            return "";
        }
        public string EvaluateExpression(List<string> exprList, List<string> exprTypeList)
        {
            // Ensure NewExprTypeList is initialized
            NewExprTypeList.Clear();

            // Convert to postfix
            ConvertToPostFix(exprList, exprTypeList);

            // Evaluate postfix expression
            return EvaluatePostFix(cpf, NewExprTypeList);
        }
        public List<string> ConvertToPostFix(List<string> ExprList, List<string> ExprTypeList)
        {
            cpf.Clear();
            StkList.Clear();
            StkTypeList.Clear();
            NewExprTypeList.Clear();

            ExprList.Add(")");
            ExprTypeList.Add("v");
            StkList.Add("(");
            StkTypeList.Add("v");

            for (int i = 0; i < ExprList.Count; i++)
            {
                if (StkList.Count < 1)
                {
                    throw new Exception("Invalid Expression");
                }

                string CurrElement = ExprList[i];
                string CurrType = ExprTypeList[i];
                string TopOfStkList = StkList[StkList.Count - 1];
                string TopOfStkTypeList = StkTypeList[StkTypeList.Count - 1];

                while (InputPrecedence(CurrElement) < StackPrecedence(TopOfStkList))
                {
                    StkList.RemoveAt(StkList.Count - 1);
                    StkTypeList.RemoveAt(StkTypeList.Count - 1);
                    cpf.Add(TopOfStkList);
                    NewExprTypeList.Add(TopOfStkTypeList);

                    TopOfStkList = StkList[StkList.Count - 1];
                    TopOfStkTypeList = StkTypeList[StkTypeList.Count - 1];
                }

                if (InputPrecedence(CurrElement) != StackPrecedence(TopOfStkList))
                {
                    StkList.Add(CurrElement);
                    StkTypeList.Add(CurrType);
                }
                else
                {
                    StkList.RemoveAt(StkList.Count - 1);
                    StkTypeList.RemoveAt(StkTypeList.Count - 1);
                }
            }

            return cpf;
        }
        public string EvaluatePostFix(List<string> ExprList, List<string> ExprTypeList)
        {
            List<string> stack = new List<string>();
            string exptype = "s";

            for (int i = 0; i < ExprList.Count; i++)
            {
                string token = ExprList[i];
                if (ISOperator(token))
                {
                    string value2 = "";
                    if (stack.Count >= 2)
                    {
                        value2 = stack[stack.Count - 1];
                        stack.RemoveAt(stack.Count - 1);
                    }
                    string value1 = stack[stack.Count - 1];
                    stack.RemoveAt(stack.Count - 1);

                    stack.Add(Compute(value1, value2, token, exptype));
                }
                else
                {
                    stack.Add(token);
                    if (ExprTypeList[i] != "v")
                        exptype = ExprTypeList[i];
                }
            }
            return (stack.Count == 1) ? stack[0] : "";
        }

        public string EvalFunction()
        {
            string s = string.Empty;
            if (CallType == "n")
            {
                var f = (FunctionInfo)FncList[FncList.Count - 1];
                s = CallFunction(f.FName, f.FParam);
                if (f.FName == "bulkexecute" && !string.IsNullOrEmpty(s))
                {
                    //if (axp.dbm.gf.isservice)
                    //    throw new Exception(s);
                }

                if (f.FName == "savesqlresult" && !string.IsNullOrEmpty(s))
                {
                    //if (axp.dbm.gf.isservice)
                    //    throw new Exception(s);
                }

                expressiontype = f.FType;
                FncList.RemoveAt(FncList.Count - 1);
                return s;
            }
            //else if (CallType == "p")
            //{
            //    f = FncList[FncList.Count - 1];

            //    fprep = new Prep
            //    {
            //        num = prepnum,
            //        FCall = new PFunctions()
            //        {
            //            Fname = f.Fname,
            //            FType = f.FType,
            //            FParamCount = f.FParamCount,
            //            FParamIndex = f.FParamIndex
            //        },
            //        ExprList = null,
            //        ExprTypeList = null,
            //        OrgList = null,
            //        TypeString = string.Empty,
            //        pIndex = -1
            //    };

            //    for (int i = 1; i <= 20; i++)
            //        fprep.FCall.FParam[i] = f.FParam[i];

            //    PrepList.Add(fprep);

            //    string result = "_xcall" + (PrepList.Count - 1);
            //    FncList.RemoveAt(FncList.Count - 1);
            //    return result;
            //}
            FncList.RemoveAt(FncList.Count - 1);
            return string.Empty;
        }

        public void EvalFun()
        {
            FunList.Clear();
            FunTypeList.Clear();

            int i = ExpList.Count - 1;
            //fprep = null;

            while (true)
            {
                string temp = ExpList[i];

                if (ExpList[i] == ((char)1).ToString()) // chr(1)
                {
                    int j = i + 1;

                    FunList.Clear();
                    FunTypeList.Clear();

                    //fprep = new FPrep();
                    //fprep.FCall = new FunctionCall();

                    while (true)
                    {
                        temp = ExpList[j];

                        if (!string.IsNullOrEmpty(ExpList[j]) &&
                            ExpList[j] != "," &&
                            ExpList[j] != ((char)2).ToString()) // chr(2)
                        {
                            FunList.Add(ExpList[j]);
                            FunTypeList.Add(ExpTypeList[j]);
                        }

                        if (ExpList[j] == ((char)2).ToString() || ExpList[j] == ",")
                        {
                            CopyParam(EvaluateExpression(FunList, FunTypeList));
                            FunList.Clear();
                            FunTypeList.Clear();
                        }

                        if (ExpList[j] == ((char)2).ToString()) // end of function
                        {
                            ExpList.RemoveAt(j);
                            ExpTypeList.RemoveAt(j);
                            break;
                        }

                        ExpList.RemoveAt(j);
                        ExpTypeList.RemoveAt(j);
                    }

                    // set function return type and evaluated result
                    var lastFunc = (FunctionInfo)FncList[FncList.Count - 1];
                    ExpTypeList[i] = lastFunc.FType;
                    ExpList[i] = EvalFunction();

                    //if (!string.IsNullOrEmpty(axp.dbm.gf.ErrorInActionExecution))
                    //    break;
                }

                if (ExpList[i] == "")
                {
                    ExpList.RemoveAt(i);
                    ExpTypeList.RemoveAt(i);
                }

                if (i == 0)
                    break;

                i--;
            }

            //fprep = null;

            //if (!string.IsNullOrEmpty(axp.dbm.gf.ErrorInActionExecution))
            //    throw new Exception(axp.dbm.gf.ErrorInActionExecution);
        }

        public void CopyParam(string s)
        {
            var f = (FunctionInfo)FncList[FncList.Count - 1];
            f.FParam[f.FParamIndex] = s;

            //if (CallType == "p")
            //{
            //    prep.PIndex = f.FParamIndex;
            //    prep.FCall = fprep.FCall;
            //}

            f.FParamIndex++;
        }

        public bool Evaluate(string expr)
        {
            try
            {
                DynamicCompute = false;
                Value = string.Empty;
                CallType = "n";
                while (true)
                {
                    int position = 1;
                    ClearFncList();

                    expression = expr.Trim();
                    ExpList.Clear();
                    ExpTypeList.Clear();

                    ExpList.Add(string.Empty);
                    ExpTypeList.Add("v");

                    ExpIndex = 0;
                    StrCount = 0;
                    expressiontype = "v";

                    bool constWord = false;
                    char ch;

                    while (true)
                    {
                        if (position <= expression.Length)
                            ch = expression[position - 1];
                        else
                            ch = (char)32;

                        ExpStrPos = position;

                        if (ch == '{')
                            StartString();

                        if (ch == '}')
                        {
                            EndString();
                            constWord = true;
                            expressiontype = "c";
                            if (CallType == "p")
                                ExpList[ExpIndex] = "_c_" + ExpList[ExpIndex];
                        }

                        if (DeLimiters.Contains(ch) || Operators.Contains(ch))
                        {
                            if (!constWord)
                            {
                                ExpTypeList[ExpIndex] = "n";

                                if (!IfVariable(ExpList[ExpIndex]))
                                {
                                    if (!IfFunction(ExpList[ExpIndex]))
                                    {
                                        if (!IfParams(ExpList[ExpIndex]))
                                        {
                                            if (CallType != "p" &&
                                                !string.IsNullOrEmpty(ExpList[ExpIndex]) &&
                                                ExpList[ExpIndex] != ((char)1).ToString() &&
                                                ExpList[ExpIndex] != ((char)2).ToString() &&
                                                !IsNumeral(ExpList[ExpIndex]))
                                            {
                                                throw new Exception(ExpList[ExpIndex] + " is not defined");
                                            }
                                        }
                                    }
                                }
                                position = ExpStrPos;
                            }
                            else
                            {
                                constWord = false;
                                ExpTypeList[ExpIndex] = "c";
                            }

                            if (ch == '(')
                            {
                                if (bracket.Length > 0 && bracket[^1] == 'F')
                                    ch = ' ';

                                bracket += "(";
                            }

                            if (ch == ')')
                            {
                                bracket = bracket.Substring(0, bracket.Length - 1);
                                if (bracket.Length > 0 && bracket[^1] == 'F')
                                {
                                    ExpList.Add(((char)2).ToString());
                                    ExpTypeList.Add("v");

                                    bracket = bracket.Substring(0, bracket.Length - 1);
                                    ch = ' ';
                                }
                            }

                            if (ch != ' ')
                            {
                                ExpList.Add(ch.ToString());
                                ExpTypeList.Add("v");
                            }

                            ExpList.Add(string.Empty);
                            ExpTypeList.Add("v");
                            ExpIndex = ExpList.Count - 1;
                        }
                        else
                        {
                            if (!((ch == '{' && StrCount == 1) || (ch == '}' && StrCount == 0)))
                                ExpList[ExpIndex] += ch;
                        }

                        if (position > expression.Length) break;
                        position++;
                    }

                    EvalFun();

                    //if (!string.IsNullOrEmpty(axp.dbm.gf.ErrorInActionExecution))
                    //    break;

                    Value = EvaluateExpression(ExpList, ExpTypeList);

                    if (Value == "~e~")
                        Value = string.Empty;

                    if (!DynamicCompute || CallType != "n")
                        break;

                    DynamicCompute = false;
                    expr = Value;
                }

                ClearFncList();
                CallType = "n";

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(JsonConvert.SerializeObject(ex));
                return false;
            }
        }
        public bool IsNumeral(string s)
        {
            if (string.IsNullOrEmpty(s))
                return false;

            foreach (char c in s)
            {
                if (!Numbers.Contains(c))
                {
                    return false;
                }
            }
            return true;
        }

        public void StartString()
        {
            StrCount++;
            DeLimiters.Clear();
            Operators.Clear();
        }

        public void EndString()
        {
            if (StrCount == 0)
                throw new Exception("Invalid expression"); // EParserError equivalent

            StrCount--;

            if (StrCount == 0)
            {
                DeLimiters = new HashSet<char> { ' ', '(', ')', ',' };
                Operators = new HashSet<char> { '+', '-', '*', '/', '>', '<', '=', '#', '&', '|', '$' };
            }
        }
        private string Eval(string expr)
        {
            DynamicCompute = true;
            return expr;
        }
        private void ClearFncList()
        {
            FncList.Clear();
        }

        private FunctionInfo ValidFnc(string s)
        {
            s = s.ToLower();
            int ind = 0;
            foreach (var f in funclist)
            {
                if (f.ToLower() == s)
                {
                    var nf = new FunctionInfo
                    {
                        FName = f,
                        FType = funcTypelist[ind],
                        FParamCount = 0,
                        FParamIndex = 0
                    };
                    return nf;
                }
                ind++;
            }

            return null; // not found
        }
        public bool IfFunction(string lastWord)
        {
            bool result = false;
            lastWord = lastWord.Trim();
            var f = ValidFnc(lastWord);

            if (f != null)
            {
                result = true;
                ExpTypeList[ExpIndex] = f.FType;
            }

            if (result)
            {
                var nf = new FunctionInfo
                {
                    FName = f.FName,
                    FType = f.FType,
                    FParamCount = 0,
                    FParamIndex = 0
                };

                FncList.Add(nf);
                bracket += "F";
                ExpList[ExpIndex] = ((char)1).ToString();
                ExpTypeList[ExpIndex] = "v";
            }

            return result;
        }

        public bool IfParams(string paramMaster)
        {
            bool result = false;
            //if (SlParams.IndexOf(paramMaster) > -1)
            //{
            //    result = true;
            //    string pv = "";

            //    if (Dict.TryGetValue(paramMaster, out string existing))
            //    {
            //        pv = existing;
            //    }

            //    if (!string.IsNullOrEmpty(pv))
            //    {
            //        if (pv.Contains(paramMaster))
            //            return result;

            //        pv = pv + "," + ParamName;
            //        Dict.Remove(paramMaster);
            //    }
            //    else
            //    {
            //        pv = ParamName;
            //    }

            //    Dict[paramMaster] = pv;
            //}

            return result;
        }

        public bool IfVariable(string lastWord)
        {
            bool result = false;
            int posn = VarList.IndexOf(lastWord.ToLower());

            if (posn >= 0)
            {
                result = true;
                IfParams(ExpList[ExpIndex]);

                //if (!VarsUsed.Contains(lastWord.ToLower()))
                //    VarsUsed.Add(lastWord.ToLower());

                if (CallType == "p")
                {
                    ExpList[ExpIndex] = "_v_" + posn.ToString();
                }

                //ExpTypeList[ExpIndex] = VarTypeStr[posn + 1].ToString();
                //expressiontype = VarTypeStr[posn + 1].ToString();
                // Changes made on 25-09-2025
                ExpTypeList[ExpIndex] = VarTypeStr[posn].ToString();
                expressiontype = VarTypeStr[posn].ToString();
            }

            if (result && CallType == "n")
            {
                if (string.IsNullOrWhiteSpace(ValueList[posn]))
                    ExpList[ExpIndex] = "~e~";
                else
                    ExpList[ExpIndex] = ValueList[posn];
            }

            return result;
        }

        public async Task<string> FireSql(string sqlName, string sqlText)
        {
            bool funcQry = false;
            sqlText = sqlText.Trim();

            if (sqlText.Length >= 6 && sqlText.Substring(0, 6).ToLower() == "select")
            {

            }
            else if (_common._dbHelper.GetDBType() == ARMCommon.Helpers.Constants.DBTYPE.POSTGRE.ToString() && sqlText.Substring(0, 4).ToLower() == "call")
            {

                if (sqlText.Length >= 8 && sqlText.Substring(0, 8).ToLower() == "callproc")
                {
                    sqlText = sqlText.Substring(8).Trim();
                    sqlText = "call " + sqlText;
                }
                else
                {
                    sqlText = sqlText.Substring(4).Trim();
                    sqlText = "select " + sqlText;
                }
                funcQry = true;
            }

            if (_common._dbHelper == null)
                await _common.OpenDBConnectionAsync();

            //sqlText = ReplaceSqlParameters(sqlText);
            var (modifiedSqlText, paramNames, paramTypes, paramValues) = _common.GetSQLParams(sqlText, VarList, ValueList, VarTypeList);

            string result = "";
            if (modifiedSqlText.Length >= 6 && modifiedSqlText.Substring(0, 6).ToLower() == "select" && !funcQry)
            {
                var sqlResult = new ARMCommon.Model.SQLResult();

                if (paramNames != null && paramNames.Length > 0)
                {
                    sqlResult = await _common._dbHelper.ExecuteSQLAsync(modifiedSqlText, paramNames, paramTypes, paramValues);
                }
                else
                {
                    sqlResult = await _common._dbHelper.ExecuteSQLAsync(modifiedSqlText);
                }


                if (sqlResult.error != null && !string.IsNullOrEmpty(sqlResult.error))
                    result = sqlResult.error;
                else if (sqlResult.error == null && sqlResult.data != null)
                    result = JsonConvert.SerializeObject(sqlResult.data);

            }
            else
            {
                var sqlResult = new ARMCommon.Model.NonQueryResult();

                if (paramNames != null && paramNames.Length > 0)
                {
                    sqlResult = await _common._dbHelper.ExecuteNonQueryAsync(modifiedSqlText, paramNames, paramTypes, paramValues);
                }
                else
                {
                    sqlResult = await _common._dbHelper.ExecuteNonQueryAsync(modifiedSqlText);
                }

                result = JsonConvert.SerializeObject(sqlResult);
                if (sqlResult.error != null && !string.IsNullOrEmpty(sqlResult.error))
                    result = sqlResult.error;
                else if (sqlResult.error == null && sqlResult.count != null)
                    result = sqlResult.count.ToString();
            }

            RegisterVar(sqlName, "s", result);
            return result;

            //    TXDS query = null;
            //    sqlText = sqlText.Trim();
            //    bool funcQry = false;

            //    // find query in list
            //    for (int i = 0; i < queryList.Count; i++)
            //    {
            //        if (((TXDS)queryList[i]).Name == "Expr_" + sqlName)
            //        {
            //            query = (TXDS)queryList[i];
            //            break;
            //        }
            //    }

            //    //// create new query if not found
            //    //if (query == null)
            //    //{
            //    //    query = axp.dbm.GetXDS(query); 
            //    //    query.Name = "Expr_" + sqlName;
            //    //    query.Buffered = true;

            //    //    if (sqlText.Length >= 6 && sqlText.Substring(0, 6).ToLower() == "select")
            //    //        queryList.Add(query);
            //    //}

            //    query.Close();

            //    // Handle Postgre specific logic
            //    if (axp.dbm.Connection.DbType == "postgre" &&
            //        sqlText.Trim().Substring(0, 4).ToLower() == "call")
            //    {
            //        sqlText = sqlText.Trim();

            //        if (sqlText.Length >= 8 && sqlText.Substring(0, 8).ToLower() == "callproc")
            //        {
            //            sqlText = sqlText.Substring(8).Trim();
            //            sqlText = "call " + sqlText;
            //        }
            //        else
            //        {
            //            sqlText = sqlText.Substring(4).Trim();
            //            sqlText = "select " + sqlText;
            //        }

            //        funcQry = true;
            //    }

            //    query.CDS.CommandText = sqlText;
            //    //ReplaceParams(query); // Mohan method need to call here 

            //    //if (sqlText.Length >= 6 && sqlText.Substring(0, 6).ToLower() == "select" && !funcQry)
            //    //    query.Open();
            //    //else
            //    //    query.ExecSql();

            //    //if (!(sqlText.Length >= 6 && sqlText.Substring(0, 6).ToLower() == "select") || funcQry)
            //    //{
            //    //    query.Close();
            //    //    query.Dispose();
            //    //    query = null;
            //    //}
        }

        public string ReplaceSqlParameters(string sql)
        {

            if (VarList == null || VarList == null || VarList.Count != VarList.Count)
                throw new ArgumentException("Parameter lists must be non-null and of equal length.");

            Regex regex = new Regex(@"\:\w+");
            StringBuilder sb = new StringBuilder(sql);
            MatchCollection matches = regex.Matches(sql);

            // Create lookup dictionary (case-insensitive) from varList/varListValues
            var caseInsensitiveParams = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < VarList.Count; i++)
            {
                caseInsensitiveParams[VarList[i]] = ValueList[i];
            }

            // Iterate in reverse to avoid index shifting
            for (int i = matches.Count - 1; i >= 0; i--)
            {
                Match match = matches[i];
                string paramKey = match.Value.Substring(1); // Remove leading ':'

                if (caseInsensitiveParams.TryGetValue(paramKey, out string value))
                {
                    sb.Remove(match.Index, match.Length);
                    sb.Insert(match.Index, "'" + value.Replace("'", "''") + "'"); // SQL-safe replacement
                }
            }

            return sb.ToString();
        }
        public string SQLGet(string sqlName, string fieldName)
        {
            string value = "";
            try
            {
                string sqlData = GetVarValue(sqlName);

                // Parse input JSON as JArray
                var arr = JArray.Parse(sqlData);

                if (arr.Count != 0)
                {
                    var firstRow = arr[0] as JObject;

                    if (firstRow != null)
                    {
                        // Try get the property by name
                        var token = firstRow[fieldName];
                        value = token?.ToString() ?? string.Empty;
                    }
                }
            }
            catch
            {
                //Do nothing
            }

            RegisterVar(fieldName, "c", value);

            return value;
        }


        public string SQLRegVar(string sqlText, string direct = "False")
        {
            return "";
            //    if (axp.dbm == null)
            //        return;

            //    //TXDS work = null;
            //    int ind = -1;

            //    //work = axp.dbm.GetXDS(work);
            //    //work.Buffered = true;
            //    //work.CDS.CommandText = sqlText;



            //    //ReplaceParams(work); // Mohan method need to call here 
            //    //work.Open();

            //    direct = direct.ToLower();

            //    if (direct == "true" || direct == "t")
            //    {
            //        while (!work.CDS.Eof)
            //        {
            //            for (int i = 0; i < work.CDS.Fields.Count; i++)
            //            {
            //                string t = axp.dbm.gf.GetDataType(work.CDS.Fields[i].DataType).ToLower();

            //                if (t == "image") continue;
            //                if (t == "text" || t == "unknown") t = "character";

            //                RegisterVar(work.CDS.Fields[i].FieldName, t[0], work.CDS.Fields[i].AsString);

            //                //ind = MemVarList.IndexOfName(work.CDS.Fields[i].FieldName);

            //                //if (ind >= 0)
            //                //{
            //                //    MemVarList[ind] = work.CDS.Fields[i].FieldName + "=" + work.CDS.Fields[i].AsString;
            //                //    MemTypeStr = MemTypeStr.Remove(ind + 1, 1).Insert(ind + 1, t[0].ToString());
            //                //}
            //                //else
            //                //{
            //                //    MemVarList.Add(work.CDS.Fields[i].FieldName + "=" + work.CDS.Fields[i].AsString);
            //                //    MemTypeStr += t[0];
            //                //}
            //            }
            //            work.CDS.Next();
            //        }
            //    }
            //    else
            //    {
            //        // Must contain varname, varvalue, vartype
            //        if (work.CDS.FindField("varname") != null &&
            //            work.CDS.FindField("varvalue") != null &&
            //            work.CDS.FindField("vartype") != null)
            //        {
            //            work.CDS.First();

            //            while (!work.CDS.Eof)
            //            {
            //                string t = work.CDS.FieldByName("vartype").AsString;
            //                string varName = work.CDS.FieldByName("varname").AsString;
            //                string varValue = work.CDS.FieldByName("varvalue").AsString;

            //                RegisterVar(varName, t[0], varValue);

            //                //ind = MemVarList.IndexOfName(varName);

            //                //if (ind >= 0)
            //                //{
            //                //    MemVarList[ind] = varName + "=" + varValue;
            //                //    MemTypeStr = MemTypeStr.Remove(ind + 1, 1).Insert(ind + 1, t[0].ToString());
            //                //}
            //                //else
            //                //{
            //                //    MemVarList.Add(varName + "=" + varValue);
            //                //    MemTypeStr += t[0];
            //                //}

            //                work.CDS.Next();
            //            }
            //        }
            //    }

            //    //work.Close();
            //    //work.Dispose(); // instead of Free
            //    //work = null;
        }

        public int RegisterVar(string varName, string varType, string value)
        {
            varType = varType.ToLower();
            string vtype = varType.ToString();
            string zval = "T";

            if (vtype == "n" && string.IsNullOrEmpty(value))
            {
                value = "0";
                zval = "F";
            }

            if (string.IsNullOrEmpty(varName))
                return -1;

            if (vtype == "n")
            {
                value = GetNumber(RemoveCommas(value));
            }

            int posn = VarList.IndexOf(varName.ToLower());
            int result = posn;

            if (posn < 0)
            {
                VarList.Add(varName.ToLower());
                VarTypeList.Add(varType);
                VarTypeStr += varType;
                ValueList.Add(value);
                ZeroValue += zval;
                result = ValueList.Count - 1;
            }
            else
            {
                ValueList[posn] = value;

                char[] chars = ZeroValue.ToCharArray();
                chars[posn] = zval[0]; // adjust for 0-based index
                ZeroValue = new string(chars);
            }

            return result;
        }

        public string GetVarValue(string varName)
        {
            string result = string.Empty;
            LastVarType = string.Empty;

            int posn = VarList.IndexOf(varName.ToLower());
            if (posn >= 0)
            {
                LastVarType = VarTypeStr[posn].ToString();
                result = ValueList[posn];
            }
            return result;
        }

        // Changed by Salman Sheriff
        //private string GetNumber(string input) => string.IsNullOrEmpty(input) ? "0" : input;
        public string GetNumber(string input) => string.IsNullOrEmpty(input) ? "0" : input;

        public void EvalExprSet(int startpos)
        {
            int i = startpos, p;
            string s, v, cond, cmd, op;
            bool cresult, skip, breakflag;
            try
            {
                skip = false;
                cresult = true;
                breakflag = false;
                List<string> loops = new List<string>();
                List<string> loopcond = new List<string>();
                while (i < exprset.Count)
                {
                    s = exprset[i];
                    cmd = s.TrimStart();

                    if (string.IsNullOrWhiteSpace(s) || cmd.StartsWith("//"))
                    {
                        i++;
                        continue;
                    }
                    if (cmd.StartsWith("{")) break;

                    op = GetNthString(cmd, 1, " ").ToLower();

                    if (breakflag)
                    {
                        if (op != "endloop")
                        {
                            i++;
                            continue;
                        }
                    }

                    if (op == "if")
                    {
                        cmd = cmd.Substring(2).Trim();
                        cond = cmd;
                        Evaluate(cond);

                        cresult = (Value == "1");
                        skip = cresult;
                    }
                    else if (op == "elseif")
                    {
                        if (skip) cresult = false;
                        else cresult = !cresult;

                        if (cresult)
                        {
                            cmd = cmd.Substring(6).Trim();
                            cond = cmd;
                            Evaluate(cond);

                            cresult = (Value == "1");
                            skip = cresult;
                        }
                    }
                    else if (op == "else")
                    {
                        if (skip) cresult = false;
                        else
                        {
                            cresult = !cresult;
                            skip = cresult;
                        }
                    }
                    else if (op == "end")
                    {
                        cresult = true;
                        skip = false;
                    }
                    else if (op == "endloop")
                    {
                        if (breakflag)
                        {
                            breakflag = false;
                            loopcond.RemoveAt(loopcond.Count - 1);
                            loops.RemoveAt(loops.Count - 1);
                        }
                        else
                        {
                            cond = loopcond[loopcond.Count - 1];
                            Evaluate(cond);

                            if (Value == "1")
                                i = int.Parse(loops[loops.Count - 1]);
                            else
                            {
                                loopcond.RemoveAt(loopcond.Count - 1);
                                loops.RemoveAt(loops.Count - 1);
                            }
                        }
                    }
                    else if (op == "while")
                    {
                        cmd = cmd.Substring(5).Trim();
                        cond = cmd;
                        Evaluate(cond);

                        if (Value == "1")
                        {
                            loops.Add(i.ToString());
                            loopcond.Add(cond);
                        }
                    }
                    else if (op == "break")
                    {
                        breakflag = true;
                        cresult = true;
                        skip = false;
                    }
                    else if (cresult)
                    {
                        p = s.IndexOf(":=");
                        v = "";
                        if (p != -1)
                        {
                            v = s.Substring(0, p).Trim();
                            s = s.Substring(p + 2);
                        }

                        Evaluate(s);

                        if (!string.IsNullOrEmpty(v))
                            RegisterVar(v, expressiontype, Value);
                    }

                    i++;
                }

                loops.Clear();
                loopcond.Clear();
            }
            catch (Exception e)
            {
            }
        }

        public void EvalExps(List<string> slist)
        {
            if (slist == null || slist.Count == 0) return;
            exprset = slist;
            EvalExprSet(0);
        }

        public string GetNthString(string srcString, int strPos, char separator)
        {
            string result = string.Empty;
            int i = 0;
            int k = 1;

            while (k <= strPos && i < srcString.Length)
            {
                if (srcString[i] == separator)
                {
                    k++;
                }
                else if (k == strPos)
                {
                    result += srcString[i];
                }
                i++;
            }

            return result;
        }

        public string GetNthString(string srcString, int strPos, string separator)
        {
            if (separator.Length == 1)
            {
                return GetNthString(srcString, strPos, separator[0]);
            }
            else
            {
                string result = string.Empty;
                int i = 0; // C# is 0-based
                int k = 1;
                int searchLength = separator.Length;

                while (k <= strPos && i < srcString.Length)
                {
                    if (i + searchLength <= srcString.Length &&
                        srcString.Substring(i, searchLength) == separator)
                    {
                        k++;
                        i += searchLength;
                    }
                    else if (k == strPos)
                    {
                        result += srcString[i];
                        i++;
                    }
                    else
                    {
                        i++;
                    }
                }
                return result;
            }
        }

        //09092025 methods 
        private string GetAllValues(string fName, string quoted, string uniqueVal = "f")
        {
            fName = fName.Trim().ToLower();
            quoted = quoted.Trim().ToLower();
            uniqueVal = uniqueVal.Trim().ToLower();
            string result = "";
            if (_structDef == null)
                return result;

            foreach (var fieldObj in _structDef.FieldList)
            {
                var field = (FieldRec)fieldObj;  // Assuming FieldList holds FieldRec objects

                if (field.RowNo > -1 && field.FieldName.Trim().ToLower() == fName)
                {
                    string s = field.Value;
                    if (string.IsNullOrEmpty(s))
                        continue;

                    // Ensure uniqueness
                    if (uniqueVal == "true" || uniqueVal == "t")
                    {
                        if (quoted == "true" || quoted == "t")
                        {
                            if ($",{result},".Contains($",'{s}',"))
                                continue;
                        }
                        else
                        {
                            if ($",{result},".Contains($",{s},"))
                                continue;
                        }
                    }

                    // Append value
                    if (quoted == "true" || quoted == "t")
                        result += $",'{s}'";
                    else
                        result += $"," + s;
                }
            }
            if (!string.IsNullOrEmpty(result))
            {
                result = result.Trim();
                result = result.Substring(1);
            }
            return result;
        }
        private int GetMin(string pFieldName)
        {
            int result = -1;
            if (WorkOnStructdef)
            {
                result = GetMinFromSD(pFieldName);
                return result;
            }
            return result;
        }
        private int GetMinFromSD(string pFieldName)
        {
            int result = -1;
            double minValue = 0;
            string sMin = string.Empty;
            if (_structDef == null)
                return result;
            pFieldName = pFieldName.ToLower();
            foreach (var fieldObj in _structDef.FieldList)
            {
                var field = (FieldRec)fieldObj;

                if (field.FieldName.ToLower() == pFieldName &&
                    field.RowNo > 0 &&
                    !string.IsNullOrEmpty(field.Value))
                {
                    if (field.DataType.ToLower() == "n")
                    {
                        double v;
                        try
                        {
                            v = double.Parse(field.Value);
                        }
                        catch (Exception e)
                        {
                            v = 0;
                        }

                        if (v < minValue || result == -1)
                        {
                            minValue = v;
                            result = field.RowNo;
                        }
                    }
                    else
                    {
                        if (string.Compare(field.Value, sMin, StringComparison.Ordinal) < 0 || result == -1)
                        {
                            sMin = field.Value;
                            result = field.RowNo;
                        }
                    }
                }
            }
            return result;
        }
        private int GetMax(string pFieldName)
        {
            int result = -1;
            if (WorkOnStructdef)
            {
                result = GetMaxFromSD(pFieldName);
                return result;
            }
            return result;
        }
        private int GetMaxFromSD(string pFieldName)
        {
            int result = -1;
            double maxValue = 0;
            string sMax = string.Empty;

            if (_structDef == null)
                return result;

            pFieldName = pFieldName.ToLower();

            foreach (var fieldObj in _structDef.FieldList)
            {
                var field = (FieldRec)fieldObj;

                if (field.FieldName.ToLower() == pFieldName &&
                    field.RowNo > 0 &&
                    !string.IsNullOrEmpty(field.Value))
                {
                    if (field.DataType.ToLower() == "n")
                    {
                        double v;
                        try
                        {
                            v = double.Parse(field.Value);
                        }
                        catch (Exception e)
                        {
                            v = 0;
                        }

                        if (v > maxValue || result == -1)
                        {
                            maxValue = v;
                            result = field.RowNo;
                        }
                    }
                    else
                    {
                        if (string.Compare(field.Value, sMax, StringComparison.Ordinal) > 0 || result == -1)
                        {
                            sMax = field.Value;
                            result = field.RowNo;
                        }
                    }
                }
            }
            return result;
        }
        private int GetRowCount(string pFieldName)
        {
            int result = 0;

            if (WorkOnStructdef)
            {
                result = GetRowCountFromSD(pFieldName);
                return result;
            }
            return result;
        }
        private int GetRowCountFromSD(string pFieldName)
        {
            int result = 0;
            if (_structDef == null)
                return result;
            pFieldName = pFieldName.ToLower();
            foreach (var fieldObj in _structDef.FieldList)
            {
                var field = (FieldRec)fieldObj;

                if (field.FieldName.ToLower() == pFieldName &&
                    field.RowNo > 0)
                {
                    result++;
                }
            }
            return result;
        }
        private double SumTill(string classificationFld, string classificationValue, string cumulationFld, string _rowNo)
        {
            double result = 0;
            int rowNo = Int32.Parse(_rowNo);

            if (WorkOnStructdef)
            {
                result = SumTillFromSD(classificationFld, classificationValue, cumulationFld, rowNo);
                return result;
            }

            return result;
        }
        private double SumTillFromSD(string classificationFld, string classificationValue, string cumulationFld, int rowNo)
        {
            double result = 0;

            if (_structDef == null)
                return result;

            classificationFld = classificationFld.ToLower();

            for (int r = 0; r < _structDef.FieldList.Count; r++)
            {
                var field = (FieldRec)_structDef.FieldList[r];

                if (field.FieldName.ToLower() == classificationFld &&
                    field.RowNo > 0 &&
                    field.RowNo <= rowNo &&
                    field.Value == classificationValue)
                {
                    var tfld = _structDef.GetField(cumulationFld);
                    string cumFieldValue = _structDef.GetFieldValue(tfld, field.RowNo); // This need to recheck for the grid fields based on the rowno.
                    if (!string.IsNullOrEmpty(cumFieldValue))
                    {
                        try
                        {
                            result += double.Parse(cumFieldValue);
                        }
                        catch (Exception e)
                        {
                            //axp.dbm.gf.DoDebug.Log(axp.dbm.gf.Axp_logstr + "\\uProfitEval\\SumTillFromSD - " + e.Message);
                        }
                    }
                }
            }
            return result;
        }
        private double Sum(string classificationFld, string classificationValue, string cumulationFld)
        {
            double result = 0;
            classificationFld = classificationFld?.Trim();
            cumulationFld = cumulationFld?.Trim();
            if (WorkOnStructdef)
            {
                result = SumFromSD(classificationFld, classificationValue, cumulationFld);
                return result;
            }

            return result;
        }
        private double SumFromSD(string classificationFld, string classificationValue, string cumulationFld)
        {
            double result = 0;
            if (_structDef == null)
                return result;
            classificationFld = classificationFld.ToLower();
            for (int r = 0; r < _structDef.FieldList.Count; r++)
            {
                var field = (FieldRec)_structDef.FieldList[r];
                if (field.FieldName.ToLower() == classificationFld && field.RowNo > 0 && field.Value == classificationValue)
                {
                    var tfld = _structDef.GetField(cumulationFld);
                    string cumFieldValue = _structDef.GetFieldValue(tfld, field.RowNo); // This need to recheck for the grid fields based on the rowno.
                    if (!string.IsNullOrEmpty(cumFieldValue))
                    {
                        try
                        {
                            result += double.Parse(cumFieldValue);
                        }
                        catch (Exception e)
                        {
                            //axp.dbm.gf.DoDebug.Log(axp.dbm.gf.Axp_logstr + "\\uProfitEval\\SumTillFromSD - " + e.Message);
                        }
                    }
                }
            }
            return result;
        }
        private int GetRow(string fieldName, string fieldValue)
        {
            int result = -1;
            if (WorkOnStructdef)
            {
                result = GetRowFromSD(fieldName, fieldValue);
                return result;
            }
            return result;
        }
        private int GetRowFromSD(string fieldName, string fieldValue)
        {
            int result = -1;
            if (_structDef == null)
                return result;
            fieldName = fieldName.ToLower();
            for (int i = 0; i < _structDef.FieldList.Count; i++)
            {
                var field = (FieldRec)_structDef.FieldList[i];
                if (field.FieldName.ToLower() == fieldName &&
                    field.RowNo > 0 &&
                    field.Value == fieldValue)
                {
                    result = field.RowNo;
                    break;
                }
            }
            return result;
        }
        private double GetSubTotal(string detFldName, string _parentRowNo)// As when required need to work on it. 
        {
            double result = 0;

            //int parentRowNo = Int32.Parse(_parentRowNo);
            //var tfld = Structdef.GetField(detFldName);
            //if (tfld == null || Structdef.GetFrame(tfld.FrameNo - 1).PopIndex == -1)
            //    return result;
            //int popIndex = Structdef.GetFrame(tfld.FrameNo - 1).PopIndex;
            //var popgrid = Structdef.GetPopGrid(popIndex);

            //// Get parent values
            //var parentList = new List<string>();
            //GetParentValue(popIndex, parentRowNo, parentList);

            //// Get actual rows
            //var actualRowsCsv = GetActualRows(popIndex, parentList);
            //var actualRows = actualRowsCsv.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();

            //if (actualRows.Count == 0)
            //    return result;

            //detFldName = detFldName.ToLower();
            //foreach (var obj in Structdef.FieldList)
            //{
            //    var field = (FieldRec)obj;
            //    if (field.FrameNo != popgrid.FrameNo || field.RowNo == -1)
            //        continue;

            //    if (detFldName != field.FieldName.ToLower())
            //        continue;

            //    int actualInd = actualRows.IndexOf(field.RowNo.ToString());
            //    if (actualInd >= 0)
            //    {
            //        result += StrToFloatZ(field.Value);
            //    }
            //}

            return result;
        }
        public double StrToFloatZ(string s)
        {
            if (!string.IsNullOrWhiteSpace(s))
            {
                string noCommas = RemoveCommas(s);
                if (double.TryParse(noCommas, out double result))
                    return result;
                else
                    return 0;
            }
            else
            {
                return 0;
            }
        }
        private string GetActualRows(int popIndex, List<string> parentList)
        {
            //dbm.gf.DoDebug.Msg("Getting actual rows of popup form");

            int rc = 0;// Structdef.GetRowCount(Structdef.GetPopGrid(popIndex).FrameNo); //need to check with unni
            string result = "";

            for (int i = 1; i <= rc; i++)
            {
                bool validRow = false;

                foreach (var entry in parentList)
                {
                    var parts = entry.Split('=');
                    string fieldName = parts[0];
                    string expectedValue = parts[1];

                    var tfld = _structDef.GetField(fieldName);

                    if (tfld.DataType == "n")
                    {
                        if (double.Parse(_structDef.GetFieldValue(tfld, i)) != double.Parse(expectedValue))
                        {
                            validRow = false;
                            break;
                        }
                        else validRow = true;
                    }
                    else
                    {
                        if (_structDef.GetFieldValue(tfld, i) != expectedValue)
                        {
                            validRow = false;
                            break;
                        }
                        else validRow = true;
                    }
                }

                if (validRow)
                    result += "," + i.ToString();
            }

            if (result.Length > 0)
                result = result.Substring(1);

            return result;
        }
        private void GetParentValue(int popIndex, int rowNo, List<string> sList)
        {
            //dbm.gf.DoDebug.Msg("Getting values of parent fields");

            //sList.Clear();
            //int i = 1;
            //string pFields = _structDef.GetPopGrid(popIndex).ParentField;

            //string pField = GetNthString(pFields, i.ToString());
            //string fno = _structDef.GetPopGrid(popIndex).FrameNo.ToString();

            //while (!string.IsNullOrWhiteSpace(pField))
            //{
            //    string fname = "sub" + fno + "_" + pField;
            //    var tfld = _structDef.GetField(fname);

            //    if (tfld != null)
            //        fname = tfld.FieldName;

            //    string val = _structDef.GetFieldValue(pField, rowNo);

            //    if (tfld.DataType == "n")
            //        val = FormatNumericData(tfld, val);

            //    sList.Add(fname + "=" + val);
            //    //dbm.gf.DoDebug.Msg("Getting parent value - " + fname + "=" + val);

            //    i++;
            //    pField = GetNthString(pFields, i.ToString());
            //}
        }
        private string GetOld(string pFieldName, string _rowNo)
        {
            int rowNo = Int32.Parse(_rowNo);
            string result = string.Empty;
            if (_structDef == null)
                return result;

            //result = _structDef.GetOldValue(pFieldName, rowNo);

            return result;
        }
        public string GetValue(string pFieldName, string _rowNo)
        {
            int rowNo = Int32.Parse(_rowNo);
            string result = string.Empty;
            if (rowNo == -1)
                return result;

            if (WorkOnStructdef)
            {
                result = GetValueFromSD(pFieldName, rowNo);
                return result;
            }
            return result;
        }
        public string GetValueFromSD(string pFieldName, int rowNo)
        {
            string result = string.Empty;
            if (_structDef == null)
                return result;
            var tfld = _structDef.GetField(pFieldName);
            if (tfld == null)
                return result;
            string fieldValue = _structDef.GetFieldValue(tfld, rowNo);
            if (string.Equals(tfld.DataType, "n", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(fieldValue))
            {
                result = "0";
            }
            else
            {
                result = fieldValue;
            }
            return result;
        }
        public string Str(object number)
        {
            if (number == null)
            {
                return string.Empty;
            }
            double parsed;
            if (!double.TryParse(number.ToString(), out parsed))
            {
                return string.Empty;
            }
            return parsed.ToString();
        }
        public double Total(string pFieldName)
        {
            double result = 0;
            pFieldName = pFieldName.Trim();
            if (WorkOnStructdef)
            {
                result = TotalFromSD(pFieldName);
                return result;
            }
            return result;
        }
        public double TotalFromSD(string pFieldName)
        {
            double result = 0;
            if (_structDef == null)
                return result;
            pFieldName = pFieldName.ToLower();
            for (int r = 0; r < _structDef.FieldList.Count; r++)
            {
                var fieldRec = _structDef.FieldList[r];
                if (fieldRec.FieldName.ToLower() == pFieldName &&
                    fieldRec.RowNo > 0 &&
                    !string.IsNullOrEmpty(fieldRec.Value))
                {
                    if (double.TryParse(fieldRec.Value, out double val))
                    {
                        result += val;
                    }
                }
            }
            return result;
        }

        public string FieldChanged(string fieldName, string _rowNo)
        {
            int kf, k;
            int rowNo = Int32.Parse(_rowNo);
            string value, oldValue;

            string result = "F";

            kf = _structDef.GetFieldIndex(fieldName, rowNo);
            if (kf == -1)
                return result;

            var fieldRec = _structDef.FieldList[kf] as FieldRec;
            if (fieldRec == null || fieldRec.RecordId == 0)
                return result;

            oldValue = fieldRec.OldValue;

            k = VarList.IndexOf(fieldName);
            if (k < 0)
                return result;

            value = ValueList[k];

            if (oldValue != value)
                result = "T";

            if (fieldRec.DataType?.ToLower() == "n")
            {
                if (double.TryParse(oldValue, out double oldNum) &&
                    double.TryParse(value, out double newNum))
                {
                    result = oldNum != newNum ? "T" : "F";
                }
                else
                {
                    result = "F";
                }
            }

            return result;
        }
    }

    public class FunctionInfo
    {
        public string FName { get; set; }
        public string FType { get; set; }
        public int FParamCount { get; set; }
        public int FParamIndex { get; set; }
        public string[] FParam { get; set; } = new string[20];
    }

}
