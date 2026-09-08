using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Payment;

namespace Qalam.Infrastructure.Configurations.Payment;

public class PaymentTransactionEventConfiguration : IEntityTypeConfiguration<PaymentTransactionEvent>
{
    public void Configure(EntityTypeBuilder<PaymentTransactionEvent> builder)
    {
        builder.ToTable("PaymentTransactionEvents", "payment");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.PaymentProvider).HasMaxLength(40).IsRequired();
        builder.Property(e => e.Currency).HasMaxLength(3);
        builder.Property(e => e.ProviderPaymentId).HasMaxLength(120);
        builder.Property(e => e.ProviderInvoiceId).HasMaxLength(120);
        builder.Property(e => e.ProviderEventId).HasMaxLength(120);
        builder.Property(e => e.CorrelationId).HasMaxLength(80);
        builder.Property(e => e.PayloadHash).HasMaxLength(64);
        builder.Property(e => e.PayloadJson).HasMaxLength(8000);
        builder.Property(e => e.ErrorMessage).HasMaxLength(1000);
        builder.Property(e => e.Notes).HasMaxLength(500);
        builder.Property(e => e.Amount).HasPrecision(18, 2);

        builder.HasIndex(e => e.PaymentId);
        builder.HasIndex(e => e.EnrollmentId);
        builder.HasIndex(e => e.EnrollmentRequestId);
        builder.HasIndex(e => e.OpenSessionRequestId);
        builder.HasIndex(e => e.ReceivedAt);
        builder.HasIndex(e => new { e.PaymentProvider, e.ProviderPaymentId });
        builder.HasIndex(e => new { e.PaymentProvider, e.ProviderInvoiceId });
        builder.HasIndex(e => new { e.PaymentProvider, e.ProviderEventId });
        builder.HasIndex(e => e.PayloadHash);

        builder.HasOne(e => e.Payment)
            .WithMany(p => p.TransactionEvents)
            .HasForeignKey(e => e.PaymentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
