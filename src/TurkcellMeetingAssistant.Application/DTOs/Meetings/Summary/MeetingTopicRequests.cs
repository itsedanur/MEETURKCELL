namespace TurkcellMeetingAssistant.Application.DTOs.Meetings.Summary;

public class CreateMeetingTopicRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
}

public class UpdateMeetingTopicRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
}

public class ReorderRequest
{
    public List<Guid> ItemIds { get; set; } = new List<Guid>();
}
