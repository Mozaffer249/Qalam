using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Payment;

namespace Qalam.Infrastructure.Configurations.Payment;

public class PayoutBatchConfiguration : IEntityTypeConfiguration<PayoutBatch>
{
    public void Configure(EntityTypeBuilder<PayoutBatch> builder)
    {
        builder.ToTable("PayoutBatches");

        builder.HasKey(b => b.Id);

        builder.HasIndex(b => b.Status);
        builder.HasIndex(b => b.PeriodStart);

        builder.Property(b => b.TotalAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(b => b.Currency).HasMaxLength(3).IsRequired();
        builder.Property(b => b.MockTransferRef).HasMaxLength(120);
        builder.Property(b => b.Status).IsRequired();

        builder.HasOne(b => b.CreatedByUser)
            .WithMany()
            .HasForeignKey(b => b.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(b => b.Items)
            .WithOne(i => i.PayoutBatch)
            .HasForeignKey(i => i.PayoutBatchId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
