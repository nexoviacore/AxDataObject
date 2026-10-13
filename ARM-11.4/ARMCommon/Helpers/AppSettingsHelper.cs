using ARMCommon.Interface;
using ARMCommon.Model;
using System.Data.Odbc;
using Microsoft.Win32;
using Npgsql;

namespace ARMCommon.Helpers
{
    public class AppSettingsHelper : IAppSettingsHelper
    {
        private readonly IAppSettingsService _appSettingsService;

        public AppSettingsHelper(IAppSettingsService appSettingsService)
        {
            _appSettingsService = appSettingsService;
        }

        public AppConnection GetAppConnection(string appName)
        {
            var connection = _appSettingsService.GetAppConnection(appName);
            if (connection == null)
                throw new InvalidOperationException($"app '{appName}' not found in app connections");

            return connection;
        }

        public Dictionary<string, string> GetDBConfigurations(string appName)
        {
            var dbConfig = new Dictionary<string, string>();
            var connection = _appSettingsService.GetAppConnection(appName);
            if (connection == null)
                throw new InvalidOperationException($"app '{appName}' not found in app connections");

            string dbuser = connection.DbUser; // dataSet.Tables[0].Rows[0]["dbuser"].ToString();
            string dbtype = connection.Db; // dataSet.Tables[0].Rows[0]["db"].ToString();
            string pwd = connection.DecryptedPwd; // dataSet.Tables[0].Rows[0]["pwd"].ToString();
            string Server = connection.DbCon; // dataSet.Tables[0].Rows[0]["dbcon"].ToString();
            string database = connection.DbUser; // dataSet.Tables[0].Rows[0]["dbuser"].ToString();
            string version = connection.Version; // dataSet.Tables[0].Rows[0]["version"].ToString().Trim();
            string driver = connection.Driver;
            string odbcdbuser = connection.ODBCDbUser;

            string dbuserALC = dbuser;
            string cs = "";

            if (dbtype.Equals("ms sql", StringComparison.OrdinalIgnoreCase) || dbtype.Equals("mssql", StringComparison.OrdinalIgnoreCase))
            {
                dbConfig.Add("DBType", Constants.DBTYPE.MSSQL.ToString());

                if (version.ToUpper() == "ABOVE 2012")
                    Server = GetServerIpAndPort(Server, dbuser, pwd);

                cs = $"Server={Server}; Database={database}; User Id={dbuser}; Password={pwd};";
                dbuserALC = dbuser;
            }
            else if (dbtype.Equals("oracle", StringComparison.OrdinalIgnoreCase))
            {
                dbConfig.Add("DBType", Constants.DBTYPE.ORACLE.ToString());

                cs = $"Data Source={Server};User Id={dbuser};Password={pwd};Pooling=False;";
                dbuserALC = dbuser;
            }
            else if (dbtype.Equals("mysql", StringComparison.OrdinalIgnoreCase) ||
                     dbtype.Equals("mariadb", StringComparison.OrdinalIgnoreCase))
            {
                dbConfig.Add("DBType", Constants.DBTYPE.MYSQL.ToString());

                if (Server.Contains(":"))
                {
                    string serverPort = Server.Substring(Server.IndexOf(':') + 1);
                    cs = $"Server={Server.Substring(0, Server.IndexOf(':'))}; Port={serverPort}; Database={database};Uid={dbuser};Pwd={pwd};";
                }
                else
                {
                    cs = $"Server={Server};Database={database};Uid={dbuser};Pwd={pwd};";
                }
                dbuserALC = dbuser;
            }
            else if (dbtype.Equals("postgresql", StringComparison.OrdinalIgnoreCase) ||
                     dbtype.Equals("postgre", StringComparison.OrdinalIgnoreCase))
            {
                dbConfig.Add("DBType", Constants.DBTYPE.POSTGRE.ToString());

                if (driver == "ado")
                {                   
                    //cs = GetPostgreODBCConnection(Server);
                    //dbuserALC = dbuser + "~" + Server;
                    //dbuser = dbuser + "~" + Server;

                    cs = GetPostgreODBCConnection(Server, odbcdbuser, pwd);
                    string _database = cs.Split('♣')[1];
                    cs = cs.Split('♣')[0];

                    //if (string.IsNullOrEmpty(dbuser) && !string.IsNullOrEmpty(odbcdbuser))
                    if (!string.IsNullOrEmpty(odbcdbuser))
                        dbuser = odbcdbuser;

                    dbuserALC = dbuser + "~" + _database;
                    dbuser = dbuser + "~" + Server;

                    dbConfig.Add("Driver", "ado");
                    dbConfig.Add("DBCon", Server);
                }
                else
                {
                    if (Server.Contains(":"))
                    {
                        if (dbuser.Contains("\\") || database.Contains("\\"))
                        {
                            string[] userDtls = dbuser.Split('\\');
                            string[] databaseDtls = database.Split('\\');
                            if (userDtls.Length > 1 && userDtls[1] != "" &&
                                databaseDtls.Length > 1 && databaseDtls[1] != "")
                            {
                                dbuser = userDtls[0];
                                database = databaseDtls[1];
                            }
                            else
                            {
                                dbuser = userDtls[0];
                                database = databaseDtls[0];
                            }
                            dbuserALC = dbuser + "~" + database;
                        }
                        else
                        {
                            database = "axpertdb";
                            dbuserALC = dbuser;
                        }
                        string serverPort = Server.Substring(Server.IndexOf(':') + 1);
                        cs = $"Server={Server.Substring(0, Server.IndexOf(':'))}; Port={serverPort}; Database={database};Uid={dbuser};Pwd={pwd};Pooling=false;";
                        dbuser = dbuser + "~" + database;
                    }
                    else
                    {
                        if (dbuser.Contains("\\") || database.Contains("\\"))
                        {
                            string[] userDtls = dbuser.Split('\\');
                            string[] databaseDtls = database.Split('\\');
                            if (userDtls.Length > 1 && userDtls[1] != "" &&
                                databaseDtls.Length > 1 && databaseDtls[1] != "")
                            {
                                dbuser = userDtls[0];
                                database = databaseDtls[1];
                            }
                            else
                            {
                                dbuser = userDtls[0];
                                database = databaseDtls[0];
                            }
                            dbuserALC = dbuser + "~" + database;
                        }
                        else
                        {
                            database = "axpertdb";
                            dbuserALC = dbuser;
                        }
                        cs = $"Server={Server};Database={database};Uid={dbuser};Pwd={pwd};Pooling=false;";
                        dbuser = dbuser + "~" + database;
                    }

                }
            }
            dbConfig.Add("ConnectionString", cs);
            dbConfig.Add("DBUser", dbuser);
            dbConfig.Add("DBUserALC", dbuserALC);
            return dbConfig;
        }

