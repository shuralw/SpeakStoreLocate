using Amazon;
using Amazon.Runtime;

namespace SpeakStoreLocate.ApiService.Options;

public abstract class AWSServiceOptionsBase
{
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string Region { get; set; } = "eu-central-1";
    
    public RegionEndpoint RegionEndpoint => RegionEndpoint.GetBySystemName(Region);
    
    public AWSCredentials GetCredentials() => new BasicAWSCredentials(AccessKey, SecretKey);
    
    public virtual void Validate()
    {
        var serviceName = GetType().Name.Replace("Options", "");
        
        if (string.IsNullOrWhiteSpace(AccessKey) || IsPlaceholder(AccessKey))
            throw new InvalidOperationException($"{serviceName}: AccessKey is required. Please set environment variable or configuration value. Current value: '{AccessKey}'");
        
        if (string.IsNullOrWhiteSpace(SecretKey) || IsPlaceholder(SecretKey))
            throw new InvalidOperationException($"{serviceName}: SecretKey is required. Please set environment variable or configuration value. Current value: '{SecretKey}'");
        
        if (string.IsNullOrWhiteSpace(Region))
            throw new InvalidOperationException($"{serviceName}: Region is required");
    }
    
    private static bool IsPlaceholder(string value)
    {
        return string.IsNullOrEmpty(value) || 
               value.Equals("<set in env>", StringComparison.OrdinalIgnoreCase) ||
               value.StartsWith("<") && value.EndsWith(">");
    }
}