// OpenAIOptions.cs

namespace SpeakStoreLocate.ApiService
{
    public class OpenAIOptions
    {
        public const string SectionName = "OpenAI";

        public required string ApiKey { get; set; }

        public string BaseUrl { get; set; } = "https://api.openai.com";

        public string DefaultModel { get; set; } = "gpt-4.1-nano-2025-04-14";

        public double Temperature { get; set; } = 0.0;
    }
}