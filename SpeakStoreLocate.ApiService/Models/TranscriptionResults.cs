using System.Text.Json.Serialization;

namespace SpeakStoreLocate.ApiService.Models;

public class TranscriptionResults
{
    [JsonPropertyName("transcripts")] public Transcript[] Transcripts { get; set; }
}