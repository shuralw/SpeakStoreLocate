using Microsoft.Extensions.Options;
using SpeakStoreLocate.ApiService.Options;

namespace SpeakStoreLocate.ApiService.Utilities;

public static class ConfigurationDebugger
{
    public static void LogAWSConfiguration(ILogger logger, IServiceProvider serviceProvider)
    {
        try
        {
            logger.LogInformation("=== AWS Configuration Debug ===");
            
            // S3 Configuration
            var s3Options = serviceProvider.GetService<IOptions<AmazonS3Options>>()?.Value;
            if (s3Options != null)
            {
                logger.LogInformation("S3 Configuration:");
                logger.LogInformation("  AccessKey: {AccessKey}", MaskCredential(s3Options.AccessKey));
                logger.LogInformation("  SecretKey: {SecretKey}", MaskCredential(s3Options.SecretKey));
                logger.LogInformation("  Region: {Region}", s3Options.Region);
                logger.LogInformation("  BucketName: {BucketName}", s3Options.BucketName);
            }
            
            // DynamoDB Configuration
            var dynamoOptions = serviceProvider.GetService<IOptions<AmazonDynamoDBOptions>>()?.Value;
            if (dynamoOptions != null)
            {
                logger.LogInformation("DynamoDB Configuration:");
                logger.LogInformation("  AccessKey: {AccessKey}", MaskCredential(dynamoOptions.AccessKey));
                logger.LogInformation("  SecretKey: {SecretKey}", MaskCredential(dynamoOptions.SecretKey));
                logger.LogInformation("  Region: {Region}", dynamoOptions.Region);
                logger.LogInformation("  TableName: {TableName}", dynamoOptions.TableName);
            }
            
            // Environment Variables Check
            logger.LogInformation("Environment Variables:");
            logger.LogInformation("  AWS_S3_ACCESS_KEY: {Value}", MaskCredential(Environment.GetEnvironmentVariable("AWS_S3_ACCESS_KEY")));
            logger.LogInformation("  AWS_S3_SECRET_KEY: {Value}", MaskCredential(Environment.GetEnvironmentVariable("AWS_S3_SECRET_KEY")));
            logger.LogInformation("  AWS_DYNAMODB_ACCESS_KEY: {Value}", MaskCredential(Environment.GetEnvironmentVariable("AWS_DYNAMODB_ACCESS_KEY")));
            logger.LogInformation("  AWS_DYNAMODB_SECRET_KEY: {Value}", MaskCredential(Environment.GetEnvironmentVariable("AWS_DYNAMODB_SECRET_KEY")));
            
            logger.LogInformation("=== End AWS Configuration Debug ===");
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
            logger.LogInformation("=== OpenAI Configuration Debug ===");
            
            var openAIOptions = serviceProvider.GetService<IOptions<OpenAIOptions>>()?.Value;
            if (openAIOptions != null)
            {
                logger.LogInformation("OpenAI Configuration:");
                logger.LogInformation("  ApiKey: {ApiKey}", MaskCredential(openAIOptions.ApiKey));
                logger.LogInformation("  BaseUrl: {BaseUrl}", openAIOptions.BaseUrl);
                logger.LogInformation("  DefaultModel: {DefaultModel}", openAIOptions.DefaultModel);
                logger.LogInformation("  Temperature: {Temperature}", openAIOptions.Temperature);
                
                // Validate BaseUrl
                if (Uri.TryCreate(openAIOptions.BaseUrl, UriKind.Absolute, out var uri))
                {
                    logger.LogInformation("  BaseUrl is valid: {IsValid}", true);
                }
                else
                {
                    logger.LogError("  BaseUrl is INVALID: {BaseUrl}", openAIOptions.BaseUrl);
                }
            }
            
            logger.LogInformation("=== End OpenAI Configuration Debug ===");
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