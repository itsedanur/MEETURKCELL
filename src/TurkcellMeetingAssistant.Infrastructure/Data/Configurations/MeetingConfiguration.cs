using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TurkcellMeetingAssistant.Domain.Entities;

namespace TurkcellMeetingAssistant.Infrastructure.Data.Configurations;

public class MeetingConfiguration : IEntityTypeConfiguration<Meeting>
{
    public void Configure(EntityTypeBuilder<Meeting> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Description)
            .HasMaxLength(2000);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(x => x.StatusBeforeArchive)
            .HasConversion<string>();

        // Navigation Properties
        builder.HasOne(x => x.OrganizerUser)
            .WithMany()
            .HasForeignKey(x => x.OrganizerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ApprovedByUser)
            .WithMany()
            .HasForeignKey(x => x.ApprovedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Participants)
            .WithOne(x => x.Meeting)
            .HasForeignKey(x => x.MeetingId)
            .OnDelete(DeleteBehavior.Cascade); // When Meeting is fully deleted, Participants can go, but soft delete will govern

        // Indexes
        builder.HasIndex(x => x.OrganizerUserId);
        builder.HasIndex(x => x.MeetingDate);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.IsArchived);

        // Global Query Filter for Soft Delete
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
