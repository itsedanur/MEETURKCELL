namespace TurkcellMeetingAssistant.Domain.Entities;

public class MeetingSummary
{
    public Guid Id { get; set; }
    public Guid MeetingId { get; set; }
    public string? MeetingPurpose { get; set; }
    public string ExecutiveSummary { get; set; } = string.Empty;
    public int Version { get; set; }
    public bool IsApproved { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public string AiProvider { get; set; } = string.Empty;
    public string? AiModelName { get; set; }
    public string PromptVersion { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public Guid? LastEditedByUserId { get; set; }
    public DateTime? LastEditedAt { get; set; }
    public int ManualRevisionNumber { get; set; }
    public string? ApprovedContentHash { get; set; }
    public DateTime? ApprovalRevokedAt { get; set; }
    public string? ApprovalRevokedReason { get; set; }

    // Navigation Properties
    public Meeting? Meeting { get; set; }
    public User? ApprovedByUser { get; set; }
    public User? LastEditedByUser { get; set; }
    public ICollection<MeetingTopic> Topics { get; set; } = new List<MeetingTopic>();
    public ICollection<MeetingDecision> Decisions { get; set; } = new List<MeetingDecision>();
    public ICollection<ActionItem> ActionItems { get; set; } = new List<ActionItem>();
    public ICollection<OpenIssue> OpenIssues { get; set; } = new List<OpenIssue>();
}
