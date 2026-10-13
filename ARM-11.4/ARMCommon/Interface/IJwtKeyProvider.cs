using Microsoft.IdentityModel.Tokens;

namespace ARMCommon.Interface
{
    public interface IJwtKeyProvider
    {
        public SecurityKey GetKey(string jwksFilePath, string kid);
    }
}
