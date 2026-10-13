using ARM_APIs.Interface;


namespace ARM_APIs.Service
{
    public class AxpertAIService : IAxpertAIService
    {
        private readonly IConfiguration _config;

        public AxpertAIService(IConfiguration config)
        {
            _config = config;
        }

        //public async Task<string> GetFieldList(string formType)
        //{
        //    if (string.IsNullOrEmpty(formType))
        //    {
        //        throw new ArgumentException("formtype is null or empty");
        //    }

        //    string prompt = _config["ARMAXPERTAI"]?.Replace("$formtype$", formType);
        //    if (string.IsNullOrEmpty(prompt))
        //    {
        //        throw new ArgumentException("ARMAXPERTAI is null or empty");
        //    }

        //    string apiKey = _config["OPENAIAPIKEY"];
        //    if (string.IsNullOrEmpty(apiKey))
        //    {
        //        throw new ArgumentException("OPENAIAPIKEY is null or empty");
        //    }

        //    string answer = string.Empty;
        //    var openai = new OpenAIAPI(apiKey);
        //    CompletionRequest completion = new CompletionRequest();
        //    completion.Prompt = prompt;
        //    completion.Model = OpenAI_API.Model.DavinciText;
        //    completion.MaxTokens = 4000;

        //    var result = await openai.Completions.CreateCompletionAsync(completion);
        //    if (result != null && result.Completions != null)
        //    {
        //        foreach (var item in result.Completions)
        //        {
        //            answer = item.Text;
        //        }

        //        return answer;
        //    }
        //    else
        //    {
        //        throw new Exception("Not found");
        //    }
        //}
    }

}
