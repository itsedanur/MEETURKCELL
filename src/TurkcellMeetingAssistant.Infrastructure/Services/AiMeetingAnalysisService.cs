using System.Text.Json;
using Microsoft.Extensions.Logging;
using TurkcellMeetingAssistant.Application.DTOs.Meetings.Analysis;
using TurkcellMeetingAssistant.Application.Interfaces;
using TurkcellMeetingAssistant.Application.Interfaces.AI;

namespace TurkcellMeetingAssistant.Infrastructure.Services;

public class AiMeetingAnalysisService : IAiMeetingAnalysisService
{
    private readonly IAiProvider _aiProvider;
    private readonly IPromptService _promptService;
    private readonly ILogger<AiMeetingAnalysisService> _logger;

    public AiMeetingAnalysisService(IAiProvider aiProvider, IPromptService promptService, ILogger<AiMeetingAnalysisService> logger)
    {
        _aiProvider = aiProvider;
        _promptService = promptService;
        _logger = logger;
    }

    public async Task<MeetingAnalysisResult> AnalyzeAsync(MeetingAnalysisInput input, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting meeting analysis with AI Provider: {ProviderName}", _aiProvider.ProviderName);

        // 1. Get the prompt template
        var promptTemplate = await _promptService.GetPromptTemplateAsync("MeetingAnalysis", cancellationToken);

        // 2. Prepare participants text
        var participantsText = string.Join("\n", input.Participants.Select(p => $"- {p.FullName} ({p.Email})"));

        // 3. Build the actual system prompt (or in this case, we'll send it all as system prompt and transcript as user prompt)
        var parameters = new Dictionary<string, string>
        {
            { "Transcript", input.TranscriptText },
            { "Participants", participantsText }
        };

        var finalPrompt = _promptService.BuildPrompt(promptTemplate, parameters);

        // 4. Send to AI
        var systemPrompt = "You are a helpful meeting analysis assistant. Always respond with valid JSON.";
        
        var jsonResponse = await _aiProvider.GenerateTextAsync(systemPrompt, finalPrompt, cancellationToken);

        _logger.LogDebug("Received JSON from AI: {Json}", jsonResponse);

        // 5. Parse JSON
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        try
        {
            var result = JsonSerializer.Deserialize<MeetingAnalysisResult>(jsonResponse, options);
            if (result == null)
            {
                throw new InvalidOperationException("AI response could not be deserialized to MeetingAnalysisResult.");
            }
            return result;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse AI response as JSON. Raw response: {Response}", jsonResponse);
            throw new InvalidOperationException("Failed to parse AI response as JSON.", ex);
        }
    }
}
