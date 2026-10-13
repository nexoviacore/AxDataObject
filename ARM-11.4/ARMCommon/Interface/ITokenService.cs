namespace ARMCommon.Interface
{
    public interface ITokenService
    {
        string BuildToken(string key,string issuer,string audience, string username, string group, string groupid);
        string BuildToken(string sessionId = "",string userName = "", string appName = "");

        string CreateToken(string key, string issuer, string username);

        //string GenerateJSONWebToken(string key, string issuer, UserDTO user);
        bool IsTokenValid(string key, string issuer, string token);
    }
}
