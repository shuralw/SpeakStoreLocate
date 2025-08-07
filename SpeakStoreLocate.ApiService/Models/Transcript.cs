using System.Text.Json.Serialization;

namespace SpeakStoreLocate.ApiService.Models;

public class Transcript
{
    // JSON-Feld heißt "transcript", C#‑Property nennen wir "Text"
    [JsonPropertyName("transcript")] public string Text { get; set; }
}