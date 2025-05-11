using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;

namespace SpeakStoreLocate.ApiService;

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
        ChatCompletion completion =
            await _chatClient.CompleteChatAsync(prompt);

        var result = completion.Content[0].Text;

        _logger.LogInformation("Chat Completion Result: " + result);
        return result;
    }
}