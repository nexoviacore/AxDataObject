using ARMCommon.Helpers;
using System.Data;

namespace ARMCommon.Model
{
    public class SQLCommandBuilder
    {
        private string _query;
        private string[] _paramName;
        private DbType[] _paramType;
        private object[] _paramValue;
        private Constants.DATA_ACTION _action;

        public string Query
        {
            get { return _query; }
            set { _query = value; }
        }

        public string[] ParameterNames
        {
            get { return _paramName; }
            set { _paramName = value; }
        }

        public DbType[] ParameterTypes
        {
            get { return _paramType; }
            set { _paramType = value; }
        }

        public object[] ParameterValues
        {
            get { return _paramValue; }
            set { _paramValue = value; }
        }

        public Constants.DATA_ACTION Action
        {
            get { return _action; }
            set { _action = value; }
        }

        public SQLCommandBuilder(string query, string[] paramNames, DbType[] paramTypes, object[] paramValues, Constants.DATA_ACTION action)
        {
            _query = query;
            _paramName = paramNames;
            _paramType = paramTypes;
            _paramValue = paramValues;
            _action = action;
        }
    }
}
