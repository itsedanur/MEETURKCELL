using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TurkcellMeetingAssistant.Domain.Entities;

namespace TurkcellMeetingAssistant.Infrastructure.Data.Configurations;

public class ActionItemConfiguration : IEntityTypeConfiguration<ActionItem>
{
    public void Configure(EntityTypeBuilder<ActionItem> builder)
    {
        builder.HasKey(e => e.Id);
        
        builder.HasIndex(e => e.MeetingId);
        builder.HasIndex(e => e.MeetingSummaryId);
        
        builder.Property(e => e.Description)
            .IsRequired();
            
        builder.Property(e => e.OwnerName)
            .HasMaxLength(200);
            
        builder.Property(e => e.OwnerEmail)
            .HasMaxLength(255);
            
        builder.Property(e => e.Priority)
            .HasConversion<string>()
            .HasMaxLength(50);
            
        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(50);
            
        builder.Property(e => e.ConfidenceScore)
            .HasPrecision(5, 4);

        builder.HasOne(e => e.Meeting)
            .WithMany(m => m.ActionItems)
            .HasForeignKey(e => e.MeetingId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasOne(e => e.MeetingSummary)
            .WithMany(m => m.ActionItems)
            .HasForeignKey(e => e.MeetingSummaryId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasOne(e => e.AssignedParticipant)
            .WithMany()
            .HasForeignKey(e => e.AssignedParticipantId)
            .OnDelete(DeleteBehavior.SetNull);
            
        builder.HasOne(e => e.LastEditedByUser)
            .WithMany()
            .HasForeignKey(e => e.LastEditedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
            
        builder.HasOne(e => e.CompletedByUser)
            .WithMany()
            .HasForeignKey(e => e.CompletedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
