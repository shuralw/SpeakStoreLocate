using Amazon.S3;
using Deepgram;
using Deepgram.Logger;
using Deepgram.Clients.Interfaces.v1;
using Deepgram.Models.Listen.v1.REST;
using Microsoft.Extensions.Options;
using SpeakStoreLocate.ApiService.Models;
using SpeakStoreLocate.ApiService.Options;
using LogLevel = Deepgram.Logger.LogLevel;

namespace SpeakStoreLocate.ApiService.Services.Transcription;

public class DeepgramTranscriptionService : ITranscriptionService
{
    private readonly IListenRESTClient _deepgramClient;
    private readonly IAmazonS3 _s3Client;
    private readonly AmazonS3Options _s3Options;
    private readonly ILogger<DeepgramTranscriptionService> _logger;

    public DeepgramTranscriptionService(
        IOptions<DeepgramOptions> deepgramOptions,
        IAmazonS3 s3Client,
        IOptions<AmazonS3Options> s3Options,
        ILogger<DeepgramTranscriptionService> logger)
    {
        // Deaktiviere Deepgram's internes Logging komplett
        Library.Initialize(LogLevel.Warning); // for real deepgram ?! Frigging hell, why is this so complicated?
        _deepgramClient = ClientFactory.CreateListenRESTClient(deepgramOptions.Value.ApiKey);
        _s3Client = s3Client;
        _s3Options = s3Options.Value;
        _logger = logger;
    }

    public async Task<string> TranscriptAudioAsync_Local(AudioUploadRequest request)
    {
        try
        {
            byte[] audioBytes;
            using (var ms = new MemoryStream())
            {
                await request.AudioFile.CopyToAsync(ms);
                audioBytes = ms.ToArray();
            }

            var response = await _deepgramClient.TranscribeFile(
                audioBytes,
                new PreRecordedSchema()
                {
                    Model = "nova-2",
                    SmartFormat = true,
                });

            _logger.LogInformation("Deepgram transcription completed for local processing");
            return response?.Results?.Channels?.FirstOrDefault()?.Alternatives?.FirstOrDefault()?.Transcript ??
                   string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during local audio transcription");
            throw;
        }
    }

    public async Task<string> TranscriptAudioAsync(AudioUploadRequest request)
    {
        try
        {
            byte[] audioBytes;
            using (var ms = new MemoryStream())
            {
                await request.AudioFile.CopyToAsync(ms);
                audioBytes = ms.ToArray();
            }

            var response = await _deepgramClient.TranscribeFile(
                audioBytes,
                new PreRecordedSchema()
                {
                    Model = "nova-3-general",
                    Punctuate = true,
                    Language = "multi",
                    SmartFormat = true,
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
            string transcriptedText = string.Join(" ", bestTranscriptionPerChannel);

            _logger.LogInformation("Transkript generiert:{transcriptedText}", transcriptedText);

            return transcriptedText;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during audio transcription");
            throw;
        }
    }
}