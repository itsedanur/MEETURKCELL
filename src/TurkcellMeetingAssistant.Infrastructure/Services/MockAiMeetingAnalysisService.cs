using Microsoft.Extensions.Configuration;
using TurkcellMeetingAssistant.Application.DTOs.Meetings.Analysis;
using TurkcellMeetingAssistant.Application.Interfaces;

namespace TurkcellMeetingAssistant.Infrastructure.Services;

public class MockAiMeetingAnalysisService : IAiMeetingAnalysisService
{
    private readonly IConfiguration _configuration;

    public MockAiMeetingAnalysisService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task<MeetingAnalysisResult> AnalyzeAsync(MeetingAnalysisInput input, CancellationToken cancellationToken)
    {
        var result = new MeetingAnalysisResult
        {
            MeetingPurpose = $"{input.MeetingTitle} değerlendirmesi",
            ExecutiveSummary = "Bu özet Mock AI servisi tarafından üretilmiştir."
        };

        var lines = input.TranscriptText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var lowerLine = line.ToLowerInvariant();

            // Simple mock logic for Decisions
            if (lowerLine.Contains("karar") || lowerLine.Contains("mutabık kalındı") || lowerLine.Contains("kararlaştırıldı"))
            {
                result.Decisions.Add(new AiMeetingDecisionResult
                {
                    Description = line.Trim(),
                    ConfidenceScore = 0.85m,
                    Evidence = line.Length > 100 ? line.Substring(0, 100) : line
                });
            }
            // Simple mock logic for ActionItems
            else if (lowerLine.Contains("yapacak") || lowerLine.Contains("gönderecek") || lowerLine.Contains("sorumlu"))
            {
                var action = new AiActionItemResult
                {
                    Description = line.Trim(),
                    ConfidenceScore = 0.75m,
                    Evidence = line.Length > 100 ? line.Substring(0, 100) : line,
                    Priority = "Medium"
                };

                // Try to map owner
                foreach (var participant in input.Participants)
                {
                    if (lowerLine.Contains(participant.FullName.ToLowerInvariant()) || 
                        (participant.FullName.Contains(" ") && lowerLine.Contains(participant.FullName.Split(' ')[0].ToLowerInvariant())))
                    {
                        action.OwnerName = participant.FullName;
                        action.OwnerEmail = participant.Email;
                        break;
                    }
                }

                // Try to find a dummy date logic
                if (lowerLine.Contains("cuma") || lowerLine.Contains("yarın") || lowerLine.Contains("ağustos"))
                {
                    action.DueDate = DateTime.UtcNow.AddDays(3); // Dummy parsing
                }

                result.ActionItems.Add(action);
            }
            // Simple mock logic for OpenIssues
            else if (lowerLine.Contains("bekliyor") || lowerLine.Contains("netleşmedi") || lowerLine.Contains("açık kaldı"))
            {
                result.OpenIssues.Add(new AiOpenIssueResult
                {
                    Description = line.Trim()
                });
            }
            else
            {
                // Just dummy topics
                if (result.Topics.Count == 0 && line.Length > 10)
                {
                    result.Topics.Add(new AiMeetingTopicResult
                    {
                        Title = line.Substring(0, Math.Min(line.Length, 50)),
                        Description = line
                    });
                }
            }
        }

        // Delay to simulate AI process
        // In real mock we can do Task.Delay(500, cancellationToken)
        return Task.FromResult(result);
    }
}
