using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TurkcellMeetingAssistant.Domain.Entities;

namespace TurkcellMeetingAssistant.Infrastructure.Data.Configurations;

public class MeetingSummaryConfiguration : IEntityTypeConfiguration<MeetingSummary>
{
    public void Configure(EntityTypeBuilder<MeetingSummary> builder)
    {
        builder.HasKey(e => e.Id);
        
        builder.HasIndex(e => e.MeetingId);
        
        builder.Property(e => e.MeetingPurpose)
            .HasMaxLength(1000);
            
        builder.Property(e => e.ExecutiveSummary)
            .IsRequired();
            
        builder.Property(e => e.AiProvider)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(e => e.AiModelName)
            .HasMaxLength(200);
            
        builder.Property(e => e.PromptVersion)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasOne(e => e.Meeting)
            .WithMany(m => m.Summaries)
            .HasForeignKey(e => e.MeetingId)
            .OnDelete(DeleteBehavior.Cascade); // Adjusting Cascade carefully if meeting deletes, summary deletes.

        builder.HasOne(e => e.ApprovedByUser)
            .WithMany()
            .HasForeignKey(e => e.ApprovedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
            
        builder.HasOne(e => e.LastEditedByUser)
            .WithMany()
            .HasForeignKey(e => e.LastEditedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
