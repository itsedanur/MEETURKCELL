using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TurkcellMeetingAssistant.Domain.Entities;

namespace TurkcellMeetingAssistant.Infrastructure.Data.Configurations;

public class OpenIssueConfiguration : IEntityTypeConfiguration<OpenIssue>
{
    public void Configure(EntityTypeBuilder<OpenIssue> builder)
    {
        builder.HasKey(e => e.Id);
        
        builder.HasIndex(e => e.MeetingSummaryId);
        builder.HasIndex(e => e.SortOrder);
        
        builder.Property(e => e.Description)
            .IsRequired();
            
        builder.Property(e => e.OwnerName)
            .HasMaxLength(200);

        builder.HasOne(e => e.MeetingSummary)
            .WithMany(m => m.OpenIssues)
            .HasForeignKey(e => e.MeetingSummaryId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
