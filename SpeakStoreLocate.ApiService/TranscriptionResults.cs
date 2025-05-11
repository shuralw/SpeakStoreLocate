using System.Text.Json.Serialization;

namespace SpeakStoreLocate.ApiService;

public class TranscriptionResults
{
    [JsonPropertyName("transcripts")] public Transcript[] Transcripts { get; set; }
}