using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TurkcellMeetingAssistant.Domain.Entities;

namespace TurkcellMeetingAssistant.Infrastructure.Data.Configurations;

public class MeetingParticipantConfiguration : IEntityTypeConfiguration<MeetingParticipant>
{
    public void Configure(EntityTypeBuilder<MeetingParticipant> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FullName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Department)
            .HasMaxLength(200);

        builder.Property(x => x.Title)
            .HasMaxLength(200);

        // Normalized Email + MeetingId must be unique for non-deleted participants
        // We will do this via Unique Index with filter
        builder.HasIndex(x => new { x.MeetingId, x.Email })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.HasIndex(x => x.MeetingId);

        // Global Query Filter for Soft Delete
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
