using System.ClientModel;
using Microsoft.Extensions.Options;
using OpenAI;
using Serilog;
using SpeakStoreLocate.ApiService.Extensions;
using SpeakStoreLocate.ApiService.Options;
using SpeakStoreLocate.ApiService.Services.ChatCompletion;
using SpeakStoreLocate.ApiService.Services.Interpretation;
using SpeakStoreLocate.ApiService.Services.Storage;
using SpeakStoreLocate.ApiService.Services.Transcription;
using SpeakStoreLocate.ServiceDefaults;

namespace SpeakStoreLocate.ApiService;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        // Configure Options
        builder.Services.Configure<DeepgramOptions>(builder.Configuration.GetSection("Deepgram"));
        builder.Services.Configure<ElevenLabsOptions>(builder.Configuration.GetSection("ElevenLabs"));
        
        // Configure OpenAI with validation
        builder.Services.Configure<OpenAIOptions>(builder.Configuration.GetSection("OpenAI"));
        builder.Services.AddOptionsWithValidateOnStart<OpenAIOptions>()
            .PostConfigure(options => 
            {
                try 
                {
                    options.Validate();
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"OpenAI Configuration Error: {ex.Message}. Please check your OpenAI configuration in appsettings.json or User Secrets.", ex);
                }
            });

        // Add HttpClient services
        builder.Services.AddHttpClient();

        // Serilog-Logger initialisieren    
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateBootstrapLogger();

        builder.Host.UseSerilog((ctx, services, cfg) =>
            cfg.ReadFrom.Configuration(ctx.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File("./logs/general.log", rollingInterval: RollingInterval.Day));

        builder.Services.AddSerilog((services, lc) => lc
            .ReadFrom.Configuration(builder.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File("./logs/general.log"));

        var corsOrigins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? Array.Empty<string>();

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("DefaultCorsPolicy", policy =>
            {
                if (builder.Environment.IsDevelopment())
                {
                    // In Development: Sehr permissive CORS-Policy
                    policy.AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                    
                    // Alternative für Credentials-Support (kann nicht mit AllowAnyOrigin kombiniert werden)
                    // policy.SetIsOriginAllowed(origin => true)
                    //     .AllowCredentials()
                    //     .AllowAnyHeader()
                    //     .AllowAnyMethod();
                }
                else
                {
                    // In Production: Nur explizit erlaubte Origins
                    policy.WithOrigins(corsOrigins)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                }
            });
            
            // Zusätzliche Policy für Development mit Credentials
            options.AddPolicy("DevelopmentWithCredentials", policy =>
            {
                policy.SetIsOriginAllowed(origin => 
                    {
                        // Handle null or empty origins
                        if (string.IsNullOrWhiteSpace(origin))
                            return true;
                        
                        try 
                        {
                            var uri = new Uri(origin);
                            return uri.Host == "localhost" || uri.Host == "127.0.0.1" || uri.Host.Contains("localhost");
                        }
                        catch
                        {
                            return true; // Allow in development even if URI parsing fails
                        }
                    })
                    .AllowCredentials()
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        builder.AddServiceDefaults();

        // Add AWS Services with proper configuration
        builder.Services.AddAWSServices(builder.Configuration);

        // OpenAI Client registrieren
        builder.Services.AddSingleton(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<OpenAIOptions>>().Value;
            if (string.IsNullOrWhiteSpace(opts.ApiKey))
                throw new InvalidOperationException("OpenAI:ApiKey ist nicht konfiguriert!");
            
            // Validate and ensure BaseUrl is a valid URI
            var baseUrl = string.IsNullOrWhiteSpace(opts.BaseUrl) ? "https://api.openai.com" : opts.BaseUrl;
            
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var validUri))
            {
                throw new InvalidOperationException($"OpenAI BaseUrl '{baseUrl}' is not a valid URI. Please check your configuration.");
            }
            
            return new OpenAIClient(new ApiKeyCredential(opts.ApiKey), new OpenAIClientOptions
            {
                Endpoint = validUri,
            });
        });

        // Register application services
        builder.Services.AddScoped<ITranscriptionService, DeepgramTranscriptionService>();
        builder.Services.AddScoped<IStorageRepository, AwsStorageRepository>();
        builder.Services.AddScoped<IChatCompletionService, OpenAiChatCompletionService>();
        builder.Services.AddScoped<IInterpretationService, InterpretationService>();

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
            
            // Add CORS debugging middleware in development
            app.UseMiddleware<SpeakStoreLocate.ApiService.Middleware.CorsDebuggingMiddleware>();
        }

        app.UseSerilogRequestLogging();
        
        // CORS MUST be before UseHttpsRedirection and UseAuthorization
        app.UseCors("DefaultCorsPolicy");
        
        app.UseHttpsRedirection();
        app.UseAuthorization();
        
        app.MapDefaultEndpoints();
        app.MapControllers();

        // Debug Configuration in Development
        if (app.Environment.IsDevelopment())
        {
            using var scope = app.Services.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            SpeakStoreLocate.ApiService.Utilities.ConfigurationDebugger.LogAWSConfiguration(logger, scope.ServiceProvider);
            SpeakStoreLocate.ApiService.Utilities.ConfigurationDebugger.LogOpenAIConfiguration(logger, scope.ServiceProvider);
            SpeakStoreLocate.ApiService.Utilities.CorsDebugger.LogCorsConfiguration(logger, scope.ServiceProvider);
        }

        app.Run();
    }
}