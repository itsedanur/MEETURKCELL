namespace TurkcellMeetingAssistant.Application.Common.Models;

public class EmailSendResult
{
    public bool IsSuccessful { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string? ProviderMessageId { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime SentAt { get; set; }
}
