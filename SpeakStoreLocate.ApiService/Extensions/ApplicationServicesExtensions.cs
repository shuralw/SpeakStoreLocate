using System.ClientModel;
using Microsoft.Extensions.Options;
using OpenAI;
using SpeakStoreLocate.ApiService.Options;
using SpeakStoreLocate.ApiService.Services.ChatCompletion;
using SpeakStoreLocate.ApiService.Services.Interpretation;
using SpeakStoreLocate.ApiService.Services.Storage;
using SpeakStoreLocate.ApiService.Services.Transcription;

namespace SpeakStoreLocate.ApiService.Extensions;

public static class ApplicationServicesExtensions
{
    /// <summary>
    /// Registers all application services and external clients
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // External clients
        services.AddHttpClient();
        
        // OpenAI Client with proper configuration
        services.AddSingleton(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<OpenAIOptions>>().Value;
            
            if (string.IsNullOrWhiteSpace(options.APIKEY))
                throw new InvalidOperationException("OpenAI:ApiKey is not configured!");

            // Validate and ensure BaseUrl is a valid URI
            var baseUrl = string.IsNullOrWhiteSpace(options.BaseUrl) ? "https://api.openai.com" : options.BaseUrl;

            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var validUri))
            {
                throw new InvalidOperationException(
                    $"OpenAI BaseUrl '{baseUrl}' is not a valid URI. Please check your configuration.");
            }

            return new OpenAIClient(new ApiKeyCredential(options.APIKEY), new OpenAIClientOptions
            {
                Endpoint = validUri,
            });
        });

        // Application services
        services.AddScoped<ITranscriptionService, DeepgramTranscriptionService>();
        services.AddScoped<IStorageRepository, AwsStorageRepository>();
        services.AddScoped<IChatCompletionService, OpenAiChatCompletionService>();
        services.AddScoped<IInterpretationService, InterpretationService>();

        return services;
    }
}