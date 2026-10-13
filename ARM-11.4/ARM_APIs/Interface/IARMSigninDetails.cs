using ARMCommon.Model;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace ARM_APIs.Interface
{
    public interface IARMSigninDetails
    {
      abstract Task<object> GetAppConfiguration(string appName);

      abstract Task<object> GetAppModificationTime(string appName);
    }
}
