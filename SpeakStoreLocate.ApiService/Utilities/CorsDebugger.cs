using Microsoft.AspNetCore.Cors.Infrastructure;

namespace SpeakStoreLocate.ApiService.Utilities;

public static class CorsDebugger
{
    public static void LogCorsConfiguration(ILogger logger, IServiceProvider serviceProvider)
    {
        try
        {
            logger.LogDebug("=== CORS Configuration Debug ===");
            
            var corsService = serviceProvider.GetService<ICorsService>();
            if (corsService != null)
            {
                logger.LogDebug("CORS Service: {CorsServiceType}", corsService.GetType().Name);
            }
            
            var corsPolicyProvider = serviceProvider.GetService<ICorsPolicyProvider>();
            if (corsPolicyProvider != null)
            {
                logger.LogDebug("CORS Policy Provider: {ProviderType}", corsPolicyProvider.GetType().Name);
            }

            // Log environment
            var env = serviceProvider.GetService<IWebHostEnvironment>();
            logger.LogDebug("Environment: {Environment}", env?.EnvironmentName);
            
            logger.LogDebug("=== End CORS Configuration Debug ===");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while debugging CORS configuration");
        }
    }
}