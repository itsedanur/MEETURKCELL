using System.Reflection;
using Microsoft.EntityFrameworkCore;
using TurkcellMeetingAssistant.Application.Interfaces;
using TurkcellMeetingAssistant.Domain.Entities;

namespace TurkcellMeetingAssistant.Infrastructure.Data.Contexts;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Meeting> Meetings => Set<Meeting>();
    public DbSet<MeetingParticipant> MeetingParticipants => Set<MeetingParticipant>();
    public DbSet<MeetingSummary> MeetingSummaries => Set<MeetingSummary>();
    public DbSet<MeetingTopic> MeetingTopics => Set<MeetingTopic>();
    public DbSet<MeetingDecision> MeetingDecisions => Set<MeetingDecision>();
    public DbSet<ActionItem> ActionItems => Set<ActionItem>();
    public DbSet<OpenIssue> OpenIssues => Set<OpenIssue>();
    public DbSet<AiAnalysisLog> AiAnalysisLogs => Set<AiAnalysisLog>();
    public DbSet<MeetingRecording> MeetingRecordings { get; set; }
    public DbSet<EmailLog> EmailLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
        });

        modelBuilder.Entity<MeetingRecording>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Meeting)
                  .WithMany()
                  .HasForeignKey(e => e.MeetingId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<User>();

        foreach (var entry in entries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Deleted:
                    if (entry.Entity is User user)
                    {
                        entry.State = EntityState.Modified;
                        user.IsDeleted = true;
                        user.DeletedAt = DateTime.UtcNow;
                    }
                    break;
            }
        }

        // For AuditLogs we can set CreatedAt automatically if missing
        var auditEntries = ChangeTracker.Entries<AuditLog>();
        foreach (var entry in auditEntries)
        {
            if (entry.State == EntityState.Added && entry.Entity.CreatedAt == default)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
