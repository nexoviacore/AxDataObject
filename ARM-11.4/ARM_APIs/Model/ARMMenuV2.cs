using ARM_APIs.Interface;
using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMCommon.Model;
using System.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using NPOI.SS.Formula.Functions;
using StackExchange.Redis;
using NPOI.POIFS.Properties;
using System.Diagnostics.Eventing.Reader;
using static ARMCommon.Helpers.Constants;

namespace ARM_APIs.Model
{
    public class ARMMenuV2 : IARMenuV2
    {
        private readonly DataContext _context;
        private readonly IRedisHelper _redis;
        private readonly IConfiguration _config;
        private readonly IPostgresHelper _postGres;
        private readonly Utils _common;

        public ARMMenuV2(DataContext context, IRedisHelper redis, IConfiguration configuration, IPostgresHelper postGres, Utils common)
        {
            _context = context;
            _redis = redis;
            _config = configuration;
            _postGres = postGres;
            _common = common;
        }


        private async Task<Dictionary<string, string>> GetLoginUser(string ARMSessionId)
        {
            var dictSession = await _redis.HashGetAllDictAsync(ARMSessionId);
            return dictSession;
        }

        public async Task<DataTable> GetHomePage(string sessionId, string userName)
        {
            var loginuser = await GetLoginUser(sessionId);
            if (string.IsNullOrEmpty(userName))
            {
                userName = loginuser["USERNAME"].ToString();
            }
            Dictionary<string, string> config = await _common.GetDBConfigurations(loginuser["APPNAME"]);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];
            string sql = Constants_SQL.HOMEPAGECARDSV2.ToString();
            string[] paramName = { "@username" };
            DbType[] paramType = { DbType.String };
            object[] paramValue = { userName };

            if (dbType.ToUpper() == Constants.DBTYPE.ORACLE.ToString())
            {
                sql = Constants_SQL.HOMEPAGECARDSV2.ToString();
            }

            IDbHelper dbHelper = DBHelper.CreateDbHelper(sql, dbType, connectionString, paramName, paramType, paramValue);
            var table = await dbHelper.ExecuteQueryAsync(sql, connectionString, paramName, paramType, paramValue);
            return table;
        }

