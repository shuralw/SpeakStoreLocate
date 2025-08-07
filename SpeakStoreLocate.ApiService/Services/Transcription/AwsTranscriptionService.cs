using Amazon.TranscribeService;
using SpeakStoreLocate.ApiService.Models;

namespace SpeakStoreLocate.ApiService.Services.Transcription;

public class AwsTranscriptionService(IConfiguration configuration, IAmazonTranscribeService amazonTranscribeService)
    : ITranscriptionService
{
    private readonly IAmazonTranscribeService _amazonTranscribeService = amazonTranscribeService;
    private readonly string? _bucketName = configuration["AWS:BucketName"];

    public Task<string> TranscriptAudioAsync(AudioUploadRequest request)
    {
        throw new NotImplementedException();
    }
}