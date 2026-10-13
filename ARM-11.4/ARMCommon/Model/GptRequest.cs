namespace ARMCommon.Model
{
    public class GptRequest
    {
        public string Prompt { get; set; }
        public int MaxTokens { get; set; } = 10;
        public int N { get; set; } = 1;
    }

    public class OpenAIGptResponse
    {
        public OpenAIGptChoice[] choices { get; set; }
    }
    public class OpenAIGptChoice
    {
        public string text { get; set; }
        public float? logprobs { get; set; }
        public string? finish_reason { get; set; }
    }

}
