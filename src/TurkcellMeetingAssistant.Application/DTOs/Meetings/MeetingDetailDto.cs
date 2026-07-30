using TurkcellMeetingAssistant.Domain.Enums;

namespace TurkcellMeetingAssistant.Application.DTOs.Meetings;

public class MeetingDetailDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime MeetingDate { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public UserSummaryDto? Organizer { get; set; }
    public MeetingStatus Status { get; set; }
    public string StatusDisplayName { get; set; } = string.Empty;
    public string? TranscriptFileName { get; set; }
    public string? TranscriptFileType { get; set; }
    public DateTime? TranscriptUploadedAt { get; set; }
    public int ParticipantCount { get; set; }
    public List<MeetingParticipantDto> Participants { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsArchived { get; set; }
    public DateTime? ArchivedAt { get; set; }
}
