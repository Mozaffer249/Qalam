using Qalam.Data.DTOs.Wallet;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Payment;

namespace Qalam.Service.Abstracts;

public interface IStudentWalletService
{
    Task<StudentWallet> GetOrCreateAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>Adds money. Idempotent on payment (TopUp/Reversal) or refund links.</summary>
    Task<WalletOperationResult> CreditAsync(WalletEntryRequest request, CancellationToken cancellationToken = default);

    /// <summary>Removes money; fails with <c>WALLET_INSUFFICIENT_BALANCE</c>. Idempotent on payment link.</summary>
    Task<WalletOperationResult> DebitAsync(WalletEntryRequest request, CancellationToken cancellationToken = default);

    Task<WalletSummaryDto> GetSummaryAsync(int userId, CancellationToken cancellationToken = default);

    Task<(List<WalletTransactionDto> Items, int TotalCount)> ListTransactionsAsync(
        int userId,
        string? type,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>Payer wallet for a student (guardian's when linked). Null when the student is unknown.</summary>
    Task<AdminStudentWalletDto?> GetForStudentAsync(int studentId, CancellationToken cancellationToken = default);
}

public class WalletEntryRequest
{
    public int UserId { get; set; }

    /// <summary>Positive amount; the sign is derived from credit vs debit.</summary>
    public decimal Amount { get; set; }

    public WalletTransactionType Type { get; set; }
    public int? PaymentId { get; set; }
    public int? RefundId { get; set; }
    public int? EnrollmentId { get; set; }
    public int? CourseScheduleId { get; set; }
    public int? ComplaintId { get; set; }
    public string? Description { get; set; }
    public string? ReasonCode { get; set; }
    public int? CreatedByUserId { get; set; }
}

public class WalletOperationResult
{
    public bool Succeeded { get; private init; }
    public string? ErrorCode { get; private init; }
    public WalletTransaction? Transaction { get; private init; }
    public bool AlreadyApplied { get; private init; }

    public static WalletOperationResult Ok(WalletTransaction tx, bool alreadyApplied = false)
        => new() { Succeeded = true, Transaction = tx, AlreadyApplied = alreadyApplied };

    public static WalletOperationResult Fail(string code) => new() { ErrorCode = code };
}
