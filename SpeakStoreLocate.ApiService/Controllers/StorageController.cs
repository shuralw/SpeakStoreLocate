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
            Response.Headers["Access-Control-Allow-Origin"] = origin;
        }
        else
        {
            // If no origin header, allow all for development
            Response.Headers["Access-Control-Allow-Origin"] = "*";
        }

        Response.Headers["Access-Control-Allow-Methods"] = "GET, POST, PUT, DELETE, OPTIONS";
        Response.Headers["Access-Control-Allow-Headers"] = "Content-Type, Authorization, X-User-Id";

        return await this._storageRepository.GetStorageItems();
    }

    [HttpOptions]
    [EnableCors("DefaultCorsPolicy")]
    public IActionResult PreflightOptionsRequest()
    {
        var origin = Request.Headers.Origin.FirstOrDefault();
        _logger.LogInformation("OPTIONS (Preflight) request from origin: {Origin}", origin ?? "(null)");

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

    public class UpdateStorageItemRequest
    {
        public string? Name { get; set; }
        public string? Location { get; set; }
    }

    [HttpPut("{id}")]
    [EnableCors("DefaultCorsPolicy")]
    public async Task<IActionResult> UpdateItem([FromRoute][Required] string id, [FromBody] UpdateStorageItemRequest request)
    {
        if (request == null)
        {
            return BadRequest("Invalid request body");
        }

        if (string.IsNullOrWhiteSpace(request.Name) && string.IsNullOrWhiteSpace(request.Location))
        {
            return BadRequest("No fields to update");
        }

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

        return Ok(updated);
    }
}