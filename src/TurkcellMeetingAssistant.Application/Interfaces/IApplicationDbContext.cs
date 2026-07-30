using Microsoft.EntityFrameworkCore;
using TurkcellMeetingAssistant.Domain.Entities;

namespace TurkcellMeetingAssistant.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<Meeting> Meetings { get; }
    DbSet<MeetingParticipant> MeetingParticipants { get; }
    DbSet<MeetingSummary> MeetingSummaries { get; }
    DbSet<MeetingTopic> MeetingTopics { get; }
    DbSet<MeetingDecision> MeetingDecisions { get; }
    DbSet<ActionItem> ActionItems { get; }
    DbSet<OpenIssue> OpenIssues { get; }
    DbSet<AiAnalysisLog> AiAnalysisLogs { get; }
    DbSet<MeetingRecording> MeetingRecordings { get; set; }
    DbSet<EmailLog> EmailLogs { get; set; }
    Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade Database { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
