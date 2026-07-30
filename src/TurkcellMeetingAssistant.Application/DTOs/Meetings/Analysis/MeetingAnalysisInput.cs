namespace TurkcellMeetingAssistant.Application.DTOs.Meetings.Analysis;

public class MeetingAnalysisInput
{
    public Guid MeetingId { get; set; }
    public string MeetingTitle { get; set; } = string.Empty;
    public DateTime MeetingDate { get; set; }
    public string TranscriptText { get; set; } = string.Empty;
    public List<AnalysisParticipantDto> Participants { get; set; } = new();
    public string Language { get; set; } = "tr";
    public string PromptVersion { get; set; } = string.Empty;
}

public class AnalysisParticipantDto
{
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Department { get; set; }
    public string? Title { get; set; }
}
