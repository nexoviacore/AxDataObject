using ARMCommon.Interface;
using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;

namespace AxStructDef.Helpers
{
    public class AxStructDefUtils
    {
        // === Private fields ===
        private bool IsConnectMap;





        private string Axp_MapDriveLetter;
        private HashSet<char> numbers; // TSetChar -> HashSet<char>
        private int DayLen, MonLen, YearLen;
        private string redis_errmsg;
        private object FRedis; // placeholder for IRedisClient

        // === Public fields ===
        public DateTimeFormatInfo ShortDateFormat { get; set; } = new DateTimeFormatInfo();
        public Dictionary<string, string> LDSKeys { get; set; }
        public int axdecimal;
        public string MatchErrorStr;

        public bool UsedQuotedStr;
        public static double GlobalSerialNo;


        public string ServiceName;

        public int nownum, Zoomvalue;
        public string LocDecimalSeparator;
        public string lastnowstring, Applanguage, ReadOnly_FontColor, ReadOnly_BgColor, schemaname, defstrtrns;
        public string UserType, SQLLower, DbDateString, fileverno, appverno, exeverno, forupdate, dbmflag, SqlNull, nullcds, remotedbType, dbdateformat, datemode, AppVarComments;
        public static int SiteNo, SesCount, remoteConnectNo;
        public bool Millions, NewGen;
        public double LastConnectNo;
        public bool TraceAppLoad, EvalCopy, AppTerminate, PartnersCopy, SiteCopy;
        public int usercategoryid, userid;
        public bool displayTotRows;
        public string startpath;




        public double BaseCurrencyId;
        public string AxpLogStr;
        public string companyname, username, usergroup, userroles, inuserroles, team, usercategory, userdepartment, dbrep, crrep, profitpath, AppTitle, StartPath, dbase, usergroupno, pageaccess;
        public string AxpertPath;
        public string _MainCurr, _SubCurr, _Millions, AppFontCharSet, AppFontName, currencyname, currencytable, connectionname, projectname;
        public int AppFontSize;
        public int _CurrDecimal, LoginLevel;
        public string LocThousandSeparator;
        public DateTime finyrst, finyred, afinyrst, afinyred;
        public List<string> Registered, BtnActions;
        public XmlNode GlobalProps, LocalProps, mailprops;
        public XmlDocument AppXML, AxProps;
        public string HeadColor, GridHeadColor, HeadFont, GridHeadFont, ContainerColor, GridOddRowColor, GridEvenRowColor, defbackcolor;
        public string MailHost, MailPort, MailUserId, MailPassword, MailFrom;
        public string SessionId, AppName, URL, wsdl, axpservice, axpport;
        public List<string> AppVars, UserVars;
        public string AppVarTypes;
        public object AMSCall, AESCall, AMSDataCall; // THTTPRIO placeholder
        public string structURL, dataURL, LicenseURL;
        public string structGetFrom, structSetTo;
        public bool RemoteLogin, KioskLogin;
        public bool AcsBuild, AcsManage, AcsTools, AcsWorkFlow, AcsSend, AcsImportStruct, AcsLanguage, AcsExportStruct, AcsAxpertManager, AcsAppParams, AcsAppManager;
        //public DoDebug DoDebug; // placeholder
        public bool IsService, FillAllFields, ServiceReconnected, ReConnectDB;
        public string verno;
        public string execActName;
        public int totalmsg, readmsg;
        public bool IcompChanged, TcompChanged, IsRapidPost;
        public object AppfontCDS; // placeholder for TClientDataSet
        public int Pagination_PageNo, pagination_pagesize, pagination_totalrows;
        public string selectTable;
        public string listvieworderby;
        public double OraErrFrom, OraErrTo;
        public bool AxConstraintExist;
        public string PktFolder;
        public bool RunMode;
        public string lastlogintime, exepath;
        public bool biDirectional;
        public object PickListMode; // placeholder
        public object FormLevelPickListMode, CompLevelPickListMode;
        public bool axpdataexchange, FromAdr;
        public string proj_usergroup_cond;
        public bool ShowChangePwd;
        public int menu_pnl_width;
        public bool NewTrans, CopyTrans;
        public DateTime AppVarsLoadDateTime;
        public bool SysCrossDate;
        public int dbtimetaken, sqltimetaken, dbconnectiontime;
        public IFormatProvider ShortDateFormatProvider;
        public int AutoMemNo;
        public List<object> AutoGenData;
        //public Font AppTitleFont, Toplinkfont, Titlefont, ToolbarFont, Textfont, RibbonFont, Gridfont;
        public string Skinname, Background, ForeColor, GridHead, Fonttype, Bordercolor, Toolbarcolor, TBStartcolor, TBEndColor, PaginationPanelColor, ButCaps, GridColorfromGUI;
        public string d_sessionid;
        public string GFontProps;
        public bool DFont;
        public string axcloudid, axRegId, axurl;
        public bool axpublish;
        public bool PostAutoGen;
        public bool AxpLocalDB;
        // … (fields truncated for brevity — but they all map one-to-one)

