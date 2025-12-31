namespace SpeakStoreLocate.ApiService.Services.Interpretation;

using SpeakStoreLocate.ApiService.Utilities;

public class InterpretationPromptBuilder(IInterpretationPromptParts parts, ILogger<InterpretationPromptBuilder>? logger)
    : IInterpretationPromptBuilder
{
    public string BuildPrompt(string transcriptedText, IEnumerable<string> existingLocations)
    {
        var system = parts.GetSystemPrompt();
        var locations = parts.GetLocationsInformationForImprovedLocationDetermination(existingLocations);

        logger?.LogDebug(
            "Prompt parts built. SystemLength={SystemLength} LocationsInfoLength={LocationsInfoLength} TranscriptLength={TranscriptLength}",
            LoggingSanitizer.SafeLength(system),
            LoggingSanitizer.SafeLength(locations),
            LoggingSanitizer.SafeLength(transcriptedText));
        
        return $"{system}\n\n{locations}\n\nTranskript (de-DE):\n\n{transcriptedText}";
    }
}