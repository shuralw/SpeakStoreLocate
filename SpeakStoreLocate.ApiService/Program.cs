using System.ClientModel;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Extensions.NETCore.Setup;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.TranscribeService;
using Microsoft.Extensions.Options;
using OpenAI;
using Serilog;
using SpeakStoreLocate.ServiceDefaults;

namespace SpeakStoreLocate.ApiService;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

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
                policy.WithOrigins(corsOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        builder.AddServiceDefaults();

        builder.Services.Configure<OpenAIOptions>(builder.Configuration.GetSection("OpenAI"));

        var awsOptions = builder.Configuration.GetAWSOptions("AWS");
// bind your own keys from Configuration:
        awsOptions.Credentials = new BasicAWSCredentials(
            builder.Configuration["AWS:AccessKey"],
            builder.Configuration["AWS:SecretKey"]
        );
        builder.Services.AddDefaultAWSOptions(awsOptions);
        builder.Services.AddAWSService<IAmazonS3>(); // Fügt S3Client mit Default Credentials
        builder.Services.AddAWSService<IAmazonTranscribeService>(); // Fügt TranscribeClient
        builder.Services.AddAWSService<IAmazonDynamoDB>(); // DynamoDBClient
// 4. DynamoDBContext für DataModel-API
        builder.Services.AddScoped<IDynamoDBContext>(sp => new DynamoDBContext(sp.GetRequiredService<IAmazonDynamoDB>())
        );


// 2. OpenAIClient registrieren
        builder.Services.AddSingleton(sp =>
        {
            // OpenAIOptions aus IOptions holen
            var opts = sp.GetRequiredService<IOptions<OpenAIOptions>>().Value;
            if (string.IsNullOrWhiteSpace(opts.ApiKey))
                throw new InvalidOperationException("OpenAI:ApiKey ist nicht konfiguriert!");
            // Hier BaseUrl optional mit übernehmen, falls du es brauchst:
            return new OpenAIClient(new ApiKeyCredential(opts.ApiKey), new OpenAIClientOptions
            {
                Endpoint = new Uri(opts.BaseUrl),
            });
        });

        builder.Services.AddScoped<ITranscriptionService, ElevenlabsTranscriptionService>();
        builder.Services.AddScoped<IStorageRepository, AwsStorageRepository>();
        builder.Services.AddScoped<IChatCompletionService, OpenAiChatCompletionService>();
        builder.Services.AddScoped<IInterpretationService, InterpretationService>();

        builder.Services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        app.UseSerilogRequestLogging();

        app.MapDefaultEndpoints();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseCors("DefaultCorsPolicy");
        app.UseHttpsRedirection();

        app.UseAuthorization();


        app.MapControllers();

        app.Run();
    }
}