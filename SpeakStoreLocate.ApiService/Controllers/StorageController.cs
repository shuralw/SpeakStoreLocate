using Amazon.S3;
using Amazon.TranscribeService;
using Deepgram.Clients.Interfaces.v1;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using SpeakStoreLocate.ApiService.Models;
using SpeakStoreLocate.ApiService.Services.Interpretation;
using SpeakStoreLocate.ApiService.Services.Storage;
using SpeakStoreLocate.ApiService.Services.Transcription;

namespace SpeakStoreLocate.ApiService.Controllers;

[ApiController]
[Route("api/storage")]
[EnableCors("DefaultCorsPolicy")]
public class StorageController : ControllerBase
{
    private readonly AmazonTranscribeServiceClient _transcribeClient;
    private readonly AmazonS3Client _s3Client;
    private readonly ILogger<StorageController> _logger;
    private readonly IListenRESTClient deepgramClient;
    private readonly ITranscriptionService transcriptionService;
    private readonly IStorageRepository _storageRepository;
    private readonly IInterpretationService _interpretationService;


    public StorageController(IConfiguration configuration,
        ITranscriptionService transcriptionService,
        IStorageRepository storageRepository,
        IInterpretationService interpretationService,
        ILogger<StorageController> logger)
    {
        _logger = logger;

        this.transcriptionService = transcriptionService;
        _storageRepository = storageRepository;
        _interpretationService = interpretationService;
    }

    [HttpGet]
    [EnableCors("DefaultCorsPolicy")]
    public async Task<IEnumerable<StorageItem>> GetItemsAsync()
    {
        var origin = Request.Headers.Origin.FirstOrDefault();
        var userAgent = Request.Headers.UserAgent.FirstOrDefault();
        var referer = Request.Headers.Referer.FirstOrDefault();

        _logger.LogInformation("GetItemsAsync called:");
        _logger.LogInformation("  Origin: {Origin}", origin ?? "(null)");
        _logger.LogInformation("  Referer: {Referer}", referer ?? "(null)");
        _logger.LogInformation("  User-Agent: {UserAgent}", userAgent ?? "(null)");

        // Manually add CORS headers if needed
        if (!string.IsNullOrEmpty(origin))
        {
            Response.Headers.Add("Access-Control-Allow-Origin", origin);
        }
        else
        {
            // If no origin header, allow all for development
            Response.Headers.Add("Access-Control-Allow-Origin", "*");
        }

        Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
        Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization");

        return await this._storageRepository.GetStorageItems();
    }

    [HttpOptions]
    [EnableCors("DefaultCorsPolicy")]
    public IActionResult PreflightOptionsRequest()
    {
        var origin = Request.Headers.Origin.FirstOrDefault();
        _logger.LogInformation("OPTIONS (Preflight) request from origin: {Origin}", origin ?? "(null)");

        // Manually set CORS headers for preflight
        Response.Headers.Add("Access-Control-Allow-Origin", origin ?? "*");
        Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
        Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization, X-Requested-With");
        Response.Headers.Add("Access-Control-Max-Age", "3600");

        return Ok();
    }

    [HttpPost("upload-audio")]
    [EnableCors("DefaultCorsPolicy")]
    public async Task<IActionResult> UploadAudio([FromForm] AudioUploadRequest request)
    {
        var origin = Request.Headers.Origin.FirstOrDefault();
        _logger.LogInformation("UploadAudio called from origin: {Origin}", origin ?? "(null)");

        var transcriptedText = await transcriptionService.TranscriptAudioAsync(request);

        _logger.LogInformation("Transkript generiert:{transcriptedText}", transcriptedText);

        var existingLocations = (await this._storageRepository.GetStorageLocations());
        var commands =
            await this._interpretationService.InterpretGeschwafelToStructuredCommands(
                transcriptedText,
                existingLocations);

        var performedActions = await _storageRepository.PerformActions(commands);

        return Ok(performedActions);
    }
}