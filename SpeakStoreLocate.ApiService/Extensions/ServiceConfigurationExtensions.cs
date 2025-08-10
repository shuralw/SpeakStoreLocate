using Microsoft.Extensions.Options;
using SpeakStoreLocate.ApiService.Options;

namespace SpeakStoreLocate.ApiService.Extensions;

public static class ServiceConfigurationExtensions
{
    /// <summary>
    /// Configures all external service options with validation and environment variable support
    /// Priority: appsettings.json -> User Secrets -> Environment Variables
    /// </summary>
    public static IServiceCollection AddExternalServiceConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure OpenAI with validation
        services.AddOptionsWithValidateOnStart<OpenAIOptions>()
            .Configure(options => configuration.GetSection(OpenAIOptions.SectionName).Bind(options))
            .PostConfigure(options =>
            {
                // Environment variable override (only if environment variable is actually set)
                var envApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
                if (!string.IsNullOrEmpty(envApiKey))
                {
                    options.APIKEY = envApiKey;
                }
                else if (IsPlaceholder(options.APIKEY))
                {
                    // Only set to empty if it's still a placeholder and no env var is set
                    options.APIKEY = string.Empty;
                }
                
                var envBaseUrl = Environment.GetEnvironmentVariable("OPENAI_BASE_URL");
                if (!string.IsNullOrEmpty(envBaseUrl))
                    options.BaseUrl = envBaseUrl;
                
                // Validate configuration
                try
                {
                    options.Validate();
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"OpenAI Configuration Error: {ex.Message}. " +
                        "Please check your OpenAI configuration in appsettings.json, User Secrets, or Environment Variables.", ex);
                }
            });

        // Configure Deepgram
        services.Configure<DeepgramOptions>(options =>
        {
            configuration.GetSection(DeepgramOptions.SectionName).Bind(options);
            
            var envApiKey = Environment.GetEnvironmentVariable("DEEPGRAM_API_KEY");
            if (!string.IsNullOrEmpty(envApiKey) || IsPlaceholder(options.ApiKey))
                options.ApiKey = envApiKey ?? string.Empty;
        });

        // Configure ElevenLabs
        services.Configure<ElevenLabsOptions>(options =>
        {
            configuration.GetSection(ElevenLabsOptions.SectionName).Bind(options);
            
            var envApiKey = Environment.GetEnvironmentVariable("ELEVENLABS_API_KEY");
            if (!string.IsNullOrEmpty(envApiKey) || IsPlaceholder(options.ApiKey))
                options.ApiKey = envApiKey ?? string.Empty;
        });

        return services;
    }

    private static bool IsPlaceholder(string? value)
    {
        return string.IsNullOrEmpty(value) || 
               value.Equals("<set in env>", StringComparison.OrdinalIgnoreCase) ||
               (value.StartsWith("<") && value.EndsWith(">"));
    }
}