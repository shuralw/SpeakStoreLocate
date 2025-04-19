using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.S3;
using Amazon.S3.Transfer;
using Amazon.TranscribeService;
using Amazon.TranscribeService.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;

namespace SpeakStoreLocate.ApiService;

[ApiController]
[Route("api/storage")]
public class StorageController : ControllerBase
{
    private readonly AmazonDynamoDBClient _dynamoDbClient;
    private readonly DynamoDBContext _dbContext;
    private readonly AmazonTranscribeServiceClient _transcribeClient;
    private readonly AmazonS3Client _s3Client;
    private readonly ChatClient chatClient;
    private readonly ILogger<StorageController> _logger;
    private const string BucketName = "speech-storage-bucket";

    public StorageController(IConfiguration configuration, OpenAIClient openAiClient,
        IOptions<OpenAIOptions> openAIOptions, ILogger<StorageController> logger)
    {
        _logger = logger;
        chatClient = new ChatClient(
            model: openAIOptions.Value.DefaultModel,
            apiKey: openAIOptions.Value.ApiKey
        );

        var awsOptions = configuration.GetSection("AWS");
        var awsCredentials = new Amazon.Runtime.BasicAWSCredentials(awsOptions["AccessKey"], awsOptions["SecretKey"]);
        var awsRegion = Amazon.RegionEndpoint.GetBySystemName(awsOptions["Region"]);


        var dynamoDbConfig = new AmazonDynamoDBConfig();
        dynamoDbConfig.RegionEndpoint = awsRegion;
        _dynamoDbClient = new AmazonDynamoDBClient(awsCredentials, dynamoDbConfig);


        _dbContext = new DynamoDBContext(_dynamoDbClient);

        var s3Config = new AmazonS3Config { RegionEndpoint = awsRegion };
        _s3Client = new AmazonS3Client(awsCredentials, s3Config);

        var transcribeConfig = new AmazonTranscribeServiceConfig { RegionEndpoint = awsRegion };
        _transcribeClient = new AmazonTranscribeServiceClient(awsCredentials, transcribeConfig);
    }

    [HttpPost("upload-audio")]
    public async Task<IActionResult> UploadAudio([FromForm] AudioUploadRequest request)
    {
        var transcriptedText = await TranscriptAudioAsync(request);

        // Bau den Prompt
        var systemPrompt = @"
            Du bist ein Parser, der aus einem Transkript alle Lager‑Aktionen extrahiert.
            Bestimme für jede Aktion exakt eine der Methoden:
              • GET    – wenn der User nach einem Artikel fragt (z.B. 'wo ist ...', 'suche ...').
              • DELETE – wenn der User etwas entfernt oder entnimmt (z.B. 'nehme ... aus ...', 'entferne ...').
              • POST   – wenn der User einen Artikel neu ablegt oder zusammen mit anderen in einen Ort legt 
                         (z.B. 'lege ... in ...', 'einlagern', 'ablegen', 'in ... tun', 'zusammen mit').
              • PUT    – wenn der User einen Artikel von einem Ort in einen anderen verschiebt 
                         (explizit Ziel und Ursprung genannt und es sich um eine Verschiebung handelt, 
                         z.B. 'verschiebe ... von ... nach ...', 'umlagern').

            Gib die Aktionen als JSON‑Array zurück mit Objekten der Form:
            [
              {
                ""method"":   ""<GET|DELETE|POST|PUT>"",
                ""count"":    <Integer>,
                ""itemName"": ""<Artikelname>"",
                ""location"": ""<Ort>""
              },
              …
            ]
            ";


        _logger.LogInformation("Transkript generiert: {transcriptedText}", transcriptedText);

        var commands = await InterpretGeschwafelToStructuredCommands(systemPrompt, transcriptedText);

        List<string> performedActions = new List<string>();

        // 7) Befehle speichern
        foreach (var cmd in commands)
        {
            if (cmd.Method == METHODS.ENTNAHME)
            {
                var storageItem = await FindStorageItemByName(cmd);

                await _dbContext.DeleteAsync(storageItem);

                _logger.LogInformation(
                    "Das Objekt {cmdItemName} wurde aus dem Lagerort {cmdLocation} erfolgreich entfernt",
                    cmd.ItemName,
                    cmd.Location);

                performedActions.Add(
                    $"Das Objekt {cmd.ItemName} wurde aus dem Lagerort {cmd.Location} erfolgreich entfernt");
            }

            if (cmd.Method == METHODS.SUCHE)
            {
                var storageItem = await FindStorageItemByName(cmd);

                _logger.LogInformation("Das gesuchte Objekt befindet sich in {storageItemLocation}",
                    storageItem.Location);
                performedActions.Add($"Das gesuchte Objekt befindet sich in {storageItem.Location}");
            }

            if (cmd.Method == METHODS.UMLAGERN)
            {
                var storageItem = await FindStorageItemByName(cmd);

                storageItem.Location = cmd.Location;

                await _dbContext.SaveAsync(storageItem);

                _logger.LogInformation(
                    "Umlagerung: {storageItemName} wurde von {storageItemLocation} nach {cmdLocation} umgelagert",
                    storageItem.Name,
                    storageItem.Location,
                    cmd.Location);

                performedActions.Add(
                    $"Umlagerung: {storageItem.Name} wurde von {storageItem.Location} nach {cmd.Location} umgelagert");
            }

            if (cmd.Method == METHODS.EINLAGERN)
            {
                var storageItem = new StorageItem
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = cmd.ItemName,
                    Location = cmd.Location,
                    NormalizedName = cmd.ItemName.NormalizeForSearch()
                };

                await _dbContext.SaveAsync(storageItem);

                _logger.LogInformation("Einlagerung: {storageItemName} wurde in {storageItemLocation} eingelagert",
                    storageItem.Name,
                    storageItem.Location);

                performedActions.Add($"{storageItem.Name} wurde in {storageItem.Location} eingelagert");
            }
        }

