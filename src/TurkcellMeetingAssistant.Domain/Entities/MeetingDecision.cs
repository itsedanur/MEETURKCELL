namespace TurkcellMeetingAssistant.Domain.Entities;

public class MeetingDecision
{
    public Guid Id { get; set; }
    public Guid MeetingSummaryId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? RelatedTopic { get; set; }
    public decimal ConfidenceScore { get; set; }
    public string? Evidence { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public TurkcellMeetingAssistant.Domain.Enums.SourceType SourceType { get; set; }
    public Guid? LastEditedByUserId { get; set; }
    public DateTime? LastEditedAt { get; set; }

    // Navigation Property
    public MeetingSummary? MeetingSummary { get; set; }
    public User? LastEditedByUser { get; set; }
}
