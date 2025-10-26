using SpeakStoreLocate.ApiService.Options;

namespace SpeakStoreLocate.ApiService.Extensions;

public static class ServiceConfigurationExtensions
{
    /// <summary>
    /// Konfiguriert alle externen Service-Optionen ausschließlich über das Options Pattern.
    /// </summary>
    public static IServiceCollection AddExternalServiceConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        // OpenAI
        services.Configure<OpenAIOptions>(configuration.GetSection(OpenAIOptions.SectionName));
        // Deepgram
        services.Configure<DeepgramOptions>(configuration.GetSection(DeepgramOptions.SectionName));
        // ElevenLabs
        services.Configure<ElevenLabsOptions>(configuration.GetSection(ElevenLabsOptions.SectionName));
        return services;
    }
}