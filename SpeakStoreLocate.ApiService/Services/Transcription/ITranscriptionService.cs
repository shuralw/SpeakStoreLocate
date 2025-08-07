using SpeakStoreLocate.ApiService.Models;

namespace SpeakStoreLocate.ApiService.Services.Transcription;

public interface ITranscriptionService
{
    Task<string> TranscriptAudioAsync(AudioUploadRequest request);
}