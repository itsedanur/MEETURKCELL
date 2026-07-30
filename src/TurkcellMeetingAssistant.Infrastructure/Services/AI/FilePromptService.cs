using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TurkcellMeetingAssistant.Application.Interfaces.AI;

namespace TurkcellMeetingAssistant.Infrastructure.Services.AI;

public class FilePromptService : IPromptService
{
    private readonly IHostEnvironment _environment;
    private readonly ILogger<FilePromptService> _logger;

    public FilePromptService(IHostEnvironment environment, ILogger<FilePromptService> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public async Task<string> GetPromptTemplateAsync(string promptName, CancellationToken cancellationToken = default)
    {
        var promptsDir = Path.Combine(_environment.ContentRootPath, "Prompts");
        var filePath = Path.Combine(promptsDir, $"{promptName}.txt");

        if (!File.Exists(filePath))
        {
            _logger.LogError("Prompt template not found at {FilePath}", filePath);
            throw new FileNotFoundException($"Prompt template '{promptName}' not found.", filePath);
        }

        return await File.ReadAllTextAsync(filePath, cancellationToken);
    }

    public string BuildPrompt(string template, Dictionary<string, string> parameters)
    {
        var result = template;
        foreach (var param in parameters)
        {
            result = result.Replace($"{{{{{param.Key}}}}}", param.Value); // We use {{Key}} in the template
        }
        return result;
    }
}
