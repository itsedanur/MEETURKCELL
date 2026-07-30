using TurkcellMeetingAssistant.Domain.Enums;

namespace TurkcellMeetingAssistant.Application.DTOs.Meetings;

public class MeetingListItemDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime MeetingDate { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public MeetingStatus Status { get; set; }
    public string StatusDisplayName { get; set; } = string.Empty;
    public int ParticipantCount { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedAt { get; set; }
}
