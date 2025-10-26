namespace SpeakStoreLocate.ApiService.Services.Interpretation;

public interface IInterpretationPromptBuilder
{
    string BuildPrompt(string transcriptedText, IEnumerable<string> existingLocations);
}