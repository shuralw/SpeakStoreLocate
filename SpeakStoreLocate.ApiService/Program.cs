using System.Configuration;
using SpeakStoreLocate.ApiService.Extensions;
using SpeakStoreLocate.ApiService.Options;
using SpeakStoreLocate.ServiceDefaults;
using SpeakStoreLocate.ApiService.Middleware;

namespace SpeakStoreLocate.ApiService;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        string env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        // 1. Logging Configuration
        builder.AddSerilogLogging();

        builder.Configuration
            .AddJsonFile("appsettings.json", false, true)
            .AddJsonFile($"appsettings.{env}.json", true, true)
            .AddUserSecrets<Program>()
            .AddEnvironmentVariables();

        builder.Services.Configure<OpenAIOptions>(builder.Configuration.GetSection(OpenAIOptions.SectionName));
        // 2. Service Configuration (Options Pattern with Environment Override)
        builder.Services.AddExternalServiceConfiguration(builder.Configuration);
        builder.Services.AddAwsConfiguration(builder.Configuration);

        // Scoped User Context
        builder.Services.AddScoped<IUserContext, UserContext>();

        // 3. CORS Configuration
        builder.Services.AddCorsConfiguration(builder.Configuration, builder.Environment);

        // 4. .NET Aspire Service Defaults
        builder.AddServiceDefaults();

        // 5. AWS Services Registration
        builder.Services.AddAWSServices(builder.Configuration);

        // 6. Application Services Registration
        builder.Services.AddApplicationServices();

        // 7. Web API Services
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        // Configure the HTTP request pipeline
        app.ConfigurePipeline();

        app.Run();
    }
}

/// <summary>
/// Extension methods for configuring the HTTP request pipeline
/// </summary>
public static class PipelineExtensions
{
    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        // 1. Development-specific features
        app.UseDevelopmentDebugging();

        // 2. Request logging (records one event per request)
        app.UseSerilogRequestLogging();

        // 3. CORS (must be before UseHttpsRedirection)
        app.UseCors("DefaultCorsPolicy");

        // 3.1 Global exception handling (logs unhandled exceptions + returns ProblemDetails)
        app.UseMiddleware<ExceptionHandlingMiddleware>();

        // 3.2 Request scope enrichment for all logs (TraceId, Path, Origin, etc.)
        app.UseMiddleware<RequestContextScopeMiddleware>();

        // 4. HTTPS Redirection
        app.UseHttpsRedirection();

        // 5. Authorization
        app.UseAuthorization();

        // 5.5 UserId Header Middleware (erzwingt X-User-Id pro Request, nach CORS und vor Endpoints)
        app.UseMiddleware<UserIdHeaderMiddleware>();

        // 6. .NET Aspire endpoints
        app.MapDefaultEndpoints();

        // 7. API Controllers
        app.MapControllers();

        return app;
    }
}