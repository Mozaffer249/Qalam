using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Payment;

namespace Qalam.Infrastructure.Configurations.Payment;

public class EnrollmentPaymentConfiguration : IEntityTypeConfiguration<EnrollmentPayment>
{
    public void Configure(EntityTypeBuilder<EnrollmentPayment> builder)
    {
        builder.ToTable("EnrollmentPayments");

        builder.HasKey(ep => ep.Id);

        builder.HasIndex(ep => ep.EnrollmentParticipantId);
        builder.HasIndex(ep => ep.PaymentId);
        builder.HasIndex(ep => ep.Status);

        builder.Property(ep => ep.Status).IsRequired();

        builder.HasOne(ep => ep.Payment)
               .WithMany(p => p.EnrollmentPayments)
               .HasForeignKey(ep => ep.PaymentId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
