

using Newtonsoft.Json.Linq;
using System.Data;

namespace ARMCommon.Model
{
    public class ARMResult
    {
        public Dictionary<string, object> result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        public ARMResult()
        {
        }

        public ARMResult(bool success = false, string message = "")
        {
            result.Add("success", success);
            result.Add("message", message);
        }

    }

    public class SQLResult
    {
        public string error { get; set; }
        public DataTable data { get; set; } = new DataTable();
        //public bool success { get; set; }
        //public int count { get; set; } = 0;
        //public string Trace { get; set; }
        //public SQLResult()
        //{
        //    data = new DataTable();
        //}

        public DataView view { get; set; } = new DataView();
        public DataRowView rowView { get; set; }
        public bool success { get; set; }
        public int count { get; set; } = 0;
        public string Trace { get; set; }
        public DataRow  row { get; set; }
        public SQLResult()
        {
            data = new DataTable();
            view = new DataView();
        }
    }


    public class SQLDataSetResult
    {
        public string error { get; set; }
        public List<int> RecordsCount { get; set; } = new List<int>();
        public DataSet DataSet { get; set; }
        public SQLDataSetResult()
        {
            DataSet = new DataSet();
        }
    }
    public class APIResult
    {
        public bool success { get; set; }
        public string error { get; set; }
        public Dictionary<string, object> data = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        public APIResult()
        {
        }
        public APIResult (bool successFlag, string errorMsg)
        {
            success = successFlag;
            error = errorMsg;
        }

        public APIResult(bool successFlag, Dictionary<string, object> dataObj)
        {
            success = successFlag;
            data = dataObj;
        }
    }

    public class NonQueryResult
    {
        public string error { get; set; }
        public string message { get; set; }
        public int count { get; set; }
    }

    public class ScalarResult
    {
        public string error { get; set; }
        public object result { get; set; }        
    }

    public class TransactionResult
    {
        public bool success { get; set; }
        public string error { get; set; }
        public Exception ExceptionDetail { get; set; } 
        public int TotalStatements { get; set; }       
        public int TotalRowsAffected { get; set; }     
    }
}
