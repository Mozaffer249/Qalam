using Microsoft.EntityFrameworkCore;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Data.Entity.Payment;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Service.Abstracts;
using Qalam.Service.Mappers;

namespace Qalam.Service.Implementations;

public class TeacherFinanceDetailService : ITeacherFinanceDetailService
{
    private readonly ApplicationDBContext _db;
    private readonly ITeacherLevelRepository _teacherLevelRepository;

    public TeacherFinanceDetailService(
        ApplicationDBContext db,
        ITeacherLevelRepository teacherLevelRepository)
    {
        _db = db;
        _teacherLevelRepository = teacherLevelRepository;
    }

    public async Task<TeacherFinanceTransactionDetailDto?> GetTransactionDetailAsync(
        int teacherId,
        string transactionKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(transactionKey))
            return null;

        if (transactionKey.StartsWith("earn-", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(transactionKey["earn-".Length..], out var lineId))
            return await LoadEarningDetailAsync(teacherId, lineId, transactionKey, cancellationToken);

        if (transactionKey.StartsWith("ref-", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(transactionKey["ref-".Length..], out var refundId))
            return await LoadRefundDetailAsync(teacherId, refundId, transactionKey, cancellationToken);

        if (transactionKey.StartsWith("payout-", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(transactionKey["payout-".Length..], out var payoutItemId))
            return await LoadPayoutDetailAsync(teacherId, payoutItemId, transactionKey, cancellationToken);

        return null;
    }

    private async Task<TeacherFinanceTransactionDetailDto?> LoadEarningDetailAsync(
        int teacherId,
        int lineId,
        string transactionKey,
        CancellationToken cancellationToken)
    {
        var line = await _db.TeacherEarningLines
            .AsNoTracking()
            .Include(l => l.Enrollment)
                .ThenInclude(e => e!.PricingSnapshot)
            .Include(l => l.Enrollment)
                .ThenInclude(e => e!.CourseSchedules)
                    .ThenInclude(s => s.TeacherAvailability)
                        .ThenInclude(a => a!.TimeSlot)
            .Include(l => l.Enrollment)
                .ThenInclude(e => e!.Course)
            .Include(l => l.Enrollment)
                .ThenInclude(e => e!.Participants)
                    .ThenInclude(p => p.Student)
                        .ThenInclude(s => s!.User)
            .Include(l => l.CourseSchedule)
                .ThenInclude(s => s!.TeacherAvailability)
                    .ThenInclude(a => a!.TimeSlot)
            .Include(l => l.PayoutItem)
                .ThenInclude(p => p!.PayoutBatch)
            .FirstOrDefaultAsync(l => l.Id == lineId && l.TeacherId == teacherId, cancellationToken);

        if (line?.Enrollment == null)
            return null;

        var enrollment = line.Enrollment;
        var snap = enrollment.PricingSnapshot;
        var (gross, credit, netDue) = FreeSessionPolicyService.ResolveFreeTrialBreakdown(enrollment);
        var starterShare = await ResolveStarterSharePctAsync(cancellationToken);

        var enrollmentId = line.EnrollmentId > 0 ? line.EnrollmentId : enrollment.Id;
        var schedules = await _db.CourseSchedules
            .AsNoTracking()
            .Where(s => s.EnrollmentId == enrollmentId
                        && s.Status != ScheduleStatus.Cancelled
                        && s.Status != ScheduleStatus.Rescheduled)
            .OrderBy(s => s.Date)
            .ThenBy(s => s.Id)
            .ToListAsync(cancellationToken);

        enrollment.CourseSchedules = schedules;
        var projection = EnrollmentEarningsProjectionHelper.Compute(enrollment, starterShare);

        var schedule = line.CourseSchedule
            ?? schedules.FirstOrDefault(s => s.Id == line.CourseScheduleId);
        var sessionIndex = schedule != null
            ? schedules.FindIndex(s => s.Id == schedule.Id)
            : -1;
        var sessionNumber = sessionIndex >= 0 ? sessionIndex + 1 : (int?)null;
        var isFreeSession = enrollment.IsFreeTrial && sessionIndex == 0;

        var totalMinutes = snap?.TotalMinutes > 0
            ? snap.TotalMinutes
            : schedules.Sum(s => s.DurationMinutes);
        var firstMinutes = schedules.FirstOrDefault()?.DurationMinutes ?? 0;
        if (firstMinutes <= 0 && totalMinutes > 0)
            firstMinutes = totalMinutes / Math.Max(1, schedules.Count);
        if (firstMinutes <= 0)
            firstMinutes = 60;

        var packageEarnings = snap?.TeacherEarnings > 0
            ? snap.TeacherEarnings
            : projection?.ProjectedTeacherEarningsDue ?? 0m;
        var earnableMinutes = enrollment.IsFreeTrial && schedules.Count > 0
            ? Math.Max(0, totalMinutes - firstMinutes)
            : totalMinutes;
        var sessionMinutes = schedule?.DurationMinutes ?? 0;

        var uiStatus = ResolveEarningUiStatus(line.Status, line.PayoutItem?.PayoutBatch?.Status);
        var primaryStudent = enrollment.Participants.FirstOrDefault();

        var allLines = await _db.TeacherEarningLines
            .AsNoTracking()
            .Include(l => l.PayoutItem)
                .ThenInclude(p => p!.PayoutBatch)
            .Where(l => l.EnrollmentId == line.EnrollmentId && l.TeacherId == teacherId)
            .OrderBy(l => l.CreatedAt)
            .ToListAsync(cancellationToken);

        var lineInfos = allLines
            .Select(l => new TeacherEnrollmentEarningsHelper.EarningLineInfo(
                l.Status,
                l.PayoutItem?.PayoutBatch?.Status,
                l.Amount))
            .ToList();
        var earningsBreakdown = TeacherEnrollmentEarningsHelper.Compute(
            enrollment,
            lineInfos,
            starterShare);

        var packageTeacherDue = earningsBreakdown.IsInterviewPendingAtQuote
            ? earningsBreakdown.ProjectedTeacherEarningsDue
            : earningsBreakdown.TeacherEarningsDue;
        var accruedNet = earningsBreakdown.AccruedNet;
        var lineByScheduleId = allLines
            .Where(l => l.CourseScheduleId.HasValue && l.Status != TeacherEarningLineStatus.Voided)
            .ToDictionary(l => l.CourseScheduleId!.Value);

        var enrollmentSessions = schedules
            .Select((s, i) =>
            {
                var isFree = enrollment.IsFreeTrial && i == 0;
                lineByScheduleId.TryGetValue(s.Id, out var accrualLine);
                return new TeacherFinanceSessionAccrualDto
                {
                    ScheduleId = s.Id,
                    SessionNumber = i + 1,
                    Date = s.Date,
                    StartTime = s.TeacherAvailability?.TimeSlot?.StartTime,
                    EndTime = s.TeacherAvailability?.TimeSlot?.EndTime,
                    DurationMinutes = s.DurationMinutes,
                    IsFreeSession = isFree,
                    Status = s.Status.ToString(),
                    AccruedAmount = isFree
                        ? null
                        : accrualLine != null
                            ? accrualLine.Amount
                            : null,
                    EarningLineKey = accrualLine != null ? $"earn-{accrualLine.Id}" : null,
                    IsHighlighted = s.Id == schedule?.Id,
                };
            })
            .ToList();

        var enrollmentEarnings = new TeacherFinanceEnrollmentEarningsDto
        {
            EnrollmentId = enrollment.Id,
            EnrollmentStatus = enrollment.EnrollmentStatus.ToString(),
            SessionsCompleted = schedules.Count(s => s.Status == ScheduleStatus.Completed),
            SessionsTotal = schedules.Count,
            AccruedNet = accruedNet,
            PackageTeacherDue = packageTeacherDue,
            RemainingToAccrue = Math.Max(0m, Math.Round(packageTeacherDue - accruedNet, 2, MidpointRounding.AwayFromZero)),
            EnrollmentEarningUiStatus = earningsBreakdown.EarningUiStatus,
            Sessions = enrollmentSessions,
            EarningLines = allLines.Select(l => new TeacherFinanceEarningLineSummaryDto
            {
                LineId = l.Id,
                TransactionKey = $"earn-{l.Id}",
                CourseScheduleId = l.CourseScheduleId,
                Amount = l.Status == TeacherEarningLineStatus.Voided ? -l.Amount : l.Amount,
                Status = l.Status.ToString(),
                EarningUiStatus = ResolveEarningUiStatus(l.Status, l.PayoutItem?.PayoutBatch?.Status),
                CreatedAt = l.CreatedAt,
            }).ToList(),
        };

        return new TeacherFinanceTransactionDetailDto
        {
            Id = transactionKey,
            Type = line.Status == TeacherEarningLineStatus.Voided ? "Refund" : "Payment",
            Status = line.Status == TeacherEarningLineStatus.Pending ? "Pending" : "Completed",
            Amount = line.Status == TeacherEarningLineStatus.Voided ? -line.Amount : line.Amount,
            Currency = line.Currency,
            CreatedAt = line.CreatedAt,
            Description = line.Status == TeacherEarningLineStatus.Voided
                ? "Earning voided (refund)"
                : "Session earning",
            RelatedStudentName = FormatStudentName(primaryStudent?.Student),
            RelatedCourseTitle = enrollment.Course?.Title,
            EnrollmentId = enrollment.Id,
            EarningUiStatus = uiStatus,
            EnrollmentEarnings = enrollmentEarnings,
            Session = schedule == null
                ? null
                : new TeacherFinanceSessionDetailDto
                {
                    ScheduleId = schedule.Id,
                    SessionNumber = sessionNumber,
                    Date = schedule.Date,
                    StartTime = schedule.TeacherAvailability?.TimeSlot?.StartTime,
                    EndTime = schedule.TeacherAvailability?.TimeSlot?.EndTime,
                    DurationMinutes = sessionMinutes,
                    IsFreeSession = isFreeSession,
                    Status = schedule.Status.ToString(),
                },
            Pricing = snap == null
                ? null
                : new TeacherFinancePricingSnapshotDto
                {
                    GrossPackageTotal = gross,
                    FreeSessionCredit = credit,
                    AmountDue = netDue,
                    PricePerHour = snap.PricePerHour,
                    EarningsPricePerHour = snap.EarningsPricePerHour,
                    TotalMinutes = snap.TotalMinutes,
                    TeacherSharePct = snap.TeacherSharePct,
                    TeacherEarningsDue = snap.TeacherEarnings,
                    PlatformShare = snap.PlatformShare,
                    IsInterviewPendingAtQuote = projection?.IsInterviewPendingAtQuote ?? false,
                },
            Projection = projection == null
                ? null
                : new TeacherFinanceProjectionDto
                {
                    ProjectedTeacherSharePct = projection.ProjectedTeacherSharePct,
                    ProjectedTeacherEarningsDue = projection.ProjectedTeacherEarningsDue,
                    ProjectedFreeSessionTeacherDeduction = projection.ProjectedFreeSessionTeacherDeduction,
                    ProjectedPerSessionTeacherValue = projection.ProjectedPerSessionTeacherValue,
                },
            Calculation = new TeacherFinanceCalculationDto
            {
                PackageEarningsUsed = packageEarnings,
                EarnableMinutes = earnableMinutes,
                SessionMinutes = sessionMinutes,
                ProratedAmount = line.Amount,
            },
        };
    }

    private async Task<TeacherFinanceTransactionDetailDto?> LoadRefundDetailAsync(
        int teacherId,
        int refundId,
        string transactionKey,
        CancellationToken cancellationToken)
    {
        var r = await _db.Refunds
            .AsNoTracking()
            .Where(x => x.Id == refundId
                        && x.Status == RefundStatus.Succeeded
                        && (x.Enrollment.ApprovedByTeacherId == teacherId
                            || (x.Enrollment.Course != null && x.Enrollment.Course.TeacherId == teacherId)))
            .Select(x => new
            {
                x.Id,
                x.PaymentId,
                x.EnrollmentId,
                x.Amount,
                x.Currency,
                x.Reason,
                x.CreatedAt,
                PaymentTotal = x.Payment.TotalAmount,
                CourseTitle = x.Enrollment.Course != null ? x.Enrollment.Course.Title : null,
                RefundedTotal = x.Payment.Refunds
                    .Where(rr => rr.Status == RefundStatus.Succeeded)
                    .Sum(rr => rr.Amount),
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (r == null)
            return null;

        var schedules = await _db.CourseSchedules
            .AsNoTracking()
            .Where(s => s.EnrollmentId == r.EnrollmentId
                        && s.Status != ScheduleStatus.Cancelled
                        && s.Status != ScheduleStatus.Rescheduled)
            .Select(s => s.Status)
            .ToListAsync(cancellationToken);

        var used = schedules.Count(s => s == ScheduleStatus.Completed);
        var unused = Math.Max(0, schedules.Count - used);

        var lines = await _db.TeacherEarningLines
            .AsNoTracking()
            .Where(l => l.EnrollmentId == r.EnrollmentId)
            .Select(l => new
            {
                l.Status,
                l.Amount,
                BatchStatus = l.PayoutItem != null
                    ? (PayoutBatchStatus?)l.PayoutItem.PayoutBatch.Status
                    : null
            })
            .ToListAsync(cancellationToken);

        var voided = lines.Where(l => l.Status == TeacherEarningLineStatus.Voided).Sum(l => l.Amount);
        var hasPaid = lines.Any(l =>
            l.Status == TeacherEarningLineStatus.IncludedInPayout
            && l.BatchStatus == PayoutBatchStatus.Paid);
        var payoutImpact = hasPaid ? "AlreadyPaid" : voided > 0 ? "VoidedPending" : "None";
        var platformBear = Math.Max(0m, Math.Round(r.Amount - voided, 2, MidpointRounding.AwayFromZero));

        return new TeacherFinanceTransactionDetailDto
        {
            Id = transactionKey,
            Type = "Refund",
            Status = "Completed",
            Amount = -r.Amount,
            Currency = r.Currency,
            CreatedAt = r.CreatedAt,
            Description = string.IsNullOrWhiteSpace(r.Reason) ? "Refund" : r.Reason,
            RelatedCourseTitle = r.CourseTitle,
            EnrollmentId = r.EnrollmentId,
            Refund = new TeacherFinanceRefundDetailDto
            {
                RefundId = r.Id,
                PaymentId = r.PaymentId,
                Reason = r.Reason,
                PaymentTotalAmount = r.PaymentTotal,
                PaymentRefundedTotal = r.RefundedTotal,
                SessionsUsed = used,
                SessionsUnused = unused,
                TeacherDeductionAmount = voided,
                PlatformBearAmount = platformBear,
                PayoutImpact = payoutImpact,
            },
        };
    }

    private async Task<TeacherFinanceTransactionDetailDto?> LoadPayoutDetailAsync(
        int teacherId,
        int payoutItemId,
        string transactionKey,
        CancellationToken cancellationToken)
    {
        var item = await _db.PayoutItems
            .AsNoTracking()
            .Include(i => i.PayoutBatch)
            .FirstOrDefaultAsync(
                i => i.Id == payoutItemId
                     && i.TeacherId == teacherId
                     && i.PayoutBatch.Status == PayoutBatchStatus.Paid,
                cancellationToken);
        if (item == null)
            return null;

        var lines = await _db.TeacherEarningLines
            .AsNoTracking()
            .Where(l => l.PayoutItemId == item.Id)
            .Select(l => new TeacherFinancePayoutLineSummaryDto
            {
                LineId = l.Id,
                EnrollmentId = l.EnrollmentId,
                CourseTitle = l.Enrollment.Course != null ? l.Enrollment.Course.Title : null,
                Amount = l.Amount,
                CreatedAt = l.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        return new TeacherFinanceTransactionDetailDto
        {
            Id = transactionKey,
            Type = "Payout",
            Status = "Completed",
            Amount = item.Amount,
            Currency = item.Currency,
            CreatedAt = item.PayoutBatch.PaidAt ?? item.PayoutBatch.CreatedAt,
            Description = item.PayoutBatch.MockTransferRef ?? "Payout",
            Payout = new TeacherFinancePayoutDetailDto
            {
                PayoutItemId = item.Id,
                BatchId = item.PayoutBatchId,
                PeriodStart = item.PayoutBatch.PeriodStart,
                PeriodEnd = item.PayoutBatch.PeriodEnd,
                MockTransferRef = item.PayoutBatch.MockTransferRef,
                TotalAmount = item.Amount,
                Lines = lines,
            },
        };
    }

    private async Task<decimal> ResolveStarterSharePctAsync(CancellationToken cancellationToken)
    {
        var starter = await _teacherLevelRepository.GetStarterLevelAsync(cancellationToken);
        return starter?.TeacherSharePct ?? 0m;
    }

    private static string ResolveEarningUiStatus(
        TeacherEarningLineStatus status,
        PayoutBatchStatus? batchStatus)
    {
        return status switch
        {
            TeacherEarningLineStatus.Pending => "Available",
            TeacherEarningLineStatus.Voided => "Refunded",
            TeacherEarningLineStatus.IncludedInPayout when batchStatus == PayoutBatchStatus.Paid => "Paid",
            TeacherEarningLineStatus.IncludedInPayout => "Pending",
            _ => "Pending",
        };
    }

    private static string? FormatStudentName(Data.Entity.Student.Student? student)
    {
        if (student?.User == null)
            return student == null ? null : $"#{student.Id}";
        var name = $"{student.User.FirstName} {student.User.LastName}".Trim();
        return string.IsNullOrWhiteSpace(name) ? $"#{student.Id}" : name;
    }
}
