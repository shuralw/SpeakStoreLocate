using System.Text.Json.Serialization;

namespace SpeakStoreLocate.ApiService.Models;

public class TranscriptionResponse
{
    [JsonPropertyName("results")] public TranscriptionResults Results { get; set; }
}