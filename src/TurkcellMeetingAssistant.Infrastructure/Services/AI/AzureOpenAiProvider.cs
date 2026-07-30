using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Options;
using TurkcellMeetingAssistant.Application.Interfaces.AI;
using TurkcellMeetingAssistant.Application.Models.AI;
using OpenAI.Chat;

namespace TurkcellMeetingAssistant.Infrastructure.Services.AI;

public class AzureOpenAiProvider : IAiProvider
{
    private readonly ChatClient _chatClient;

    public string ProviderName => "AzureOpenAI";

    public AzureOpenAiProvider(IOptions<AiSettings> settings)
    {
        var aiSettings = settings.Value;
        if (string.IsNullOrEmpty(aiSettings.AzureOpenAiApiKey) || string.IsNullOrEmpty(aiSettings.AzureOpenAiEndpoint) || string.IsNullOrEmpty(aiSettings.AzureOpenAiDeploymentName))
        {
            throw new ArgumentException("Azure OpenAI configuration is missing.");
        }

        var azureClient = new AzureOpenAIClient(
            new Uri(aiSettings.AzureOpenAiEndpoint),
            new AzureKeyCredential(aiSettings.AzureOpenAiApiKey));

        _chatClient = azureClient.GetChatClient(aiSettings.AzureOpenAiDeploymentName);
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
            Temperature = 0.2f
        };

        var response = await _chatClient.CompleteChatAsync(messages, options, cancellationToken);
        var result = response.Value.Content[0].Text;
        
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
