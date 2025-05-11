using Amazon.DynamoDBv2.DataModel;

namespace SpeakStoreLocate.ApiService;

[DynamoDBTable("StorageItems")]
public class StorageItem
{
    [DynamoDBHashKey] public string Id { get; set; }
    [DynamoDBProperty] public string Name { get; set; }
    [DynamoDBProperty] public string NormalizedName { get; set; }
    [DynamoDBProperty] public string Location { get; set; }
}