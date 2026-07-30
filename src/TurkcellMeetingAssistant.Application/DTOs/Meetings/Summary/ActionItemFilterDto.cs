using TurkcellMeetingAssistant.Domain.Enums;

namespace TurkcellMeetingAssistant.Application.DTOs.Meetings.Summary;

public class ActionItemFilterDto
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }
    public Guid? MeetingId { get; set; }
    public string? OwnerEmail { get; set; }
    public Guid? AssignedParticipantId { get; set; }
    public ActionItemStatus? Status { get; set; }
    public ActionPriority? Priority { get; set; }
    public DateTime? DueDateFrom { get; set; }
    public DateTime? DueDateTo { get; set; }
    public bool? IsOverdue { get; set; }
    public string? SortBy { get; set; }
    public string? SortDirection { get; set; }
}
