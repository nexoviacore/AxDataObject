
using ARM_APIs.Interface;
//using ARM_APIs.Model;
using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMCommon.Model;
using System.Data;
using System.Data.Common;
using System.Xml.Linq;

namespace ARM_APIs.Repositories
{
	public class ValidateDataRepository : IValidateDataRepository
	{
		//private DbConnection _connection = null;
		public string _connectionString = string.Empty;
		public string _dbType = string.Empty;
		private IDbHelper _dbHelper = null;
		//private readonly IRedisHelper _redis;
		private readonly Utils _common;
		private readonly IARMLogTraceService _traceService;
		//private bool isTracing;
		//private string _axAppsXML = "";
		//public string _schemaName = "";
		//private string _dbUserName = "";
		//private string _dbName = "";
		//private string _axPropsXML = "";
		//private Dictionary<string, string> _dbVars = new Dictionary<string, string>();

		public ValidateDataRepository(Utils common, IARMLogTraceService traceService)
		{
			//_redis = redis;
			_common = common;
			_traceService = traceService;
		}


        public async Task<IDbHelper> GetDbHelper()
		{
			try
			{
                if (_common._dbHelper == null)
                    await _common.OpenDBConnectionAsync();
                _dbHelper = _common._dbHelper;
                _dbType = _common._dbHelper?.GetDBType();
                _connectionString = _common._dbHelper?.ConnectionString;

                return _dbHelper;
			}
			catch(Exception ex)
			{
				_traceService.LogMessage("Error in ValidateData repo", ex?.Message);
				return null;
            }
            
		}

        //public IDbHelper GetDbType()
        //{
        //    return _dbType;
        //}

  //      public Dictionary<string, string> GetVars()
		//{
		//	return _dbVars;
		//}

		//public string GetDBSchemaName()
		//{
		//	return _schemaName;
		//}

		//public async Task<string> OpenDBConnectionAsync(string appName)
		//{
		//	try
		//	{
		//		Dictionary<string, string> config = await _common.GetDBConfigurations(appName);
		//		string connectionString = config["ConnectionString"];
		//		_dbType = config["DBType"];

		//		_dbHelper = DBHelper.CreateDbHelper(_dbType, null);

		//		DateTime startTime = DateTime.Now;
		//		_connection = DBHelper.OpenDBConnection(connectionString, _dbType);
		//		_connectionString = connectionString;
		//		_dbHelper.SetDBConnectionTime(startTime, DateTime.Now);
		//	}
		//	catch (Exception ex)
		//	{
		//		return ex.Message;
		//	}

		//	if (_connection?.State == ConnectionState.Open)
		//		return "Connected to DB";
		//	else
		//		return "Not able to Connect to DB";
		//}

		//public async Task<string> ConnectToDB(string appName)
		//{
		//	try
		//	{
		//		//TODO Keys format.
		//		//_axAppsXML = await _common.GetAxApps(appName);
		//		//_axPropsXML = await _common.GetAxProps(appName);

		//		//if (_traceService.IsTracingEnabled())
		//		//{
		//		//	_traceService.LogMessage($"Reading _axAppsXML: {_axAppsXML}");
		//		//	_traceService.LogMessage($"Reading AxPropsXML: {_axPropsXML}");
		//		//}

		//		DateTime startTime = DateTime.Now;

  //              var connectionStatus = await OpenDBConnectionAsync(appName);

		//		if (connectionStatus == "Connected to DB")
		//		{

		//			_dbVars = DBHelper.GetDBVars(_dbType);

		//			//_schemaName = await _common.GetSchemaName(appName, _axAppsXML);
		//			//if (_schemaName.Split('~').Length == 2)
		//			//{
		//			//	_dbUserName = _schemaName.Split('~')[0];
		//			//	_dbName = _schemaName.Split('~')[1];
		//			//}
		//			//else
		//			//{
		//			//	_dbUserName = _schemaName;
		//			//	_dbName = _schemaName;
		//			//}

