namespace TurkcellMeetingAssistant.Application.DTOs.Meetings;

public class MeetingParticipantDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string? Title { get; set; }
    public bool IsOrganizer { get; set; }
    public bool IsRequired { get; set; }
    public DateTime CreatedAt { get; set; }
}
