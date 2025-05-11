namespace SpeakStoreLocate.ApiService;

internal interface IChatCompletionService
{
    Task<string> CompleteChat(string prompt);
}