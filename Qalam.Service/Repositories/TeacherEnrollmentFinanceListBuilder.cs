using Microsoft.EntityFrameworkCore;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Data.Entity.Payment;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Service.Mappers;

namespace Qalam.Service.Repositories;

public class TeacherEnrollmentFinanceListBuilder : ITeacherEnrollmentFinanceListBuilder
{
    private readonly ApplicationDBContext _db;
    private readonly ITeacherLevelRepository _teacherLevelRepository;
    private readonly ITeacherLedgerReadRepository _ledger;

    public TeacherEnrollmentFinanceListBuilder(
        ApplicationDBContext db,
        ITeacherLevelRepository teacherLevelRepository,
        ITeacherLedgerReadRepository ledger)
    {
        _db = db;
        _teacherLevelRepository = teacherLevelRepository;
        _ledger = ledger;
    }

    public async Task<List<TeacherFinanceTransactionDto>> BuildAsync(
        int teacherId,
        int? enrollmentId,
        string? typeFilter,
        CancellationToken cancellationToken = default)
    {
        var enrollmentRows = await BuildEnrollmentRowsAsync(teacherId, enrollmentId, cancellationToken);
        var teacherLevelRows = await BuildTeacherLevelRowsAsync(teacherId, cancellationToken);

        var items = enrollmentRows.Concat(teacherLevelRows)
            .OrderByDescending(t => t.CreatedAt)
            .ToList();

        return ApplyTypeFilter(items, typeFilter);
    }

