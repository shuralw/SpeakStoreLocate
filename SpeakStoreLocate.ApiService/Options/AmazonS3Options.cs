using Amazon;

namespace SpeakStoreLocate.ApiService.Options;

public class AmazonS3Options : AWSServiceOptionsBase
{
    public const string SectionName = "AWS:S3";
    
    public string BucketName { get; set; } = string.Empty;
    
    public override void Validate()
    {
        base.Validate();
        
        if (string.IsNullOrWhiteSpace(BucketName))
            throw new InvalidOperationException("AWS S3 BucketName is required");
    }
}