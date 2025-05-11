namespace SpeakStoreLocate.ApiService;

public  interface IInterpretationService
{
    public Task<List<StorageCommand>> InterpretGeschwafelToStructuredCommands(string transcriptedText);
}