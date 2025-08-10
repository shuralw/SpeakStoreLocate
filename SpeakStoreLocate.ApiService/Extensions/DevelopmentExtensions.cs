namespace SpeakStoreLocate.ApiService.Extensions;

public static class DevelopmentExtensions
{
    /// <summary>
    /// Adds development-specific debugging and middleware
    /// </summary>
    public static WebApplication UseDevelopmentDebugging(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
            return app;

        // Swagger
        app.UseSwagger();
        app.UseSwaggerUI();

        // CORS debugging middleware
        app.UseMiddleware<SpeakStoreLocate.ApiService.Middleware.CorsDebuggingMiddleware>();

        // Log configuration on startup
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        LogStartupConfiguration(logger, scope.ServiceProvider, app.Environment);

        return app;
    }

    private static void LogStartupConfiguration(ILogger logger, IServiceProvider serviceProvider, IWebHostEnvironment environment)
    {
        logger.LogDebug("=== Startup Configuration Debug ===");
        logger.LogDebug("Environment: {Environment}", environment.EnvironmentName);
        
        try
        {
            // Log AWS Configuration
            SpeakStoreLocate.ApiService.Utilities.ConfigurationDebugger.LogAWSConfiguration(logger, serviceProvider);
            
            // Log OpenAI Configuration  
            SpeakStoreLocate.ApiService.Utilities.ConfigurationDebugger.LogOpenAIConfiguration(logger, serviceProvider);
            
            // Log CORS Configuration
            SpeakStoreLocate.ApiService.Utilities.CorsDebugger.LogCorsConfiguration(logger, serviceProvider);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error during configuration debugging");
        }
        
        logger.LogDebug("=== End Startup Configuration Debug ===");
    }
}