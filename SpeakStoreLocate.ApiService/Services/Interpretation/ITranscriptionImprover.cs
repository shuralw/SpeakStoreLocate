namespace SpeakStoreLocate.ApiService.Services.Interpretation;

public interface ITranscriptionImprover
{
    Task<string> ImproveTranscriptedText(string transcriptedText);
}