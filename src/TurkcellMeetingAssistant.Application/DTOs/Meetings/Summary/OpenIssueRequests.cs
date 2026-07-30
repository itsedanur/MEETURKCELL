namespace TurkcellMeetingAssistant.Application.DTOs.Meetings.Summary;

public class CreateOpenIssueRequest
{
    public string Description { get; set; } = string.Empty;
    public string? OwnerName { get; set; }
    public int SortOrder { get; set; }
}

public class UpdateOpenIssueRequest
{
    public string Description { get; set; } = string.Empty;
    public string? OwnerName { get; set; }
    public int SortOrder { get; set; }
}
