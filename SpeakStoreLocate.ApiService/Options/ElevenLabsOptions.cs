namespace SpeakStoreLocate.ApiService.Options;

public class ElevenLabsOptions
{
    public const string SectionName = "ElevenLabs";
    
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.elevenlabs.io";
    
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
            throw new InvalidOperationException("ElevenLabs ApiKey is required");
        
        if (string.IsNullOrWhiteSpace(BaseUrl))
            throw new InvalidOperationException("ElevenLabs BaseUrl is required");
    }
}