using Microsoft.Extensions.Options;
using SpeakStoreLocate.ApiService.Options;

namespace SpeakStoreLocate.ApiService.Extensions;

public static class ConfigurationExtensions
{
    /// <summary>
    /// Adds AWS configuration from environment variables with fallback to configuration
    /// Priority: appsettings.json -> User Secrets -> Environment Variables
    /// </summary>
    public static IServiceCollection AddAWSConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        // S3 Configuration
        services.Configure<AmazonS3Options>(options =>
        {
            configuration.GetSection(AmazonS3Options.SectionName).Bind(options);
            OverrideWithEnvironmentVariables(options, "AWS_S3");
        });

        // DynamoDB Configuration
        services.Configure<AmazonDynamoDBOptions>(options =>
        {
            configuration.GetSection(AmazonDynamoDBOptions.SectionName).Bind(options);
            OverrideWithEnvironmentVariables(options, "AWS_DYNAMODB");
            
            // DynamoDB specific environment variables
            var envTableName = Environment.GetEnvironmentVariable("AWS_DYNAMODB_TABLE_NAME");
            if (!string.IsNullOrEmpty(envTableName))
                options.TableName = envTableName;
        });

        // Transcribe Configuration
        services.Configure<AmazonTranscribeServiceOptions>(options =>
        {
            configuration.GetSection(AmazonTranscribeServiceOptions.SectionName).Bind(options);
            OverrideWithEnvironmentVariables(options, "AWS_TRANSCRIBE");
            
            // Transcribe specific environment variables
            var envLanguageCode = Environment.GetEnvironmentVariable("AWS_TRANSCRIBE_LANGUAGE_CODE");
            if (!string.IsNullOrEmpty(envLanguageCode))
                options.LanguageCode = envLanguageCode;
                
            if (int.TryParse(Environment.GetEnvironmentVariable("AWS_TRANSCRIBE_SAMPLE_RATE"), out var sampleRate))
            {
                options.SampleRateHertz = sampleRate;
            }
        });

        return services;
    }

    /// <summary>
    /// Generic method to override AWS options with environment variables
    /// </summary>
    private static void OverrideWithEnvironmentVariables(AWSServiceOptionsBase options, string prefix)
    {
        var envAccessKey = Environment.GetEnvironmentVariable($"{prefix}_ACCESS_KEY");
        var envSecretKey = Environment.GetEnvironmentVariable($"{prefix}_SECRET_KEY");
        var envRegion = Environment.GetEnvironmentVariable($"{prefix}_REGION");
        
        if (!string.IsNullOrEmpty(envAccessKey) || IsPlaceholder(options.AccessKey))
            options.AccessKey = envAccessKey ?? string.Empty;
            
        if (!string.IsNullOrEmpty(envSecretKey) || IsPlaceholder(options.SecretKey))
            options.SecretKey = envSecretKey ?? string.Empty;
            
        if (!string.IsNullOrEmpty(envRegion))
            options.Region = envRegion;
    }
    
    private static bool IsPlaceholder(string? value)
    {
        return string.IsNullOrEmpty(value) || 
               value.Equals("<set in env>", StringComparison.OrdinalIgnoreCase) ||
               (value.StartsWith("<") && value.EndsWith(">"));
    }
}