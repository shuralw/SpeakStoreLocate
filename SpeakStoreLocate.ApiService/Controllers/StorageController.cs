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
    private readonly ITranscriptionImprover _transcriptionImprover;


    public StorageController(IConfiguration configuration,
        ITranscriptionService transcriptionService,
        IStorageRepository storageRepository,
        IInterpretationService interpretationService,
        ITranscriptionImprover transcriptionImprover,
        ILogger<StorageController> logger)
    {
        _logger = logger;

        this.transcriptionService = transcriptionService;
        _storageRepository = storageRepository;
        _interpretationService = interpretationService;
        _transcriptionImprover = transcriptionImprover;
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

        // Edge Case Validierungen
        if (request?.AudioFile == null)
        {
            _logger.LogWarning("Audio upload failed: No file provided");
            return BadRequest(new { error = "No audio file provided" });
        }

        if (request.AudioFile.Length == 0)
        {
            _logger.LogWarning("Audio upload failed: Empty file provided (Length: 0)");
            return BadRequest(new { error = "Audio file is empty" });
        }

        // Prüfe auf gültige Audio-Dateiformate
        var allowedContentTypes = new[]
        {
            "audio/mpeg", "audio/mp3", "audio/wav", "audio/m4a", 
            "audio/aac", "audio/ogg", "audio/webm", "audio/flac"
        };
        
        if (!allowedContentTypes.Contains(request.AudioFile.ContentType?.ToLowerInvariant()))
        {
            _logger.LogWarning("Audio upload failed: Invalid content type {ContentType}", request.AudioFile.ContentType);
            return BadRequest(new { error = $"Unsupported audio format. Allowed formats: {string.Join(", ", allowedContentTypes)}" });
        }

        // Prüfe Dateigröße (z.B. max 50MB)
        const long maxFileSize = 50 * 1024 * 1024; // 50MB
        if (request.AudioFile.Length > maxFileSize)
        {
            _logger.LogWarning("Audio upload failed: File too large ({Size} bytes)", request.AudioFile.Length);
            return BadRequest(new { error = $"File too large. Maximum size allowed: {maxFileSize / (1024 * 1024)}MB" });
        }

        try
        {
            var transcriptedText = await transcriptionService.TranscriptAudioAsync(request);
            
            var improvedTranscriptedText = await _transcriptionImprover.ImproveTranscriptedText(transcriptedText);

            var commands = await this._interpretationService.InterpretGeschwafelToStructuredCommands(improvedTranscriptedText);

            var performedActions = await _storageRepository.PerformActions(commands);

            return Ok(performedActions);
        }
        catch (ArgumentException ex)
        {
            // Edge Cases aus InterpretationService: leere/bedeutungslose Transkription oder keine gültigen Kommandos
            _logger.LogWarning(ex, "Invalid transcription or no valid commands found");
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            // AI Service Probleme (ungültiges JSON Response)
            _logger.LogError(ex, "AI service error during interpretation");
            return StatusCode(500, new { error = "Error interpreting audio content" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing audio upload");
            return StatusCode(500, new { error = "Internal server error while processing audio" });
        }
    }
}