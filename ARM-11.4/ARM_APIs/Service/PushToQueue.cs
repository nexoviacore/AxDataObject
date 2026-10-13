using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMCommon.Model;
using Newtonsoft.Json;
using ARM_APIs.Model;
using Newtonsoft.Json.Linq;
using System.Data;

namespace ARM_APIs.Services
{
    public class PushTOQueue : IARMPushToQueue
    {
        private readonly IRabbitMQProducer _imessageProducer;
        private readonly IConfiguration _config;
        private readonly Utils _common;

        public PushTOQueue(IConfiguration configuration, IRabbitMQProducer iMessageProducer, Utils common)
        {
            _config = configuration;
            _imessageProducer = iMessageProducer;
            _common = common;
        }

        public bool PushToQueue(ARMQueueData queueData)
        {
            if (queueData.trace == null)
            {
                queueData.trace = false;
            }

            if (queueData.timespandelay == null)
            {
                queueData.timespandelay = 0;
            }

            if (string.IsNullOrEmpty(queueData.signalrclient))
            {
                queueData.signalrclient = "";
            }

            if (queueData.MsgExpirySecs == null)
            {
                queueData.MsgExpirySecs = 0;
            }

            string serializedData = queueData.queuejson is null
                ? JsonConvert.SerializeObject(queueData)
                : System.Text.Json.JsonSerializer.Serialize(queueData);

            return _imessageProducer.SendMessages(serializedData, queueData.queuename, Convert.ToBoolean(queueData.trace), Convert.ToInt32(queueData.timespandelay), Convert.ToInt32(queueData.MsgExpirySecs));
        }
       public async Task<SQLResult> GetInboundQueue(string appName, string queueName)
        {
            SQLResult sqlresult = new SQLResult();
            Dictionary<string, string> config = await _common.GetDBConfigurations(appName);
            if (config == null || config?.Count == 0)
            {
                sqlresult.error = "Invalid project details.";
                return sqlresult;
            }

            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];
            string sql = Constants_SQL.GET_INBOUNDQUEUE.ToString();
            string[] paramName = { "@queuename" };
            DbType[] paramType = { DbType.String };
            object[] paramValue = { queueName };
            IDbHelper dbHelper = DBHelper.CreateDbHelper(sql, dbType, connectionString, new string[] { }, new DbType[] { }, new object[] { });
            sqlresult.data = await dbHelper.ExecuteQueryAsync(sql, connectionString, paramName, paramType, paramValue);
            return sqlresult;
        }

        public async Task<ARMInboundQueue> GetQueueObject(SQLResult apiDetails)
        {
            return DataRowToARMInboundQueueObject(apiDetails.data.Rows[0]);
        }

        private ARMInboundQueue DataRowToARMInboundQueueObject(DataRow row)
        {
            ARMInboundQueue obj = new ARMInboundQueue();
            try
            {
                obj.SecretKey = row["secretkey"]?.ToString();
                obj.UserName = row["uname"]?.ToString();
            }
            catch (Exception ex)
            {
                return null;
            }

            return obj;
        }


        public async Task<string> SendToQueue(ARMInboundQueue queueData, ARMInboundQueue queueObj)
        {
            string apiCallTime = _common.DecryptSecret(queueData.SecretKey, queueObj.SecretKey);
            if (string.IsNullOrEmpty(apiCallTime))
            {
                return "API authentication failed due to invalid secret.";
            }
            if (!_common.IsValidAPITime(apiCallTime))
            {
                return "API authentication failed due to timeout.";
            }

            if (queueData.Trace == null)
            {
                queueData.Trace = false;
            }

            if (queueData.Delay == null)
            {
                queueData.Delay = 0;
            }

            if (string.IsNullOrEmpty(queueData.SignalRClient))
            {
                queueData.SignalRClient = "";
            }
            if (string.IsNullOrEmpty(queueData.UserName))
            {
                queueData.UserName = queueObj.UserName;
            }

            string AxpertWebScriptsURL = _config[$"ConnectionStrings:{queueData.Project}_scriptsurl"];
            if (string.IsNullOrEmpty(AxpertWebScriptsURL))
                AxpertWebScriptsURL = await _common.AxpertWebScriptsURL(queueData.Project);
            string apiUrl = AxpertWebScriptsURL + "/ASBRapidSave.dll/datasnap/rest/TASBRapidSave/submitdata";

            queueData.URL = apiUrl;
            queueData.Method = "POST";

            AxpertRestAPIToken axpertRestAPIToken = new AxpertRestAPIToken(queueData.UserName);
            queueData.Seed = axpertRestAPIToken.seed;
            queueData.Token = axpertRestAPIToken.token;
            queueData.UserAuthKey = axpertRestAPIToken.userAuthKey;

            string submitDataJson = System.Text.Json.JsonSerializer.Serialize(queueData.SubmitData);
            JObject submitDataObj = JObject.Parse(submitDataJson);

            queueData.QueueData = JsonConvert.SerializeObject(JsonConvert.SerializeObject(new { submitdata = submitDataObj }));
            queueData.SubmitData = new Dictionary<string, object>();

            bool msgSent = _imessageProducer.SendMessages(JsonConvert.SerializeObject(queueData), queueData.QueueName, Convert.ToBoolean(queueData.Trace), Convert.ToInt32(queueData.Delay));
            return msgSent.ToString();
        }
    }
}
