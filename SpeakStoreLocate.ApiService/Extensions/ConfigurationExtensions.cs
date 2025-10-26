using SpeakStoreLocate.ApiService.Options;

namespace SpeakStoreLocate.ApiService.Extensions;

public static class ConfigurationExtensions
{
    /// <summary>
    /// Fügt AWS-Konfigurationen ausschließlich über das Options Pattern hinzu.
    /// </summary>
    public static IServiceCollection AddAwsConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AmazonS3Options>(configuration.GetSection(AmazonS3Options.SectionName));
        services.Configure<AmazonDynamoDBOptions>(configuration.GetSection(AmazonDynamoDBOptions.SectionName));
        services.Configure<AmazonTranscribeServiceOptions>(configuration.GetSection(AmazonTranscribeServiceOptions.SectionName));
        return services;
    }
}