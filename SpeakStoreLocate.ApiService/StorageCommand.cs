using System.Text.Json.Serialization;

namespace SpeakStoreLocate.ApiService;

public class StorageCommand
{
    [JsonPropertyName("method")] public string Method { get; set; }

    [JsonPropertyName("count")] public int Count { get; set; }

    [JsonPropertyName("itemName")] public string ItemName { get; set; }

    [JsonPropertyName("destination")] public string Destination { get; set; }
}