using System.Text.Json.Serialization;

namespace SpeakStoreLocate.ApiService;

public class TranscriptionResponse
{
    [JsonPropertyName("results")] public TranscriptionResults Results { get; set; }
}