    private async Task<List<TeacherFinanceTransactionDto>> BuildEnrollmentRowsAsync(
        int teacherId,
        int? enrollmentIdFilter,
        CancellationToken cancellationToken)
    {
        var enrollmentIds = await ResolveEnrollmentIdsAsync(teacherId, enrollmentIdFilter, cancellationToken);
        if (enrollmentIds.Count == 0)
            return [];

        var starterShare = await ResolveStarterSharePctAsync(cancellationToken);

        var enrollments = await _db.Enrollments
            .AsNoTracking()
            .Include(e => e.Course)
            .Include(e => e.PricingSnapshot)
            .Include(e => e.Participants)
                .ThenInclude(p => p.Student)
                    .ThenInclude(s => s!.User)
            .Where(e => enrollmentIds.Contains(e.Id))
            .ToListAsync(cancellationToken);

        var schedulesByEnrollment = await _db.CourseSchedules
            .AsNoTracking()
            .Where(s => enrollmentIds.Contains(s.EnrollmentId)
                        && s.Status != ScheduleStatus.Cancelled
                        && s.Status != ScheduleStatus.Rescheduled)
            .OrderBy(s => s.Date)
            .ThenBy(s => s.Id)
            .ToListAsync(cancellationToken);

        var linesByEnrollment = await _db.TeacherEarningLines
            .AsNoTracking()
            .Include(l => l.PayoutItem)
                .ThenInclude(p => p!.PayoutBatch)
            .Where(l => l.TeacherId == teacherId && enrollmentIds.Contains(l.EnrollmentId))
            .ToListAsync(cancellationToken);

        var refundsByEnrollment = await _db.Refunds
            .AsNoTracking()
            .Where(r => r.Status == RefundStatus.Succeeded && enrollmentIds.Contains(r.EnrollmentId))
            .GroupBy(r => r.EnrollmentId)
            .Select(g => new
            {
                EnrollmentId = g.Key,
                Total = g.Sum(r => r.Amount),
                LatestAt = g.Max(r => r.CreatedAt),
                Currency = g.Select(r => r.Currency).FirstOrDefault() ?? "SAR",
            })
            .ToListAsync(cancellationToken);

        var adjustments = await _db.TeacherBalanceAdjustments
            .AsNoTracking()
            .Where(a => a.TeacherId == teacherId
                        && a.Status == TeacherBalanceAdjustmentStatus.Applied)
            .Select(a => new
            {
                a.Id,
                a.Amount,
                a.CreatedAt,
                a.Kind,
                a.RelatedRefundId,
                a.RelatedEarningLineId,
                a.RelatedComplaintId,
            })
            .ToListAsync(cancellationToken);

        var earningLineEnrollmentMap = await _db.TeacherEarningLines
            .AsNoTracking()
            .Where(l => l.TeacherId == teacherId)
            .Select(l => new { l.Id, l.EnrollmentId })
            .ToDictionaryAsync(l => l.Id, l => l.EnrollmentId, cancellationToken);

        var refundEnrollmentMap = await _db.Refunds
            .AsNoTracking()
            .Where(r => enrollmentIds.Contains(r.EnrollmentId))
            .Select(r => new { r.Id, r.EnrollmentId })
            .ToDictionaryAsync(r => r.Id, r => r.EnrollmentId, cancellationToken);

        var complaintEnrollmentMap = await _db.SessionComplaints
            .AsNoTracking()
            .Where(c => enrollmentIds.Contains(c.EnrollmentId))
            .Select(c => new { c.Id, c.EnrollmentId })
            .ToDictionaryAsync(c => c.Id, c => c.EnrollmentId, cancellationToken);

        var penaltiesByEnrollment = await _db.TeacherDisciplinaryRecords
            .AsNoTracking()
            .Where(p => p.TeacherId == teacherId
                        && p.ComplaintId != null
                        && p.Kind != TeacherDisciplinaryKind.Warning
                        && p.Amount != null
                        && p.Amount > 0)
            .Join(
                _db.SessionComplaints.AsNoTracking(),
                p => p.ComplaintId,
                c => (int?)c.Id,
                (p, c) => new { c.EnrollmentId, Amount = p.Amount!.Value, p.CreatedAt })
            .Where(x => enrollmentIds.Contains(x.EnrollmentId))
            .GroupBy(x => x.EnrollmentId)
            .Select(g => new
            {
                EnrollmentId = g.Key,
                Total = g.Sum(x => x.Amount),
                LatestAt = g.Max(x => x.CreatedAt),
            })
            .ToListAsync(cancellationToken);

        var adjustmentTotals = new Dictionary<int, (decimal Total, DateTime LatestAt)>();
        foreach (var adj in adjustments)
        {
            int? linkedEnrollmentId = null;
            if (adj.RelatedEarningLineId.HasValue
                && earningLineEnrollmentMap.TryGetValue(adj.RelatedEarningLineId.Value, out var lineEnr))
                linkedEnrollmentId = lineEnr;
            else if (adj.RelatedRefundId.HasValue
                     && refundEnrollmentMap.TryGetValue(adj.RelatedRefundId.Value, out var refEnr))
                linkedEnrollmentId = refEnr;
            else if (adj.RelatedComplaintId.HasValue
                     && complaintEnrollmentMap.TryGetValue(adj.RelatedComplaintId.Value, out var compEnr))
                linkedEnrollmentId = compEnr;

            if (!linkedEnrollmentId.HasValue || !enrollmentIds.Contains(linkedEnrollmentId.Value))
                continue;

            if (!adjustmentTotals.TryGetValue(linkedEnrollmentId.Value, out var bucket))
                bucket = (0m, adj.CreatedAt);

            bucket.Total += adj.Amount;
            if (adj.CreatedAt > bucket.LatestAt)
                bucket.LatestAt = adj.CreatedAt;
            adjustmentTotals[linkedEnrollmentId.Value] = bucket;
        }

        var rows = new List<TeacherFinanceTransactionDto>();

        foreach (var enrollment in enrollments)
        {
            var schedules = schedulesByEnrollment
                .Where(s => s.EnrollmentId == enrollment.Id)
                .ToList();
            enrollment.CourseSchedules = schedules;

            var lines = linesByEnrollment
                .Where(l => l.EnrollmentId == enrollment.Id)
                .ToList();

            var lineInfos = lines
                .Select(l => new TeacherEnrollmentEarningsHelper.EarningLineInfo(
                    l.Status,
                    l.PayoutItem?.PayoutBatch?.Status,
                    l.Amount))
                .ToList();

            var breakdown = TeacherEnrollmentEarningsHelper.Compute(enrollment, lineInfos, starterShare);
            var voided = lines
                .Where(l => l.Status == TeacherEarningLineStatus.Voided)
                .Sum(l => l.Amount);

            var refundBucket = refundsByEnrollment.FirstOrDefault(r => r.EnrollmentId == enrollment.Id);
            var refundTotal = refundBucket?.Total ?? 0m;

            adjustmentTotals.TryGetValue(enrollment.Id, out var adjBucket);
            var adjustmentTotal = adjBucket.Total;

            var penaltyBucket = penaltiesByEnrollment.FirstOrDefault(p => p.EnrollmentId == enrollment.Id);
            var penaltyTotal = penaltyBucket?.Total ?? 0m;

            var net = Math.Round(
                breakdown.AccruedNet - voided - refundTotal - adjustmentTotal - penaltyTotal,
                2,
                MidpointRounding.AwayFromZero);

            var latestAt = lines.Count > 0
                ? lines.Max(l => l.CreatedAt)
                : enrollment.CreatedAt;
            if (refundBucket != null && refundBucket.LatestAt > latestAt)
                latestAt = refundBucket.LatestAt;
            if (adjBucket.Total > 0 && adjBucket.LatestAt > latestAt)
                latestAt = adjBucket.LatestAt;
            if (penaltyBucket != null && penaltyBucket.LatestAt > latestAt)
                latestAt = penaltyBucket.LatestAt;

            var completedSessions = schedules.Count(s => s.Status == ScheduleStatus.Completed);
            var courseTitle = enrollment.Course?.Title ?? $"Enrollment #{enrollment.Id}";
            var description = $"{courseTitle} · {completedSessions}/{schedules.Count} sessions";

            var currency = lines.FirstOrDefault()?.Currency
                ?? refundBucket?.Currency
                ?? "SAR";

            var primaryStudent = enrollment.Participants.FirstOrDefault();

            rows.Add(new TeacherFinanceTransactionDto
            {
                Id = $"enr-{enrollment.Id}",
                Type = "EnrollmentRevenue",
                Status = "Completed",
                Amount = net,
                Currency = currency,
                CreatedAt = latestAt,
                Description = description,
                RelatedStudentName = FormatStudentName(primaryStudent?.Student),
                RelatedCourseTitle = enrollment.Course?.Title,
                EnrollmentId = enrollment.Id,
                EarningUiStatus = breakdown.EarningUiStatus,
                ReasonCode = "EnrollmentRevenue",
                Source = "Enrollment",
                LedgerCategory = "Financial",
            });
        }

        return rows;
    }

