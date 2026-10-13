using System;
using System.Net.Http;
using System.Text;

namespace ARMEncryptSecret
{
    public static class EncryptDecryptSecretKeys
    {
        public static string EncryptSecretKey(string armUrl, string secret)
        {
            if(string.IsNullOrEmpty(armUrl) || string.IsNullOrEmpty(secret))
            {
                return "Error: ARM URL or Secret Key is null or empty.";
            }

            string url = $"{armUrl}/api/v1/ARMGetEncryptedSecret";

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    var json = $"{{ \"SecretKey\": \"{secret}\" }}";
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    // Call API synchronously
                    HttpResponseMessage response = client.PostAsync(url, content).Result;

                    response.EnsureSuccessStatusCode();

                    string result = response.Content.ReadAsStringAsync().Result;

                    return result;   
                }
            }
            catch (HttpRequestException ex)
            {
                return "HTTP Error: " + ex.Message;
            }
            catch (Exception ex)
            {
                return "Error: " + ex.Message;
            }
        }


    }
}
