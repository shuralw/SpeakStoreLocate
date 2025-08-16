using System.Text.Json.Serialization;

namespace SpeakStoreLocate.ApiService.Models;

public class StorageCommand
{
    [JsonPropertyName("method")] public string Method { get; set; }

    [JsonPropertyName("count")] public int Count { get; set; }

    [JsonPropertyName("itemName")] public string ItemName { get; set; }

    [JsonPropertyName("source")] public string? Source { get; set; } // neu

    [JsonPropertyName("destination")] public string Destination { get; set; }
}