        // === Constants ===
        public const string LDSExt = ".LDS";
        public const string LDSTempExt = ".tmp";
        public const string LDSDataDateTime = "axldsdatadatetime";
        public const string DefaultRedisHost = "127.0.0.1";
        public const int DefaultRedisPort = 6379;
        private ARMCommon.Helpers.Utils _common;
        private IDbHelper _dbHelper;

        // === Constructor / Destructor ===
        public AxStructDefUtils(ARMCommon.Helpers.Utils common)
        {
            numbers = new HashSet<char>();
            Registered = new List<string>();
            BtnActions = new List<string>();
            AppVars = new List<string>();
            UserVars = new List<string>();
            LocThousandSeparator = ",";
            LocDecimalSeparator = ".";
            UsedQuotedStr = false;
            LDSKeys = new Dictionary<string, string>();
            AddLDSKeys();
            MatchErrorStr = "";
            axdecimal = -1;
            AxpLocalDB = false;
            IsService = false;
            IsRapidPost = false;
            username = "";
            usergroup = "";
            startpath = "";

            AxpLocalDB = false;


            displayTotRows = true;
            _common = common;

            _dbHelper = _common._dbHelper;


        }

        ~AxStructDefUtils() { }

        // ===  Converted Methods ===

        private string QuoteString(string value)
        {
            return $"'{value}'";
        }

        //public int strtointz(string s) => int.Parse(s);
        public int strtointz(string s)
        {
            int result;
            if (string.IsNullOrEmpty(s))
            {
                result = 0;

            }
            else
            {
                result = int.Parse(s);
            }

            return result;
        }
        public string DummyDate => $"01{ShortDateFormat.DateSeparator}01{ShortDateFormat.DateSeparator}1900";

        public bool IsSysVar(string s)
        {
            string vstr = ";username;recordid;finyrst;finyred;afinyrst;afinyred;usergroup;";
            vstr += "usergroupno;activerow;_maincurr;rolename;responsibilities;";
            vstr += "siteno;wf_applevel;wf_status;wf_maxlevel;axp_language;";
            vstr += "axp_timezone;axp_service;axp_apipath;axp_appschema;axp_devschema;";

            return vstr.Contains($";{s.ToLower()};");
        }
        public void AddLDSKeys()
        {
            LDSKeys.Add("ldsfrom_", ">= ");
            LDSKeys.Add("ldsto_", "<= ");
            LDSKeys.Add("ldsgt_", "> ");
            LDSKeys.Add("ldslt_", "< ");
            LDSKeys.Add("ldscontains_", "c_like");
            LDSKeys.Add("ldsstarts_", "s_like");
            LDSKeys.Add("ldsends_", "e_like");
            LDSKeys.Add("ldslower_", "lower");
            LDSKeys.Add("ldsupper_", "upper");
            LDSKeys.Add("ldsin_", " in ");
        }
        public string ToShortDateFormat(string dbtype, string MyDate) { return MyDate; }
        public double Gen_id(string TableName, int Incr) { return 0; }
        public string GetLocalSystemName() { return Environment.MachineName; }
        public double RealRound(double num, int d) { return Math.Round(num, d); }
        /// <summary>
        /// Checks if a value is empty based on its data type
        /// </summary>
        /// <param name="value">The value to check</param>
        /// <param name="dataType">The data type of the value</param>
        /// <returns>"T" if empty, "F" if not empty</returns>
        public string IsEmptyValue(string value, string dataType)
        {
            string result = "F";
            dataType = dataType.ToLower();
            value = value?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(value) || value.ToLower() == "null")
            {
                result = "T";
            }
            else
            {
                if (dataType == "d" && value == ShortDateFormat.DateSeparator + "  " + ShortDateFormat.DateSeparator)
                {
                    result = "T";
                }
            }

