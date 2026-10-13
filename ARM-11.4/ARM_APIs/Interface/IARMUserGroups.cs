using ARMCommon.Model;
using Microsoft.AspNetCore.Mvc;

namespace ARM_APIs.Interface
{
    public interface IARMUserGroups
    {
        abstract Task<object> GetARMUserGroups(string appname);
    }
}
