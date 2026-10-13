using ARMCommon.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ARMAPIService
{
    public class APICustom
    {
        public string BeforeAPICall(string inputJson)
        {
            string outputJson = string.Empty;

            //Write custom logics here to parse the JSON in expected formats.
            if (inputJson.IndexOf("itemm") > -1 && inputJson.IndexOf("submitdata") > -1)
            {
                JObject tempJson = JObject.Parse(inputJson);
                string submitStr = tempJson["queuedata"].ToString();
                JObject submitJson = JObject.Parse(submitStr);
                return AxpertItemmToShopifyProductJson(submitJson);

            }
            else
                outputJson = inputJson;

            return outputJson;
        }

        private string AxpertItemmToShopifyProductJson(JObject inputJson)
        {
            // Extract relevant data from the input JSON
            string title = inputJson["payload"]?["submitdata"]?["dataarray"]?["data"]?["dc1"]?["row1"]?["itemdesc"]?.ToString();
            string productType = inputJson["payload"]?["submitdata"]?["dataarray"]?["data"]?["dc1"]?["row1"]?["itemcategory"]?.ToString();
            string project = inputJson["payload"]?["submitdata"]?["project"]?.ToString();
            // Build the output JSON dynamically
            JObject product = new JObject
            {
                ["product"] = new JObject
                {
                    ["title"] = title,
                    ["product_type"] = productType
                }
            };

            ApiRequest apiRequest = new ApiRequest();
            apiRequest.Method = "POST";
            apiRequest.Project = project;
            apiRequest.Url = "https://b843cb-2.myshopify.com/admin/api/2024-01/products.json";
            apiRequest.RequestString = product;
            apiRequest.HeaderString = new Dictionary<string, string>();
            apiRequest.HeaderString.Add("X-Shopify-Access-Token", "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");
            return JsonConvert.SerializeObject(apiRequest); ;
        }
    }
}
