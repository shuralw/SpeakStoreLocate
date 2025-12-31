using Amazon.S3;
using Amazon.TranscribeService;
using Deepgram.Clients.Interfaces.v1;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using SpeakStoreLocate.ApiService.Models;
using SpeakStoreLocate.ApiService.Services.Interpretation;
using SpeakStoreLocate.ApiService.Services.Storage;
using SpeakStoreLocate.ApiService.Services.Transcription;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using SpeakStoreLocate.ApiService.Utilities;

namespace SpeakStoreLocate.ApiService.Controllers;

[ApiController]
[Route("api/storage")]
[EnableCors("DefaultCorsPolicy")]
public class StorageController : ControllerBase
{
    private readonly ILogger<StorageController> _logger;
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
        var sw = Stopwatch.StartNew();
        var origin = Request.Headers.Origin.FirstOrDefault();
        var userAgent = Request.Headers.UserAgent.FirstOrDefault();
        var referer = Request.Headers.Referer.FirstOrDefault();

        _logger.LogInformation("GetItemsAsync called");
        _logger.LogDebug("Request headers. Origin={Origin} Referer={Referer} UserAgent={UserAgent}",
            origin ?? "(null)",
            referer ?? "(null)",
            userAgent ?? "(null)");

        // Manually add CORS headers if needed
        if (!string.IsNullOrEmpty(origin))
        {
            Response.Headers["Access-Control-Allow-Origin"] = origin;
        }
        else
        {
            // If no origin header, allow all for development
            Response.Headers["Access-Control-Allow-Origin"] = "*";
        }

        Response.Headers["Access-Control-Allow-Methods"] = "GET, POST, PUT, DELETE, OPTIONS";
        Response.Headers["Access-Control-Allow-Headers"] = "Content-Type, Authorization, X-User-Id";

        var items = (await this._storageRepository.GetStorageItems()).ToList();
        _logger.LogInformation("GetItemsAsync completed. Count={Count} ElapsedMs={ElapsedMs}", items.Count, sw.ElapsedMilliseconds);
        return items;
    }

    [HttpOptions]
    [EnableCors("DefaultCorsPolicy")]
    public IActionResult PreflightOptionsRequest()
    {
        var origin = Request.Headers.Origin.FirstOrDefault();
        _logger.LogDebug("OPTIONS (Preflight) request. Origin={Origin}", origin ?? "(null)");

        // Manually set CORS headers for preflight
    Response.Headers["Access-Control-Allow-Origin"] = origin ?? "*";
    Response.Headers["Access-Control-Allow-Methods"] = "GET, POST, PUT, DELETE, OPTIONS";
    Response.Headers["Access-Control-Allow-Headers"] = "Content-Type, Authorization, X-Requested-With, X-User-Id";
    Response.Headers["Access-Control-Max-Age"] = "3600";

        return Ok();
    }

    [HttpPost("upload-audio")]
    [EnableCors("DefaultCorsPolicy")]
    public async Task<IActionResult> UploadAudio([FromForm] AudioUploadRequest request)
    {
        var sw = Stopwatch.StartNew();
        var origin = Request.Headers.Origin.FirstOrDefault();
        _logger.LogInformation("UploadAudio called");
        _logger.LogDebug("UploadAudio request meta. Origin={Origin} ContentType={ContentType} FileName={FileName} FileLength={FileLength}",
            origin ?? "(null)",
            request.AudioFile?.ContentType,
            request.AudioFile?.FileName,
            request.AudioFile?.Length);

        var transcriptedText = await transcriptionService.TranscriptAudioAsync(request);

        _logger.LogInformation("Transcription completed. TranscriptLength={TranscriptLength}", LoggingSanitizer.SafeLength(transcriptedText));

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            // Debug-only: transcript text can be large/sensitive
            const int maxDebugTranscriptLength = 20000;
            var truncated = LoggingSanitizer.Truncate(transcriptedText, maxDebugTranscriptLength);
            var suffix = transcriptedText != null && transcriptedText.Length > maxDebugTranscriptLength ? "…(truncated)" : string.Empty;
            _logger.LogDebug("Transcription text (debug). Transcript={Transcript}{Suffix}", truncated, suffix);
        }

        var existingLocations = (await this._storageRepository.GetStorageLocations());
        var existingLocationsList = existingLocations as ICollection<string> ?? existingLocations.ToList();
        _logger.LogDebug("Existing locations loaded. Count={Count}", existingLocationsList.Count);

        var commands =
            await this._interpretationService.InterpretGeschwafelToStructuredCommands(
                transcriptedText,
                existingLocationsList);

        _logger.LogInformation("Interpretation completed. CommandCount={CommandCount}", commands?.Count ?? 0);

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            // Debug-only: show parsed commands as JSON
            var commandJson = System.Text.Json.JsonSerializer.Serialize(
                commands,
                new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                });

            const int maxDebugCommandsLength = 20000;
            var truncated = LoggingSanitizer.Truncate(commandJson, maxDebugCommandsLength);
            var suffix = commandJson.Length > maxDebugCommandsLength ? "…(truncated)" : string.Empty;
            _logger.LogDebug("Commands JSON (debug). CommandsJson={CommandsJson}{Suffix}", truncated, suffix);
        }

        var performedActions = await _storageRepository.PerformActions(commands);

        _logger.LogInformation("UploadAudio completed. ActionCount={ActionCount} ElapsedMs={ElapsedMs}",
            performedActions?.Count ?? 0,
            sw.ElapsedMilliseconds);

        return Ok(performedActions);
    }

    public class UpdateStorageItemRequest
    {
        public string? Name { get; set; }
        public string? Location { get; set; }
    }

    [HttpPut("{id}")]
    [EnableCors("DefaultCorsPolicy")]
    public async Task<IActionResult> UpdateItem([FromRoute][Required] string id, [FromBody] UpdateStorageItemRequest request)
    {
        var sw = Stopwatch.StartNew();
        if (request == null)
        {
            return BadRequest("Invalid request body");
        }

        if (string.IsNullOrWhiteSpace(request.Name) && string.IsNullOrWhiteSpace(request.Location))
        {
            return BadRequest("No fields to update");
        }

        _logger.LogInformation("UpdateItem called. ItemId={ItemId} UpdateName={UpdateName} UpdateLocation={UpdateLocation}",
            id,
            !string.IsNullOrWhiteSpace(request.Name),
            !string.IsNullOrWhiteSpace(request.Location));

        var updated = await _storageRepository.UpdateStorageItemAsync(id, request.Name, request.Location);
        if (updated == null)
        {
            return NotFound();
        }

        // CORS headers similar to GET
        var origin = Request.Headers.Origin.FirstOrDefault();
    Response.Headers["Access-Control-Allow-Origin"] = origin ?? "*";
    Response.Headers["Access-Control-Allow-Methods"] = "GET, POST, PUT, DELETE, OPTIONS";
    Response.Headers["Access-Control-Allow-Headers"] = "Content-Type, Authorization, X-User-Id";

        _logger.LogInformation("UpdateItem completed. ItemId={ItemId} ElapsedMs={ElapsedMs}", id, sw.ElapsedMilliseconds);
        return Ok(updated);
    }
}