        public static string GetServerIpAndPort(string serverName, string uid, string pwd)
        {
            string connStr = "DSN=" + serverName + ";Uid=" + uid + ";Pwd=" + pwd + ";";
            string dataSourceName = String.Empty;
            using (OdbcConnection odbcConn = new OdbcConnection(connStr))
            {
                try
                {
                    odbcConn.Open();
                    try
                    {
                        using (OdbcCommand cmd = odbcConn.CreateCommand())
                        {
                            cmd.CommandText = @"
                            SELECT local_net_address, local_tcp_port 
                            FROM sys.dm_exec_connections 
                            WHERE session_id = @@SPID";
                            using (OdbcDataReader reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    string ip = reader["local_net_address"].ToString();
                                    string port = reader["local_tcp_port"].ToString();
                                    dataSourceName = ip + "\\" + serverName + "," + port;
                                }
                            }
                        }
                    }
                    catch
                    {
                        string ds = GetServerFromDsn(serverName);
                        dataSourceName = !string.IsNullOrEmpty(ds) ? ds : serverName;
                    }
                }
                catch (Exception ex)
                {
                    return serverName;
                }
                finally
                {
                    odbcConn.Close();
                    odbcConn.Dispose();
                }
            }
            if (!string.IsNullOrEmpty(dataSourceName))
                return dataSourceName;
            else
                return serverName;
        }

        private static string GetServerFromDsn(string dsnName)
        {
            string result = string.Empty;
            string[] keys = new string[]
            {
        @"SOFTWARE\ODBC\ODBC.INI\" + dsnName,
        @"SOFTWARE\WOW6432Node\ODBC\ODBC.INI\" + dsnName
            };

            foreach (string key in keys)
            {
                using (RegistryKey regKey = Registry.LocalMachine.OpenSubKey(key))
                {
                    if (regKey != null)
                    {
                        object server = regKey.GetValue("Server");
                        if (server != null)
                        {
                            result = server.ToString();
                            break;
                        }
                    }
                }
            }
            return result;
        }

