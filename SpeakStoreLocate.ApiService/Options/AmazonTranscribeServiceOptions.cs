namespace SpeakStoreLocate.ApiService.Options;

public class AmazonTranscribeServiceOptions
{
    public string LanguageCode { get; set; }
    public int? SampleRateHertz { get; set; }
}