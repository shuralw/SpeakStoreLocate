using SpeakStoreLocate.ApiService.Models;

namespace SpeakStoreLocate.ApiService.Services.Interpretation;

public interface IInterpretationService
{
    public Task<List<StorageCommand>> InterpretGeschwafelToStructuredCommands(
        string transcriptedText,
        IEnumerable<string> existingLocations);
}