        public string GetPostgreODBCConnection(string dsnName, string odbcdbuser = "", string pwd = "")
        {
            //string odbcConnStr = "DSN=" + dsnName + ";";
            //using (var odbcConn = new OdbcConnection(odbcConnStr))
            //{
            //    odbcConn.Open(); 
            //}
            Dictionary<string, string> dsnData = ReadOdbcDsn(dsnName);
            string host = dsnData.ContainsKey("Servername") ? dsnData["Servername"] : null;
            string database = dsnData.ContainsKey("Database") ? dsnData["Database"] : null;
            string port = dsnData.ContainsKey("Port") ? dsnData["Port"] : "5432";
            string username = dsnData.ContainsKey("Username") ? dsnData["Username"] : null;
            string password = dsnData.ContainsKey("Password") ? dsnData["Password"] : null;
            
            if (string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(odbcdbuser) && string.IsNullOrEmpty(password) && !string.IsNullOrEmpty(pwd))
                password = pwd;
            else if (string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(odbcdbuser) && string.IsNullOrEmpty(password) && string.IsNullOrEmpty(pwd))
                password = "log";

            if (string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(odbcdbuser))
                username = odbcdbuser;

            string sslModeOdbc = dsnData.ContainsKey("SSLmode") ? dsnData["SSLmode"].ToLower() : "prefer";
            Npgsql.SslMode sslMode = Npgsql.SslMode.Prefer;
            if (sslModeOdbc == "disable")
                sslMode = Npgsql.SslMode.Disable;
            else if (sslModeOdbc == "require")
                sslMode = Npgsql.SslMode.Require;
            else if (sslModeOdbc == "verify-ca")
                sslMode = Npgsql.SslMode.VerifyCA;
            else if (sslModeOdbc == "verify-full")
                sslMode = Npgsql.SslMode.VerifyFull;

            var builder = new NpgsqlConnectionStringBuilder();
            builder.Host = host;
            builder.Port = Convert.ToInt32(port);
            builder.Database = database;
            builder.SslMode = sslMode;

            if (!string.IsNullOrEmpty(username))
                builder.Username = username;
            //if (!string.IsNullOrEmpty(password))
            //    builder.Password = password;
            if (!string.IsNullOrEmpty(password))
            {
                password = Uri.UnescapeDataString(password);
                builder.Password = password;
            }
            builder.TrustServerCertificate = (sslMode == Npgsql.SslMode.Require);
            return builder.ConnectionString + "♣" + database;
        }

        private Dictionary<string, string> ReadOdbcDsn(string dsnName)
        {
            string[] registryPaths = new string[]
            {
    @"SOFTWARE\ODBC\ODBC.INI\" + dsnName, // System DSN
            };

            foreach (string path in registryPaths)
            {
                RegistryKey key =
                    Registry.LocalMachine.OpenSubKey(path) ??
                    Registry.CurrentUser.OpenSubKey(path);

                if (key == null)
                    continue;

                var dict = new Dictionary<string, string>();
                foreach (string valueName in key.GetValueNames())
                {
                    dict[valueName] = Convert.ToString(key.GetValue(valueName));
                }
                return dict;
            }
            throw new Exception("ODBC DSN not found: " + dsnName);
        }

        public string GetARMUrl(string appName)
        {
            var armSettings = _appSettingsService.GetARMSettings(appName);
            return armSettings?.ARM_URL ?? throw new InvalidOperationException($"ARM settings ARM_URL for app '{appName}' not found");
        }

        public string GetARMNotificationUrl(string appName)
        {
            var armSettings = _appSettingsService.GetARMSettings(appName);
            return armSettings?.ARM_Notification_URL ?? throw new InvalidOperationException($"ARM settings ARM_Notification_URL for app '{appName}' not found");
        }

        public string GetARMNotificationExpiry(string appName)
        {
            var armSettings = _appSettingsService.GetARMSettings(appName);
            return armSettings?.ARM_Notification_ExpiryHours ?? "12";
        }

        public string GetARMNotificationMaxLimit(string appName)
        {
            var armSettings = _appSettingsService.GetARMSettings(appName);
            return armSettings?.ARM_Notification_MaxPerUser ?? "10";
        }

