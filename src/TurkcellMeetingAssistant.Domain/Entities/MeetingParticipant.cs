namespace TurkcellMeetingAssistant.Domain.Entities;

public class MeetingParticipant
{
    public Guid Id { get; set; }
    public Guid MeetingId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string? Title { get; set; }
    public bool IsOrganizer { get; set; }
    public bool IsRequired { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Navigation Property
    public Meeting? Meeting { get; set; }
}
