using TurkcellMeetingAssistant.Domain.Enums;

namespace TurkcellMeetingAssistant.Domain.Entities;

public class MeetingRecording
{
    public Guid Id { get; set; }
    public Guid MeetingId { get; set; }
    
    public string OriginalFileName { get; set; } = string.Empty;
    public string StorageKey { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public int? DurationSeconds { get; set; }
    
    public RecordingStatus Status { get; set; } = RecordingStatus.Uploaded;
    public string? SpeechProvider { get; set; }
    public string? Language { get; set; }
    public string? DetectedLanguage { get; set; }
    
    public string? ErrorCode { get; set; }
    public string? SafeErrorMessage { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessingStartedAt { get; set; }
    public DateTime? ProcessingCompletedAt { get; set; }
    
    public Meeting Meeting { get; set; } = null!;
}
