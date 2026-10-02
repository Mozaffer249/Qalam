using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Payment;

namespace Qalam.Infrastructure.Configurations.Payment;

public class StudentWalletConfiguration : IEntityTypeConfiguration<StudentWallet>
{
    public void Configure(EntityTypeBuilder<StudentWallet> builder)
    {
        builder.ToTable("StudentWallets");

        builder.HasKey(w => w.Id);
        builder.HasIndex(w => w.UserId).IsUnique();

        builder.Property(w => w.Balance).HasPrecision(18, 2).IsRequired();
        builder.Property(w => w.Currency).HasMaxLength(3).IsRequired();

        builder.HasOne(w => w.User)
            .WithMany()
            .HasForeignKey(w => w.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class WalletTransactionConfiguration : IEntityTypeConfiguration<WalletTransaction>
{
    public void Configure(EntityTypeBuilder<WalletTransaction> builder)
    {
        builder.ToTable("WalletTransactions");

        builder.HasKey(t => t.Id);
        builder.HasIndex(t => new { t.WalletId, t.CreatedAt });

        // Idempotency: one top-up credit / one wallet debit per payment, one credit per refund.
        builder.HasIndex(t => new { t.Type, t.PaymentId })
            .IsUnique()
            .HasFilter($"[PaymentId] IS NOT NULL AND [Type] IN ({(int)WalletTransactionType.TopUp}, {(int)WalletTransactionType.Payment}, {(int)WalletTransactionType.Reversal})");
        builder.HasIndex(t => t.RefundId)
            .IsUnique()
            .HasFilter("[RefundId] IS NOT NULL");

        builder.Property(t => t.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(t => t.BalanceAfter).HasPrecision(18, 2).IsRequired();
        builder.Property(t => t.Currency).HasMaxLength(3).IsRequired();
        builder.Property(t => t.Description).HasMaxLength(300);
        builder.Property(t => t.ReasonCode).HasMaxLength(64);

        builder.HasOne(t => t.Wallet)
            .WithMany(w => w.Transactions)
            .HasForeignKey(t => t.WalletId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Payment)
            .WithMany()
            .HasForeignKey(t => t.PaymentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Refund)
            .WithMany()
            .HasForeignKey(t => t.RefundId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Enrollment)
            .WithMany()
            .HasForeignKey(t => t.EnrollmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
