using Serilog;

namespace SpeakStoreLocate.ApiService.Extensions;

public static class SerilogExtensions
{
    /// <summary>
    /// Configures Serilog with consistent settings for console and file logging
    /// </summary>
    public static WebApplicationBuilder AddSerilogLogging(this WebApplicationBuilder builder)
    {
        // Create bootstrap logger for early startup logging
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateBootstrapLogger();

        // Configure Serilog for the host
        builder.Host.UseSerilog((context, services, configuration) =>
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File("./logs/general.log", 
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 14));

        return builder;
    }
    
    /// <summary>
    /// Adds Serilog request logging to the pipeline
    /// </summary>
    public static WebApplication UseSerilogRequestLogging(this WebApplication app)
    {
        app.UseSerilogRequestLogging();
        return app;
    }
}