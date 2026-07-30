namespace TurkcellMeetingAssistant.Application.Interfaces.AI;

public interface IAiProvider
{
    string ProviderName { get; }
    Task<string> GenerateTextAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default);
    Task<string> GenerateTextAsync(string systemPrompt, string userPrompt, object requestOptions, CancellationToken cancellationToken = default);
}
