using AgileConnect.EncrDecr.cs;
using AgileConnect.Filter;
using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMCommon.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NPOI.SS.Formula.Functions;
using Oracle.ManagedDataAccess.Client;
using StackExchange.Redis;
using System;
using System.Data;
using System.Data.Odbc;
using System.Data.SqlClient;

namespace AgileConnect.Controllers
{
    [ServiceFilter(typeof(SessionActionFilter))]
    [Authorize]
    public class ARMAppsController : Controller
    {
        private readonly IConfiguration _config;
        private readonly DataContext _context;
        private readonly IRedisHelper _redis;
        private readonly Utils _common;
        private string privateKey = EncDec.RefreshKey(16);
        private readonly IRedisConnection _redisConn;

        public ARMAppsController(IConfiguration config, IRedisHelper redis, DataContext context, Utils common, IRedisConnection redisConn)
        {
            _config = config;
            _redis = redis;
            _context = context;
            _common = common;
            _redisConn = redisConn;
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(ARMApp model)
        {
            if (ModelState.IsValid)
            {
                model.modifiedon = DateTime.Now;
                string rediskey = $"{Constants.REDIS_PREFIX.ARMRedisConfiguration.ToString()}_{model.AppName}";
                string dbkey = $"{Constants.DB_PREFIX.ARMConnectionString.ToString()}_{model.AppName}";

                // Clear existing Redis cache keys and values
                await ClearAllRedisCacheAsync();

                model.PrivateKey = privateKey;
                model.ConnectionName = EncDec.Encrypt(model.ConnectionName, privateKey);
                model.DataBase = EncDec.Encrypt(model.DataBase, privateKey);
                model.DBVersion = EncDec.Encrypt(model.DBVersion, privateKey);
                model.UserName = EncDec.Encrypt(model.UserName, privateKey);
                model.Password = EncDec.Encrypt(model.Password, privateKey);
                model.RedisIP = EncDec.Encrypt(model.RedisIP, privateKey);
                model.RedisPassword = EncDec.Encrypt(model.RedisPassword, privateKey);
                if (model.AppLogoBase64 == null || model.AppLogoBase64.Length == 0)
                {
                    return BadRequest("File not found");
                }
                using (var memoryStream = new MemoryStream())
                {
                    await model.AppLogoBase64.CopyToAsync(memoryStream);
                    var fileBytes = memoryStream.ToArray();
                    model.AppLogo = Convert.ToBase64String(fileBytes);
                }

                _context.Add(model);
                await _context.SaveChangesAsync();
                await _redis.StringSetAsync(rediskey, _common.GetRedisConnections(model.RedisIP, model.RedisPassword));
                await _redis.StringSetAsync(dbkey, _common.GetDBConnections(model.DataBase, model.UserName, model.Password, model.DBVersion,model.Sqlversion, model.ConnectionName));
                return RedirectToAction("List");
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string appName)
        {
            var userGroup = GetUserGroupById(appName);
            var Key = userGroup.PrivateKey;
            if (Key != null)
            {
                userGroup.ConnectionName = EncDec.Decrypt(userGroup.ConnectionName, Key);
                userGroup.DataBase = EncDec.Decrypt(userGroup.DataBase, Key);
                userGroup.DBVersion = EncDec.Decrypt(userGroup.DBVersion, Key);
                userGroup.UserName = EncDec.Decrypt(userGroup.UserName, Key);
                userGroup.Password = EncDec.Decrypt(userGroup.Password, Key);
                userGroup.RedisIP = EncDec.Decrypt(userGroup.RedisIP, Key);
                userGroup.RedisPassword = EncDec.Decrypt(userGroup.RedisPassword, Key);

                var con = _common.GetRedisConnections(userGroup.RedisIP, userGroup.RedisPassword);
                string dbConnectionString = await _common.GetDBConfiguration(appName);
                var redisConfig = await _redisConn.GetRedisConfiguration(appName);
            }
            return View(userGroup);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(string appName, ARMApp model)
        {
            if (appName != model.AppName)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    model.modifiedon = DateTime.Now;
                    string rediskey = $"{Constants.REDIS_PREFIX.ARMRedisConfiguration.ToString()}_{appName}";
                    string dbkey = $"{Constants.DB_PREFIX.ARMConnectionString.ToString()}_{appName}";


                    var result = await ClearAllRedisCacheAsync();

                    model.PrivateKey = privateKey;
                    model.ConnectionName = EncDec.Encrypt(model.ConnectionName, privateKey);
                    model.DataBase = EncDec.Encrypt(model.DataBase, privateKey);
                    model.DBVersion = EncDec.Encrypt(model.DBVersion, privateKey);
                    model.UserName = EncDec.Encrypt(model.UserName, privateKey);
                    model.Password = EncDec.Encrypt(model.Password, privateKey);
                    model.RedisIP = EncDec.Encrypt(model.RedisIP, privateKey);
                    model.RedisPassword = EncDec.Encrypt(model.RedisPassword, privateKey);
                    if (model.AppLogoBase64 == null || model.AppLogoBase64.Length == 0)
                    {
                        return BadRequest("Please upload logo");
                    }
                    using (var memoryStream = new MemoryStream())
                    {
                        await model.AppLogoBase64.CopyToAsync(memoryStream);
                        var fileBytes = memoryStream.ToArray();
                        model.AppLogo = Convert.ToBase64String(fileBytes);
                    }

                    _context.Update(model);
                    await _context.SaveChangesAsync();
                    await _redis.StringSetAsync(rediskey, _common.GetRedisConnections(model.RedisIP, model.RedisPassword));
                    await _redis.StringSetAsync(dbkey, _common.GetDBConnections(model.DataBase, model.UserName, model.Password, model.DBVersion,model.Sqlversion, model.ConnectionName));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AppExists(model.AppName))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction("List");
            }
            return View(model);
        }

        public IActionResult List()
        {
            var appLists = AppList();
            return View(appLists);
        }

        private ARMApp GetUserGroupById(string appName)
        {
            return GetUserGroupBy(appName);
        }

        private ARMApp GetUserGroupBy(string appName)
        {
            var app = _context.ARMApps.Find(appName);
            if (app == null)
                throw new KeyNotFoundException("App not found");
            return app;
        }

        private bool AppExists(string appName)
        {
            return _context.ARMApps.Any(e => e.AppName == appName);
        }

        private IEnumerable<ARMApp> AppList()
        {
            var appList = _context.ARMApps.ToList();
            return appList;
        }

        public string ConvertToBase64(IFormFile file)
        {
            using (var memoryStream = new MemoryStream())
            {
                file.CopyTo(memoryStream);
                var fileBytes = memoryStream.ToArray();
                return Convert.ToBase64String(fileBytes);
            }
        }

        [HttpPost]
        public async Task<IActionResult> TestConnection(string dbType, string connectionString)
        {
            try
            {
                IDbConnection connection;

                if (dbType.Equals("postgre", StringComparison.OrdinalIgnoreCase))
                {
                    connection = new NpgsqlConnection(connectionString);
                }
                else if (dbType.Equals("oracle", StringComparison.OrdinalIgnoreCase))
                {
                    connection = new OracleConnection(connectionString);
                }
                else if (dbType.Equals("mssql", StringComparison.OrdinalIgnoreCase))
                {
                    connection = new SqlConnection(connectionString);
                }
                else
                {
                    return Json(new { success = false, message = "Invalid database type specified." });
                }

                connection.Open();
                connection.Close();

                return Json(new { success = true, message = "Connection Successful." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


        [HttpPost]
        public async Task<IActionResult> GetServerIpAndPort(string dbVersion, string uid, string pwd, string database)
        {
            string connStr = $"DSN={dbVersion};Uid={uid};Pwd={pwd};";
            string connectionString = "";
            string dataSourceName = string.Empty;

            try
            {
                using (OdbcConnection connection = new OdbcConnection(connStr))
                {
                    await connection.OpenAsync();

                    using (OdbcCommand cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = @"
                SELECT local_net_address, local_tcp_port 
                FROM sys.dm_exec_connections 
                WHERE session_id = @@SPID";

                        using (OdbcDataReader reader = (OdbcDataReader)await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection))
                        {
                            if (await reader.ReadAsync())
                            {
                                string ip = reader["local_net_address"]?.ToString();
                                string port = reader["local_tcp_port"]?.ToString();

                                if (!string.IsNullOrEmpty(ip) && !string.IsNullOrEmpty(port))
                                {
                                    dataSourceName = $"{ip}\\{dbVersion},{port}";
                                }
                                else
                                {
                                    return Json(new { success = false, message = "Could not retrieve IP and Port" });
                                }
                            }
                        }
                    }
                }

                connectionString = $"Server={dataSourceName};Database={database};User Id={uid};Password={pwd};";

                return Json(new { success = true, connectionString });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Failed to retrieve server details", error = ex.Message });
            }
        }






        //[HttpPost]
        //public ActionResult<string> GetServerIpAndPort(string deVersion, string uid, string pwd, string database)
        //{
        //    string connStr = $"DSN={deVersion};Uid={uid};Pwd={pwd};";
        //    string connectionString = "";
        //    string dataSourceName = string.Empty;

        //    using (OdbcConnection odbcConn = new OdbcConnection(connStr))
        //    {
        //        try
        //        {
        //            odbcConn.Open();
        //            using (OdbcCommand cmd = odbcConn.CreateCommand())
        //            {
        //                cmd.CommandText = @"
        //      SELECT local_net_address, local_tcp_port 
        //      FROM sys.dm_exec_connections 
        //      WHERE session_id = @@SPID";

        //                using (OdbcDataReader reader = cmd.ExecuteReader())
        //                {
        //                    if (reader.Read())
        //                    {
        //                        string ip = reader["local_net_address"].ToString();
        //                        string port = reader["local_tcp_port"].ToString();
        //                        dataSourceName = $"{ip}\\{deVersion},{port}";
        //                    }
        //                }
        //            }
        //            connectionString = $"Server={dataSourceName};Database={database};User Id={uid};Password={pwd};";
        //        }
        //        catch (Exception ex)
        //        {
        //            return BadRequest($"Error: {ex.Message}");
        //        }
        //        finally
        //        {
        //            odbcConn.Close();
        //        }
        //    }

        //    return !string.IsNullOrEmpty(connectionString) ? Ok(connectionString) : BadRequest("Failed to get connection string.");
        //}

        [HttpPost]
        public async Task<IActionResult> TestRedisConnection(string redisIP, string redisPassword)
        {
            try
            {
                var options = new ConfigurationOptions
                {
                    EndPoints = { redisIP },
                    Password = redisPassword
                };

                var connection = ConnectionMultiplexer.Connect(options);
                var database = connection.GetDatabase();

                database.StringSet("testkey", "testvalue");

                if (database.StringGet("testkey") == "testvalue")
                {
                    return Json(new { success = true, message = "Connection successful." });
                }
                else
                {
                    return Json(new { success = false, message = "Connection failed." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private async Task<bool> ClearAllRedisCacheAsync()
        {
            var result = await _redis.ExecuteAsync("FLUSHDB");
            return result.ToString() == "OK";
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string appName)
        {
            try
            {
                var app = await _context.ARMApps.FindAsync(appName);

                if (app == null)
                {
                    return NotFound();
                }

                _context.ARMApps.Remove(app);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Record deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Failed to delete record: {ex.Message}" });
            }
        }



    }
}
