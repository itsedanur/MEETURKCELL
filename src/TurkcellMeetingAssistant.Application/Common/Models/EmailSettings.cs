namespace TurkcellMeetingAssistant.Application.Common.Models;

public class EmailSettings
{
    public const string SectionName = "Email";

    public string Provider { get; set; } = "Mock";
    public string DefaultSenderName { get; set; } = "Meeting Assistant";
    public string DefaultSenderEmail { get; set; } = "no-reply@meetingassistant.local";
    public bool StoreEmailBody { get; set; } = true;
    public int MaximumRecipientCount { get; set; } = 100;
    public bool AllowExternalRecipients { get; set; } = false;
    public string[] AllowedEmailDomains { get; set; } = Array.Empty<string>();
    public bool EnableTestEmail { get; set; } = true;
    public bool MockFailureMode { get; set; } = false;
    public int MockFailureRate { get; set; } = 0;
    public string EmailFooterText { get; set; } = "Bu e-posta Meeting Assistant tarafından oluşturulmuştur.";
}
