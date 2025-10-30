using System.Security.Claims;
using SpeakStoreLocate.ApiService.Extensions;
using SpeakStoreLocate.ServiceDefaults;
using Microsoft.AspNetCore.Authentication.JwtBearer;

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

        // Lade erlaubte Google-Audiences aus Konfiguration (mehrere Client-IDs für Angular/Web)
        var googleAudiences = builder.Configuration
            .GetSection("Authentication:Google:ValidAudiences")
            .Get<string[]>() ?? Array.Empty<string>();
        var fallbackAudience = builder.Configuration["Authentication:Google:ClientId"]
                               ?? "10257677510-53t6v51uie2rd1lg4j6tmrojklrjj6mh.apps.googleusercontent.com";

        builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = "Combined";
                options.DefaultChallengeScheme = "Combined";
            })
            .AddPolicyScheme("Combined", "Combined", options =>
            {
                options.ForwardDefaultSelector = context =>
                {
                    var auth = context.Request.Headers["Authorization"].ToString();
                    if (!string.IsNullOrWhiteSpace(auth) && auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        var token = auth.Substring("Bearer ".Length).Trim();
                        // JWT hat 3 Segmente
                        if (token.Split('.').Length == 3) return "Bearer";
                        // sonst als Google Access Token behandeln
                        return "GoogleAccessToken";
                    }
                    return "Bearer";
                };
            })
            .AddJwtBearer("Bearer", o =>
            {
                o.Authority = "https://accounts.google.com"; // OIDC Issuer
                o.MapInboundClaims = false; // behalte JWT-Claim-Namen wie "sub"
                o.IncludeErrorDetails = builder.Environment.IsDevelopment();
                o.TokenValidationParameters = new()
                {
                    ValidIssuers = new[] { "https://accounts.google.com", "accounts.google.com" },
                    ValidAudiences = (googleAudiences.Length > 0 ? googleAudiences : new[] { fallbackAudience }),
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.FromMinutes(2)
                };
                o.RequireHttpsMetadata = true;
            })
            .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, GoogleAccessTokenAuthenticationHandler>(
                "GoogleAccessToken", _ => { });

        builder.Services.AddAuthorization();

        // 7. Web API Services
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        
        // 8. Health Checks for App Runner
        builder.Services.AddHealthChecks();

        var app = builder.Build();

        // Configure the HTTP request pipeline
        app.ConfigurePipeline();
        
        app.MapGet("/me", (ClaimsPrincipal user) =>
        {
            var sub = user.FindFirstValue("sub") ?? user.FindFirstValue("oid");
            return Results.Ok(new { sub });
        }).RequireAuthorization();

        app.Run();
    }
}


// Validiert ein Google Access Token, indem der UserInfo-Endpunkt aufgerufen wird.
// Bei Erfolg wird ein Principal mit mindestens dem Claim "sub" erstellt.

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
        app.UseAuthentication();
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