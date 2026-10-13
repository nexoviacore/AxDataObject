using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMCommon.Model;
using System.Data;

using static ARMCommon.Helpers.Constants;

namespace ARMNotificationHub
{
    public class ARMNotificationService : IARMNotificationService
        {
            private readonly IRedisHelper _redis;
            private readonly Utils _common;
            public ARMNotificationService(IRedisHelper redis, Utils common)
            {
                _redis = redis;
                _common = common;
            }


        public async Task<object> ProcessARMMobileNotification(MobileNotification notification)
        {
            ARMResult result = new ARMResult();
            SQLResult axnotification = new SQLResult();
            SQLResult axmobilenotify = new SQLResult();
            string SqlQuery = string.Empty;
            string userName = string.Empty;
            string firebaseid = string.Empty;
            var loginuser = await _redis.HashGetAllDictAsync(notification.ARMSessionId);
            if (loginuser == null)
            {
                return RESULTS.INVALIDPASSWORD;
            }

            firebaseid = notification.firebaseId;
            userName = loginuser["USERNAME"];
            string appName = loginuser["APPNAME"];

            Dictionary<string, string> config = await _common.GetDBConfigurations(appName);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];

            string[] paramName = { "@username", "@firebaseid", "@appname" };
            DbType[] paramType = { DbType.String, DbType.String, DbType.String };
            object[] paramValue = { userName, firebaseid, appName.ToLower() };

            IDbHelper dbHelper = DBHelper.CreateDbHelper(SqlQuery, dbType, connectionString, paramName, paramType, paramValue);
            SqlQuery = Constants_SQL.SELECT_AXMOBILENOTIFY.ToString();
            axnotification.data = await dbHelper.ExecuteQueryAsync(SqlQuery, connectionString, paramName, paramType, paramValue);
            var Id = Guid.NewGuid();
            notification.guid = Id;

            if (axnotification.data.Rows.Count > 0)
            {
                SqlQuery = Constants_SQL.DELETE_AXMOBILENOTIFY.ToString();
                await dbHelper.ExecuteQueryAsync(SqlQuery, connectionString, paramName, paramType, paramValue);

                SqlQuery = Constants_SQL.INSERT_AXMOBILENOTIFY.ToString()
                    .Replace("$userName$", userName)
                    .Replace("$appName$", appName)
                    .Replace("$notificationguid$", notification.guid.ToString())
                    .Replace("$notificationfirebaseId$", notification.firebaseId)
                    .Replace("$notificationImeiNo$", notification.ImeiNo)
                    .Replace("$notificationstatus$", notification.status);

                await dbHelper.ExecuteQueryAsync(SqlQuery, connectionString, new string[] { }, new DbType[] { }, new object[] { });
                return RESULTS.RECORDUPDATED;
            }
            else
            {
                SqlQuery = Constants_SQL.INSERT_AXMOBILENOTIFY.ToString()
                    .Replace("$userName$", userName)
                    .Replace("$appName$", appName)
                    .Replace("$notificationguid$", notification.guid.ToString())
                    .Replace("$notificationfirebaseId$", notification.firebaseId)
                    .Replace("$notificationImeiNo$", notification.ImeiNo)
                    .Replace("$notificationstatus$", notification.status);

                await dbHelper.ExecuteQueryAsync(SqlQuery, connectionString, new string[] { }, new DbType[] { }, new object[] { });
                return RESULTS.RECORDINSERTED;
            }
        }


        public async Task<bool> DisableMobileNotificationForUserAsync(string ARMSessionId)
        {
            string SqlQuery = string.Empty;
            string userName = string.Empty;
            var loginuser = await _redis.HashGetAllDictAsync(ARMSessionId);
            if (loginuser == null)
            {
                return false;
            }
            userName = loginuser["USERNAME"];
            string appName = loginuser["APPNAME"];

            Dictionary<string, string> config = await _common.GetDBConfigurations(appName);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];
            SqlQuery = Constants_SQL.DELETE_AXMOBILENOTIFYFORUSER.ToString();
            string[] paramName = { "@username", "@appname" };
            DbType[] paramType = { DbType.String, DbType.String };
            object[] paramValue = { userName, appName.ToLower() };
            IDbHelper dbHelper = DBHelper.CreateDbHelper(SqlQuery, dbType, connectionString, paramName, paramType, paramValue);
            await dbHelper.ExecuteQueryAsync(SqlQuery, connectionString, paramName, paramType, paramValue);

            return true;
        }


        //public async Task<IActionResult> SendEmailNotification(ARMNotify model)
        //{
           
        //    string strJSON = String.Empty;
        //    var appSettingsPath = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "appsettings.json");
        //    var json = System.IO.File.ReadAllText(appSettingsPath);
        //    var jsonSettings = new JsonSerializerSettings();
        //    jsonSettings.Converters.Add(new ExpandoObjectConverter());
        //    jsonSettings.Converters.Add(new StringEnumConverter());
        //    dynamic config = JsonConvert.DeserializeObject<ExpandoObject>(json, jsonSettings);
        //    config.ParallelTasksCount = 25;
        //    var newJson = JsonConvert.SerializeObject(config, Formatting.Indented, jsonSettings);
        //    System.IO.File.WriteAllText(appSettingsPath, newJson);
        //    var TemplateId = model.TemplateId;
        //    var Notifydata = model.NotificationData?.ElementAt(0).Value;
        //    var resultdata = _context.NotificationTemplate.Where(p => p.TemplateId == model.TemplateId).ToList();
        //    var Template = resultdata.Select(p => p.TemplateString).FirstOrDefault();
        //    string TemplateString = "";

        //    if (string.IsNullOrEmpty(model.TemplateId))
        //    {
        //        TemplateString = model.TemplateString;
        //    }
        //    else
        //    {
        //        TemplateString = $"Hi, {Template} ";
        //    }
        //    var replaceString = TemplateString.Replace("{{otp}}", Notifydata);
        //    var message = new Message(model.EmailDetails.To, model.EmailDetails.Bcc, model.EmailDetails.cc, model.EmailDetails.Subject, replaceString);
        //    await _emailSender.SendEmailAsync(message);
        //    return new OkObjectResult("SUCCESS");
        //}
    }
}

    

