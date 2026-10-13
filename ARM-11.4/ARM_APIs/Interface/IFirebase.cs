using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;

namespace ARM_APIs.Interface
{
    public interface IFirebase
    {
        Task <string> GetAccessTokenAsync();
    }
}
