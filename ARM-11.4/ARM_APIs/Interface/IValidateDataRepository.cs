using ARMCommon.Interface;
//using ARMCommon.Model;
using System.Configuration;
using System.Data;

namespace ARM_APIs.Interface
{
    public interface IValidateDataRepository
    {
        public Task<IDbHelper> GetDbHelper();

        //Dictionary<string, string> GetVars();
        //string GetDBSchemaName();
        //Task<string> OpenDBConnectionAsync(string schemaName);
        //Task<string> ConnectToDB(string appName);
        //Task CloseDBConnectionAsync();
        //Task<string> GetAxPropsXMLFromDB();

    }

}
