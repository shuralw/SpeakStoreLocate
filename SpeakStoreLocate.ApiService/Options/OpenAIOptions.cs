// OpenAIOptions.cs

namespace SpeakStoreLocate.ApiService.Options
{
    public class OpenAIOptions
    {
        public const string SectionName = "OpenAI";

        public string ApiKey { get; set; } = string.Empty;

        public string BaseUrl { get; set; } = "https://api.openai.com";

        public string DefaultModel { get; set; } = "gpt-4.1-nano-2025-04-14";

        public double Temperature { get; set; } = 0.0;
        
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(ApiKey))
                throw new InvalidOperationException("OpenAI ApiKey is required");
            
            if (string.IsNullOrWhiteSpace(BaseUrl))
                throw new InvalidOperationException("OpenAI BaseUrl is required");
            
            if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out _))
                throw new InvalidOperationException($"OpenAI BaseUrl '{BaseUrl}' is not a valid URI");
            
            if (Temperature < 0.0 || Temperature > 2.0)
                throw new InvalidOperationException("OpenAI Temperature must be between 0.0 and 2.0");
        }
    }
}