		//			if (_dbHelper.GetDBType() == Constants.DBTYPE.POSTGRE.ToString())
		//			{
		//				string searchPath = "";

		//				if (!string.IsNullOrEmpty(_dbUserName))
		//				{
		//					if (!_dbVars.ContainsKey("postgre_search_path"))
		//					{
		//						string postgreSearchPath = "";
		//						try
		//						{
		//							XDocument doc = XDocument.Parse(_axPropsXML);
		//							XElement postgreSearchPathElement = doc.Root.Element(_dbUserName);

		//							if (postgreSearchPathElement != null)
		//							{
		//								postgreSearchPath = postgreSearchPathElement.Value;
		//							}
		//						}
		//						catch (Exception ex)
		//						{
		//							//Do nothing
		//						}

		//						_dbVars.Add("postgre_search_path", postgreSearchPath);
		//					}
		//				}

		//				if (_dbVars.ContainsKey("postgre_search_path") && !string.IsNullOrEmpty(_dbVars["postgre_search_path"]))
		//				{
		//					if (!string.IsNullOrEmpty(_dbUserName))
		//						searchPath = $"{_dbUserName},{_dbVars["postgre_search_path"]},public";
		//					else if (!string.IsNullOrEmpty(_dbName))
		//						searchPath = $"{_dbUserName},{_dbName},{_dbVars["postgre_search_path"]},public";
		//					else
		//						searchPath = $"{_dbUserName},{_dbVars["postgre_search_path"]},public";
		//				}
		//				else
		//					searchPath = $"{_schemaName},public";

		//				string[] paramName = { "@searchpath" };
		//				DbType[] paramType = { DbType.String };
		//				object[] paramValue = { searchPath };

		//				await _dbHelper.ExecuteSQLAsync(Constants_SQL.SET_SEARCH_PATH, _connection, paramName, paramType, paramValue);
		//			}

		//			if (!_dbVars.ContainsKey("date"))
		//			{
		//				_dbVars.Add("date", await GetCurrentDateTime());
		//			}
		//			return connectionStatus;
		//		}
		//		else
		//		{
		//			return $"Error: {connectionStatus}";
		//		}

		//	}
		//	catch (Exception ex)
		//	{
		//		return $"Error: {ex.Message}";
		//	}
		//}

		//public Task CloseDBConnectionAsync()
		//{
		//	if (_connection?.State == ConnectionState.Open)
		//		return _connection.CloseAsync();
		//	return Task.CompletedTask;
		//}

		//public async Task<string> GetAxPropsXMLFromDB()
		//{
		//	string key = $"ARM-{_schemaName}-AxPropsXML-FromDB";
		//	var result = await _redis.StringGetAsync(key);
		//	if (string.IsNullOrEmpty(result))
		//	{
		//		SQLResult axPropsResult = await FetchAxPropsXMLFromDB();
		//		if (axPropsResult != null && string.IsNullOrEmpty(axPropsResult.error) && axPropsResult.data != null && axPropsResult.data.Rows.Count > 0)
		//		{
		//			foreach (DataRow dr in axPropsResult.data.Rows)
		//			{
		//				result += dr[0].ToString();
		//			}
		//		}

		//		if (!string.IsNullOrEmpty(result))
		//			await _redis.StringSetAsync(key, result);
		//	}

		//	return result;
		//}

		//public async Task<string> GetCurrentDateTime()
		//{
		//	if (_dbVars.ContainsKey("datesql"))
		//	{
		//		string sql = _dbVars["datesql"];
		//		var dateResult = await _dbHelper.ExecuteSQLAsync(sql, _connection);
		//		if (dateResult != null && string.IsNullOrEmpty(dateResult.error) && dateResult.data != null && dateResult.data.Rows.Count > 0)
		//		{
		//			return dateResult.data.Rows[0][0].ToString();
		//		}

		//		if (dateResult != null && !string.IsNullOrEmpty(dateResult.error))
		//			return dateResult.error;
		//	}

		//	return "";
		//}

	}

}
