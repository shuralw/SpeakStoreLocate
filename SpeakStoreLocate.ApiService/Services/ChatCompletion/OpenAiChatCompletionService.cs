using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using SpeakStoreLocate.ApiService.Options;

namespace SpeakStoreLocate.ApiService.Services.ChatCompletion;

class OpenAiChatCompletionService : IChatCompletionService
{
    private readonly ILogger<OpenAiChatCompletionService> _logger;
    private readonly ChatClient _chatClient;

    public OpenAiChatCompletionService(
        ILogger<OpenAiChatCompletionService> logger,
        OpenAIClient openAiClient,
        IOptions<OpenAIOptions> openAIOptions)
    {
        _logger = logger;
        this._chatClient = new ChatClient(
            model: openAIOptions.Value.DefaultModel,
            apiKey: openAIOptions.Value.ApiKey
        );
    }

    public async Task<string> CompleteChat(string prompt)
    {
        OpenAI.Chat.ChatCompletion completion =
            await _chatClient.CompleteChatAsync(prompt);

        var result = completion.Content[0].Text;

        _logger.LogInformation("Chat Completion Result: " + result);
        return result;
    }
}