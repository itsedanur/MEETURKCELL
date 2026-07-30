namespace TurkcellMeetingAssistant.Application.DTOs.Meetings.Summary;

public class CreateMeetingDecisionRequest
{
    public string Description { get; set; } = string.Empty;
    public string? RelatedTopic { get; set; }
    public decimal ConfidenceScore { get; set; }
    public string? Evidence { get; set; }
    public int SortOrder { get; set; }
}

public class UpdateMeetingDecisionRequest
{
    public string Description { get; set; } = string.Empty;
    public string? RelatedTopic { get; set; }
    public decimal ConfidenceScore { get; set; }
    public string? Evidence { get; set; }
    public int SortOrder { get; set; }
}
