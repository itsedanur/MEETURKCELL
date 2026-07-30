using TurkcellMeetingAssistant.Domain.Enums;

namespace TurkcellMeetingAssistant.Domain.Entities;

public class ActionItem
{
    public Guid Id { get; set; }
    public Guid MeetingId { get; set; }
    public Guid MeetingSummaryId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? OwnerName { get; set; }
    public string? OwnerEmail { get; set; }
    public DateTime? DueDate { get; set; }
    public ActionPriority Priority { get; set; }
    public ActionItemStatus Status { get; set; }
    public decimal ConfidenceScore { get; set; }
    public string? Evidence { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? AssignedParticipantId { get; set; }
    public SourceType SourceType { get; set; }
    public Guid? LastEditedByUserId { get; set; }
    public DateTime? LastEditedAt { get; set; }
    public Guid? CompletedByUserId { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }

    // Navigation Properties
    public Meeting? Meeting { get; set; }
    public MeetingSummary? MeetingSummary { get; set; }
    public MeetingParticipant? AssignedParticipant { get; set; }
    public User? LastEditedByUser { get; set; }
    public User? CompletedByUser { get; set; }
}
