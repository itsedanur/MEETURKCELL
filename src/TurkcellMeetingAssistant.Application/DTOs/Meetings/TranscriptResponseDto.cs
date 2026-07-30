namespace TurkcellMeetingAssistant.Application.DTOs.Meetings;

public class TranscriptResponseDto
{
    public Guid MeetingId { get; set; }
    public string? TranscriptText { get; set; }
    public string? TranscriptFileName { get; set; }
    public string? TranscriptFileType { get; set; }
    public DateTime? TranscriptUploadedAt { get; set; }
    public int CharacterCount { get; set; }
    public int WordCount { get; set; }
}
