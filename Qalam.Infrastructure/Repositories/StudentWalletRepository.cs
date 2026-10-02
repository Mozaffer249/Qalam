using Microsoft.EntityFrameworkCore;
using Qalam.Data.DTOs.Wallet;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Payment;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;

namespace Qalam.Infrastructure.Repositories;

public class StudentWalletRepository : IStudentWalletRepository
{
    private readonly ApplicationDBContext _context;

    public StudentWalletRepository(ApplicationDBContext context)
    {
        _context = context;
    }

    public Task<StudentWallet?> GetByUserIdAsync(int userId, bool track, CancellationToken cancellationToken = default)
    {
        var q = track ? _context.StudentWallets : _context.StudentWallets.AsNoTracking();
        return q.FirstOrDefaultAsync(w => w.UserId == userId, cancellationToken);
    }

    public Task ReloadAsync(StudentWallet wallet, CancellationToken cancellationToken = default)
        => _context.Entry(wallet).ReloadAsync(cancellationToken);

    public async Task AddWalletAsync(StudentWallet wallet, CancellationToken cancellationToken = default)
    {
        await _context.StudentWallets.AddAsync(wallet, cancellationToken);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            _context.Entry(wallet).State = EntityState.Detached;
            throw;
        }
    }

    public void AddTransaction(WalletTransaction transaction)
        => _context.WalletTransactions.Add(transaction);

    public Task<WalletTransaction?> FindIdempotentAsync(
        WalletTransactionType type,
        int? paymentId,
        int? refundId,
        CancellationToken cancellationToken = default)
    {
        if (refundId.HasValue)
        {
            return _context.WalletTransactions.AsNoTracking()
                .FirstOrDefaultAsync(t => t.RefundId == refundId, cancellationToken);
        }

        if (paymentId.HasValue && type is WalletTransactionType.TopUp
                or WalletTransactionType.Payment
                or WalletTransactionType.Reversal)
        {
            return _context.WalletTransactions.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Type == type && t.PaymentId == paymentId, cancellationToken);
        }

        return Task.FromResult<WalletTransaction?>(null);
    }

    public async Task MarkReversedAsync(int transactionId, int reversedByTransactionId, CancellationToken cancellationToken = default)
    {
        var tx = await _context.WalletTransactions.FirstOrDefaultAsync(t => t.Id == transactionId, cancellationToken);
        if (tx == null) return;
        tx.Status = WalletTransactionStatus.Reversed;
        tx.ReversedByTransactionId = reversedByTransactionId;
    }

    public bool HasActiveTransaction => _context.Database.CurrentTransaction != null;

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_context.Database.IsRelational())
            await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_context.Database.CurrentTransaction != null)
            await _context.Database.CommitTransactionAsync(cancellationToken);
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_context.Database.CurrentTransaction != null)
            await _context.Database.RollbackTransactionAsync(cancellationToken);
    }

    public async Task AcquireLockAsync(int userId, CancellationToken cancellationToken = default)
    {
        if (!_context.Database.IsRelational() || _context.Database.CurrentTransaction == null)
            return;

        var resource = $"student-wallet-{userId}";
        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"""
             DECLARE @result int;
             EXEC @result = sp_getapplock @Resource = {resource}, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 30000;
             IF @result < 0 THROW 50002, 'Could not acquire wallet lock.', 1;
             """,
            cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);

    public async Task<(decimal Added, decimal Spent, decimal Refunded)> GetTotalsAsync(
        int walletId,
        CancellationToken cancellationToken = default)
    {
        var rows = await _context.WalletTransactions.AsNoTracking()
            .Where(t => t.WalletId == walletId)
            .GroupBy(t => t.Type)
            .Select(g => new { Type = g.Key, Sum = g.Sum(t => t.Amount) })
            .ToListAsync(cancellationToken);

        decimal Sum(params WalletTransactionType[] types)
            => rows.Where(r => types.Contains(r.Type)).Sum(r => r.Sum);

        var added = Sum(WalletTransactionType.TopUp, WalletTransactionType.AdminCredit);
        var spent = -Sum(WalletTransactionType.Payment, WalletTransactionType.AdminDebit)
                    - Sum(WalletTransactionType.Reversal);
        var refunded = Sum(WalletTransactionType.Refund, WalletTransactionType.PolicyReversal);
        return (added, Math.Max(0, spent), refunded);
    }

    public async Task<(int UserId, string? Name, bool IsGuardian)?> ResolvePayerForStudentAsync(
        int studentId,
        CancellationToken cancellationToken = default)
    {
        var row = await _context.Students.AsNoTracking()
            .Where(s => s.Id == studentId)
            .Select(s => new
            {
                s.UserId,
                StudentName = (s.User.FirstName + " " + s.User.LastName).Trim(),
                GuardianUserId = s.Guardian != null ? s.Guardian.UserId : null,
                GuardianName = s.Guardian != null ? s.Guardian.FullName : null
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (row == null)
            return null;
        if (row.GuardianUserId is int guardianUserId)
            return (guardianUserId, row.GuardianName, true);
        return (row.UserId, row.StudentName, false);
    }

    public async Task<(List<WalletTransactionDto> Items, int TotalCount)> ListTransactionsAsync(
        int walletId,
        IReadOnlyCollection<WalletTransactionType>? types,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var q = _context.WalletTransactions.AsNoTracking().Where(t => t.WalletId == walletId);
        if (types is { Count: > 0 })
            q = q.Where(t => types.Contains(t.Type));

        var total = await q.CountAsync(cancellationToken);

        var rows = await q
            .OrderByDescending(t => t.CreatedAt)
            .ThenByDescending(t => t.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new
            {
                t.Id,
                t.Type,
                t.Amount,
                t.BalanceBefore,
                t.BalanceAfter,
                t.Status,
                t.PolicyCaseId,
                t.Currency,
                t.CreatedAt,
                t.Description,
                t.ReasonCode,
                t.PaymentId,
                t.RefundId,
                t.EnrollmentId,
                t.CourseScheduleId,
                t.ComplaintId,
                CourseTitle = t.Enrollment != null && t.Enrollment.Course != null
                    ? t.Enrollment.Course.Title
                    : null,
                SubjectAr = t.Enrollment != null && t.Enrollment.OpenSessionRequest != null
                    ? t.Enrollment.OpenSessionRequest.Subject.NameAr
                    : null,
                SubjectEn = t.Enrollment != null && t.Enrollment.OpenSessionRequest != null
                    ? t.Enrollment.OpenSessionRequest.Subject.NameEn
                    : null,
                RefundReason = t.Refund != null ? t.Refund.Reason : null
            })
            .ToListAsync(cancellationToken);

        var scheduleIds = rows.Where(r => r.CourseScheduleId.HasValue)
            .Select(r => r.CourseScheduleId!.Value)
            .Distinct()
            .ToList();
        var scheduleDates = scheduleIds.Count == 0
            ? new Dictionary<int, DateOnly>()
            : await _context.CourseSchedules.AsNoTracking()
                .Where(s => scheduleIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, s => s.Date, cancellationToken);

        var items = rows.Select(r => new WalletTransactionDto
        {
            Id = r.Id,
            Type = r.Type.ToString(),
            Amount = r.Amount,
            BalanceBefore = r.BalanceBefore,
            BalanceAfter = r.BalanceAfter,
            Status = r.Status.ToString(),
            PolicyCaseId = r.PolicyCaseId,
            Currency = r.Currency,
            CreatedAt = r.CreatedAt,
            Title = r.CourseTitle ?? r.SubjectAr ?? r.SubjectEn,
            SessionLabel = r.CourseScheduleId.HasValue
                           && scheduleDates.TryGetValue(r.CourseScheduleId.Value, out var date)
                ? date.ToString("yyyy-MM-dd")
                : null,
            Description = r.Description ?? r.RefundReason,
            ReasonCode = r.ReasonCode,
            PaymentId = r.PaymentId,
            RefundId = r.RefundId,
            EnrollmentId = r.EnrollmentId,
            CourseScheduleId = r.CourseScheduleId,
            ComplaintId = r.ComplaintId
        }).ToList();

        return (items, total);
    }
}
