using TurkcellMeetingAssistant.Domain.Enums;

namespace TurkcellMeetingAssistant.Domain.Entities;

public class Meeting
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime MeetingDate { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public Guid OrganizerUserId { get; set; }
    public MeetingStatus Status { get; set; }
    public MeetingStatus? StatusBeforeArchive { get; set; }
    public string? TranscriptText { get; set; }
    public string? TranscriptFileName { get; set; }
    public string? TranscriptFileType { get; set; }
    public DateTime? TranscriptUploadedAt { get; set; }
    public DateTime? AnalysisStartedAt { get; set; }
    public DateTime? AnalysisCompletedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public bool IsArchived { get; set; } = false;
    public DateTime? ArchivedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Navigation Properties
    public User? OrganizerUser { get; set; }
    public User? ApprovedByUser { get; set; }
    public ICollection<MeetingParticipant> Participants { get; set; } = new List<MeetingParticipant>();
    public ICollection<MeetingSummary> Summaries { get; set; } = new List<MeetingSummary>();
    public ICollection<ActionItem> ActionItems { get; set; } = new List<ActionItem>();
    public ICollection<AiAnalysisLog> AiAnalysisLogs { get; set; } = new List<AiAnalysisLog>();
}
