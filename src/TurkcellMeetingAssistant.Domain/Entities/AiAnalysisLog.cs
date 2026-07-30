namespace TurkcellMeetingAssistant.Domain.Entities;

public class AiAnalysisLog
{
    public Guid Id { get; set; }
    public Guid MeetingId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string? ModelName { get; set; }
    public string PromptVersion { get; set; } = string.Empty;
    public int InputCharacterCount { get; set; }
    public int? InputTokenCount { get; set; }
    public int? OutputTokenCount { get; set; }
    public long? DurationMilliseconds { get; set; }
    public string? RawResponse { get; set; }
    public bool IsSuccessful { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation Property
    public Meeting? Meeting { get; set; }
}
