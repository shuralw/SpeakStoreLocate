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
using Deepgram;
using Deepgram.Clients.Interfaces.v1;
using Deepgram.Models.PreRecorded.v1;

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
    private readonly IListenRESTClient deepgramClient;
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

        // Set "DEEPGRAM_API_KEY" environment variable to your Deepgram API Key
        this.deepgramClient = ClientFactory.CreateListenRESTClient();
    }

    [HttpGet]
    public async Task<IEnumerable<StorageItem>> GetItemsAsync()
    {
        var conditions = new List<ScanCondition>();
        var results = await _dbContext.ScanAsync<StorageItem>(conditions).GetRemainingAsync();

        return results;
    }

    [HttpPost("upload-audio")]
    public async Task<IActionResult> UploadAudio([FromForm] AudioUploadRequest request)
    {
        var transcriptedText = await TranscriptAudioAsync(request);

        // Bau den Prompt
        var systemPrompt = @"
            Du bist ein Parser, der aus einem deutschsprachigen Transkript alle Lager‑Aktionen extrahiert.  
            Erzeuge **striktes** JSON (keine Freitext‑Antwort!), und zwar ein Array von Objekten mit genau diesen vier Feldern:

            • method        – eine der Zeichenketten ""GET"", ""DELETE"", ""POST"" oder ""PUT""  
            • count         – eine Ganzzahl (1,2,3…)  
            • itemName      – der exakte Artikelname (inkl. Groß‑/Kleinschreibung wie im Transkript)  
            • source        – die Quelllokation (optional, nur bei PUT mandatory) 
            • destination   – die Ziellokation (optional - bleibt leer bei GET)  

            **Regeln für die Methodenwahl:**  
            1. **PUT**  nur wenn **im selben Satz**  
                - eine **Quelllokation** (z.B. „von Regal A“)  
                - **und** eine **Ziellokation** (z.B. „nach Regal B“) explizit genannt werden.  
            2. **DELETE** wenn der User etwas „entnimmt“, „ausschüttet“, „herausnimmt“ o. Ä.  
            3. **GET**    wenn der User nach dem Ort fragt („wo ist…“, „suche…“).  
            4. **POST**  in **allen anderen Fällen**, also  
                - „einlagern“, „ablegen“, „in … tun“, „hängen“, „befestigen“, „stellen“,  
                - oder wenn nur eine Lokation angegeben ist ohne Quelle.  

            **Weiteres Optimierungspotenzial:**  
                - Füge bei PUT‑Befehlen das Feld `""source""` hinzu, um die Quell‑Lokation zu speichern.  
                - Gib immer `""count"": 1`, wenn keine Zahl genannt wird.  
                - Ersetze ausgeschriebene Zahlen („drei“) durch Ziffern (3).  
                - Normalisiere Leer‑ und Sonderzeichen (Trim, keine führenden/trailenden Leerzeichen).  
                - Wenn ein Satz kein valides Kommando enthält, ignoriere ihn schlicht.  
                - Falls du offensichtliche Rechtschreibfehler erkennst, die der Transkriptor erzeugt haben könnte, korrigiere diese. Das kommt aber relativ selten vor.
                - Filler‑Wörter: Entferne Artikel (der, die, das) und Füllwörter, aber nur soweit, dass der eigentliche Artikelname klar bleibt.    

            **Beispiel-Ausgabe** für deinen Text:
                >„Und das Fahrrad wird an der Wand aufgehangen.“
                [
                    {
                        ""method"":   ""POST"",
                        ""count"":    1,
                        ""itemName"": ""Fahrrad"",
                        ""destination"": ""Wand""
                    }
                ]
            Und für
                >„Verschiebe die Lampe von Regal A nach Regal B.“
                [
                    {
                        ""method"":   ""PUT"",
                        ""count"":    1,
                        ""itemName"": ""Lampe"",
                        ""source"":   ""Regal A"",
                        ""destination"": ""Regal B""
                    }
                ]";


        _logger.LogInformation("Transkript generiert:{transcriptedText}", transcriptedText);

        var commands = await InterpretGeschwafelToStructuredCommands(systemPrompt, transcriptedText);

        List<string> performedActions = new List<string>();

        // 7) Befehle speichern
        foreach (var cmd in commands)
        {
            if (cmd.Method == METHODS.ENTNAHME)
            {
                var storageItem = await FindStorageItemByNameAndLocation(cmd);

                await _dbContext.DeleteAsync(storageItem);

                _logger.LogInformation(
                    "Das Objekt {cmdItemName} wurde aus dem Lagerort {cmdDestination} erfolgreich entfernt",
                    cmd.ItemName,
                    cmd.Destination);

                performedActions.Add(
                    $"Das Objekt {cmd.ItemName} wurde aus dem Lagerort {cmd.Destination} erfolgreich entfernt");
            }

            if (cmd.Method == METHODS.SUCHE)
            {
                var storageItem = await FindStorageItemByNameAndLocation(cmd);

                _logger.LogInformation("{itemName} befindet sich hier: {storageItemDestination}",
                    cmd.ItemName,
                    storageItem.Location);
                performedActions.Add($"{cmd.ItemName} befindet sich hier: {storageItem.Location}");
            }

            if (cmd.Method == METHODS.UMLAGERN)
            {
                var storageItem = await FindStorageItemByNameAndLocation(cmd);

                storageItem.Location = cmd.Destination;

                await _dbContext.SaveAsync(storageItem);

                _logger.LogInformation(
                    "Umlagerung: {storageItemName} wurde von {storageItemDestination} nach {cmdDestination} umgelagert",
                    storageItem.Name,
                    storageItem.Location,
                    cmd.Destination);

                performedActions.Add(
                    $"Umlagerung: {storageItem.Name} wurde von {storageItem.Location} nach {cmd.Destination} umgelagert");
            }

            if (cmd.Method == METHODS.EINLAGERN)
            {
                var storageItem = new StorageItem
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = cmd.ItemName,
                    Location = cmd.Destination,
                    NormalizedName = cmd.ItemName.NormalizeForSearch()
                };

                await _dbContext.SaveAsync(storageItem);

                _logger.LogInformation("Einlagerung: {storageItemName} wurde in {storageItemDestination} eingelagert",
                    storageItem.Name,
                    storageItem.Location);

                performedActions.Add($"{storageItem.Name} wurde in {storageItem.Location} eingelagert");
            }
        }

        _logger.LogDebug("Anzahl an Ergebnissen: {performedActions}", performedActions.Count().ToString());
        string results = string.Join("\n", performedActions);
        _logger.LogDebug("Ergebnisse: {results}", results);

        return Ok(results);
    }

    private async Task<StorageItem> FindStorageItemByNameAndLocation(StorageCommand cmd)
    {
        var normalizedQuery = cmd.ItemName.NormalizeForSearch();
        _logger.LogDebug("itemname: {itemname}", cmd.ItemName);
        _logger.LogDebug("itemname: {itemname}", cmd.Destination);
        _logger.LogDebug("normalizedQuery: {normalizedQuery}", normalizedQuery);
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

        var response = await deepgramClient.TranscribeUrl(
            new UrlSource(audioFileUri),
            new PreRecordedSchema()
            {
                Model = "nova-3",
                Punctuate = true,
                Language = "de",
            });

        // 1. Für jeden Kanal die beste Alternative herausholen
        var bestTranscriptionPerChannel = response
            .Results
            .Channels
            .Select(channel =>
                channel.Alternatives
                    .OrderByDescending(alt => alt.Confidence)
                    .First()
                    .Transcript
            );

        // 2. Alle Kanal-Transkripte zu einem Gesamtstring verbinden
        string transcriptText = string.Join(" ", bestTranscriptionPerChannel);


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

        [JsonPropertyName("destination")] public string Destination { get; set; }
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