        public Int32 GetARMTokenExpiryMinutes(string appName)
        {
            var armSettings = _appSettingsService.GetARMSettings(appName);
            Int32 expiryMins = 0;
            if (string.IsNullOrEmpty(armSettings?.ExpiryMinutes))
                expiryMins = 60;
            else
                Int32.TryParse(armSettings.ExpiryMinutes, out expiryMins);

            if(expiryMins == 0) expiryMins = 60;

            return expiryMins;
        }

        public string GetARMPrivateKey(string appName)
        {
            var armSettings = _appSettingsService.GetARMSettings(appName);
            return armSettings?.ARM_PrivateKey ?? throw new InvalidOperationException($"ARM settings ARM_PrivateKey for app '{appName}' not found");
        }

        public string GetARMScriptsURL(string appName)
        {
            var armSettings = _appSettingsService.GetARMSettings(appName);
            return armSettings?.ARM_Scripts_URL ?? throw new InvalidOperationException($"ARM settings ARM_Scripts_URL for app '{appName}' not found");
        }

        public string GetLicDomain(string appName)
        {
            var armSettings = _appSettingsService.GetLicDomain(appName);
            return armSettings?.DomainName ?? throw new InvalidOperationException($"License Domain Name for app '{appName}' not found");
        }

        public ARMSettings GetARMSettings(string appName)
        {
            var armSettings = _appSettingsService.GetARMSettings(appName);
            if (armSettings == null)
                throw new InvalidOperationException($"ARM settings for app '{appName}' not found");

            return armSettings;
        }

        public RedisSettings GetRedisSettings(string appName)
        {
            var redisSettings = _appSettingsService.GetRedisSettings(appName);
            if (redisSettings == null)
                throw new InvalidOperationException($"Redis settings for app '{appName}' not found");

            return redisSettings;
        }
        public MobileSettings GetMobileSettings(string appName)
        {
            var mobileSettings = _appSettingsService.GetMobileSettings(appName);
            if (mobileSettings == null)
                throw new InvalidOperationException($"Mobile settings for app '{appName}' not found");

            return mobileSettings;
        }

        /// <summary>
        /// Get File configuration for a specific project
        /// </summary>
        public FileConfig GetFileConfig(string appName)
        {
            var fileConfig = _appSettingsService.GetFileConfig(appName);
            if (fileConfig == null)
                throw new InvalidOperationException($"File configuration for project '{appName}' not found");

            return fileConfig;
        }

        /// <summary>
        /// Get Email settings for a specific project
        /// </summary>
        public EmailSettings GetEmailSettings(string appName)
        {
            var emailSettings = _appSettingsService.GetEmailSettings(appName);
            if (emailSettings == null)
                throw new InvalidOperationException($"Email settings for project '{appName}' not found");

            return emailSettings;
        }

        /// <summary>
        /// Check if OTP authentication is enabled for a specific project
        /// </summary>
        public bool IsOTPAuthEnabled(string appName)
        {
            var emailSettings = _appSettingsService.GetEmailSettings(appName);
            return emailSettings?.AxOTPAuth?.ToUpper() == "TRUE";
        }

        /// <summary>
        /// Get OTP authentication settings for a specific project
        /// </summary>
        public (int chars, int expiry) GetOTPAuthSettings(string appName)
        {
            var emailSettings = _appSettingsService.GetEmailSettings(appName);
            if (emailSettings == null)
                return (4, 2); // Default values

            int.TryParse(emailSettings.AxOTPAuthCahrs, out int chars);
            int.TryParse(emailSettings.AxOTPAuthExpiry, out int expiry);

            return (chars > 0 ? chars : 4, expiry > 0 ? expiry : 2);
        }
        public InternalResources GetInternalResources(string appName)
        {
            return _appSettingsService.GetInternalResources(appName);           
        }

        public AxpDevOptsMenu GetAxpDevOption(string appName)
        {
            return _appSettingsService.GetAxDevOption(appName);
        }

        public string GetRMQueueURL()
        {
            return _appSettingsService.GetRMQueueURL();
        }

        public Dictionary<string, Dictionary<string, string>> GetSSOSettings(string appName)
        {
            var ssoSettings =  _appSettingsService.GetSSOSettings(appName);
            if (ssoSettings == null || ssoSettings.Count == 0)
            {
                ssoSettings = new Dictionary<string, Dictionary<string, string>>();
            }
            return ssoSettings;
        }
    }
}
