using System.Diagnostics;
using Serilog;
using Serilog.Events;
using SpeakStoreLocate.ApiService.Options;

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
            .Enrich.WithProperty("Application", "SpeakStoreLocate.ApiService")
            .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
            .CreateBootstrapLogger();

        // Configure Serilog for the host
        builder.Host.UseSerilog((context, services, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", "SpeakStoreLocate.ApiService")
                .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName);

            // Optional file logging (primarily intended for Production when needed)
            var loggingOptions = context.Configuration.GetSection("Logging").Get<LoggingOptions>();
            if (loggingOptions?.File?.Enabled == true)
            {
                if (Enum.TryParse<RollingInterval>(loggingOptions.File.RollingInterval, ignoreCase: true, out var rollingInterval) == false)
                {
                    rollingInterval = RollingInterval.Day;
                }

                configuration.WriteTo.File(
                    path: loggingOptions.File.Path,
                    rollingInterval: rollingInterval,
                    retainedFileCountLimit: loggingOptions.File.RetainedFileCountLimit,
                    outputTemplate: loggingOptions.File.OutputTemplate);
            }
        });

        return builder;
    }
    
    /// <summary>
    /// Adds Serilog request logging to the pipeline
    /// </summary>
    public static WebApplication UseSerilogRequestLogging(this WebApplication app)
    {
        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate =
                "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";

            options.GetLevel = (httpContext, elapsed, ex) =>
            {
                if (ex != null)
                {
                    return LogEventLevel.Error;
                }

                var statusCode = httpContext.Response.StatusCode;
                if (statusCode >= 500)
                {
                    return LogEventLevel.Error;
                }

                if (statusCode >= 400)
                {
                    return LogEventLevel.Warning;
                }

                if (elapsed > 2_000)
                {
                    return LogEventLevel.Warning;
                }

                return LogEventLevel.Information;
            };

            options.EnrichDiagnosticContext = (diag, ctx) =>
            {
                // Correlation / tracing
                diag.Set("TraceIdentifier", ctx.TraceIdentifier);
                diag.Set("TraceId", Activity.Current?.TraceId.ToString());
                diag.Set("SpanId", Activity.Current?.SpanId.ToString());

                // Request basics
                diag.Set("Scheme", ctx.Request.Scheme);
                diag.Set("Host", ctx.Request.Host.Value);
                diag.Set("QueryString", ctx.Request.QueryString.Value);

                // User context (header is enforced by middleware for most endpoints)
                var userId = ctx.Request.Headers["X-User-Id"].ToString();
                if (!string.IsNullOrWhiteSpace(userId))
                {
                    diag.Set("UserId", userId);
                }

                // CORS / client hints
                diag.Set("Origin", ctx.Request.Headers.Origin.ToString());
                diag.Set("Referer", ctx.Request.Headers.Referer.ToString());
                diag.Set("UserAgent", ctx.Request.Headers.UserAgent.ToString());

                // Network
                diag.Set("RemoteIp", ctx.Connection.RemoteIpAddress?.ToString());

                // Payload metadata (never log bodies)
                diag.Set("RequestContentType", ctx.Request.ContentType);
                diag.Set("RequestContentLength", ctx.Request.ContentLength);
            };
        });
        return app;
    }
}