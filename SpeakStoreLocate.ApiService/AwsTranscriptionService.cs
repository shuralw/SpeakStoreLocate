using Amazon.TranscribeService;

namespace SpeakStoreLocate.ApiService;

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