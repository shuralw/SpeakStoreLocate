using Amazon.DynamoDBv2.DataModel;

namespace SpeakStoreLocate.ApiService.Models;

[DynamoDBTable("StorageItems")]
public class StorageItem
{
    [DynamoDBHashKey] public string Id { get; set; }
    [DynamoDBProperty] public string Name { get; set; }
    [DynamoDBProperty] public string NormalizedName { get; set; }
    [DynamoDBProperty] public string Location { get; set; }
    [DynamoDBProperty] public string NormalizedLocation { get; set; }
    [DynamoDBProperty] public string UserId { get; set; }
}