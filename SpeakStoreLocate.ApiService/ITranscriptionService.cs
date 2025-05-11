namespace SpeakStoreLocate.ApiService;

public interface ITranscriptionService
{
    Task<string> TranscriptAudioAsync(AudioUploadRequest request);
}