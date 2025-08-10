namespace SpeakStoreLocate.ApiService.Extensions;

public static class CorsExtensions
{
    /// <summary>
    /// Configures CORS policies for development and production environments
    /// </summary>
    public static IServiceCollection AddCorsConfiguration(this IServiceCollection services, 
        IConfiguration configuration, 
        IWebHostEnvironment environment)
    {
        var corsOrigins = configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? Array.Empty<string>();

        services.AddCors(options =>
        {
            // Default policy for most use cases
            options.AddPolicy("DefaultCorsPolicy", policy =>
            {
                if (environment.IsDevelopment())
                {
                    // Development: Allow any origin for simplicity
                    policy.AllowAnyOrigin()
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                }
                else
                {
                    // Production: Strict origin whitelist
                    policy.WithOrigins(corsOrigins)
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                }
            });

            // Alternative policy for development with credentials support
            if (environment.IsDevelopment())
            {
                options.AddPolicy("DevelopmentWithCredentials", policy =>
                {
                    policy.SetIsOriginAllowed(origin => IsLocalhost(origin))
                          .AllowCredentials()
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            }
        });

        return services;
    }

    private static bool IsLocalhost(string? origin)
    {
        if (string.IsNullOrWhiteSpace(origin))
            return true;

        try
        {
            var uri = new Uri(origin);
            return uri.Host == "localhost" || 
                   uri.Host == "127.0.0.1" || 
                   uri.Host.Contains("localhost");
        }
        catch
        {
            return true; // Allow in development even if URI parsing fails
        }
    }
}