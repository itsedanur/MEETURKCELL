namespace TurkcellMeetingAssistant.Application.DTOs.Meetings.Summary;

public class UpdateMeetingSummaryRequest
{
    public string? MeetingPurpose { get; set; }
    public string ExecutiveSummary { get; set; } = string.Empty;
    public int ExpectedVersion { get; set; }
    public int ExpectedManualRevisionNumber { get; set; }
}
