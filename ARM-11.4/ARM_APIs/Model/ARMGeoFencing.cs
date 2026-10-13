using ARM_APIs.Interface;
using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMCommon.Model;
using Newtonsoft.Json;
using System.Data;


namespace ARM_APIs.Model
{
    public class ARMGeoFencing : IARMGeoFencing
    {
        private readonly IConfiguration _config;
        private readonly DataContext _context;
        private readonly IRedisHelper _redis;
        private readonly IPostgresHelper _postGres;
        private readonly Utils _common;
        private readonly IRabbitMQProducer _iMessageProducer;
        public ARMGeoFencing(DataContext context, IConfiguration configuration, ITokenService tokenService, IRedisHelper redis, IPostgresHelper postGres, IAPI api, Utils common, IRabbitMQProducer iMessageProducer)
        {
            _context = context;
            _config = configuration;
            _redis = redis;
            _postGres = postGres;
            _common = common;
            _iMessageProducer = iMessageProducer;
        }
        public async Task<string> SaveGeoFencingData(ARMGeoFencingModel geofencing)
        {
            // string connectionString = await _common.GetDBConfigurationBySessionId(geofencing.ARMSessionid);
            var loginuser = await GetLoginUser(geofencing.ARMSessionid);
            Dictionary<string, string> config = await _common.GetDBConfigurations(loginuser["APPNAME"]);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];
            var dictSession = await _redis.HashGetAllDictAsync(geofencing.ARMSessionid);
            string sql = "";
            if (dbType.ToLower() == "oracle")
            {
                sql = $"INSERT INTO axgeofencing ( USERNAME , IDENTIFIER , CURRENT_NAME , CURRENT_LAT , CURRENT_LONG , SRC_NAME , SRC_LAT , SRC_LONG , DISTANCE , IS_WITHINRADIUS)VALUES('{dictSession["USERNAME"]}', '{geofencing.identifier}', '{geofencing.current_name}', '{geofencing.current_lat}', '{geofencing.current_long}', '{geofencing.src_name}', '{geofencing.src_lat}', '{geofencing.src_long}', {geofencing.distance}, '{geofencing.is_withinradius}')";
            }
            else
            {
                sql = $"INSERT INTO axgeofencing (username, identifier, current_name, current_lat, current_long, src_name, src_lat, src_long, distance, is_withinradius)VALUES('{dictSession["USERNAME"]}', '{geofencing.identifier}', '{geofencing.current_name}', '{geofencing.current_lat}', '{geofencing.current_long}', '{geofencing.src_name}', '{geofencing.src_lat}', '{geofencing.src_long}', {geofencing.distance}, '{geofencing.is_withinradius}')";

            }
            try
            {
                IDbHelper dbHelper = DBHelper.CreateDbHelper(sql, dbType, connectionString, new string[] { }, new DbType[] { }, new object[] { });
                await dbHelper.ExecuteQueryAsync(sql, connectionString, new string[] { }, new DbType[] { }, new object[] { });
                return Constants.RESULTS.INSERTED.ToString();
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public async Task<string> UpdateGeoLocation(ARMUpdateLocationModel location)
        {
            Dictionary<string, string> config = await _common.GetDBConfigurations(location.project);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];
            string sql = Constants_SQL.INSERT_USERGEOLOCATIONDATA; 
            string[] paramName = { "@username", "@current_name", "@current_loc", "@expectedlocations", "@location_array" , "@identifier" };
            DbType[] paramType = { DbType.String, DbType.String, DbType.String, DbType.String ,DbType.String,DbType.String};
            object[] paramValue = { location.username, location.current_name, location.current_loc , location.expectedlocations,location.location_array,location.identifier };

            try
            {

                IDbHelper dbHelper = DBHelper.CreateDbHelper(sql, dbType, connectionString, paramName, paramType, paramValue);
                await dbHelper.ExecuteQueryAsync(sql, connectionString, paramName, paramType, paramValue);
                var queueData = new
                {
                    queuedata = $"{location.project}~{location.username}~{location.logintime}"
                };

                _iMessageProducer.SendMessages(JsonConvert.SerializeObject(queueData), location.queuename, false, Convert.ToInt32(location.interval * 1000));
                return Constants.RESULTS.INSERTED.ToString();
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        private async Task<Dictionary<string, string>> GetLoginUser(string ARMSessionId)
        {
            var dictSession = await _redis.HashGetAllDictAsync(ARMSessionId);
            return dictSession;
        }

        public async Task<DataTable> GetGeoFencingData(string ARMSessionid)
        {
            var dictSession = await _redis.HashGetAllDictAsync(ARMSessionid);
            Dictionary<string, string> config = await _common.GetDBConfigurations(dictSession["APPNAME"]);
            string connectionString = config["ConnectionString"];
            string dbType = config["DBType"];
            string sql = "";
            sql = Constants_SQL.GETGEOFENCINGDATA.ToString();
            string[] paramName = { "@username" };
            DbType[] paramType = { DbType.String };
            object[] paramValue = { dictSession["USERNAME"] };
            IDbHelper dbHelper = DBHelper.CreateDbHelper(sql, dbType, connectionString, paramName, paramType, paramValue);
            var table =  await dbHelper.ExecuteQueryAsync(sql, connectionString, paramName, paramType, paramValue);
            if (dbType.ToLower() == "oracle")
            {

                foreach (DataColumn column in table.Columns)
                {

                    column.ColumnName = column.ColumnName.ToLower();

                }

            }
            return table;
        }
    }
}
