namespace TurkcellMeetingAssistant.Application.DTOs.Meetings.Summary;

public class ApproveMeetingSummaryRequest
{
    public int ExpectedVersion { get; set; }
    public int ExpectedManualRevisionNumber { get; set; }
    public bool Confirmation { get; set; }
}

public class RevokeApprovalRequest
{
    public string Reason { get; set; } = string.Empty;
}

public class ApproveMeetingSummaryResponse
{
    public Guid MeetingId { get; set; }
    public Guid SummaryId { get; set; }
    public int Version { get; set; }
    public int ManualRevisionNumber { get; set; }
    public bool IsApproved { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovedBy { get; set; }
    public int LowConfidenceItemCount { get; set; }
}
