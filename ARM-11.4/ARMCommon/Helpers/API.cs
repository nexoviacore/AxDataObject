

using ARMCommon.Interface;
using ARMCommon.Model;
using System.Text;

namespace ARMCommon.Helpers
{
    public class API : IAPI
    {

        public async Task<ARMResult> GetData(string url)
        {
                  
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    var request = await client.GetAsync(url);
                    request.EnsureSuccessStatusCode();
                    return new ARMResult(request.IsSuccessStatusCode, await request.Content.ReadAsStringAsync());
                }
                catch (Exception ex)
                {
                    return new ARMResult(false, ex.Message);

                }
            }
        }

        public async Task<ARMResult> POSTData(string url, string body, string Mediatype)
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    var request = await client.PostAsync(new Uri(url), new StringContent(body, Encoding.UTF8, Mediatype));
                    request.EnsureSuccessStatusCode();
                    return new ARMResult(request.IsSuccessStatusCode, await request.Content.ReadAsStringAsync());

                }
                catch (Exception ex)
                {
                    return new ARMResult(false, ex.Message);
                }
            }
        }


       
    }
}
