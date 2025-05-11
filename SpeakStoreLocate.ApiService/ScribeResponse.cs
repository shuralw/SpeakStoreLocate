using System.Text.Json.Serialization;

namespace SpeakStoreLocate.ApiService;

public class ScribeResponse
{
    [JsonPropertyName("text")] public string Text { get; set; }

    // Falls Ihr die Words-/Timestamp-Daten braucht, ergänzt hier z.B.:
    // public List<WordInfo> Words { get; set; }
}