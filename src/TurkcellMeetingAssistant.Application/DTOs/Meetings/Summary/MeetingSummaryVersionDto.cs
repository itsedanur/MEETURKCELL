namespace TurkcellMeetingAssistant.Application.DTOs.Meetings.Summary;

public class MeetingSummaryVersionDto
{
    public Guid SummaryId { get; set; }
    public int Version { get; set; }
    public int ManualRevisionNumber { get; set; }
    public bool IsApproved { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string? ModelName { get; set; }
    public string PromptVersion { get; set; } = string.Empty;
    public int TopicCount { get; set; }
    public int DecisionCount { get; set; }
    public int ActionItemCount { get; set; }
    public int OpenIssueCount { get; set; }
}