        return Ok(string.Join("\n", performedActions));
    }

    private async Task<StorageItem> FindStorageItemByName(StorageCommand cmd)
    {
        var normalizedQuery = cmd.ItemName.NormalizeForSearch();
        var conditions = new List<ScanCondition>
        {
            new ScanCondition("NormalizedName", ScanOperator.Contains, normalizedQuery)
        };

        IEnumerable<StorageItem> results =
            await _dbContext.ScanAsync<StorageItem>(conditions).GetRemainingAsync();

        var storageItem = results.Single();
        return storageItem;
    }

    private async Task<List<StorageCommand>> InterpretGeschwafelToStructuredCommands(
        string systemPrompt,
        string transcriptedText)
    {
        // Kompletten Prompt zusammenfügen und absenden
        string fullPrompt = $"System: {systemPrompt}\n" +
                            $"User: {transcriptedText}";

        _logger.LogDebug("Vollständiger Prompt: {fullPrompt}", fullPrompt);

        ChatCompletion completion =
            await chatClient.CompleteChatAsync(fullPrompt);

        // 5) Reinen JSON‑String extrahieren
        var jsonPayload = completion.Content[0].Text;

        _logger.LogInformation(jsonPayload);

        // 6) In List<StorageCommand> deserialisieren
        var commands = JsonSerializer.Deserialize<List<StorageCommand>>(
            jsonPayload,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        ) ?? new List<StorageCommand>();
        return commands;
    }

    private async Task<string> TranscriptAudioAsync(AudioUploadRequest request)
    {
        var fileTransferUtility = new TransferUtility(_s3Client);
        using (var memoryStream = new MemoryStream())
        {
            await request.AudioFile.CopyToAsync(memoryStream);
            memoryStream.Position = 0;
            await fileTransferUtility.UploadAsync(memoryStream, BucketName, request.AudioFile.FileName);
        }

        var audioFileUri = $"s3://{BucketName}/{request.AudioFile.FileName}";

        // 1) Transkriptionsjob starten
        var startReq = new StartTranscriptionJobRequest
        {
            TranscriptionJobName = "StorageSpeechJob" + Guid.NewGuid(),
            Media = new Media { MediaFileUri = audioFileUri },
            MediaFormat = MediaFormat.Mp3,
            LanguageCode = LanguageCode.DeDE
        };
        var startResp = await _transcribeClient.StartTranscriptionJobAsync(startReq);
        var jobName = startResp.TranscriptionJob.TranscriptionJobName;

        // 2) Polling bis COMPLETED
        GetTranscriptionJobResponse jobResp;
        do
        {
            await Task.Delay(5000);
            jobResp = await _transcribeClient.GetTranscriptionJobAsync(
                new GetTranscriptionJobRequest { TranscriptionJobName = jobName });
        } while (jobResp.TranscriptionJob.TranscriptionJobStatus
                 == TranscriptionJobStatus.IN_PROGRESS);

        if (jobResp.TranscriptionJob.TranscriptionJobStatus != TranscriptionJobStatus.COMPLETED)
            throw new Exception(
                $"Transkriptionsjob fehlgeschlagen. (Job {jobResp.TranscriptionJob.TranscriptionJobName})");

        // 3) JSON abrufen und deserialisieren
        var transcriptUrl = jobResp.TranscriptionJob.Transcript.TranscriptFileUri;
        using var http = new HttpClient();
        var json = await http.GetStringAsync(transcriptUrl);
        var transcriptionResult = JsonSerializer.Deserialize<TranscriptionResponse>(json);
        var transcriptText = transcriptionResult
                                 .Results
                                 .Transcripts[0]
                                 .Text // statt .Text
                             ?? throw new Exception("Kein Transcript.");


        return transcriptText;
    }

    // 1) JSON‑Modelle
    public class TranscriptionResponse
    {
        [JsonPropertyName("results")] public TranscriptionResults Results { get; set; }
    }

    public class TranscriptionResults
    {
        [JsonPropertyName("transcripts")] public Transcript[] Transcripts { get; set; }
    }

    public class Transcript
    {
        // JSON-Feld heißt "transcript", C#‑Property nennen wir "Text"
        [JsonPropertyName("transcript")] public string Text { get; set; }
    }

    public class StorageCommand
    {
        [JsonPropertyName("method")] public string Method { get; set; }

        [JsonPropertyName("count")] public int Count { get; set; }

        [JsonPropertyName("itemName")] public string ItemName { get; set; }

        [JsonPropertyName("location")] public string Location { get; set; }
    }
}

public class METHODS
{
    public const string SUCHE = "GET";
    public const string ENTNAHME = "DELETE";
    public const string EINLAGERN = "POST";
    public const string UMLAGERN = "PUT";
}

public class AudioUploadRequest
{
    public IFormFile AudioFile { get; set; }
}

[DynamoDBTable("StorageItems")]
public class StorageItem
{
    [DynamoDBHashKey] public string Id { get; set; }
    [DynamoDBProperty] public string Name { get; set; }

    [DynamoDBProperty] public string NormalizedName { get; set; }
    [DynamoDBProperty] public string Location { get; set; }
}