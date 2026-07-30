using TurkcellMeetingAssistant.Domain.Enums;

namespace TurkcellMeetingAssistant.Application.DTOs.Speech;

public class MeetingRecordingDto
{
    public Guid Id { get; set; }
    public Guid MeetingId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public RecordingStatus Status { get; set; }
    public string? ErrorCode { get; set; }
    public string? SafeErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessingStartedAt { get; set; }
    public DateTime? ProcessingCompletedAt { get; set; }
}

public class UploadRecordingRequestDto
{
    public Guid MeetingId { get; set; }
    public Stream FileStream { get; set; } = null!;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string? Language { get; set; }
}