            return result;
        }



        public string GetQuotedStr(string DelimitedStr, string Delimiter)
        {
            string result = "";
            if (string.IsNullOrEmpty(Delimiter)) Delimiter = ",";

            int i = 1;
            string s = GetNthString(DelimitedStr, i, Delimiter);
            while (!string.IsNullOrEmpty(s))
            {
                result += "," + QuoteString(s);
                i++;
                s = GetNthString(DelimitedStr, i, Delimiter);
            }

            if (!string.IsNullOrEmpty(result))
            {
                result = result.Substring(1); // Remove leading comma
            }

            return result;
        }

        public string GetNthString(string SrcString, int StrPos)
        {

            string result = "";
            int i = 0;
            int k = 1;

            while (k <= StrPos && i < SrcString.Length)
            {
                if (SrcString[i] == ',')
                {
                    k++;
                }
                else if (k == StrPos)
                {
                    result += SrcString[i];
                }
                i++;
            }

            return result;


        }
        public string GetNthString(string SrcString, int StrPos, char Separator)
        {
            string result = "";
            int i = 0;
            int k = 1;

            while (k <= StrPos && i < SrcString.Length)
            {
                if (SrcString[i] == Separator)
                {
                    k++;
                }
                else if (k == StrPos)
                {
                    result += SrcString[i];
                }
                i++;
            }

            return result;
        }
        public string GetNthString(string SrcString, int StrPos, string Separator)
        {
            if (Separator.Length == 1)
            {
                return GetNthString(SrcString, StrPos, Separator[0]);
            }
            else
            {
                string result = "";
                int i = 0;
                int k = 1;
                int searchLength = Separator.Length;

                while (k <= StrPos && i < SrcString.Length)
                {
                    if (i + searchLength <= SrcString.Length && SrcString.Substring(i, searchLength) == Separator)
                    {
                        k++;
                        i += searchLength;
                    }
                    else if (k == StrPos)
                    {
                        result += SrcString[i];
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

        public string Pad(string s, int maxLength, char c)
        {
            string result = s;
            int l = s.Length;

            if (l >= maxLength)
                return result;

            for (int i = l + 1; i <= maxLength; i++)
            {
                result += c;
            }

            return result;
        }
        public string LeftPad(string s, int maxLength, char c) { return s.PadLeft(maxLength, c); }

        public string AddString(string str, int RepeatTime) { return new string(str[0], RepeatTime); }
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

        public string InsertCommas(string value) { return value; }
        public string RemoveCommas(string s)
        {
            while (true)
            {
                int k = s.IndexOf(LocThousandSeparator);
                if (k == -1) break;
                s = s.Remove(k, 1);
            }
            return s;
        }
        public string TrimToSingleSpace(string s) { return System.Text.RegularExpressions.Regex.Replace(s, @"\s+", " "); }

        public string FormatValue(string value, int declen) { return value; }
        public string RemoveAmpherSand(string s) { return s.Replace("&", ""); }
        public string RemoveSlash(string s) { return s.Replace("/", ""); }

        public bool CheckAlpha(string str)
        {
            Regex regex = new Regex(@"^[a-zA-Z ]+$");
            return regex.IsMatch(str);
        }
        public bool CheckIsNumeric(string str)
        {
            Regex regex = new Regex(@"^[0-9]+$");
            return regex.IsMatch(str);
        }
        public bool CheckAlphaNumeric(string str)
        {
            Regex regex = new Regex(@"^[a-zA-Z0-9]*$");
            return regex.IsMatch(str);
        }


        // ... (dozens of other methods would follow the same pattern)

        // === Redis placeholders ===
        public void ConnectToRedisServer(string host, string pwd = "") { }
        public void ConnectToRedisServerWithXML(string host) { }
        public string GetValuesFromRedis(string host, string keyName, string pwd = "") { return string.Empty; }
        public void SetValuesToRedis(string host, string keyName, string keyValue, string pwd = "", int timeout = 0) { }

        // === Example Encryption placeholders ===
        public string EncryptPwd(string pwd) { return pwd; }
        public string AESDecrypt(string value) { return value; }
        public string AESEncrypt(string value) { return value; }

        public double RoundEx(double aInput, int aPlaces)
        {
            double k;

            if (aPlaces == 0)
            {
                return Math.Round(aInput);
            }
            else
            {
                if (aPlaces > 0)
                {
                    k = Math.Pow(10, aPlaces);
                    return Math.Round(aInput * k) / k;
                }
                else
                {
                    k = Math.Pow(10, -aPlaces);
                    return Math.Round(aInput / k) * k;
                }
            }


        }

        public double StrToFloatZ(string s)
        {
            if (!string.IsNullOrWhiteSpace(s))
                return double.Parse(RemoveCommas(s));
            return 0;
        }

        public bool CheckAxpEmail(string email)
        {
            Regex regex = new Regex(@"^[a-zA-Z0-9._-]+@[a-zA-Z0-9._-]+\.[a-zA-Z]{2,4}$");
            return regex.IsMatch(email);
        }

        public bool CheckIsPhone(string phone)
        {
            Regex regex = new Regex(@"^(\(?\d{3}\)?)?[- ]?(\d{3})[- ]?(\d{4})$");
            return regex.IsMatch(phone);
        }

        public bool CheckIsInit(string str)
        {
            string[] strSplit = str.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string checkInit = "";

            foreach (string item in strSplit)
            {
                checkInit += IsUppercase(item).ToString();
            }

            return !checkInit.ToLower().Contains("false");
        }

        private bool IsUppercase(string str)
        {
            return !string.IsNullOrEmpty(str) && char.IsUpper(str[0]);
        }

        public dynamic Connection { get; set; }

        public bool CheckIsCaps(string str)
        {
            return Regex.IsMatch(str, @"^[A-Z ]+$");
        }

        public bool CheckIsSmall(string str)
        {
            return Regex.IsMatch(str, @"^[a-z ]+$");
        }

        public bool CheckIsValidTime(string str, string matchStr)
        {
            if (matchStr == "24h format")
            {
                // 24-Hour: HH:mm[:ss]
                return Regex.IsMatch(str, @"^([01][0-9]|2[0-3]):[0-5][0-9](:[0-5][0-9])?$");
            }
            else
            {
                // 12-Hour: hh:mm[:ss] AM/PM
                return Regex.IsMatch(str, @"^(0[1-9]|1[0-2]):[0-5][0-9](:[0-5][0-9])?\s?(AM|PM)$", RegexOptions.IgnoreCase);
            }
        }

        public bool CheckMatch(string matchString, string value)
        {
            MatchErrorStr = "";
            bool result = true;
            matchString = matchString.ToLower();

            if (matchString == "isemail")
            {
                if (!CheckAxpEmail(value))
                {
                    MatchErrorStr = "Invalid email address";
                    result = false;
                }
            }
            else if (matchString == "isalpha")
            {
                if (!CheckAlpha(value))
                {
                    MatchErrorStr = "Only characters are allowed";
                    result = false;
                }
            }
            else if (matchString == "isnumeric")
            {
                if (!CheckIsNumeric(value))
                {
                    MatchErrorStr = "Only numbers are allowed";
                    result = false;
                }
            }
            else if (matchString == "isalphanumeric")
            {
                if (!CheckAlphaNumeric(value))
                {
                    MatchErrorStr = "Only alphanumeric characters are allowed";
                    result = false;
                }
            }
            else if (matchString == "isphone")
            {
                if (!CheckIsPhone(value))
                {
                    MatchErrorStr = "Please enter a valid PhoneNumber";
                    result = false;
                }
            }
            else if (matchString == "initcaps")
            {
                if (!CheckIsInit(value))
                {
                    MatchErrorStr = "First character of each word should be in Caps";
                    result = false;
                }
            }
            else if (matchString == "caps")
            {
                if (!CheckIsCaps(value))
                {
                    MatchErrorStr = "Only Uppercase characters are allowed";
                    result = false;
                }
            }
            else if (matchString == "small")
            {
                if (!CheckIsSmall(value))
                {
                    MatchErrorStr = "Only Lowercase characters are allowed";
                    result = false;
                }
            }
            else if (matchString == "24h format" || matchString == "12h format")
            {
                if (!CheckIsValidTime(value, matchString))
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

        public static double GenerateNumber()
        {
            GlobalSerialNo++;
            string paddedSescount = SesCount.ToString("D3"); // Left pad with zeros to 3 digits
            string paddedSerialNo = GlobalSerialNo.ToString("D5"); // Left pad with zeros to 5 digits
            return double.Parse(paddedSescount + paddedSerialNo);
        }



        public string ChangeSqlForPagination(string orgSql, bool findCount)
        {
            string result = orgSql;
            if (Pagination_PageNo == 0) return result;
            if (Pagination_PageNo == -1) Pagination_PageNo = 1;

            string s = "";
            string dbType = _dbHelper.GetDBType();
            if (dbType.ToLower() == "oracle")
            {
                if (findCount)
                {
                    s = $"SELECT COUNT(*) recno FROM (SELECT a.*, ROWNUM rnum FROM ({orgSql}) a)";
                    return s;
                }

                int eno = Pagination_PageNo * pagination_pagesize;
                int sno = eno - pagination_pagesize + 1;
                s = $"SELECT * FROM (SELECT a.*, ROWNUM axrnum FROM ({orgSql}) a) WHERE axrnum BETWEEN {sno} AND {eno}";
                result = s;
            }
            else if (dbType.ToLower() == "mysql" || dbType.ToLower() == "postgre")
            {
                if (findCount)
                {
                    s = $"SELECT COUNT(*) recno FROM (SELECT a.* FROM ({orgSql}) a) aa";
                    return s;
                }

                if (Pagination_PageNo == 1)
                {
                    s = $"{orgSql} LIMIT {pagination_pagesize}";
                }
                else
                {
                    int eno = Pagination_PageNo * pagination_pagesize;
                    int sno = eno - pagination_pagesize;
                    s = $"{orgSql} LIMIT {pagination_pagesize} OFFSET {sno}";
                }
                result = s;
            }
            else if (dbType.ToLower().Replace(" ", "") == "mssql")
            {
                string s1 = "";
                orgSql = FindAndReplace(orgSql, "\r\n", " ");
                orgSql = RemoveExtraSpaces(orgSql);
                string sql = orgSql.Trim().ToLower();

                string orderby = "";
                int orderbypos = 0;
                if (sql.Contains("order by"))
                {
                    orderbypos = sql.IndexOf("order by");
                    orderby = sql.Substring(orderbypos + 8);
                    while (orderby.Contains("order by"))
                    {
                        orderbypos += orderby.IndexOf("order by") + 7;
                        orderby = sql.Substring(orderbypos + 8);
                    }

                    if (sql.Contains(") ft"))
                    {
                        orderby = "order by " + orderby.Substring(0, orderby.IndexOf(") ft")).Trim();
                        orgSql = orgSql.Remove(orderbypos, orderby.Length);
                        sql = sql.Remove(orderbypos, orderby.Length);
                    }
                    else
                    {
                        orderby = "order by " + orderby.Trim();
                        orgSql = orgSql.Remove(orderbypos, sql.Length - orderbypos);
                        sql = sql.Remove(orderbypos, sql.Length - orderbypos);
                    }

                    string ordfld = orderby.Substring(orderby.IndexOf("by") + 2).Trim();
                    string col = "";
                    string fld = "";
                    if (ordfld.Contains(","))
                    {
                        string[] fields = ordfld.Split(',');
                        for (int i = 0; i < fields.Length; i++)
                        {
                            string s2 = "";
                            s = fields[i].Trim();
                            if (s.Contains(" "))
                            {
                                s2 = s.Substring(s.IndexOf(" ")).Trim();
                                s = s.Substring(0, s.IndexOf(" "));
                            }
                            s = s.Trim();
                            if (!IsNumeric(s) && !sql.Contains(s))
                            {
                                fld += "," + s;
                            }
                            if (s.Contains("."))
                            {
                                s = s.Substring(s.IndexOf(".") + 1);
                            }
                            col += s + " " + s2 + ",";
                        }
                        col = col.TrimEnd(',');
                    }
                    else
                    {
                        if (ordfld.Contains(" "))
                        {
                            s1 = ordfld.Substring(ordfld.IndexOf(" "));
                            ordfld = ordfld.Substring(0, ordfld.IndexOf(" "));
                        }
                        s = ordfld;
                        if (!sql.Contains(s)) fld += "," + s;
                        if (s.Contains(".")) s = s.Substring(s.IndexOf(".") + 1);
                        col = s;
                    }

                    if (!sql.Contains(" distinct ") && !string.IsNullOrEmpty(fld))
                    {
                        fld += "  ";
                        if (sql.Contains(":axp_searchval1"))
                        {
                            sql = sql.Substring(21);
                            int i = sql.IndexOf(" from ");
                            orgSql = orgSql.Insert(i + 21, fld);
                        }
                        else
                        {
                            int i = sql.IndexOf(" from ");
                            orgSql = orgSql.Insert(i, fld);
                        }
                    }

                    col = FindActualColNames(col, sql);
                    if (!string.IsNullOrEmpty(s1)) col += " " + s1;
                    orderby = " ORDER BY " + col;
                }
                else
                {
                    throw new Exception("Order by clause is must for SQL pagination. Please check the defined SQL");
                }

                if (findCount)
                {
                    s = $"SELECT COUNT(*) recno FROM (SELECT ROW_NUMBER() OVER ({orderby}) AS axrnum, * FROM ({orgSql}) AS abc) xy";
                    return s;
                }

                int eno = Pagination_PageNo * pagination_pagesize;
                int sno = eno - pagination_pagesize + 1;
                s = $"SELECT * FROM (SELECT *, ROW_NUMBER() OVER ({orderby}) AS axrnum FROM ({orgSql}) AS abc) xy WHERE axrnum BETWEEN {sno} AND {eno} ORDER BY axrnum";
                result = s;
            }

            return result;
        }

        public string FindActualColNames(string cols, string sqltext)
        {
            if (sqltext.Contains(":axp_searchval1"))
            {
                sqltext = sqltext.Substring(21).Trim();
            }
            else
            {
                sqltext = sqltext.Substring(7);
                int fromIndex = sqltext.ToLower().IndexOf("from");
                if (fromIndex >= 0)
                {
                    sqltext = sqltext.Substring(0, fromIndex).Trim();
                }
            }

            string result = "";
            int i = 1;
            cols += ",";

            while (true)
            {
                string s = GetNthString(cols, i);
                if (string.IsNullOrEmpty(s)) break;

                if (IsNumeric(s))
                {
                    string s1 = GetNthString(sqltext, int.Parse(s));
                    s1 = s1.Trim();
                    if (s1.Contains(" "))
                    {
                        s1 = s1.Substring(s1.IndexOf(" ") + 1);
                        if (s1.Contains(" "))
                        {
                            s1 = s1.Substring(s1.IndexOf(" ") + 1);
                        }
                    }
                    else if (s1.Contains("."))
                    {
                        s1 = s1.Substring(s1.IndexOf(".") + 1);
                    }
                    result += s1 + ",";
                }
                else
                {
                    result += s + ",";
                }
                i++;
            }

            if (result.Length > 0)
            {
                result = result.Remove(result.Length - 1); // Remove trailing comma
            }

            return result;
        }

        public bool IsNumeric(string value)
        {
            return double.TryParse(value, out _);
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




        //public string GetRowValue(DataRow row, string columnName)
        //{
        //    return row.Table.Columns.Contains(columnName) ?
        //           row[columnName]?.ToString() ?? "" : "";
        //}

        //public string GetRowValue(DataRow row, string columnName, string customDataType)
        //{
        //    string _exeDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log");
        //    Directory.CreateDirectory(_exeDir);
        //    string _logFileName = "Sdef_Utils-" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".log";
        //    _exeDir = Path.Combine(_exeDir, _logFileName);

        //    File.AppendAllText(_exeDir, $"ColumnName: {columnName}, CustomDataType: {customDataType}\n\n");

        //    if (!row.Table.Columns.Contains(columnName))
        //        return "";

        //    var value = row[columnName];
        //    var orgValue = row[columnName];

        //    File.AppendAllText(_exeDir, $"orgValue: {orgValue}\n\n");


        //    if (value == null || value == DBNull.Value)
        //        return "";

        //    try
        //    {
        //        if (customDataType.ToUpper() == "DATE")
        //        {
        //            var fmtValue = value.ToString().Split(' ')[0];

        //            DateTime dateValue = DateTime.ParseExact(
        //                fmtValue,
        //                new[] { "dd-MM-yyyy", "dd/MM/yyyy" },
        //                CultureInfo.InvariantCulture,
        //                DateTimeStyles.None);

        //            value = dateValue.ToString(
        //                _common.ApiDateFormat,
        //                CultureInfo.InvariantCulture);

        //            File.AppendAllText(_exeDir, $"Value: {value}\n\n");

        //        }
        //        else if (customDataType.ToUpper() == "TIMESTAMP")
        //        {
        //            DateTime dateValue = DateTime.ParseExact(value.ToString(), new[] { $"{_common.ApiDateFormat} {_common.ApiTimeFormat}",
        //                $"dd-MM-yyyy {_common.ApiTimeFormat}", $"dd/MM/yyyy {_common.ApiTimeFormat}" },
        //                CultureInfo.InvariantCulture, DateTimeStyles.None);

        //            value = dateValue.ToString($"{_common.ApiDateFormat} {_common.ApiTimeFormat}", CultureInfo.InvariantCulture);
        //            File.AppendAllText(_exeDir, $"Value: {value}\n\n");

        //        }

        //        //if (customDataType.ToUpper() == "DATE")
        //        //{
        //        //    var fmtValue = value.ToString().Split(" ")[0];
        //        //    DateTime dateValue = DateTime.ParseExact(fmtValue, _common.ApiDateFormat, CultureInfo.InvariantCulture);
        //        //    value = dateValue.ToString(_common.ApiDateFormat, CultureInfo.InvariantCulture);
        //        //    File.AppendAllText(_exeDir, $"Value: {value}\n\n");

        //        //}
        //        //else if (customDataType.ToUpper() == "TIMESTAMP")
        //        //{
        //        //    DateTime dateValue = DateTime.ParseExact(value.ToString(), $"{_common.ApiDateFormat} {_common.ApiTimeFormat}", CultureInfo.InvariantCulture);
        //        //    value = dateValue.ToString($"{_common.ApiDateFormat} {_common.ApiTimeFormat}", CultureInfo.InvariantCulture);
        //        //    File.AppendAllText(_exeDir, $"Value: {value}\n\n");

        //        //}
        //    }
        //    catch (Exception e)
        //    {

        //        File.AppendAllText(_exeDir, $"{e}\n\n" + $"ColumnName: {columnName}, CustomDataType: {customDataType}, orgValue: {orgValue}, Value: {value}\n\n");

        //    }


        //    //if (value is DateTime dateValue)
        //    //{
        //    //    if (!string.IsNullOrEmpty(ClientDateFormat))
        //    //        return dateValue.ToString(
        //    //            ClientDateFormat,
        //    //            CultureInfo.InvariantCulture);

        //    //    return dateValue.ToString();
        //    //}

        //    //if (value is DateTime dateValue)
        //    //{
        //    //    var format = _common.ApiDateFormat;

        //    //    if (dateValue.TimeOfDay != TimeSpan.Zero)
        //    //    {
        //    //        format += " " + _common.ApiTimeFormat;
        //    //    }

        //    //    return dateValue.ToString(
        //    //        format,
        //    //        CultureInfo.InvariantCulture);

        //    //if (value.ToString().Contains(":"))
        //    //{
        //    //    var mon = dateValue.Month;
        //    //    var year = dateValue.Year.ToString();
        //    //    var date = dateValue.Day;
        //    //    var hours = dateValue.Hour;
        //    //    var min = dateValue.Minute;
        //    //    var seconds = dateValue.Second;

        //    //    if (format.ToLower() == "dd/mm/yyyy hh:mm:ss")
        //    //        return $"{date}/{mon}/{year} {hours}:{min}:{seconds}";
        //    //    else
        //    //        return $"{mon}/{date}/{year} {hours}:{min}:{seconds}";
        //    //}
        //    //else
        //    //{
        //    //    var mon = dateValue.Month;
        //    //    var year = dateValue.Year.ToString();
        //    //    var date = dateValue.Day;

        //    //    if (_common.ApiDateFormat.ToLower() == "dd/mm/yyyy")
        //    //        return $"{date}/{mon}/{year}";
        //    //    else
        //    //        return $"{mon}/{date}/{year}";
        //    //}

        //    //}

        //    return value.ToString() ?? "";
        //}

        public string GetRowValue(DataRow row, string columnName, string customDataType)
        {
            if (!row.Table.Columns.Contains(columnName))
                return "";

            var value = row[columnName];

            if (value == null || value == DBNull.Value)
                return "";

            if (customDataType.ToUpper() == "DATE")
            {
                DateTime dateValue;

                if (value is DateTime dt)
                {
                    dateValue = dt;
                }
                else
                {
                    dateValue = Convert.ToDateTime(value, CultureInfo.InvariantCulture);
                }

                value = dateValue.ToString(_common.ApiDateFormat, CultureInfo.InvariantCulture).Split(" ")[0];
            }
            else if (customDataType.ToUpper() == "TIMESTAMP")
            {
                DateTime dateValue;

                if (value is DateTime dt)
                {
                    dateValue = dt;
                }
                else
                {
                    dateValue = Convert.ToDateTime(value, CultureInfo.InvariantCulture);
                }

                value = dateValue.ToString(
                    $"{_common.ApiDateFormat} {_common.ApiTimeFormat}",
                    CultureInfo.InvariantCulture);
            }

            return value.ToString() ?? "";
        }

        public DataTable AddCsvValuesToDataTable(DataTable dt, string csvValues, string columnName = "Value")

            {
                // Create new DataTable if null
                if (dt == null)
                {
                    dt = new DataTable();
                }

                // Ensure DataTable has the column

                if (!dt.Columns.Contains(columnName))

                {

                    dt.Columns.Add(columnName, typeof(string));

                }

                // Split CSV and add each value as a row

                string[] values = csvValues.Split(',');

                foreach (string val in values)

                {

                    dt.Rows.Add(val.Trim());

                }

                return dt;

            }



        public bool IsKeyword(string fname)
        {
            var _keywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);




            string keywordList =
                "ADD,ALTER,AND,ANY,AS,ASC,AUDIT,BETWEEN,BY,CHAR,CHECK,CLUSTER,COLUMN,COMMENT" +
                ",COMPRESS,CONNECT,CREATE,CURRENT,DATE,DECIMAL,DEFAULT,DELETUE" +
                ",DESC,DISTINCT,DROP,ELSE,EXCLUSIVE,EXISTS,FILE,FLOAT,FOR,FROM" +
                ",GRANT,GROUP,HAVING,IDENTIFIED,IMMEDIATE,IN,INCREMENT,INDEX,INITIAL" +
                ",INSERT,INTEGER,INTERSECT,INTO,IS,LEVEL,LIKE,LOCK,LONG,MAXEXTENTS,MINUS" +
                ",MODE,MODIFY,NOAUDIT,NOCOMPRESS,NOT,NOWAIT,NULL,NUMBER,OF,OFFLINE,OPTION" +
                ",OR,ORDER,PCTFREE,PRIOR,PRIVILEGES,PUBLIC,RAW,RENAME,RESOURCE,REVOKE" +
                ",ROW,ROWID,ROWLABEL,ROWNUM,ROWS,SELECT,SESSION,SET,SHARE,SIZE,SMALLINT" +
                ",START,SUCCESSFUL,SYNONYM,SYSDATE,TABLE,THEN,TO,TRIGGER,UID,UNION" +
                ",UNIQUE,USER,VALIDATE,VALUES,VARCHAR,VARCHAR2,VIEW,WHENEVER,WHERE,WITH" +
                ",SIZE,SELECT,DROP,AS,IN,ADD,UPDATE,ALTER,FIELD,COLUMN,ORDER,VIEW" +
                ",TABLE,TAB,FROM,DATABASE,VALUES,ORDER BY,DEFAULT,VALUE,JOINT,PASSWORD" +
                ",GROUP,NO,NOT,TO,FieldChanged,GetRCell,Upper,Lower,DTOC" +
                ",CTOD,RND,ROUND,STUFF,IIF,AmtWord,Val,STR,SUBSTR,Convert,RevConvert" +
                ",Encode,Decode,HashChar,MandY,DaysElapsed,TimeElapsed,AddToTime" +
                ",AddToDate,AddToMonth,IsEmptyValue,LastDayOfMonth,ValidEncodeDate" +
                ",Eval,IsEmpty,Abs,MakeDate,Date,FormatDateTime,Time,DayofDate" +
                ",MonthofDate,YearofDate,Gen_id,Total,Sum,GetMax,GetValue,GetId" +
                ",GetRow,GetRowCount,GetCostRate,GetAdjustedAmount,SumTill" +
                ",GetCell,GETINTEGER,Power,CreatedBy,CreatedOn" +
                ",AxpCeil,AxpFloor,Days360,data";

            foreach (var keyword in keywordList.Split(','))
            {
                _keywords.Add(keyword.Trim().ToUpper());
            }


            return _keywords.Contains(fname?.ToUpper() ?? string.Empty);
        }


    }
}
