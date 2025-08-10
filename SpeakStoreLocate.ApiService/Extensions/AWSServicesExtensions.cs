using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.S3;
using Amazon.TranscribeService;
using Microsoft.Extensions.Options;
using SpeakStoreLocate.ApiService.Options;

namespace SpeakStoreLocate.ApiService.Extensions;

public static class AWSServicesExtensions
{
    /// <summary>
    /// Registers all AWS services with proper configuration and dependency injection
    /// </summary>
    public static IServiceCollection AddAWSServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Amazon S3 Client
        services.AddSingleton<IAmazonS3>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<AmazonS3Options>>().Value;
            options.Validate(); // Validate configuration
            
            return new AmazonS3Client(
                options.GetCredentials(),
                options.RegionEndpoint);
        });

        // Amazon DynamoDB Client  
        services.AddSingleton<IAmazonDynamoDB>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<AmazonDynamoDBOptions>>().Value;
            options.Validate(); // Validate configuration
            
            return new AmazonDynamoDBClient(
                options.GetCredentials(),
                options.RegionEndpoint);
        });

        // DynamoDB Context (scoped for proper lifecycle)
        services.AddScoped<IDynamoDBContext>(serviceProvider =>
        {
            var dynamoClient = serviceProvider.GetRequiredService<IAmazonDynamoDB>();
            return new DynamoDBContext(dynamoClient);
        });

        // Amazon Transcribe Client (optional, only if needed)
        services.AddSingleton<IAmazonTranscribeService>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<AmazonTranscribeServiceOptions>>().Value;
            
            // Transcribe is optional, only create if configured
            if (string.IsNullOrWhiteSpace(options.AccessKey) || 
                options.AccessKey.StartsWith("<") && options.AccessKey.EndsWith(">"))
            {
                // Return a dummy implementation or throw if actually needed
                return null!; // This should be handled by the consuming service
            }
            
            options.Validate();
            
            return new AmazonTranscribeServiceClient(
                options.GetCredentials(),
                options.RegionEndpoint);
        });

        return services;
    }
}