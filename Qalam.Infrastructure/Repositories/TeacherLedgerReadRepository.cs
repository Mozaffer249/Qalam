using Microsoft.EntityFrameworkCore;
using Qalam.Data.DTOs.Admin;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;

namespace Qalam.Infrastructure.Repositories;

public class TeacherLedgerReadRepository : ITeacherLedgerReadRepository
{
    private readonly ApplicationDBContext _context;

    public TeacherLedgerReadRepository(ApplicationDBContext context)
    {
        _context = context;
    }

    public async Task<List<TeacherLedgerEntryDto>> BuildLedgerAsync(
        int? teacherId,
        int? enrollmentId,
        string? typeFilter,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken = default)
    {
        var entries = new List<TeacherLedgerEntryDto>();

        var earningQ = _context.TeacherEarningLines.AsNoTracking().AsQueryable();
        if (teacherId.HasValue)
            earningQ = earningQ.Where(l => l.TeacherId == teacherId.Value);
        if (enrollmentId.HasValue)
            earningQ = earningQ.Where(l => l.EnrollmentId == enrollmentId.Value);
        if (fromUtc.HasValue)
            earningQ = earningQ.Where(l => l.CreatedAt >= fromUtc.Value);
        if (toUtc.HasValue)
            earningQ = earningQ.Where(l => l.CreatedAt <= toUtc.Value);

        var earnings = await earningQ.Select(l => new
        {
            l.Id,
            l.TeacherId,
            l.Amount,
            l.Currency,
            l.CreatedAt,
            l.Status,
            l.EnrollmentId,
            l.CourseScheduleId,
            BatchStatus = l.PayoutItem != null
                ? (PayoutBatchStatus?)l.PayoutItem.PayoutBatch.Status
                : null,
            CourseTitle = l.Enrollment.Course != null ? l.Enrollment.Course.Title : null,
        }).ToListAsync(cancellationToken);

        foreach (var e in earnings)
        {
            if (e.Status == TeacherEarningLineStatus.Voided)
            {
                entries.Add(new TeacherLedgerEntryDto
                {
                    TransactionKey = $"earn-{e.Id}",
                    Type = "Deduction",
                    Category = "Financial",
                    Direction = "Debit",
                    Amount = e.Amount,
                    Currency = e.Currency,
                    ReasonCode = "RefundClawback",
                    Reason = "Earning voided",
                    Source = "ComplaintResolution",
                    RelatedTransactionKey = $"earn-{e.Id}",
                    EnrollmentId = e.EnrollmentId,
                    ScheduleId = e.CourseScheduleId,
                    TeacherId = e.TeacherId,
                    Status = "Applied",
                    OccurredAt = e.CreatedAt,
                    CourseTitle = e.CourseTitle,
                });
                continue;
            }

            var status = e.Status switch
            {
                TeacherEarningLineStatus.Pending => "Available",
                TeacherEarningLineStatus.OnHold => "OnHold",
                TeacherEarningLineStatus.IncludedInPayout when e.BatchStatus == PayoutBatchStatus.Paid => "Paid",
                TeacherEarningLineStatus.IncludedInPayout => "PendingPayout",
                _ => e.Status.ToString(),
            };

            entries.Add(new TeacherLedgerEntryDto
            {
                TransactionKey = $"earn-{e.Id}",
                Type = "Earning",
                Category = "Financial",
                Direction = "Credit",
                Amount = e.Amount,
                Currency = e.Currency,
                ReasonCode = "SessionEarning",
                Reason = "Session earning",
                Source = "SessionCompleted",
                EnrollmentId = e.EnrollmentId,
                ScheduleId = e.CourseScheduleId,
                TeacherId = e.TeacherId,
                Status = status,
                OccurredAt = e.CreatedAt,
                CourseTitle = e.CourseTitle,
            });
        }

        var refundQ = _context.Refunds.AsNoTracking()
            .Where(r => r.Status == RefundStatus.Succeeded);
        if (teacherId.HasValue)
            refundQ = refundQ.Where(r => r.Enrollment.ApprovedByTeacherId == teacherId.Value);
        if (enrollmentId.HasValue)
            refundQ = refundQ.Where(r => r.EnrollmentId == enrollmentId.Value);
        if (fromUtc.HasValue)
            refundQ = refundQ.Where(r => r.CreatedAt >= fromUtc.Value);
        if (toUtc.HasValue)
            refundQ = refundQ.Where(r => r.CreatedAt <= toUtc.Value);

        var refunds = await refundQ.Select(r => new
        {
            r.Id,
            r.Amount,
            r.Currency,
            r.CreatedAt,
            r.Reason,
            r.EnrollmentId,
            TeacherId = r.Enrollment.ApprovedByTeacherId,
            CourseTitle = r.Enrollment.Course != null ? r.Enrollment.Course.Title : null,
        }).ToListAsync(cancellationToken);

        foreach (var r in refunds)
        {
            entries.Add(new TeacherLedgerEntryDto
            {
                TransactionKey = $"ref-{r.Id}",
                Type = "Refund",
                Category = "Financial",
                Direction = "Debit",
                Amount = r.Amount,
                Currency = r.Currency,
                ReasonCode = "Refund",
                Reason = string.IsNullOrWhiteSpace(r.Reason) ? "Refund" : r.Reason,
                Source = "Refund",
                EnrollmentId = r.EnrollmentId,
                TeacherId = r.TeacherId,
                Status = "Completed",
                OccurredAt = r.CreatedAt,
                CourseTitle = r.CourseTitle,
            });
        }

        var payoutQ = _context.PayoutItems.AsNoTracking()
            .Where(i => i.PayoutBatch.Status == PayoutBatchStatus.Paid);
        if (teacherId.HasValue)
            payoutQ = payoutQ.Where(i => i.TeacherId == teacherId.Value);
        if (fromUtc.HasValue)
            payoutQ = payoutQ.Where(i => i.PayoutBatch.PaidAt >= fromUtc.Value);
        if (toUtc.HasValue)
            payoutQ = payoutQ.Where(i => i.PayoutBatch.PaidAt <= toUtc.Value);

        var payouts = await payoutQ.Select(i => new
        {
            i.Id,
            i.TeacherId,
            i.Amount,
            i.Currency,
            PaidAt = i.PayoutBatch.PaidAt ?? i.PayoutBatch.CreatedAt,
            i.PayoutBatch.MockTransferRef,
        }).ToListAsync(cancellationToken);

        foreach (var p in payouts)
        {
            entries.Add(new TeacherLedgerEntryDto
            {
                TransactionKey = $"payout-{p.Id}",
                Type = "Payout",
                Category = "Financial",
                Direction = "Debit",
                Amount = p.Amount,
                Currency = p.Currency,
                ReasonCode = "PayoutTransfer",
                Reason = p.MockTransferRef ?? "Payout",
                Source = "PayoutBatch",
                TeacherId = p.TeacherId,
                Status = "Paid",
                OccurredAt = p.PaidAt,
            });
        }

        var adjQ = _context.TeacherBalanceAdjustments.AsNoTracking().AsQueryable();
        if (teacherId.HasValue)
            adjQ = adjQ.Where(a => a.TeacherId == teacherId.Value);
        if (fromUtc.HasValue)
            adjQ = adjQ.Where(a => a.CreatedAt >= fromUtc.Value);
        if (toUtc.HasValue)
            adjQ = adjQ.Where(a => a.CreatedAt <= toUtc.Value);

        var adjustments = await adjQ.Select(a => new
        {
            a.Id,
            a.TeacherId,
            a.Amount,
            a.Currency,
            a.Kind,
            a.Status,
            a.ReasonCode,
            a.ReasonText,
            a.RelatedRefundId,
            a.RelatedEarningLineId,
            a.RelatedComplaintId,
            a.CreatedAt,
        }).ToListAsync(cancellationToken);

        foreach (var a in adjustments)
        {
            var type = a.Kind switch
            {
                TeacherBalanceAdjustmentKind.Settlement => "Settlement",
                TeacherBalanceAdjustmentKind.Correction => "Correction",
                _ => "Deduction",
            };

            entries.Add(new TeacherLedgerEntryDto
            {
                TransactionKey = $"adj-{a.Id}",
                Type = type,
                Category = "Financial",
                Direction = "Debit",
                Amount = a.Amount,
                Currency = a.Currency,
                ReasonCode = a.ReasonCode,
                Reason = a.ReasonText,
                Source = a.Kind == TeacherBalanceAdjustmentKind.Settlement ? "Refund" : "AdminAction",
                RelatedTransactionKey = a.RelatedEarningLineId.HasValue
                    ? $"earn-{a.RelatedEarningLineId.Value}"
                    : a.RelatedRefundId.HasValue ? $"ref-{a.RelatedRefundId.Value}" : null,
                ComplaintId = a.RelatedComplaintId,
                TeacherId = a.TeacherId,
                Status = a.Status.ToString(),
                OccurredAt = a.CreatedAt,
            });
        }

        var penQ = _context.TeacherDisciplinaryRecords.AsNoTracking().AsQueryable();
        if (teacherId.HasValue)
            penQ = penQ.Where(p => p.TeacherId == teacherId.Value);
        if (fromUtc.HasValue)
            penQ = penQ.Where(p => p.CreatedAt >= fromUtc.Value);
        if (toUtc.HasValue)
            penQ = penQ.Where(p => p.CreatedAt <= toUtc.Value);

        var penalties = await penQ.Select(p => new
        {
            p.Id,
            p.TeacherId,
            p.Kind,
            p.Amount,
            p.Currency,
            p.ComplaintId,
            p.CourseScheduleId,
            p.ResolutionCode,
            p.Notes,
            p.CreatedAt,
        }).ToListAsync(cancellationToken);

        foreach (var p in penalties)
        {
            entries.Add(new TeacherLedgerEntryDto
            {
                TransactionKey = $"pen-{p.Id}",
                Type = "Penalty",
                Category = p.Kind == TeacherDisciplinaryKind.Warning ? "Disciplinary" : "Financial",
                Direction = p.Amount.GetValueOrDefault() > 0 ? "Debit" : "None",
                Amount = p.Amount.GetValueOrDefault(),
                Currency = p.Currency,
                ReasonCode = p.Kind switch
                {
                    TeacherDisciplinaryKind.Warning => "AdminWarning",
                    TeacherDisciplinaryKind.Fine => "AdminFine",
                    _ => "ComplaintDeduction",
                },
                Reason = p.Notes ?? p.ResolutionCode ?? p.Kind.ToString(),
                Source = p.ComplaintId.HasValue ? "ComplaintResolution" : "AdminAction",
                ComplaintId = p.ComplaintId,
                ScheduleId = p.CourseScheduleId,
                TeacherId = p.TeacherId,
                Status = "Applied",
                OccurredAt = p.CreatedAt,
            });
        }

        if (!string.IsNullOrWhiteSpace(typeFilter) && !typeFilter.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            var filter = typeFilter.Trim();
            entries = entries.Where(e =>
                e.Type.Equals(filter, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        return entries.OrderByDescending(e => e.OccurredAt).ToList();
    }

    public async Task<(decimal Deductions, decimal Penalties, decimal Settlements, int WarningsCount)> GetImpactBucketsAsync(
        int teacherId,
        CancellationToken cancellationToken = default)
    {
        var voided = await _context.TeacherEarningLines.AsNoTracking()
            .Where(l => l.TeacherId == teacherId && l.Status == TeacherEarningLineStatus.Voided)
            .SumAsync(l => (decimal?)l.Amount, cancellationToken) ?? 0m;

        var adjDeductions = await _context.TeacherBalanceAdjustments.AsNoTracking()
            .Where(a => a.TeacherId == teacherId
                        && a.Status == TeacherBalanceAdjustmentStatus.Applied
                        && a.Kind == TeacherBalanceAdjustmentKind.Deduction)
            .SumAsync(a => (decimal?)a.Amount, cancellationToken) ?? 0m;

        var settlements = await _context.TeacherBalanceAdjustments.AsNoTracking()
            .Where(a => a.TeacherId == teacherId
                        && a.Status == TeacherBalanceAdjustmentStatus.Applied
                        && a.Kind == TeacherBalanceAdjustmentKind.Settlement)
            .SumAsync(a => (decimal?)a.Amount, cancellationToken) ?? 0m;

        var penalties = await _context.TeacherDisciplinaryRecords.AsNoTracking()
            .Where(p => p.TeacherId == teacherId
                        && p.Kind != TeacherDisciplinaryKind.Warning
                        && p.Amount != null && p.Amount > 0)
            .SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0m;

        var warnings = await _context.TeacherDisciplinaryRecords.AsNoTracking()
            .CountAsync(p => p.TeacherId == teacherId && p.Kind == TeacherDisciplinaryKind.Warning,
                cancellationToken);

        return (voided + adjDeductions, penalties, settlements, warnings);
    }
}
