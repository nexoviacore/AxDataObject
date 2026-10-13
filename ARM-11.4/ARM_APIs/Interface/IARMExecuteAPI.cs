using ARMCommon.Model;
using System.Data;

namespace ARM_APIs.Interface
{
    public interface IARMExecuteAPI
    {
        abstract Task<SQLResult> GetPublishedAPI(string appName, string publicKey);
        abstract Task<APIResult> ExecuteAPI(ARMPublishedAPI publishedAPI, ARMAPIDetails apiDetails, bool validateSecret = true);
        abstract Task<ARMAPIDetails> GetAPIObject(SQLResult apiDetails);

    }
}
