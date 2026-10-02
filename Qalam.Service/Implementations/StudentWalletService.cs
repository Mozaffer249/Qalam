using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Qalam.Data.DTOs.Wallet;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Payment;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class StudentWalletService : IStudentWalletService
{
    public const string InsufficientBalance = "WALLET_INSUFFICIENT_BALANCE";
    public const string InvalidAmount = "WALLET_INVALID_AMOUNT";

    private readonly IStudentWalletRepository _wallets;
    private readonly PaymentSettings _settings;

    public StudentWalletService(IStudentWalletRepository wallets, IOptions<PaymentSettings> settings)
    {
        _wallets = wallets;
        _settings = settings.Value;
    }

    public async Task<StudentWallet> GetOrCreateAsync(int userId, CancellationToken cancellationToken = default)
    {
        var wallet = await _wallets.GetByUserIdAsync(userId, track: true, cancellationToken);
        if (wallet != null)
            return wallet;

        wallet = new StudentWallet
        {
            UserId = userId,
            Balance = 0,
            Currency = _settings.DefaultCurrency,
            CreatedAt = DateTime.UtcNow
        };
        try
        {
            await _wallets.AddWalletAsync(wallet, cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Concurrent first access created it; use the committed row.
            wallet = await _wallets.GetByUserIdAsync(userId, track: true, cancellationToken)
                     ?? throw new InvalidOperationException($"Wallet for user {userId} could not be created.");
        }
        return wallet;
    }

    public Task<WalletOperationResult> CreditAsync(WalletEntryRequest request, CancellationToken cancellationToken = default)
        => ApplyAsync(request, sign: 1, cancellationToken);

    public Task<WalletOperationResult> DebitAsync(WalletEntryRequest request, CancellationToken cancellationToken = default)
        => ApplyAsync(request, sign: -1, cancellationToken);

    private async Task<WalletOperationResult> ApplyAsync(
        WalletEntryRequest request,
        int sign,
        CancellationToken cancellationToken)
    {
        var amount = Math.Round(request.Amount, 2, MidpointRounding.AwayFromZero);
        if (amount <= 0)
            return WalletOperationResult.Fail(InvalidAmount);

        var ownsTransaction = !_wallets.HasActiveTransaction;
        if (ownsTransaction)
            await _wallets.BeginTransactionAsync(cancellationToken);

        try
        {
            var wallet = await GetOrCreateAsync(request.UserId, cancellationToken);
            await _wallets.AcquireLockAsync(request.UserId, cancellationToken);
            await _wallets.ReloadAsync(wallet, cancellationToken);

            var existing = await _wallets.FindIdempotentAsync(
                request.Type, request.PaymentId, request.RefundId, cancellationToken);
            if (existing != null)
            {
                if (ownsTransaction)
                    await _wallets.CommitAsync(cancellationToken);
                return WalletOperationResult.Ok(existing, alreadyApplied: true);
            }

            var signed = sign * amount;
            if (signed < 0 && wallet.Balance + signed < 0)
            {
                if (ownsTransaction)
                    await _wallets.RollbackAsync(cancellationToken);
                return WalletOperationResult.Fail(InsufficientBalance);
            }

            var balanceBefore = wallet.Balance;
            wallet.Balance += signed;
            wallet.UpdatedAt = DateTime.UtcNow;

            var tx = new WalletTransaction
            {
                WalletId = wallet.Id,
                Type = request.Type,
                Amount = signed,
                BalanceBefore = balanceBefore,
                BalanceAfter = wallet.Balance,
                Status = WalletTransactionStatus.Completed,
                Currency = wallet.Currency,
                PaymentId = request.PaymentId,
                RefundId = request.RefundId,
                EnrollmentId = request.EnrollmentId,
                CourseScheduleId = request.CourseScheduleId,
                ComplaintId = request.ComplaintId,
                PolicyCaseId = request.PolicyCaseId,
                Description = Truncate(request.Description, 300),
                ReasonCode = Truncate(request.ReasonCode, 64),
                CreatedByUserId = request.CreatedByUserId,
                CreatedAt = DateTime.UtcNow
            };
            _wallets.AddTransaction(tx);
            await _wallets.SaveChangesAsync(cancellationToken);

            if (request.ReversesTransactionId is int reversedId)
            {
                await _wallets.MarkReversedAsync(reversedId, tx.Id, cancellationToken);
                await _wallets.SaveChangesAsync(cancellationToken);
            }

            if (ownsTransaction)
                await _wallets.CommitAsync(cancellationToken);
            return WalletOperationResult.Ok(tx);
        }
        catch
        {
            if (ownsTransaction)
                await _wallets.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<WalletSummaryDto> GetSummaryAsync(int userId, CancellationToken cancellationToken = default)
    {
        var wallet = await _wallets.GetByUserIdAsync(userId, track: false, cancellationToken);
        var summary = new WalletSummaryDto
        {
            Currency = wallet?.Currency ?? _settings.DefaultCurrency,
            MinTopUp = _settings.Wallet.MinTopUp,
            MaxTopUp = _settings.Wallet.MaxTopUp
        };
        if (wallet == null)
            return summary;

        var (added, spent, refunded) = await _wallets.GetTotalsAsync(wallet.Id, cancellationToken);
        summary.WalletId = wallet.Id;
        summary.Balance = wallet.Balance;
        summary.TotalAdded = added;
        summary.TotalSpent = spent;
        summary.TotalRefunded = refunded;
        return summary;
    }

    public async Task<(List<WalletTransactionDto> Items, int TotalCount)> ListTransactionsAsync(
        int userId,
        string? type,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var wallet = await _wallets.GetByUserIdAsync(userId, track: false, cancellationToken);
        if (wallet == null)
            return (new List<WalletTransactionDto>(), 0);

        return await _wallets.ListTransactionsAsync(
            wallet.Id, ResolveTypes(type), page, pageSize, cancellationToken);
    }

    public async Task<AdminStudentWalletDto?> GetForStudentAsync(int studentId, CancellationToken cancellationToken = default)
    {
        var payer = await _wallets.ResolvePayerForStudentAsync(studentId, cancellationToken);
        if (payer == null)
            return null;

        return new AdminStudentWalletDto
        {
            PayerUserId = payer.Value.UserId,
            PayerName = payer.Value.Name,
            IsGuardianWallet = payer.Value.IsGuardian,
            Summary = await GetSummaryAsync(payer.Value.UserId, cancellationToken)
        };
    }

    /// <summary>Maps the UI filter (Added / Spent / Refunded) or an exact type name.</summary>
    private static IReadOnlyCollection<WalletTransactionType>? ResolveTypes(string? filter)
    {
        switch (filter?.Trim().ToLowerInvariant())
        {
            case null or "" or "all":
                return null;
            case "added":
                return new[] { WalletTransactionType.TopUp, WalletTransactionType.AdminCredit };
            case "spent":
                return new[] { WalletTransactionType.Payment, WalletTransactionType.AdminDebit, WalletTransactionType.Reversal };
            case "refunded":
                return new[] { WalletTransactionType.Refund, WalletTransactionType.PolicyReversal };
        }

        return Enum.TryParse<WalletTransactionType>(filter, ignoreCase: true, out var exact)
            ? new[] { exact }
            : null;
    }

    private static string? Truncate(string? s, int max)
        => string.IsNullOrEmpty(s) ? s : (s.Length <= max ? s : s[..max]);
}
