using Microsoft.Extensions.Options;
using SpeakStoreLocate.ApiService.Options;

namespace SpeakStoreLocate.ApiService.Utilities;

public static class ConfigurationDebugger
{
    public static void LogAWSConfiguration(ILogger logger, IServiceProvider serviceProvider)
    {
        try
        {
            logger.LogDebug("=== AWS Configuration Debug ===");
            
            // S3 Configuration
            var s3Options = serviceProvider.GetService<IOptions<AmazonS3Options>>()?.Value;
            if (s3Options != null)
            {
                logger.LogDebug("S3 Configuration:");
                logger.LogDebug("  AccessKey: {AccessKey}", MaskCredential(s3Options.AccessKey));
                logger.LogDebug("  SecretKey: {SecretKey}", MaskCredential(s3Options.SecretKey));
                logger.LogDebug("  Region: {Region}", s3Options.Region);
                logger.LogDebug("  BucketName: {BucketName}", s3Options.BucketName);
            }
            
            // DynamoDB Configuration
            var dynamoOptions = serviceProvider.GetService<IOptions<AmazonDynamoDBOptions>>()?.Value;
            if (dynamoOptions != null)
            {
                logger.LogDebug("DynamoDB Configuration:");
                logger.LogDebug("  AccessKey: {AccessKey}", MaskCredential(dynamoOptions.AccessKey));
                logger.LogDebug("  SecretKey: {SecretKey}", MaskCredential(dynamoOptions.SecretKey));
                logger.LogDebug("  Region: {Region}", dynamoOptions.Region);
                logger.LogDebug("  TableName: {TableName}", dynamoOptions.TableName);
            }
            
            // Environment Variables Check
            logger.LogDebug("Environment Variables:");
            logger.LogDebug("  AWS_S3_ACCESS_KEY: {Value}", MaskCredential(Environment.GetEnvironmentVariable("AWS_S3_ACCESS_KEY")));
            logger.LogDebug("  AWS_S3_SECRET_KEY: {Value}", MaskCredential(Environment.GetEnvironmentVariable("AWS_S3_SECRET_KEY")));
            logger.LogDebug("  AWS_DYNAMODB_ACCESS_KEY: {Value}", MaskCredential(Environment.GetEnvironmentVariable("AWS_DYNAMODB_ACCESS_KEY")));
            logger.LogDebug("  AWS_DYNAMODB_SECRET_KEY: {Value}", MaskCredential(Environment.GetEnvironmentVariable("AWS_DYNAMODB_SECRET_KEY")));
            
            logger.LogDebug("=== End AWS Configuration Debug ===");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while debugging AWS configuration");
        }
    }
    
    public static void LogOpenAIConfiguration(ILogger logger, IServiceProvider serviceProvider)
    {
        try
        {
            logger.LogDebug("=== OpenAI Configuration Debug ===");
            
            var openAIOptions = serviceProvider.GetService<IOptions<OpenAIOptions>>()?.Value;
            if (openAIOptions != null)
            {
                logger.LogDebug("OpenAI Configuration:");
                logger.LogDebug("  ApiKey: {ApiKey}", MaskCredential(openAIOptions.APIKEY));
                logger.LogDebug("  BaseUrl: {BaseUrl}", openAIOptions.BaseUrl);
                logger.LogDebug("  DefaultModel: {DefaultModel}", openAIOptions.DefaultModel);
                logger.LogDebug("  Temperature: {Temperature}", openAIOptions.Temperature);
                
                // Validate BaseUrl
                if (Uri.TryCreate(openAIOptions.BaseUrl, UriKind.Absolute, out var uri))
                {
                    logger.LogDebug("  BaseUrl is valid: {IsValid}", true);
                }
                else
                {
                    logger.LogWarning("  BaseUrl is INVALID: {BaseUrl}", openAIOptions.BaseUrl);
                }
            }
            
            logger.LogDebug("=== End OpenAI Configuration Debug ===");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while debugging OpenAI configuration");
        }
    }
    
    private static string MaskCredential(string? credential)
    {
        if (string.IsNullOrEmpty(credential))
            return "(not set)";
        
        if (credential.Length <= 4)
            return "****";
        
        return credential.Substring(0, 4) + "****";
    }
}