using Microsoft.Extensions.Options;
using SpeakStoreLocate.ApiService.Options;

namespace SpeakStoreLocate.ApiService.Extensions;

public static class ConfigurationExtensions
{
    /// <summary>
    /// Adds AWS configuration from environment variables with fallback to configuration
    /// </summary>
    public static IServiceCollection ConfigureAWSFromEnvironment(this IServiceCollection services, IConfiguration configuration)
    {
        // S3 Configuration
        services.Configure<AmazonS3Options>(options =>
        {
            configuration.GetSection(AmazonS3Options.SectionName).Bind(options);
            
            // Override with environment variables if available, or if config values are placeholders
            var envAccessKey = Environment.GetEnvironmentVariable("AWS_S3_ACCESS_KEY");
            var envSecretKey = Environment.GetEnvironmentVariable("AWS_S3_SECRET_KEY");
            var envRegion = Environment.GetEnvironmentVariable("AWS_S3_REGION");
            var envBucketName = Environment.GetEnvironmentVariable("AWS_S3_BUCKET_NAME");
            
            if (!string.IsNullOrEmpty(envAccessKey) || IsPlaceholder(options.AccessKey))
                options.AccessKey = envAccessKey ?? string.Empty;
            if (!string.IsNullOrEmpty(envSecretKey) || IsPlaceholder(options.SecretKey))
                options.SecretKey = envSecretKey ?? string.Empty;
            if (!string.IsNullOrEmpty(envRegion))
                options.Region = envRegion;
            if (!string.IsNullOrEmpty(envBucketName))
                options.BucketName = envBucketName;
        });

        // DynamoDB Configuration
        services.Configure<AmazonDynamoDBOptions>(options =>
        {
            configuration.GetSection(AmazonDynamoDBOptions.SectionName).Bind(options);
            
            var envAccessKey = Environment.GetEnvironmentVariable("AWS_DYNAMODB_ACCESS_KEY");
            var envSecretKey = Environment.GetEnvironmentVariable("AWS_DYNAMODB_SECRET_KEY");
            var envRegion = Environment.GetEnvironmentVariable("AWS_DYNAMODB_REGION");
            var envTableName = Environment.GetEnvironmentVariable("AWS_DYNAMODB_TABLE_NAME");
            
            if (!string.IsNullOrEmpty(envAccessKey) || IsPlaceholder(options.AccessKey))
                options.AccessKey = envAccessKey ?? string.Empty;
            if (!string.IsNullOrEmpty(envSecretKey) || IsPlaceholder(options.SecretKey))
                options.SecretKey = envSecretKey ?? string.Empty;
            if (!string.IsNullOrEmpty(envRegion))
                options.Region = envRegion;
            if (!string.IsNullOrEmpty(envTableName))
                options.TableName = envTableName;
        });

        // Transcribe Configuration
        services.Configure<AmazonTranscribeServiceOptions>(options =>
        {
            configuration.GetSection(AmazonTranscribeServiceOptions.SectionName).Bind(options);
            
            var envAccessKey = Environment.GetEnvironmentVariable("AWS_TRANSCRIBE_ACCESS_KEY");
            var envSecretKey = Environment.GetEnvironmentVariable("AWS_TRANSCRIBE_SECRET_KEY");
            var envRegion = Environment.GetEnvironmentVariable("AWS_TRANSCRIBE_REGION");
            var envLanguageCode = Environment.GetEnvironmentVariable("AWS_TRANSCRIBE_LANGUAGE_CODE");
            
            if (!string.IsNullOrEmpty(envAccessKey) || IsPlaceholder(options.AccessKey))
                options.AccessKey = envAccessKey ?? string.Empty;
            if (!string.IsNullOrEmpty(envSecretKey) || IsPlaceholder(options.SecretKey))
                options.SecretKey = envSecretKey ?? string.Empty;
            if (!string.IsNullOrEmpty(envRegion))
                options.Region = envRegion;
            if (!string.IsNullOrEmpty(envLanguageCode))
                options.LanguageCode = envLanguageCode;
            
            if (int.TryParse(Environment.GetEnvironmentVariable("AWS_TRANSCRIBE_SAMPLE_RATE"), out var sampleRate))
            {
                options.SampleRateHertz = sampleRate;
            }
        });

        return services;
    }
    
    private static bool IsPlaceholder(string value)
    {
        return string.IsNullOrEmpty(value) || 
               value.Equals("<set in env>", StringComparison.OrdinalIgnoreCase) ||
               value.StartsWith("<") && value.EndsWith(">");
    }
}