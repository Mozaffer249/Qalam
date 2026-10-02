using Qalam.Data.DTOs.Wallet;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Payment;

namespace Qalam.Infrastructure.Abstracts;

public interface IStudentWalletRepository
{
    Task<StudentWallet?> GetByUserIdAsync(int userId, bool track, CancellationToken cancellationToken = default);

    /// <summary>Refreshes a tracked wallet from the database (after acquiring the lock).</summary>
    Task ReloadAsync(StudentWallet wallet, CancellationToken cancellationToken = default);

    Task AddWalletAsync(StudentWallet wallet, CancellationToken cancellationToken = default);

    void AddTransaction(WalletTransaction transaction);

    Task MarkReversedAsync(int transactionId, int reversedByTransactionId, CancellationToken cancellationToken = default);

    Task<WalletTransaction?> FindIdempotentAsync(
        WalletTransactionType type,
        int? paymentId,
        int? refundId,
        CancellationToken cancellationToken = default);

    /// <summary>True when the caller already has an open EF transaction.</summary>
    bool HasActiveTransaction { get; }

    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    Task CommitAsync(CancellationToken cancellationToken = default);

    Task RollbackAsync(CancellationToken cancellationToken = default);

    /// <summary>Serializes balance changes per user (sp_getapplock, transaction-owned).</summary>
    Task AcquireLockAsync(int userId, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<(decimal Added, decimal Spent, decimal Refunded)> GetTotalsAsync(
        int walletId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Wallet owner for a student: the guardian's user when the student has a guardian with a login,
    /// otherwise the student's own user. Null when the student does not exist.
    /// </summary>
    Task<(int UserId, string? Name, bool IsGuardian)?> ResolvePayerForStudentAsync(
        int studentId,
        CancellationToken cancellationToken = default);

    Task<(List<WalletTransactionDto> Items, int TotalCount)> ListTransactionsAsync(
        int walletId,
        IReadOnlyCollection<WalletTransactionType>? types,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
