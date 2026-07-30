namespace TurkcellMeetingAssistant.Domain.Entities;

public class MeetingTopic
{
    public Guid Id { get; set; }
    public Guid MeetingSummaryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Navigation Property
    public MeetingSummary? MeetingSummary { get; set; }
}
