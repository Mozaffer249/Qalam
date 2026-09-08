using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Payment;

namespace Qalam.Infrastructure.Configurations.Payment;

public class PaymentReconciliationRunConfiguration : IEntityTypeConfiguration<PaymentReconciliationRun>
{
    public void Configure(EntityTypeBuilder<PaymentReconciliationRun> builder)
    {
        builder.ToTable("PaymentReconciliationRuns", "payment");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.PaymentProvider).HasMaxLength(40).IsRequired();
        builder.Property(e => e.ScheduleKey).HasMaxLength(80);
        builder.Property(e => e.ErrorSummary).HasMaxLength(2000);

        builder.HasIndex(e => e.StartedAt);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.ScheduleKey)
            .IsUnique()
            .HasFilter("[ScheduleKey] IS NOT NULL");
    }
}
