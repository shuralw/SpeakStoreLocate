using System.Text;
using System.Text.Json;
using System.Diagnostics;
using Microsoft.Extensions.Options;
using SpeakStoreLocate.ApiService.Models;
using SpeakStoreLocate.ApiService.Options;
using SpeakStoreLocate.ApiService.Services.ChatCompletion;
using SpeakStoreLocate.ApiService.Utilities;

namespace SpeakStoreLocate.ApiService.Services.Interpretation;

class InterpretationService(
    ILogger<InterpretationService> _logger,
    IChatCompletionService _chatCompletionService,
    IInterpretationPromptBuilder _promptBuilder,
    IOptions<LoggingOptions> _loggingOptions)
    : IInterpretationService
{
    public async Task<List<StorageCommand>> InterpretGeschwafelToStructuredCommands(string transcriptedText,
        IEnumerable<string> existingLocations)
    {
        var sw = Stopwatch.StartNew();
        string fullPrompt = _promptBuilder.BuildPrompt(transcriptedText, existingLocations);

        var locationsCount = existingLocations is ICollection<string> c ? c.Count : existingLocations.Count();
        _logger.LogInformation(
            "Interpretation started. TranscriptLength={TranscriptLength} LocationsCount={LocationsCount} PromptLength={PromptLength}",
            LoggingSanitizer.SafeLength(transcriptedText),
            locationsCount,
            LoggingSanitizer.SafeLength(fullPrompt));

        string chatResponse = await _chatCompletionService.CompleteChat(fullPrompt);

        _logger.LogInformation("Chat completion received. ResponseLength={ResponseLength} ElapsedMs={ElapsedMs}",
            LoggingSanitizer.SafeLength(chatResponse),
            sw.ElapsedMilliseconds);

        var debugPayloadEnabled = _loggingOptions.Value.DebugPayload.Enabled;
        var maxPayloadLength = _loggingOptions.Value.DebugPayload.MaxLength;
        if (debugPayloadEnabled || _logger.IsEnabled(LogLevel.Debug))
        {
            var truncated = LoggingSanitizer.Truncate(chatResponse, maxPayloadLength);
            var suffix = chatResponse != null && chatResponse.Length > maxPayloadLength ? "…(truncated)" : string.Empty;

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Chat response JSON (payload). ChatResponse={ChatResponse}{Suffix}", truncated, suffix);
            }
            else
            {
                _logger.LogInformation("Chat response JSON (payload). ChatResponse={ChatResponse}{Suffix}", truncated, suffix);
            }
        }

        try
        {
            var commands = JsonSerializer.Deserialize<List<StorageCommand>>(
                chatResponse,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            ) ?? new List<StorageCommand>();

            _logger.LogInformation("Interpretation parsed. CommandCount={CommandCount}", commands.Count);

            if (debugPayloadEnabled || _logger.IsEnabled(LogLevel.Debug))
            {
                var commandJson = JsonSerializer.Serialize(commands, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var truncated = LoggingSanitizer.Truncate(commandJson, maxPayloadLength);
                var suffix = commandJson.Length > maxPayloadLength ? "…(truncated)" : string.Empty;

                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("Parsed commands JSON (payload). CommandsJson={CommandsJson}{Suffix}", truncated, suffix);
                }
                else
                {
                    _logger.LogInformation("Parsed commands JSON (payload). CommandsJson={CommandsJson}{Suffix}", truncated, suffix);
                }
            }
            return commands;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse chat response as commands. ResponsePrefix={ResponsePrefix}",
                LoggingSanitizer.Truncate(chatResponse, 500));
            throw;
        }
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