using SpeakStoreLocate.ApiService.Extensions;
using SpeakStoreLocate.ServiceDefaults;

namespace SpeakStoreLocate.ApiService;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // 1. Logging Configuration
        builder.AddSerilogLogging();

        // 2. Service Configuration (Options Pattern with Environment Override)
        builder.Services.AddExternalServiceConfiguration(builder.Configuration);
        builder.Services.AddAWSConfiguration(builder.Configuration);

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
        
        // 8. Health Checks for App Runner
        builder.Services.AddHealthChecks();

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

        // 2. Request logging
        app.UseSerilogRequestLogging();

        // 3. CORS (must be before UseHttpsRedirection)
        app.UseCors("DefaultCorsPolicy");

        // 4. HTTPS Redirection (only in Development, App Runner handles HTTPS)
        if (app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }

        // 5. Authorization
        app.UseAuthorization();

        // 6. .NET Aspire endpoints
        app.MapDefaultEndpoints();

        // 7. Health Checks for App Runner
        app.MapHealthChecks("/health");

        // 8. API Controllers
        app.MapControllers();

        return app;
    }
}