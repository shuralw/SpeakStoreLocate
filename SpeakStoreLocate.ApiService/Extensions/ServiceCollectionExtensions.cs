using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.S3;
using Amazon.TranscribeService;
using Microsoft.Extensions.Options;
using SpeakStoreLocate.ApiService.Options;

namespace SpeakStoreLocate.ApiService.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAWSServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure AWS options with environment variable support
        services.ConfigureAWSFromEnvironment(configuration);

        // Validate AWS options on startup with detailed error messages
        services.AddOptionsWithValidateOnStart<AmazonS3Options>()
            .PostConfigure(options => 
            {
                try 
                {
                    options.Validate();
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"AWS S3 Configuration Error: {ex.Message}. Please check your environment variables: AWS_S3_ACCESS_KEY, AWS_S3_SECRET_KEY", ex);
                }
            });

        services.AddOptionsWithValidateOnStart<AmazonDynamoDBOptions>()
            .PostConfigure(options => 
            {
                try 
                {
                    options.Validate();
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"AWS DynamoDB Configuration Error: {ex.Message}. Please check your environment variables: AWS_DYNAMODB_ACCESS_KEY, AWS_DYNAMODB_SECRET_KEY", ex);
                }
            });

        services.AddOptionsWithValidateOnStart<AmazonTranscribeServiceOptions>()
            .PostConfigure(options => 
            {
                try 
                {
                    options.Validate();
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"AWS Transcribe Configuration Error: {ex.Message}. Please check your environment variables: AWS_TRANSCRIBE_ACCESS_KEY, AWS_TRANSCRIBE_SECRET_KEY", ex);
                }
            });

        // Register AWS services with error handling
        services.AddSingleton<IAmazonS3>(sp =>
        {
            try
            {
                var options = sp.GetRequiredService<IOptions<AmazonS3Options>>().Value;
                return new AmazonS3Client(options.GetCredentials(), options.RegionEndpoint);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to create Amazon S3 client. Please verify your AWS S3 credentials and configuration.", ex);
            }
        });

        services.AddSingleton<IAmazonTranscribeService>(sp =>
        {
            try
            {
                var options = sp.GetRequiredService<IOptions<AmazonTranscribeServiceOptions>>().Value;
                return new AmazonTranscribeServiceClient(options.GetCredentials(), options.RegionEndpoint);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to create Amazon Transcribe client. Please verify your AWS Transcribe credentials and configuration.", ex);
            }
        });

        services.AddSingleton<IAmazonDynamoDB>(sp =>
        {
            try
            {
                var options = sp.GetRequiredService<IOptions<AmazonDynamoDBOptions>>().Value;
                return new AmazonDynamoDBClient(options.GetCredentials(), options.RegionEndpoint);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to create Amazon DynamoDB client. Please verify your AWS DynamoDB credentials and configuration.", ex);
            }
        });

        // Register DynamoDB context
        services.AddScoped<IDynamoDBContext>(sp => 
            new DynamoDBContext(sp.GetRequiredService<IAmazonDynamoDB>()));

        return services;
    }
}