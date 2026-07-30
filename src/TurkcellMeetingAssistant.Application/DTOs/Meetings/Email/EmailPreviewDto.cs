namespace TurkcellMeetingAssistant.Application.DTOs.Meetings.Email;

public class EmailPreviewDto
{
    public string Subject { get; set; } = string.Empty;
    public string? HtmlBody { get; set; }
    public string? TextBody { get; set; }
}
