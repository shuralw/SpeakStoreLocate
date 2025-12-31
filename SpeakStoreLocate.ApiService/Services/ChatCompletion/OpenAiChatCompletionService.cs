using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using SpeakStoreLocate.ApiService.Options;
using System.Diagnostics;
using SpeakStoreLocate.ApiService.Utilities;

namespace SpeakStoreLocate.ApiService.Services.ChatCompletion;

class OpenAiChatCompletionService : IChatCompletionService
{
    private readonly ILogger<OpenAiChatCompletionService> _logger;
    private readonly ChatClient _chatClient;
    private readonly string _model;

    public OpenAiChatCompletionService(
        ILogger<OpenAiChatCompletionService> logger,
        OpenAIClient openAiClient,
        IOptions<OpenAIOptions> openAIOptions)
    {
        _logger = logger;
        _model = openAIOptions.Value.DefaultModel;
        this._chatClient = new ChatClient(
            model: openAIOptions.Value.DefaultModel,
            apiKey: openAIOptions.Value.APIKEY
        );
    }

    public async Task<string> CompleteChat(string prompt)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogDebug("OpenAI chat completion started. Model={Model} PromptLength={PromptLength}",
            _model,
            LoggingSanitizer.SafeLength(prompt));

        OpenAI.Chat.ChatCompletion completion =
            await _chatClient.CompleteChatAsync(prompt);

        var result = completion.Content[0].Text;

        _logger.LogInformation("OpenAI chat completion finished. Model={Model} ResultLength={ResultLength} ElapsedMs={ElapsedMs}",
            _model,
            LoggingSanitizer.SafeLength(result),
            sw.ElapsedMilliseconds);
        return result;
    }
}