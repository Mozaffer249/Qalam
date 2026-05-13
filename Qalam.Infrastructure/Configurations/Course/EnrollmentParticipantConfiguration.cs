using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Course;

namespace Qalam.Infrastructure.Configurations.Course;

public class EnrollmentParticipantConfiguration : IEntityTypeConfiguration<EnrollmentParticipant>
{
    public void Configure(EntityTypeBuilder<EnrollmentParticipant> builder)
    {
        builder.ToTable("EnrollmentParticipants", "course");

        builder.HasKey(p => p.Id);

        // Indexes
        builder.HasIndex(p => p.EnrollmentId);
        builder.HasIndex(p => p.StudentId);
        builder.HasIndex(p => new { p.EnrollmentId, p.StudentId }).IsUnique();
        builder.HasIndex(p => p.PaymentStatus);

        // Properties
        builder.Property(p => p.PaymentStatus).IsRequired();

        // Relationships
        builder.HasOne(p => p.Student)
               .WithMany()
               .HasForeignKey(p => p.StudentId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.EnrollmentPayments)
               .WithOne(ep => ep.EnrollmentParticipant)
               .HasForeignKey(ep => ep.EnrollmentParticipantId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
