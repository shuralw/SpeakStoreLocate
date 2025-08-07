namespace SpeakStoreLocate.ApiService.Services.ChatCompletion;

internal interface IChatCompletionService
{
    Task<string> CompleteChat(string prompt);
}