    private async Task<List<TeacherFinanceTransactionDto>> BuildTeacherLevelRowsAsync(
        int teacherId,
        CancellationToken cancellationToken)
    {
        var ledger = await _ledger.BuildLedgerAsync(
            teacherId,
            enrollmentId: null,
            typeFilter: null,
            fromUtc: null,
            toUtc: null,
            cancellationToken);

        var enrollmentLinkedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in ledger)
        {
            if (entry.EnrollmentId.HasValue
                && entry.Type is "Earning" or "Refund" or "Deduction" or "Settlement")
                enrollmentLinkedKeys.Add(entry.TransactionKey);
        }

        var rows = new List<TeacherFinanceTransactionDto>();

        foreach (var entry in ledger)
        {
            if (entry.EnrollmentId.HasValue
                && entry.Type is "Earning" or "Refund" or "Deduction")
                continue;

            if (entry.Type == "Settlement" && entry.EnrollmentId.HasValue)
                continue;

            if (entry.Type == "Penalty" && entry.ComplaintId.HasValue)
                continue;

            if (entry.Type is "Deduction" or "Correction" or "Settlement"
                && !string.IsNullOrWhiteSpace(entry.RelatedTransactionKey)
                && enrollmentLinkedKeys.Contains(entry.RelatedTransactionKey))
                continue;

            var signedAmount = entry.Direction.Equals("Debit", StringComparison.OrdinalIgnoreCase)
                ? -entry.Amount
                : entry.Amount;

            var legacyType = entry.Type switch
            {
                "Earning" => "Payment",
                _ => entry.Type,
            };

            rows.Add(new TeacherFinanceTransactionDto
            {
                Id = entry.TransactionKey,
                Type = legacyType,
                Status = entry.Status,
                Amount = signedAmount,
                Currency = entry.Currency,
                CreatedAt = entry.OccurredAt,
                Description = entry.Reason,
                RelatedCourseTitle = entry.CourseTitle,
                EnrollmentId = entry.EnrollmentId,
                EarningUiStatus = entry.Type == "Earning" ? entry.Status : null,
                ScheduleId = entry.ScheduleId,
                ReasonCode = entry.ReasonCode,
                Source = entry.Source,
                RelatedTransactionKey = entry.RelatedTransactionKey,
                LedgerCategory = entry.Category,
                ComplaintId = entry.ComplaintId,
            });
        }

