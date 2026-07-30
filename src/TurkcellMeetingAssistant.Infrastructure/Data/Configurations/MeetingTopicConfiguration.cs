using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TurkcellMeetingAssistant.Domain.Entities;

namespace TurkcellMeetingAssistant.Infrastructure.Data.Configurations;

public class MeetingTopicConfiguration : IEntityTypeConfiguration<MeetingTopic>
{
    public void Configure(EntityTypeBuilder<MeetingTopic> builder)
    {
        builder.HasKey(e => e.Id);
        
        builder.HasIndex(e => e.MeetingSummaryId);
        builder.HasIndex(e => e.SortOrder);
        
        builder.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(500);
            
        builder.HasOne(e => e.MeetingSummary)
            .WithMany(m => m.Topics)
            .HasForeignKey(e => e.MeetingSummaryId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
