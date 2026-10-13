
using ARMCommon.Interface;
using ARMCommon.Helpers;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;

namespace ARMCommon.Services
{
    public class TokenService : ITokenService
    {
        private Int32 EXPIRY_DURATION_MINUTES;
        private readonly IConfiguration _config;
        private readonly IJwtKeyProvider _jwt;
        public IAppSettingsHelper _appSettingsHelper;
        public TokenService(IConfiguration config, IRedisHelper redis, IJwtKeyProvider jwt, IAppSettingsHelper appSettingsHelper)
        {
            _config = config;
            _jwt = jwt;
            _appSettingsHelper = appSettingsHelper;
            EXPIRY_DURATION_MINUTES = 60;
        }
        public string BuildToken(string key,
        string issuer, string audience, string username, string group, string groupId)
        {
            var claims = new[] {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, group),
                new Claim("GroupId", groupId),
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
            };

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256Signature);
            var tokenDescriptor = new JwtSecurityToken(issuer, issuer, claims,
                expires: DateTime.Now.AddMinutes(EXPIRY_DURATION_MINUTES), signingCredentials: credentials);
            return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
        }

        public string BuildToken(string armSessionId = "", string userName = "", string appName = "")
        {
            var issuer = _config["Jwt:Issuer"];
            var audience = _config["Jwt:Audience"];

            if (string.IsNullOrEmpty(armSessionId))
                armSessionId = $"{Constants.REDIS_PREFIX.ARMSESSION.ToString()}-{Guid.NewGuid().ToString()}";

            var claims = new[] {
                new Claim(ClaimTypes.Name, userName),
                new Claim("sid", armSessionId),
                new Claim(ClaimTypes.NameIdentifier,armSessionId)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature);

            var header = new JwtHeader(credentials)
            {
                { "kid", _config["Jwt:Kid"] }
            };

            if (!string.IsNullOrEmpty(appName))
            {
                EXPIRY_DURATION_MINUTES = _appSettingsHelper.GetARMTokenExpiryMinutes(appName);
            }

            var tokenDescriptor = new JwtSecurityToken(header,
                new JwtPayload(
                    issuer,
                    audience,
                    claims,
                    notBefore: DateTime.UtcNow,
                    expires: DateTime.UtcNow.AddMinutes(EXPIRY_DURATION_MINUTES)
                )
            );

            var handler = new JwtSecurityTokenHandler();
            return handler.WriteToken(tokenDescriptor);

        }

        //public string GenerateJSONWebToken(string key, string issuer, UserDTO user)
        //{
        //    var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        //    var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        //    var token = new JwtSecurityToken(issuer, issuer,
        //      null,
        //      expires: DateTime.Now.AddMinutes(120),
        //      signingCredentials: credentials);

        //    return new JwtSecurityTokenHandler().WriteToken(token);
        //}

        public string CreateToken(string key,
    string issuer, string username)
        {
            var claims = new[] {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
            };

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256Signature);
            var tokenDescriptor = new JwtSecurityToken(issuer, issuer, claims,
                expires: DateTime.Now.AddMinutes(EXPIRY_DURATION_MINUTES), signingCredentials: credentials);
            return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
        }
        public bool IsTokenValid(string key, string issuer, string token)
        {
            var mySecret = Encoding.UTF8.GetBytes(key);
            var mySecurityKey = new SymmetricSecurityKey(mySecret);

            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidIssuer = issuer,
                    ValidAudience = issuer,
                    ValidateLifetime = true,
                    IssuerSigningKey = mySecurityKey,
                }, out SecurityToken validatedToken);
            }
            catch
            {
                return false;
            }
            return true;
        }
    }
}
