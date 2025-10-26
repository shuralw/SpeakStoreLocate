namespace SpeakStoreLocate.ApiService.Services.Interpretation;

public class InterpretationPromptBuilder(IInterpretationPromptParts parts, ILogger<InterpretationPromptBuilder> logger)
    : IInterpretationPromptBuilder
{
    public string BuildPrompt(string transcriptedText, IEnumerable<string> existingLocations)
    {
        var system = parts.GetSystemPrompt();
        var locations = parts.GetLocationsInformationForImprovedLocationDetermination(existingLocations);
        
        logger.LogDebug("System: {System}", system);
        logger.LogDebug("Locations: {Locations}", locations);
        
        return $"{system}\n\n{locations}\n\nTranskript (de-DE):\n\n{transcriptedText}";
    }
}