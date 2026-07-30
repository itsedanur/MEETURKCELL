using TurkcellMeetingAssistant.Domain.Enums;

namespace TurkcellMeetingAssistant.Application.DTOs.Meetings.Email;

public class EmailStatusDto
{
    public Guid MeetingId { get; set; }
    public MeetingStatus MeetingStatus { get; set; }
    public string MeetingStatusDisplayName => MeetingStatus.ToString();
    public bool IsSummaryApproved { get; set; }
    public bool IsApprovalStillValid { get; set; }
    public bool HasSentEmail { get; set; }
    public EmailDeliveryStatus? LastEmailStatus { get; set; }
    public string? LastEmailStatusDisplayName => LastEmailStatus?.ToString();
    public DateTime? LastEmailSentAt { get; set; }
    public string? LastEmailError { get; set; }
    public bool CanPreview { get; set; }
    public bool CanSend { get; set; }
    public bool CanSendTestEmail { get; set; }
}
