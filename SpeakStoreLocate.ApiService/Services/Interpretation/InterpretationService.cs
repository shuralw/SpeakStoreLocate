using System.Text;
using System.Text.Json;
using SpeakStoreLocate.ApiService.Models;
using SpeakStoreLocate.ApiService.Services.ChatCompletion;

namespace SpeakStoreLocate.ApiService.Services.Interpretation;

class InterpretationService(ILogger<InterpretationService> _logger, IChatCompletionService _chatCompletionService, IInterpretationPromptBuilder _promptBuilder)
    : IInterpretationService
{
    public async Task<List<StorageCommand>> InterpretGeschwafelToStructuredCommands(string transcriptedText,
        IEnumerable<string> existingLocations)
    {
        string fullPrompt = _promptBuilder.BuildPrompt(transcriptedText, existingLocations);

        _logger.LogDebug("Vollständiger Prompt: {fullPrompt}", fullPrompt);

        string chatResponse = await _chatCompletionService.CompleteChat(fullPrompt);

        var commands = JsonSerializer.Deserialize<List<StorageCommand>>(
            chatResponse,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        ) ?? new List<StorageCommand>();
        return commands;
    }
}

class SearchRequestStringifier : ISearchRequestStringifier
{
    public string Stringify(StorageCommand searchCommand)
    {
        return searchCommand.ItemName;
    }
}

internal interface ISearchRequestStringifier
{
    string Stringify(StorageCommand searchCommand);
}

internal interface ISearchCommandGeneration
{
    string GenerateSearchCommand(IEnumerable<StorageCommand> searchRequests);
}

class SearchCommandGeneration : ISearchCommandGeneration
{
    public string GenerateSearchCommand(IEnumerable<StorageCommand> searchRequests)
    {
        StringBuilder result = new StringBuilder();

        string systemPrompt =
            "Du bist ein Kommissionierungssystem für ein Lager, das für den User herausfindet, wo sich Objekte befinden. Du lieferst ";
        foreach (var searchRequest in searchRequests)
        {
            result.Append(searchRequest.ItemName);
        }

        return result.ToString();
    }
}