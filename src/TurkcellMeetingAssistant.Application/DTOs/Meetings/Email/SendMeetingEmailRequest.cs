namespace TurkcellMeetingAssistant.Application.DTOs.Meetings.Email;

public class SendMeetingEmailRequest
{
    public List<EmailRecipientRequest>? AdditionalTo { get; set; }
    public List<EmailRecipientRequest>? Cc { get; set; }
    
    public string? SubjectOverride { get; set; }
    public string? IntroText { get; set; }
    public string? ClosingText { get; set; }
    
    public bool IncludeParticipants { get; set; } = true;
    public bool IncludeTopics { get; set; } = true;
    public bool IncludeDecisions { get; set; } = true;
    public bool IncludeActionItems { get; set; } = true;
    public bool IncludeOpenIssues { get; set; } = true;
    public bool IncludeActionStatus { get; set; } = true;
    public bool IncludeEvidence { get; set; } = false;

    public string IdempotencyKey { get; set; } = string.Empty;
}
