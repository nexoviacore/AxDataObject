using ARMCommon.Interface;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace ARMCommon.Helpers
{
    public class JwtKeyProvider : IJwtKeyProvider
    {
        public SecurityKey _authKey;

        //public JwtKeyProvider(IConfiguration _config)
        //{
          

        //    if (_config["Jwt:LoadKeyFromJWKS"] != null && Convert.ToBoolean(_config["Jwt:LoadKeyFromJWKS"]) == true && File.Exists(jwksFilePath))
        //    {
        //        _authKey = Utils.LoadKeyFromJwksFile(jwksFilePath, _config["Jwt:Key"]);
        //    }
        //    else
        //    {
        //        _authKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
        //    }

        //}

        public SecurityKey GetKey(string jwksFilePath,string kid)
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), jwksFilePath);
            if (_authKey is null)
            {
                _authKey = Utils.LoadKeyFromJwksFile(filePath, kid);
            }
            return _authKey;

        }
    }
}
