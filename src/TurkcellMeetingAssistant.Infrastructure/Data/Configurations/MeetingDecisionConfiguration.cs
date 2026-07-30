using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TurkcellMeetingAssistant.Domain.Entities;

namespace TurkcellMeetingAssistant.Infrastructure.Data.Configurations;

public class MeetingDecisionConfiguration : IEntityTypeConfiguration<MeetingDecision>
{
    public void Configure(EntityTypeBuilder<MeetingDecision> builder)
    {
        builder.HasKey(e => e.Id);
        
        builder.HasIndex(e => e.MeetingSummaryId);
        builder.HasIndex(e => e.SortOrder);
        
        builder.Property(e => e.Description)
            .IsRequired();
            
        builder.Property(e => e.RelatedTopic)
            .HasMaxLength(500);
            
        builder.Property(e => e.ConfidenceScore)
            .HasPrecision(5, 4); // e.g. 0.9500
            
        builder.HasOne(e => e.MeetingSummary)
            .WithMany(m => m.Decisions)
            .HasForeignKey(e => e.MeetingSummaryId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasOne(e => e.LastEditedByUser)
            .WithMany()
            .HasForeignKey(e => e.LastEditedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
            
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
