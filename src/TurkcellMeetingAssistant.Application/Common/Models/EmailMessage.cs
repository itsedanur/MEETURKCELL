namespace TurkcellMeetingAssistant.Application.Common.Models;

public class EmailMessage
{
    public EmailAddressModel? From { get; set; }
    public List<EmailAddressModel> To { get; set; } = new();
    public List<EmailAddressModel> Cc { get; set; } = new();
    
    public string Subject { get; set; } = string.Empty;
    public string? HtmlBody { get; set; }
    public string? TextBody { get; set; }
    
    public string IdempotencyKey { get; set; } = string.Empty;
    public Dictionary<string, string> Metadata { get; set; } = new();
}
