namespace TurkcellMeetingAssistant.Application.Models.AI;

public class AiSettings
{
    public const string SectionName = "AiSettings";

    public string Provider { get; set; } = "Mock"; // "Mock", "OpenAI", "AzureOpenAI"
    
    // OpenAI specific
    public string? OpenAiApiKey { get; set; }
    public string? OpenAiModelName { get; set; }
    
    // Azure OpenAI specific
    public string? AzureOpenAiEndpoint { get; set; }
    public string? AzureOpenAiApiKey { get; set; }
    public string? AzureOpenAiDeploymentName { get; set; }
}
