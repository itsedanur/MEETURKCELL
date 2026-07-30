using TurkcellMeetingAssistant.Domain.Enums;

namespace TurkcellMeetingAssistant.Application.DTOs.Meetings.Email;

public class EmailLogDetailDto
{
    public Guid Id { get; set; }
    public Guid MeetingId { get; set; }
    public EmailType EmailType { get; set; }
    public EmailDeliveryStatus Status { get; set; }
    public string? Subject { get; set; }
    public string? ToRecipientsJson { get; set; }
    public string? CcRecipientsJson { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public string? ErrorMessage { get; set; }
    
    // Yalnızca yetkili ve ayar açıksa
    public string? BodyHtml { get; set; }
    public string? BodyText { get; set; }
}
