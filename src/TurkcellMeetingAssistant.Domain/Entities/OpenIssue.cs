namespace TurkcellMeetingAssistant.Domain.Entities;

public class OpenIssue
{
    public Guid Id { get; set; }
    public Guid MeetingSummaryId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? OwnerName { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Navigation Property
    public MeetingSummary? MeetingSummary { get; set; }
}
