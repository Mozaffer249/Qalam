using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Course;

namespace Qalam.Infrastructure.Configurations.Course;

public class GroupEnrollmentMemberPaymentConfiguration : IEntityTypeConfiguration<GroupEnrollmentMemberPayment>
{
    public void Configure(EntityTypeBuilder<GroupEnrollmentMemberPayment> builder)
    {
        builder.ToTable("GroupEnrollmentMemberPayments", "course");

        builder.HasKey(e => e.Id);

        builder.HasIndex(e => e.CourseGroupEnrollmentMemberId);
        builder.HasIndex(e => e.PaymentId);
        builder.HasIndex(e => new { e.CourseGroupEnrollmentMemberId, e.PaymentId }).IsUnique();

        builder.HasOne(e => e.CourseGroupEnrollmentMember)
               .WithMany(m => m.GroupEnrollmentMemberPayments)
               .HasForeignKey(e => e.CourseGroupEnrollmentMemberId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Payment)
               .WithMany(p => p.GroupEnrollmentMemberPayments)
               .HasForeignKey(e => e.PaymentId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
