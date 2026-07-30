namespace TurkcellMeetingAssistant.Application.Interfaces.AI;

public interface IPromptService
{
    Task<string> GetPromptTemplateAsync(string promptName, CancellationToken cancellationToken = default);
    string BuildPrompt(string template, Dictionary<string, string> parameters);
}