        return rows;
    }

    private async Task<HashSet<int>> ResolveEnrollmentIdsAsync(
        int teacherId,
        int? enrollmentIdFilter,
        CancellationToken cancellationToken)
    {
        if (enrollmentIdFilter.HasValue)
            return [enrollmentIdFilter.Value];

        var fromLines = await _db.TeacherEarningLines
            .AsNoTracking()
            .Where(l => l.TeacherId == teacherId && l.EnrollmentId > 0)
            .Select(l => l.EnrollmentId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var fromRefunds = await _db.Refunds
            .AsNoTracking()
            .Where(r => r.Status == RefundStatus.Succeeded
                        && (r.Enrollment.ApprovedByTeacherId == teacherId
                            || (r.Enrollment.Course != null && r.Enrollment.Course.TeacherId == teacherId)))
            .Select(r => r.EnrollmentId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var fromAdjustments = await _db.TeacherBalanceAdjustments
            .AsNoTracking()
            .Where(a => a.TeacherId == teacherId && a.Status == TeacherBalanceAdjustmentStatus.Applied)
            .Select(a => new { a.RelatedEarningLineId, a.RelatedRefundId, a.RelatedComplaintId })
            .ToListAsync(cancellationToken);

        var ids = new HashSet<int>(fromLines);
        foreach (var id in fromRefunds)
            ids.Add(id);

        var earningLineIds = fromAdjustments
            .Where(a => a.RelatedEarningLineId.HasValue)
            .Select(a => a.RelatedEarningLineId!.Value)
            .Distinct()
            .ToList();

        if (earningLineIds.Count > 0)
        {
            var linked = await _db.TeacherEarningLines
                .AsNoTracking()
                .Where(l => earningLineIds.Contains(l.Id))
                .Select(l => l.EnrollmentId)
                .ToListAsync(cancellationToken);
            foreach (var id in linked)
                ids.Add(id);
        }

        var refundIds = fromAdjustments
            .Where(a => a.RelatedRefundId.HasValue)
            .Select(a => a.RelatedRefundId!.Value)
            .Distinct()
            .ToList();

        if (refundIds.Count > 0)
        {
            var linked = await _db.Refunds
                .AsNoTracking()
                .Where(r => refundIds.Contains(r.Id))
                .Select(r => r.EnrollmentId)
                .ToListAsync(cancellationToken);
            foreach (var id in linked)
                ids.Add(id);
        }

        var complaintIds = fromAdjustments
            .Where(a => a.RelatedComplaintId.HasValue)
            .Select(a => a.RelatedComplaintId!.Value)
            .Distinct()
            .ToList();

        if (complaintIds.Count > 0)
        {
            var linked = await _db.SessionComplaints
                .AsNoTracking()
                .Where(c => complaintIds.Contains(c.Id))
                .Select(c => c.EnrollmentId)
                .ToListAsync(cancellationToken);
            foreach (var id in linked)
                ids.Add(id);
        }

        return ids;
    }

    private static List<TeacherFinanceTransactionDto> ApplyTypeFilter(
        List<TeacherFinanceTransactionDto> items,
        string? typeFilter)
    {
        if (string.IsNullOrWhiteSpace(typeFilter) || typeFilter.Equals("all", StringComparison.OrdinalIgnoreCase))
            return items;

        var filter = typeFilter.Trim();
        return filter.ToLowerInvariant() switch
        {
            "payment" or "earning" or "earnings" or "enrollmentrevenue" =>
                items.Where(i => i.Type.Equals("EnrollmentRevenue", StringComparison.OrdinalIgnoreCase)).ToList(),
            "refund" or "refunds" =>
                items.Where(i =>
                    i.Type.Equals("EnrollmentRevenue", StringComparison.OrdinalIgnoreCase) && i.Amount < 0
                    || i.Type.Equals("Refund", StringComparison.OrdinalIgnoreCase)).ToList(),
            "payout" or "payouts" =>
                items.Where(i => i.Type.Equals("Payout", StringComparison.OrdinalIgnoreCase)).ToList(),
            "deduction" or "deductions" =>
                items.Where(i =>
                    i.Type.Equals("Deduction", StringComparison.OrdinalIgnoreCase)
                    || (i.Type.Equals("EnrollmentRevenue", StringComparison.OrdinalIgnoreCase) && i.Amount < 0))
                    .ToList(),
            "penalty" or "penalties" =>
                items.Where(i => i.Type.Equals("Penalty", StringComparison.OrdinalIgnoreCase)).ToList(),
            "settlement" or "settlements" =>
                items.Where(i => i.Type.Equals("Settlement", StringComparison.OrdinalIgnoreCase)).ToList(),
            _ => items.Where(i => i.Type.Equals(filter, StringComparison.OrdinalIgnoreCase)).ToList(),
        };
    }

    private async Task<decimal> ResolveStarterSharePctAsync(CancellationToken cancellationToken)
    {
        var starter = await _teacherLevelRepository.GetStarterLevelAsync(cancellationToken);
        return starter?.TeacherSharePct ?? 0m;
    }

    private static string? FormatStudentName(Data.Entity.Student.Student? student)
    {
        if (student?.User == null)
            return student == null ? null : $"#{student.Id}";
        var name = $"{student.User.FirstName} {student.User.LastName}".Trim();
        return string.IsNullOrWhiteSpace(name) ? $"#{student.Id}" : name;
    }
}
