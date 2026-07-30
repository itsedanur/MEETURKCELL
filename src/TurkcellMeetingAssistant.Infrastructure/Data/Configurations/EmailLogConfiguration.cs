using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TurkcellMeetingAssistant.Domain.Entities;

namespace TurkcellMeetingAssistant.Infrastructure.Data.Configurations;

public class EmailLogConfiguration : IEntityTypeConfiguration<EmailLog>
{
    public void Configure(EntityTypeBuilder<EmailLog> builder)
    {
        builder.ToTable("EmailLogs");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.EmailType)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(50);
            
        builder.Property(e => e.Status)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.Subject)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(e => e.ErrorCode)
            .HasMaxLength(100);

        builder.Property(e => e.ErrorMessage)
            .HasMaxLength(1000);

        builder.Property(e => e.Provider)
            .HasMaxLength(100);

        builder.Property(e => e.ProviderMessageId)
            .HasMaxLength(250);

        builder.Property(e => e.ToRecipientsJson)
            .IsRequired();

        builder.Property(e => e.RetryCount)
            .HasDefaultValue(0);

        builder.Property(e => e.IdempotencyKey)
            .IsRequired()
            .HasMaxLength(100);

        // Indexes
        builder.HasIndex(e => e.MeetingId);
        builder.HasIndex(e => e.MeetingSummaryId);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.RequestedAt);
        builder.HasIndex(e => e.IdempotencyKey).IsUnique();

        // Relationships
        builder.HasOne(e => e.Meeting)
            .WithMany()
            .HasForeignKey(e => e.MeetingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.MeetingSummary)
            .WithMany()
            .HasForeignKey(e => e.MeetingSummaryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.RequestedByUser)
            .WithMany()
            .HasForeignKey(e => e.RequestedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Hard delete yapılmamalı. Soft delete de uygulanmıyor, audit amaçlı tutuluyor.
    }
}
