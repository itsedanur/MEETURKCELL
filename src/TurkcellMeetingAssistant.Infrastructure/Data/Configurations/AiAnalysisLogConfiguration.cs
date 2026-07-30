using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TurkcellMeetingAssistant.Domain.Entities;

namespace TurkcellMeetingAssistant.Infrastructure.Data.Configurations;

public class AiAnalysisLogConfiguration : IEntityTypeConfiguration<AiAnalysisLog>
{
    public void Configure(EntityTypeBuilder<AiAnalysisLog> builder)
    {
        builder.HasKey(e => e.Id);
        
        builder.HasIndex(e => e.MeetingId);
        
        builder.Property(e => e.Provider)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(e => e.ModelName)
            .HasMaxLength(200);
            
        builder.Property(e => e.PromptVersion)
            .IsRequired()
            .HasMaxLength(50);
            
        builder.Property(e => e.ErrorCode)
            .HasMaxLength(100);
            
        builder.Property(e => e.ErrorMessage)
            .HasMaxLength(1000);

        builder.HasOne(e => e.Meeting)
            .WithMany(m => m.AiAnalysisLogs)
            .HasForeignKey(e => e.MeetingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
