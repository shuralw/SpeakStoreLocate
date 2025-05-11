using System.Text.Json.Serialization;

namespace SpeakStoreLocate.ApiService;

public class Transcript
{
    // JSON-Feld heißt "transcript", C#‑Property nennen wir "Text"
    [JsonPropertyName("transcript")] public string Text { get; set; }
}