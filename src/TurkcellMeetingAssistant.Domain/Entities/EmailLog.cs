using TurkcellMeetingAssistant.Domain.Enums;

namespace TurkcellMeetingAssistant.Domain.Entities;

public class EmailLog
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid MeetingId { get; set; }
    public Guid MeetingSummaryId { get; set; }
    public int SummaryVersion { get; set; }
    public int SummaryManualRevisionNumber { get; set; }
    
    public EmailType EmailType { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string? ProviderMessageId { get; set; }
    
    public string? SenderEmail { get; set; }
    public string ToRecipientsJson { get; set; } = string.Empty;
    public string? CcRecipientsJson { get; set; }
    
    public string Subject { get; set; } = string.Empty;
    public string? BodyHtml { get; set; }
    public string? BodyText { get; set; }
    
    public EmailDeliveryStatus Status { get; set; }
    public bool IsTestEmail { get; set; }
    
    public Guid RequestedByUserId { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? FailedAt { get; set; }
    
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    
    public int RetryCount { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    
    public Meeting? Meeting { get; set; }
    public MeetingSummary? MeetingSummary { get; set; }
    public User? RequestedByUser { get; set; }
}