        public async Task<DataTable> GETMENUFORDEFAULTROLE_V2(string sessionId)
        {
            ParamsDetails parameters = new ParamsDetails();
            parameters.ParamsNames = new List<ConnectionParamsList>();

            var loginuser = await GetLoginUser(sessionId);
            Dictionary<string, string> config = await _common.GetDBConfigurations(loginuser["APPNAME"]);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];
            string sql = Constants_SQL.GETMENUFORDEFAULTROLE_V2.ToString();
            IDbHelper dbHelper = DBHelper.CreateDbHelper(sql, dbType, connectionString, new string[] { }, new DbType[] { }, new object[] { });
            if (dbType.ToUpper() == Constants.DBTYPE.ORACLE.ToString())
            {
                sql = Constants_SQL.GETMENUFORDEFAULTROLE_V2.ToString();
            }
            var table = await dbHelper.ExecuteQueryAsync(sql, connectionString, new string[] { }, new DbType[] { }, new object[] { });
            return table;
        }

        public async Task<DataTable> GETMENUFOROTHERROLE_V2(string sessionId, string allRole)
        {
            ParamsDetails parameters = new ParamsDetails();
            parameters.ParamsNames = new List<ConnectionParamsList>();
            var loginuser = await GetLoginUser(sessionId);
            Dictionary<string, string> config = await _common.GetDBConfigurations(loginuser["APPNAME"]);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];
            string sql = Constants_SQL.GETMENUFOROTHERROLE_V2.ToString().Replace("$allRole$", allRole);
            IDbHelper dbHelper = DBHelper.CreateDbHelper(sql, dbType, connectionString, new string[] { }, new DbType[] { }, new object[] { });
            if (dbType.ToUpper() == Constants.DBTYPE.ORACLE.ToString())
            {
                sql = Constants_SQL.GETMENUFOROTHERROLE_V2.ToString().Replace("$allRole$", allRole);
            }

            var table = await dbHelper.ExecuteQueryAsync(sql, connectionString, new string[] { }, new DbType[] { }, new object[] { });
            return table;
        }

        public async Task<List<string>> GetallRole(string sessionId)
        {
            string sessionValues = await _redis.HashGetAsync(sessionId, Constants.SESSION_DATA.USER_ROLES.ToString());
            List<string> userroles = sessionValues.Split(',').ToList();
            return userroles;
        }

        public async Task<ARMResult> GetHomePageCards(ARMProcessFlowTask process)
        {
            List<string> userroles = await GetallRole(process.ARMSessionId);
            string allRole = string.Join(", ", userroles.Select(role => $"'{role}'"));

            DataTable MenuList;
            if (userroles.Contains("default"))
            {
                MenuList = await GETMENUFORDEFAULTROLE_V2(process.ARMSessionId);
            }
            else
            {
                MenuList = await GETMENUFOROTHERROLE_V2(process.ARMSessionId, allRole);
            }

            var Homepagecardslist = await GetHomePage(process.ARMSessionId, process.ToUser);
            var axPagesView = new DataView(MenuList);
            var homePageCardsView = new DataView(Homepagecardslist);
            var axPagesTable = axPagesView.ToTable();
            var homePageCardsTable = homePageCardsView.ToTable();

            var menuoptions = from card in homePageCardsTable.AsEnumerable()
                              join menu in axPagesTable.AsEnumerable()
                              on card["pgname"].ToString() equals menu["name"].ToString()
                              where menu["type"].ToString() == "p"
                              && card["panelType"].ToString() == "Menu option"
                              select new
                              {
                                  cardid = card["cardid"],
                                  caption = card["caption"],
                                  pgname = card["pgname"],
                                  displayicon = card["displayicon"],
                                  stransid = card["stransid"],
                                  datasource = card["datasource"],
                                  moreoption = card["moreoption"],
                                  colorcode = card["colorcode"],
                                  groupfolder = card["groupfolder"],
                                  grppageid = card["grppageid"],
                                  carddesc = card["carddesc"],
                                  cardhide = card["cardhide"],
                                  html_editor_card = card["html_editor_card"],
                                  paneltypecnd = card["paneltypecnd"],
                                  //paneltype = card["paneltype"]
                              };

            var parentIds = homePageCardsTable.AsEnumerable()
                                    .Where(row => row["paneltype"].ToString() == "Menu folder")
                                    .Select(row => (parentId: row["pgname"].ToString(), parentCaption: row["caption"].ToString()))
                                    .Distinct();

            DataTable finalMenuFolder = new DataTable();
            finalMenuFolder.Columns.Add("cardid", typeof(string));
            finalMenuFolder.Columns.Add("caption", typeof(string));
            finalMenuFolder.Columns.Add("groupfolder", typeof(string));
            finalMenuFolder.Columns.Add("grppageid", typeof(string));
            finalMenuFolder.Columns.Add("paneltype", typeof(string));
            finalMenuFolder.Columns.Add("target", typeof(string));

            foreach (var (parentId, parentCaption) in parentIds)
            {
                if (!string.IsNullOrEmpty(parentId))
                {
                    var menufolder = await GetAllGrandChildren(axPagesTable, homePageCardsTable, parentId, parentId, parentCaption);
                    if (menufolder.Rows.Count > 0)
                    {
                        finalMenuFolder.Merge(menufolder);
                    }
                }
            }

            return new ARMResult
            {
                result = new Dictionary<string, object>
                    {
                        { "message", "SUCCESS" },
                        { "menu folder", finalMenuFolder },
                        { "menu option", menuoptions }
                    }
            };

            throw new Exception("No valid Parent ID found in the HomePageCards.");
        }

        public async Task<DataTable> GetAllGrandChildren(DataTable axPagesTable, DataTable homePageCardsTable, string parentId, string mainParentId, string mainParentCaption)
        {
            DataTable resultTable = new DataTable();
            resultTable.Columns.Add("cardid", typeof(string));
            resultTable.Columns.Add("caption", typeof(string));
            resultTable.Columns.Add("groupfolder", typeof(string));
            resultTable.Columns.Add("grppageid", typeof(string));
            resultTable.Columns.Add("paneltype", typeof(string));
            resultTable.Columns.Add("target", typeof(string));

            await PopulateChildren(axPagesTable, resultTable, parentId, mainParentId, mainParentCaption);

            return resultTable;
        }

        public async Task PopulateChildren(DataTable axPagesTable, DataTable resultTable, string parentId, string mainParentId, string mainParentCaption)
        {
            var childRows = axPagesTable.AsEnumerable()
                                        .Where(row => row["parent"].ToString() == parentId && row["visible"].ToString() == "T");

            if (!childRows.Any())
            {
                return;
            }

            foreach (var childRow in childRows)
            {
                if (childRow["type"].ToString() != "h")
                {
                    var target = "";
                    if (childRow["pagetype"].ToString().ToLower().StartsWith("i"))
                    {
                        target = "iview.aspx?ivname=" + childRow["pagetype"].ToString();
                    }
                    else if (childRow["pagetype"].ToString().ToLower().StartsWith("t"))
                    {
                        target = "tstruct.aspx?transid=" + childRow["pagetype"].ToString();
                    }
                    else if (childRow["pagetype"].ToString().ToLower().StartsWith("w"))
                    {
                        target = "html.aspx?pname=" + childRow["name"].ToString();
                    }


                    if (target != "")
                    {
                        var newRow = resultTable.NewRow();
                        newRow["cardid"] = childRow["name"].ToString();
                        newRow["caption"] = childRow["caption"].ToString();
                        newRow["groupfolder"] = mainParentCaption;
                        newRow["grppageid"] = mainParentId;
                        newRow["paneltype"] = "Menu folder";
                        newRow["target"] = target;
                        resultTable.Rows.Add(newRow);
                    }
                }

                string childId = childRow["name"].ToString();
                await PopulateChildren(axPagesTable, resultTable, childId, mainParentId, mainParentCaption);
            }
        }


        public async Task<ARMResult> GetMenu(ARMSession model)
        {
            DataTable axPages = new DataTable();
            List<string> userRoles = await GetallRole(model.ARMSessionId);

            var dictSession = await _redis.HashGetAllDictAsync(model.ARMSessionId);
            var appName = dictSession["APPNAME"];

            if (userRoles.Count == 0)
            {
                throw new Exception("NO_RECORD_IN_AX_PAGES");
            }

            string allRoles = string.Join(", ", userRoles.Select(role => $"'{role}'"));

            axPages = userRoles.Contains(Constants.Roles.DEFAULT.ToString().ToLower())
                ? await GETMENUFORDEFAULTROLE_V2(model.ARMSessionId)
                : await GETMENUFOROTHERROLE_V2(model.ARMSessionId, allRoles);

            if (axPages.Rows.Count > 0)
            {
                // Adding a new column for URL
                axPages.Columns.Add(new DataColumn("url", typeof(string)));

                SQLResult sqlResult = await ExecuteSQLAsync(appName);
                Dictionary<string, string> pageConfigDict = ConvertSQLResultToDictionary(sqlResult) ?? new Dictionary<string, string>();

                foreach (DataRow row in axPages.Rows)
                {
                    string pageType = row["pagetype"].ToString().Trim();
                    if (string.IsNullOrEmpty(pageType)) continue;

                    string pagePrefix = pageType.Substring(0, 1);
                    string webPagePrefix = "h" + row["name"]?.ToString().Substring(2);

                    if (pageType.StartsWith("tt"))
                    {
                        string modifiedPageType = pageType.Substring(1); // Remove first 'T'
                        if (pageConfigDict.ContainsKey(modifiedPageType))
                        {
                            row["url"] = $"aspx/AxMain.aspx?authKey=AXPERT-{model.ARMSessionId}&pname=l{modifiedPageType}";
                            continue;
                        }
                    }

                    switch (pagePrefix)
                    {
                        case "i":
                            row["url"] = $"aspx/AxMain.aspx?authKey=AXPERT-{model.ARMSessionId}&pname={pageType}";
                            break;
                        case "t":
                            if (pageConfigDict.Count <= 0 || !pageConfigDict.ContainsKey(pageType))
                            {
                                row["url"] = $"aspx/AxMain.aspx?authKey=AXPERT-{model.ARMSessionId}&pname={pageType}";
                            }
                            else if (pageConfigDict.ContainsKey("ALL Forms"))
                            {
                                row["url"] = $"aspx/AxMain.aspx?authKey=AXPERT-{model.ARMSessionId}&pname=l{pageType}";
                            }
                            else
                            {
                                row["url"] = pageConfigDict[pageType];
                            }
                            break;
                        case "w":
                            row["url"] = $"aspx/AxMain.aspx?authKey=AXPERT-{model.ARMSessionId}&pname={webPagePrefix}";
                            break;
                        default:
                            row["url"] = "";
                            break;
                    }
                }
            }

            return new ARMResult
            {
                result = { { "message", "SUCCESS" }, { "data", axPages } }
            };
        }



        private Dictionary<string, string> ConvertSQLResultToDictionary(SQLResult sqlResult)
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();

            if (sqlResult != null && sqlResult.data.Rows.Count > 0)
            {
                foreach (DataRow row in sqlResult.data.Rows)
                {
                    string key = row["structname"]?.ToString();
                    string value = row["propsval"]?.ToString();

                    if (!string.IsNullOrEmpty(key) && !dict.ContainsKey(key))
                    {
                        dict.Add(key, value);
                    }
                }
            }
            return dict;
        }

        private async Task<SQLResult> ExecuteSQLAsync(string appName)
        {
            DataTable table = new DataTable();
            IDbHelper dbHelper = DBHelper.CreateDbHelper("postgre"); // Assuming PostgreSQL
            string sql = "SELECT structname, propsval FROM axpstructconfig WHERE asprops = 'Listview as Default' AND stype = 'Tstruct'";
            Dictionary<string, string> config = await _common.GetDBConfigurations(appName);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];
            SQLResult sqlResult = await dbHelper.ExecuteSQLAsync(sql, connectionString);
            return sqlResult;
        }

        //public async Task<ARMResult> ARMConnectionTestDetails()
        //{
        //    var appSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
        //    var json = await System.IO.File.ReadAllTextAsync(appSettingsPath);
        //    var builder = new ConfigurationBuilder()
        //        .SetBasePath(Directory.GetCurrentDirectory())
        //        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

        //    var configuration = builder.Build();
        //    string host = configuration.GetConnectionString("WebApiDatabase");
        //    string Redisport = configuration["AppConfig:RedisHost"];
        //    string Redispass = configuration["AppConfig:RedisPassword"];
        //    string Rabbitmq = configuration["AppConfig:RMQIP"];
        //    string axpertredis = configuration["AppConfig:AxpertRedisHost"];
        //    string axpertpass = configuration["AppConfig:AxpertRedisHost"];

        //    var responseObject = new
        //    {
        //        WebApiDatabase = host,
        //        RedisHost = Redisport,
        //        RedisPassword = Redispass,
        //        RMQIP = Rabbitmq,
        //        AxpertRedisHost = axpertredis,
        //        AxpertRedisPassword = axpertpass
        //    };


        //    ARMResult result = new ARMResult();
        //    result.result.Add("message", "SUCCESS");
        //    result.result.Add("data", responseObject);
        //    return result;

        //}

        public async Task<SQLResult> GlobalSearch(GlobalSearch search)
        {
            var sessionVars = await _common.GetSessionData(search.ARMSessionId);

            DataTable table = new DataTable();
            var appName = sessionVars[SESSION_DATA.APPNAME.ToString()].ToString();
            var userName = sessionVars[SESSION_DATA.USERNAME.ToString()].ToString();



            Dictionary<string, string> config = await _common.GetDBConfigurations(appName);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];

            string sql = Constants_SQL.GET_GLOBALSEARCH_DATA;

            string[] paramName = { "@username", "@searchtext"};
            DbType[] paramType = { DbType.String, DbType.String};
            object[] paramValue = { userName.ToString().ToLower(),  $"%{search.SearchText.ToLower()}%" };

            var dsResult = await GetData(dbType, sql, connectionString, paramName, paramType, paramValue);
            return dsResult;

        }

        private async Task<SQLResult> GetData(string dbType, string sql, string connectionString, string[] paramName = null, DbType[] paramType = null, object[] paramValue = null)
        {
            IDbHelper dbHelper = DBHelper.CreateDbHelper(dbType);
            return await dbHelper.ExecuteSQLAsync(sql, connectionString, paramName, paramType, paramValue);
        }


    }


}
