namespace TurkcellMeetingAssistant.Application.DTOs.Meetings.Analysis;

public class MeetingAnalysisResult
{
    public string? MeetingPurpose { get; set; }
    public string ExecutiveSummary { get; set; } = string.Empty;
    public List<AiMeetingTopicResult> Topics { get; set; } = new();
    public List<AiMeetingDecisionResult> Decisions { get; set; } = new();
    public List<AiActionItemResult> ActionItems { get; set; } = new();
    public List<AiOpenIssueResult> OpenIssues { get; set; } = new();
    public string? NextMeetingSuggestions { get; set; }
}

public class AiMeetingTopicResult
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class AiMeetingDecisionResult
{
    public string Description { get; set; } = string.Empty;
    public string? RelatedTopic { get; set; }
    public decimal ConfidenceScore { get; set; }
    public string? Evidence { get; set; }
}

public class AiActionItemResult
{
    public string Description { get; set; } = string.Empty;
    public string? OwnerName { get; set; }
    public string? OwnerEmail { get; set; }
    public DateTime? DueDate { get; set; }
    public string Priority { get; set; } = "Medium";
    public decimal ConfidenceScore { get; set; }
    public string? Evidence { get; set; }
}

public class AiOpenIssueResult
{
    public string Description { get; set; } = string.Empty;
    public string? OwnerName { get; set; }
}
