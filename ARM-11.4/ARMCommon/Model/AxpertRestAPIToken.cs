using System;
using System.Security.Cryptography;
using System.Text;
using AgileConnect.EncrDecr.cs;
using ARMCommon.Helpers;

namespace ARMCommon.Model
{
    public class AxpertRestAPIToken
    {
        public string seed = string.Empty;
        public string token = string.Empty;        
        public string userAuthKey = string.Empty;

        public AxpertRestAPIToken(string userName) 
        {
            GenerateAxpertToken();
            userAuthKey = EncrDecr.AxpertEncryptString(userName);
        }

        public bool ValidateAxpertToken(string validateToken, string validateSeed) {
            string secretKey = Constants.KeyForAxpertToken;
            string strPlain = validateSeed + secretKey + validateSeed;
            return MD5Hash(strPlain) == validateToken;
        }


        private void GenerateAxpertToken() {
            string secretKey = Constants.KeyForAxpertToken;
            Random random = new Random();
            seed = random.Next(100000, 1000000).ToString("D6");
            string strPlain = seed + secretKey + seed;
            token = MD5Hash(strPlain);
        }

        private string MD5Hash(string inputString) {
            string hashString = "";
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(inputString);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                StringBuilder stringBuilder = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    stringBuilder.Append(hashBytes[i].ToString("x2"));
                }

                hashString = stringBuilder.ToString();
            }
            return hashString;
        }
    }
}
