using Amazon;

namespace SpeakStoreLocate.ApiService.Options;

public class AmazonTranscribeServiceOptions : AWSServiceOptionsBase
{
    public const string SectionName = "AWS:Transcribe";
    
    public string LanguageCode { get; set; } = "de-DE";
    public int? SampleRateHertz { get; set; } = 16000;
    
    public override void Validate()
    {
        base.Validate();
        
        if (string.IsNullOrWhiteSpace(LanguageCode))
            throw new InvalidOperationException("AWS Transcribe LanguageCode is required");
    }
}