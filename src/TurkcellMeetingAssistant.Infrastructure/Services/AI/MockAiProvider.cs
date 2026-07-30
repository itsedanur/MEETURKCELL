using TurkcellMeetingAssistant.Application.Interfaces.AI;
using System.Text.Json;
using TurkcellMeetingAssistant.Application.DTOs.Meetings.Analysis;

namespace TurkcellMeetingAssistant.Infrastructure.Services.AI;

public class MockAiProvider : IAiProvider
{
    public string ProviderName => "Mock";

    public async Task<string> GenerateTextAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default)
    {
        return await GenerateTextAsync(systemPrompt, userPrompt, null, cancellationToken);
    }

    public async Task<string> GenerateTextAsync(string systemPrompt, string userPrompt, object requestOptions, CancellationToken cancellationToken = default)
    {
        // Yalandan bir bekleme süresi ekliyoruz
        await Task.Delay(1000, cancellationToken);

        // Sistemin daha önceki Mock serviste yaptığı gibi dummy bir analiz nesnesi oluşturup JSON'a çevireceğiz
        var result = new MeetingAnalysisResult
        {
            MeetingPurpose = "Toplantı Amacı (Mock)",
            ExecutiveSummary = "Bu özet Mock AI servisi tarafından üretilmiştir."
        };

        var lines = userPrompt.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var lowerLine = line.ToLowerInvariant();

            if (lowerLine.Contains("karar") || lowerLine.Contains("mutabık kalındı"))
            {
                result.Decisions.Add(new AiMeetingDecisionResult
                {
                    Description = line.Trim(),
                    ConfidenceScore = 0.85m,
                    Evidence = line.Length > 100 ? line.Substring(0, 100) : line
                });
            }
            else if (lowerLine.Contains("yapacak") || lowerLine.Contains("gönderecek") || lowerLine.Contains("sorumlu"))
            {
                var action = new AiActionItemResult
                {
                    Description = line.Trim(),
                    ConfidenceScore = 0.75m,
                    Evidence = line.Length > 100 ? line.Substring(0, 100) : line,
                    Priority = "Medium"
                };

                // Try to find a dummy date logic
                if (lowerLine.Contains("cuma") || lowerLine.Contains("yarın") || lowerLine.Contains("ağustos"))
                {
                    action.DueDate = DateTime.UtcNow.AddDays(3);
                }

                result.ActionItems.Add(action);
            }
            else if (lowerLine.Contains("bekliyor") || lowerLine.Contains("netleşmedi") || lowerLine.Contains("açık kaldı"))
            {
                result.OpenIssues.Add(new AiOpenIssueResult
                {
                    Description = line.Trim()
                });
            }
            else
            {
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

        return JsonSerializer.Serialize(result, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }
}
