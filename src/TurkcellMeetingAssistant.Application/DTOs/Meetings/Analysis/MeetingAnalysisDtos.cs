using TurkcellMeetingAssistant.Domain.Enums;

namespace TurkcellMeetingAssistant.Application.DTOs.Meetings.Analysis;

public class MeetingAnalysisStatusDto
{
    public Guid MeetingId { get; set; }
    public MeetingStatus Status { get; set; }
    public string StatusDisplayName => Status.ToString();
    public bool HasTranscript { get; set; }
    public bool HasAnalysis { get; set; }
    public Guid? CurrentSummaryId { get; set; }
    public int CurrentVersion { get; set; }
    public DateTime? AnalysisStartedAt { get; set; }
    public DateTime? AnalysisCompletedAt { get; set; }
    public string? LastErrorMessage { get; set; }
    public bool CanAnalyze { get; set; }
    public bool CanReanalyze { get; set; }
}

public class MeetingSummaryDto
{
    public Guid MeetingId { get; set; }
    public Guid SummaryId { get; set; }
    public int Version { get; set; }
    public string? MeetingPurpose { get; set; }
    public string ExecutiveSummary { get; set; } = string.Empty;
    public bool IsApproved { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovedBy { get; set; }
    public int ManualRevisionNumber { get; set; }
    public DateTime? LastEditedAt { get; set; }
    public string? LastEditedBy { get; set; }
    public int LowConfidenceItemCount { get; set; }
    public bool RequiresReview { get; set; }
    public bool CanEdit { get; set; }
    public bool CanApprove { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string? ModelName { get; set; }
    public string PromptVersion { get; set; } = string.Empty;
    
    public List<MeetingTopicDto> Topics { get; set; } = new();
    public List<MeetingDecisionDto> Decisions { get; set; } = new();
    public List<ActionItemDto> ActionItems { get; set; } = new();
    public List<OpenIssueDto> OpenIssues { get; set; } = new();
    
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class MeetingTopicDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool CanEdit { get; set; }
}

public class MeetingDecisionDto
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? RelatedTopic { get; set; }
    public decimal ConfidenceScore { get; set; }
    public bool RequiresReview => ConfidenceScore < 0.70m;
    public string? Evidence { get; set; }
    public int SortOrder { get; set; }
    public SourceType SourceType { get; set; }
    public string SourceTypeDisplayName => SourceType.ToString();
    public DateTime? LastEditedAt { get; set; }
}

public class ActionItemDto
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? OwnerName { get; set; }
    public string? OwnerEmail { get; set; }
    public DateTime? DueDate { get; set; }
    public ActionPriority Priority { get; set; }
    public string PriorityDisplayName => Priority.ToString();
    public ActionItemStatus Status { get; set; }
    public string StatusDisplayName => Status.ToString();
    public decimal ConfidenceScore { get; set; }
    public bool RequiresReview => ConfidenceScore < 0.70m;
    public string? Evidence { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public Guid? AssignedParticipantId { get; set; }
    public SourceType SourceType { get; set; }
    public string SourceTypeDisplayName => SourceType.ToString();
    public bool IsOverdue { get; set; }
    public DateTime? LastEditedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? CompletedBy { get; set; }
    public string? CancellationReason { get; set; }
    
    public Guid? MeetingId { get; set; }
    public string? MeetingTitle { get; set; }
}

public class OpenIssueDto
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? OwnerName { get; set; }
    public int SortOrder { get; set; }
    public bool CanEdit { get; set; }
}
