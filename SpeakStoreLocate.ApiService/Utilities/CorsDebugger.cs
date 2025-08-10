using Microsoft.AspNetCore.Cors.Infrastructure;

namespace SpeakStoreLocate.ApiService.Utilities;

public static class CorsDebugger
{
    public static void LogCorsConfiguration(ILogger logger, IServiceProvider serviceProvider)
    {
        try
        {
            logger.LogInformation("=== CORS Configuration Debug ===");
            
            var corsService = serviceProvider.GetService<ICorsService>();
            if (corsService != null)
            {
                logger.LogInformation("CORS Service: {CorsServiceType}", corsService.GetType().Name);
            }
            
            var corsPolicyProvider = serviceProvider.GetService<ICorsPolicyProvider>();
            if (corsPolicyProvider != null)
            {
                logger.LogInformation("CORS Policy Provider: {ProviderType}", corsPolicyProvider.GetType().Name);
            }

            // Log environment
            var env = serviceProvider.GetService<IWebHostEnvironment>();
            logger.LogInformation("Environment: {Environment}", env?.EnvironmentName);
            
            logger.LogInformation("=== End CORS Configuration Debug ===");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while debugging CORS configuration");
        }
    }
}