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
        builder.Services.Configure<DeepgramOptions>(builder.Configuration.GetSection("Deepgram"));
        builder.Services.Configure<ElevenLabsOptions>(builder.Configuration.GetSection("ElevenLabs"));
        builder.Services.Configure<OpenAIOptions>(builder.Configuration.GetSection("OpenAI"));
        builder.Services.Configure<AmazonS3Options>(builder.Configuration.GetSection("AWS:S3"));
        builder.Services.Configure<AmazonTranscribeServiceOptions>(builder.Configuration.GetSection("AWS:Transcribe"));
        builder.Services.Configure<AmazonDynamoDBOptions>(builder.Configuration.GetSection("AWS:DynamoDB"));


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
                policy.WithOrigins(corsOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        builder.AddServiceDefaults();

        builder.Services.Configure<OpenAIOptions>(builder.Configuration.GetSection("OpenAI"));

        builder.Services.AddDefaultAWSOptions(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<AmazonS3LoginOptions>>().Value;

            return new AWSOptions
            {
                Credentials = new BasicAWSCredentials(opts.AccessKey, opts.SecretKey),
                Region = opts.Region != null
                    ? Amazon.RegionEndpoint.GetBySystemName(opts.Region)
                    : throw new InvalidOperationException("AWS Region is not configured!")
            };
        });

        builder.Services.AddAWSService<IAmazonS3>(ServiceLifetime.Singleton);
        builder.Services.AddAWSService<IAmazonTranscribeService>((sp, cfg) =>
        {
            var opts = sp.GetRequiredService<IOptions<AmazonTranscribeServiceOptions>>().Value;
            cfg.Credentials = new BasicAWSCredentials(opts.AccessKey, opts.SecretKey);
            cfg.Region = opts.Region;
        });
        builder.Services.AddAWSService<IAmazonDynamoDB>((sp, cfg) =>
        {
            var opts = sp.GetRequiredService<IOptions<AmazonDynamoDBOptions>>().Value;
            cfg.Credentials = new BasicAWSCredentials(opts.AccessKey, opts.SecretKey);
            cfg.Region = opts.Region;
        });
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

        //builder.Services.AddScoped<ITranscriptionService, ElevenlabsTranscriptionService>();
        builder.Services.AddScoped<ITranscriptionService, DeepgramTranscriptionService>();
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