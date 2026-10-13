using ARMCommon.Model;

namespace AxTstructData
{
    public interface IAxUserPermissionService
    {
        Task<APIResult> GetUserPermission(AxUserPermission tstructData);
    }
}
