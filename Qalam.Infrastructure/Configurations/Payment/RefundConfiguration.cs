using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Payment;

namespace Qalam.Infrastructure.Configurations.Payment;

public class RefundConfiguration : IEntityTypeConfiguration<Refund>
{
    public void Configure(EntityTypeBuilder<Refund> builder)
    {
        builder.ToTable("Refunds");

        builder.HasKey(r => r.Id);

        builder.HasIndex(r => r.PaymentId);
        builder.HasIndex(r => r.EnrollmentId);
        builder.HasIndex(r => r.Status);
        builder.HasIndex(r => r.CreatedAt);

        builder.Property(r => r.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(r => r.Currency).HasMaxLength(3).IsRequired();
        builder.Property(r => r.Reason).HasMaxLength(500).IsRequired();
        builder.Property(r => r.ProviderRefundId).HasMaxLength(120);
        builder.Property(r => r.Status).IsRequired();

        builder.HasOne(r => r.Payment)
            .WithMany(p => p.Refunds)
            .HasForeignKey(r => r.PaymentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Enrollment)
            .WithMany()
            .HasForeignKey(r => r.EnrollmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.InitiatedByUser)
            .WithMany()
            .HasForeignKey(r => r.InitiatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
