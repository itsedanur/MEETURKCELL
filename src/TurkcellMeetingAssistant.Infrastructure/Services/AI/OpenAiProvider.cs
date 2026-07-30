using System.ClientModel;
using Microsoft.Extensions.Options;
using TurkcellMeetingAssistant.Application.Interfaces.AI;
using TurkcellMeetingAssistant.Application.Models.AI;
using OpenAI.Chat;

namespace TurkcellMeetingAssistant.Infrastructure.Services.AI;

public class OpenAiProvider : IAiProvider
{
    private readonly ChatClient _chatClient;

    public string ProviderName => "OpenAI";

    public OpenAiProvider(IOptions<AiSettings> settings)
    {
        var aiSettings = settings.Value;
        if (string.IsNullOrEmpty(aiSettings.OpenAiApiKey))
        {
            throw new ArgumentException("OpenAI API Key is missing.");
        }

        var modelName = !string.IsNullOrEmpty(aiSettings.OpenAiModelName) ? aiSettings.OpenAiModelName : "gpt-4o";
        _chatClient = new ChatClient(modelName, aiSettings.OpenAiApiKey);
    }

    public async Task<string> GenerateTextAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default)
    {
        return await GenerateTextAsync(systemPrompt, userPrompt, null, cancellationToken);
    }

    public async Task<string> GenerateTextAsync(string systemPrompt, string userPrompt, object requestOptions, CancellationToken cancellationToken = default)
    {
        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(userPrompt)
        };

        var options = new ChatCompletionOptions
        {
            Temperature = 0.2f // Düşük tutuyoruz ki JSON formatında saçmalamasın
        };

        var response = await _chatClient.CompleteChatAsync(messages, options, cancellationToken);
        var result = response.Value.Content[0].Text;
        
        // Remove markdown JSON code blocks if present
        if (result.StartsWith("```json"))
        {
            result = result.Substring(7);
            if (result.EndsWith("```"))
            {
                result = result.Substring(0, result.Length - 3);
            }
        }
        else if (result.StartsWith("```"))
        {
            result = result.Substring(3);
            if (result.EndsWith("```"))
            {
                result = result.Substring(0, result.Length - 3);
            }
        }

        return result.Trim();
    }
}
