using System.ClientModel;
using Amazon.S3;
using Amazon.TranscribeService;
using Microsoft.Extensions.Options;
using OpenAI;
using SpeakStoreLocate.ServiceDefaults;

namespace SpeakStoreLocate.ApiService;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Configuration
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        builder.AddServiceDefaults();

        builder.Services.Configure<OpenAIOptions>(builder.Configuration.GetSection("OpenAI"));

        // var awsCredentials = new Amazon.Runtime.BasicAWSCredentials(awsOptions["AccessKey"], awsOptions["SecretKey"]);
        var awsRegion = Amazon.RegionEndpoint.EUCentral1;
        var awsCredentials =
            new Amazon.Runtime.BasicAWSCredentials("AKIARWPFIBL7FT3N6S33", "2qDDZ+zEXZvVdX4VVx0sKv1eByjp6uNBMVkv6SxG");
        builder.Services.AddSingleton<AmazonS3Client>(sp => new AmazonS3Client(awsCredentials, awsRegion));
        builder.Services.AddSingleton<AmazonTranscribeServiceClient>(sp =>
            new AmazonTranscribeServiceClient(awsCredentials, awsRegion));

        
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

        builder.Services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        app.MapDefaultEndpoints();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();


        app.MapControllers();

        app.Run();
    }
}