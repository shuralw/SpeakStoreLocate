using SpeakStoreLocate.ApiService.Models;

namespace SpeakStoreLocate.ApiService.Services.Interpretation;

public  interface IInterpretationService
{
     Task<List<StorageCommand>> InterpretGeschwafelToStructuredCommands(string transcriptedText);
}