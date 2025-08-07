using Amazon.S3;
using Amazon.TranscribeService;
using Deepgram.Clients.Interfaces.v1;
using Microsoft.AspNetCore.Mvc;
using SpeakStoreLocate.ApiService.Models;
using SpeakStoreLocate.ApiService.Services.Interpretation;
using SpeakStoreLocate.ApiService.Services.Storage;
using SpeakStoreLocate.ApiService.Services.Transcription;

namespace SpeakStoreLocate.ApiService.Controllers;

[ApiController]
[Route("api/storage")]
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
    public async Task<IEnumerable<StorageItem>> GetItemsAsync()
    {
        return await this._storageRepository.GetStorageItems();
    }


    [HttpPost("upload-audio")]
    public async Task<IActionResult> UploadAudio([FromForm] AudioUploadRequest request)
    {
        var transcriptedText = await transcriptionService.TranscriptAudioAsync(request);

        _logger.LogInformation("Transkript generiert:{transcriptedText}", transcriptedText);

        var commands = await this._interpretationService.InterpretGeschwafelToStructuredCommands(transcriptedText);

        var performedActions = await _storageRepository.PerformActions(commands);

        return Ok(performedActions);
    }
}