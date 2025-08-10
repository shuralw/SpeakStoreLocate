using Amazon;

namespace SpeakStoreLocate.ApiService.Options;

public class AmazonDynamoDBOptions : AWSServiceOptionsBase
{
    public const string SectionName = "AWS:DynamoDB";
    
    public string TableName { get; set; } = string.Empty;
    
    public override void Validate()
    {
        base.Validate();
        
        if (string.IsNullOrWhiteSpace(TableName))
            throw new InvalidOperationException("AWS DynamoDB TableName is required");
    }
}