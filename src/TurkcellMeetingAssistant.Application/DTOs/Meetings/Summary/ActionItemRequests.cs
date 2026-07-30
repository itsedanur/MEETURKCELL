using TurkcellMeetingAssistant.Domain.Enums;

namespace TurkcellMeetingAssistant.Application.DTOs.Meetings.Summary;

public class CreateActionItemRequest
{
    public string Description { get; set; } = string.Empty;
    public Guid? AssignedParticipantId { get; set; }
    public string? OwnerName { get; set; }
    public string? OwnerEmail { get; set; }
    public DateTime? DueDate { get; set; }
    public ActionPriority Priority { get; set; }
    public ActionItemStatus? Status { get; set; }
    public decimal ConfidenceScore { get; set; } = 1.0m;
    public string? Evidence { get; set; }
}

public class UpdateActionItemRequest
{
    public string Description { get; set; } = string.Empty;
    public Guid? AssignedParticipantId { get; set; }
    public string? OwnerName { get; set; }
    public string? OwnerEmail { get; set; }
    public DateTime? DueDate { get; set; }
    public ActionPriority Priority { get; set; }
    public decimal ConfidenceScore { get; set; }
    public string? Evidence { get; set; }
    public DateTime? ExpectedUpdatedAt { get; set; }
}

public class UpdateActionItemStatusRequest
{
    public ActionItemStatus Status { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime? ExpectedUpdatedAt { get; set; }
}
