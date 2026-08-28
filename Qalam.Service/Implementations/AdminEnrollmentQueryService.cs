using Microsoft.EntityFrameworkCore;
using Qalam.Data.DTOs.Admin;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Service.Abstracts;
using Qalam.Service.Implementations;
using Qalam.Service.Mappers;

namespace Qalam.Service.Implementations;

public class AdminEnrollmentQueryService : IAdminEnrollmentQueryService
{
    private readonly ApplicationDBContext _db;
    private readonly ITeacherLevelRepository _teacherLevelRepository;

    public AdminEnrollmentQueryService(
        ApplicationDBContext db,
        ITeacherLevelRepository teacherLevelRepository)
    {
        _db = db;
        _teacherLevelRepository = teacherLevelRepository;
    }

    public async Task<List<AdminEnrollmentListItemDto>> ListAsync(
        AdminEnrollmentListFilter filter,
        CancellationToken cancellationToken = default)
    {
        var q = _db.Enrollments
            .AsNoTracking()
            .Include(e => e.Course)
            .Include(e => e.PricingSnapshot)
            .Include(e => e.Participants)
                .ThenInclude(p => p.Student)
            .Include(e => e.CourseSchedules)
            .Include(e => e.OpenSessionRequest)
            .AsQueryable();

        if (filter.Status.HasValue)
            q = q.Where(e => e.EnrollmentStatus == filter.Status.Value);
        if (filter.Source.HasValue)
            q = q.Where(e => e.Source == filter.Source.Value);
        if (filter.Kind.HasValue)
            q = q.Where(e => e.Kind == filter.Kind.Value);
        if (filter.IsFreeTrial.HasValue)
            q = q.Where(e => e.IsFreeTrial == filter.IsFreeTrial.Value);
        if (filter.TeacherId.HasValue)
            q = q.Where(e => e.ApprovedByTeacherId == filter.TeacherId.Value
                             || (e.Course != null && e.Course.TeacherId == filter.TeacherId.Value));
        if (filter.StudentId.HasValue)
            q = q.Where(e => e.Participants.Any(p => p.StudentId == filter.StudentId.Value));
        if (filter.CourseId.HasValue)
            q = q.Where(e => e.CourseId == filter.CourseId.Value);
        if (filter.FromUtc.HasValue)
            q = q.Where(e => e.ApprovedAt >= filter.FromUtc.Value || e.CreatedAt >= filter.FromUtc.Value);
        if (filter.ToUtc.HasValue)
            q = q.Where(e => e.ApprovedAt <= filter.ToUtc.Value || e.CreatedAt <= filter.ToUtc.Value);

        var rows = await q
            .OrderByDescending(e => e.ApprovedAt != default ? e.ApprovedAt : e.CreatedAt)
            .Take(500)
            .ToListAsync(cancellationToken);

        var teacherIds = rows.Select(e => e.ApprovedByTeacherId > 0
                ? e.ApprovedByTeacherId
                : e.Course?.TeacherId ?? 0)
            .Where(id => id > 0)
            .Distinct()
            .ToList();
        var teacherNameById = await LoadTeacherNamesAsync(teacherIds, cancellationToken);
        var starterSharePct = await ResolveStarterSharePctAsync(cancellationToken);

        return rows.Select(e => MapListItem(e, teacherNameById, starterSharePct)).ToList();
    }

    public async Task<AdminEnrollmentDetailDto?> GetByIdAsync(
        int enrollmentId,
        CancellationToken cancellationToken = default)
    {
        var e = await _db.Enrollments
            .AsNoTracking()
            .Include(x => x.Course)
                .ThenInclude(c => c!.TeacherSubject)
                    .ThenInclude(ts => ts!.Subject)
                        .ThenInclude(s => s!.Domain)
            .Include(x => x.PricingSnapshot)
            .Include(x => x.Participants)
                .ThenInclude(p => p.Student)
                    .ThenInclude(s => s!.User)
            .Include(x => x.CourseSchedules)
                .ThenInclude(s => s.TeacherAvailability)
                    .ThenInclude(a => a!.TimeSlot)
            .Include(x => x.OpenSessionRequest)
            .FirstOrDefaultAsync(x => x.Id == enrollmentId, cancellationToken);
        if (e == null)
            return null;

        var teacherId = e.ApprovedByTeacherId > 0
            ? e.ApprovedByTeacherId
            : e.Course?.TeacherId ?? 0;
        var teacherNames = await LoadTeacherNamesAsync(
            teacherId > 0 ? [teacherId] : [], cancellationToken);
        var starterSharePct = await ResolveStarterSharePctAsync(cancellationToken);

        var list = MapListItem(e, teacherNames, starterSharePct);
        var orderedSchedules = e.CourseSchedules
            .OrderBy(s => s.Date)
            .ThenBy(s => s.Id)
            .ToList();
        var freeSessions = e.IsFreeTrial && orderedSchedules.Count > 0 ? 1 : 0;
        var paidSessions = Math.Max(0, orderedSchedules.Count - freeSessions);
        var participantCount = e.Participants.Count;
        var succeededCount = e.Participants.Count(p => p.PaymentStatus == PaymentStatus.Succeeded);
        var baseShare = participantCount > 0
            ? Math.Round(list.AmountDue / participantCount, 2, MidpointRounding.AwayFromZero)
            : 0m;

        var detail = new AdminEnrollmentDetailDto
        {
            Id = list.Id,
            EnrollmentStatus = list.EnrollmentStatus,
            Kind = list.Kind,
            Source = list.Source,
            IsFreeTrial = list.IsFreeTrial,
            CourseId = list.CourseId,
            CourseTitle = list.CourseTitle,
            SubjectNameEn = e.Course?.TeacherSubject?.Subject?.NameEn
                ?? list.SubjectNameEn,
            SubjectNameAr = e.Course?.TeacherSubject?.Subject?.NameAr
                ?? list.SubjectNameAr,
            DomainNameEn = e.Course?.TeacherSubject?.Subject?.Domain?.NameEn
                ?? list.DomainNameEn,
            DomainNameAr = e.Course?.TeacherSubject?.Subject?.Domain?.NameAr
                ?? list.DomainNameAr,
            TeacherId = list.TeacherId,
            TeacherName = list.TeacherName,
            PrimaryStudentId = list.PrimaryStudentId,
            PrimaryStudentName = list.PrimaryStudentName,
            ParticipantCount = list.ParticipantCount,
            GrossPackageTotal = list.GrossPackageTotal,
            FreeSessionCredit = list.FreeSessionCredit,
            AmountDue = list.AmountDue,
            AmountPaid = list.AmountPaid,
            PlatformCostAmount = list.PlatformCostAmount,
            Currency = list.Currency,
            ApprovedAt = list.ApprovedAt,
            ActivatedAt = list.ActivatedAt,
            CancelledAt = list.CancelledAt,
            CompletedAt = list.CompletedAt,
            PaymentDeadline = list.PaymentDeadline,
            SessionsCompleted = list.SessionsCompleted,
            SessionsTotal = list.SessionsTotal,
            EnrollmentRequestId = e.EnrollmentRequestId,
            SessionRequestId = e.SessionRequestId,
            SessionOfferId = e.SessionOfferId,
            OwnerUserId = e.OwnerUserId,
            PaidByUserId = e.PaidByUserId,
            CancelledByUserId = e.CancelledByUserId,
            SnapshotTotalPrice = e.PricingSnapshot?.TotalPrice ?? e.AmountDue,
            SnapshotTeacherSharePct = e.PricingSnapshot?.TeacherSharePct ?? 0,
            SnapshotTeacherEarnings = e.PricingSnapshot?.TeacherEarnings ?? 0,
            SnapshotPlatformShare = e.PricingSnapshot?.PlatformShare ?? 0,
            SnapshotTotalMinutes = e.PricingSnapshot?.TotalMinutes ?? 0,
            SnapshotPricePerHour = e.PricingSnapshot?.PricePerHour ?? 0,
            SnapshotEarningsPricePerHour = e.PricingSnapshot?.EarningsPricePerHour,
            SnapshotMarketCode = e.PricingSnapshot?.MarketCode,
            SnapshotSessionTypeCode = e.PricingSnapshot?.SessionTypeCode,
            IsInterviewProofSession = e.IsFreeTrial
                && (e.PricingSnapshot?.TeacherSharePct ?? 0) <= 0,
            IsInterviewPendingAtQuote = list.IsInterviewPendingAtQuote,
            ProjectedTeacherSharePct = list.ProjectedTeacherSharePct,
            ProjectedTeacherEarningsDue = list.ProjectedTeacherEarningsDue,
            ProjectedFreeSessionTeacherDeduction = list.ProjectedFreeSessionTeacherDeduction,
            ProjectedPerSessionTeacherValue = list.ProjectedPerSessionTeacherValue,
            AmountRemaining = Math.Max(0m, list.AmountDue - list.AmountPaid),
            FreeSessionsCount = freeSessions,
            PaidSessionsCount = paidSessions,
            Participants = e.Participants
                .OrderBy(p => p.Id)
                .Select(p =>
                {
                    var isLastPending = e.Kind == EnrollmentKind.Group
                                        && p.PaymentStatus == PaymentStatus.Pending
                                        && e.Participants.Count(x => x.PaymentStatus == PaymentStatus.Pending) == 1;
                    var share = e.Kind == EnrollmentKind.Individual
                        ? list.AmountDue
                        : (isLastPending
                            ? list.AmountDue - (baseShare * succeededCount)
                            : baseShare);
                    return new AdminEnrollmentParticipantDto
                    {
                        ParticipantId = p.Id,
                        StudentId = p.StudentId,
                        StudentName = FormatStudentName(p.Student),
                        PaymentStatus = p.PaymentStatus.ToString(),
                        PaidAt = p.PaidAt,
                        Share = share,
                    };
                }).ToList(),
            Sessions = orderedSchedules
                .Select((s, i) => new AdminEnrollmentSessionDto
                {
                    ScheduleId = s.Id,
                    SessionNumber = i + 1,
                    Date = s.Date,
                    DurationMinutes = s.DurationMinutes,
                    Status = s.Status.ToString(),
                    IsFreeSession = e.IsFreeTrial && i == 0,
                    Title = s.TeacherAvailability?.TimeSlot?.LabelEn
                            ?? s.TeacherAvailability?.TimeSlot?.LabelAr,
                    StartTime = s.TeacherAvailability?.TimeSlot?.StartTime,
                    EndTime = s.TeacherAvailability?.TimeSlot?.EndTime,
                }).ToList()
        };

        detail.PaymentMethod = await _db.EnrollmentPayments
            .AsNoTracking()
            .Where(ep => ep.EnrollmentParticipant.EnrollmentId == enrollmentId
                         && ep.Payment.Status == PaymentStatus.Succeeded)
            .OrderByDescending(ep => ep.Payment.CreatedAt)
            .Select(ep => ep.Payment.PaymentProvider)
            .FirstOrDefaultAsync(cancellationToken);

        detail.Payments = await _db.EnrollmentPayments
            .AsNoTracking()
            .Where(ep => ep.EnrollmentParticipant.EnrollmentId == enrollmentId)
            .OrderByDescending(ep => ep.Payment.CreatedAt)
            .Select(ep => new AdminEnrollmentPaymentDto
            {
                PaymentId = ep.PaymentId,
                Provider = ep.Payment.PaymentProvider,
                InvoiceNumber = ep.Payment.InvoiceNumber,
                TotalAmount = ep.Payment.TotalAmount,
                PaidAt = ep.Payment.Status == PaymentStatus.Succeeded ? ep.Payment.UpdatedAt : null,
                Status = ep.Payment.Status.ToString(),
            })
            .ToListAsync(cancellationToken);

        if (e.CancelledByUserId.HasValue)
        {
            var user = await _db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == e.CancelledByUserId.Value, cancellationToken);
            detail.CancelledByLabel = user == null
                ? $"#{e.CancelledByUserId}"
                : $"{user.FirstName} {user.LastName}".Trim();
        }

        var consumption = await _db.StudentFreeTrialConsumptions
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.EnrollmentId == enrollmentId, cancellationToken);
        if (consumption != null)
        {
            detail.FreeTrialConsumption = new AdminEnrollmentFreeTrialDto
            {
                ConsumptionId = consumption.Id,
                StudentId = consumption.StudentId,
                Status = consumption.Status.ToString(),
                Source = consumption.Source.ToString(),
                ReservedAt = consumption.ReservedAt,
                ConsumedAt = consumption.ConsumedAt,
                CancelledAt = consumption.CancelledAt,
                RestoredEligibility = consumption.RestoredEligibility
            };
        }

        detail.RefundCount = await _db.Refunds
            .AsNoTracking()
            .CountAsync(r => r.EnrollmentId == enrollmentId, cancellationToken);

        return detail;
    }

    private AdminEnrollmentListItemDto MapListItem(
        Data.Entity.Course.Enrollment e,
        Dictionary<int, string> teacherNameById,
        decimal starterSharePct)
    {
        var teacherId = e.ApprovedByTeacherId > 0
            ? e.ApprovedByTeacherId
            : e.Course?.TeacherId ?? 0;
        teacherNameById.TryGetValue(teacherId, out var teacherName);

        var primary = e.Kind == EnrollmentKind.Group && e.LeaderStudentId.HasValue
            ? e.Participants.FirstOrDefault(p => p.StudentId == e.LeaderStudentId)
              ?? e.Participants.FirstOrDefault()
            : e.Participants.FirstOrDefault();

        var snapshot = e.PricingSnapshot;
        var pricePerHour = snapshot?.PricePerHour ?? 0m;
        var totalMinutes = snapshot?.TotalMinutes ?? 0;
        var firstMinutes = e.CourseSchedules
            .OrderBy(s => s.Date).ThenBy(s => s.Id)
            .Select(s => s.DurationMinutes)
            .FirstOrDefault();
        if (firstMinutes <= 0 && totalMinutes > 0)
        {
            var sessionsTotal = Math.Max(1, e.CourseSchedules.Count);
            firstMinutes = totalMinutes / sessionsTotal;
        }
        if (firstMinutes <= 0)
            firstMinutes = 60;

        var netDue = e.AmountDue;
        var gross = netDue;
        var credit = 0m;
        if (e.IsFreeTrial)
        {
            if (pricePerHour > 0)
            {
                var engineGross = totalMinutes > 0
                    ? Math.Round(pricePerHour * totalMinutes / 60m, 2, MidpointRounding.AwayFromZero)
                    : netDue;
                credit = FreeSessionPolicyService.ComputeFreeSessionCredit(
                    pricePerHour, firstMinutes, engineGross > 0 ? engineGross : netDue + credit);
                // Prefer reconstructing: gross ≈ net + credit when credit known
                if (engineGross > 0)
                    gross = engineGross;
                else
                    gross = netDue + credit;
                credit = FreeSessionPolicyService.ComputeFreeSessionCredit(
                    pricePerHour, firstMinutes, gross);
                // Align credit with stored net when possible
                if (gross - credit != netDue && gross >= netDue)
                    credit = Math.Max(0m, gross - netDue);
            }
            else if (snapshot != null && snapshot.TotalMinutes > 0)
            {
                gross = netDue; // unknown hourly
            }
        }

        var platformCost = 0m;
        if (e.IsFreeTrial && snapshot != null && credit > 0)
        {
            if (EnrollmentEarningsProjectionHelper.IsInterviewPendingAtQuote(e) && starterSharePct > 0)
            {
                platformCost = EnrollmentEarningsProjectionHelper.Compute(e, starterSharePct)
                    ?.ProjectedFreeSessionTeacherDeduction ?? 0m;
            }
            else if (snapshot.TeacherSharePct > 0)
            {
                // Snapshot.TeacherEarnings already excludes the free first session; reconstruct forgone share.
                var earnableMinutes = totalMinutes > firstMinutes
                    ? totalMinutes - firstMinutes
                    : 0;
                if (snapshot.TeacherEarnings > 0 && earnableMinutes > 0 && firstMinutes > 0)
                {
                    platformCost = Math.Round(
                        snapshot.TeacherEarnings * firstMinutes / (decimal)earnableMinutes,
                        2,
                        MidpointRounding.AwayFromZero);
                }
                else
                {
                    var earningsHourly = snapshot.EarningsPricePerHour ?? snapshot.PricePerHour;
                    platformCost = Math.Round(
                        earningsHourly * firstMinutes / 60m * (snapshot.TeacherSharePct / 100m),
                        2,
                        MidpointRounding.AwayFromZero);
                }
            }
        }

        var projection = EnrollmentEarningsProjectionHelper.Compute(e, starterSharePct);

        var amountPaid = e.Participants
            .Where(p => p.PaymentStatus == PaymentStatus.Succeeded)
            .Sum(_ => 0m);
        // Prefer enrollment-level paid if PaidByUserId set: use AmountDue when paid
        if (e.PaidByUserId.HasValue || e.Participants.Any(p => p.PaymentStatus == PaymentStatus.Succeeded))
        {
            if (e.EnrollmentStatus is EnrollmentStatus.Active or EnrollmentStatus.Completed
                || e.Participants.All(p => p.PaymentStatus is PaymentStatus.Succeeded or PaymentStatus.Refunded))
                amountPaid = e.AmountDue;
            else
                amountPaid = e.Participants.Count(p => p.PaymentStatus == PaymentStatus.Succeeded) > 0
                    ? e.AmountDue
                    : 0;
        }

        var completed = e.CourseSchedules.Count(s => s.Status == ScheduleStatus.Completed);
        var total = e.CourseSchedules.Count;

        return new AdminEnrollmentListItemDto
        {
            Id = e.Id,
            EnrollmentStatus = e.EnrollmentStatus.ToString(),
            Kind = e.Kind.ToString(),
            Source = e.Source.ToString(),
            IsFreeTrial = e.IsFreeTrial,
            CourseId = e.CourseId,
            CourseTitle = e.Course?.Title,
            TeacherId = teacherId,
            TeacherName = teacherName,
            PrimaryStudentId = primary?.StudentId,
            PrimaryStudentName = FormatStudentName(primary?.Student),
            ParticipantCount = e.Participants.Count,
            GrossPackageTotal = gross,
            FreeSessionCredit = credit,
            AmountDue = netDue,
            AmountPaid = amountPaid,
            PlatformCostAmount = platformCost,
            Currency = snapshot?.Currency ?? "SAR",
            ApprovedAt = e.ApprovedAt,
            ActivatedAt = e.ActivatedAt,
            CancelledAt = e.CancelledAt,
            CompletedAt = e.CompletedAt,
            PaymentDeadline = e.PaymentDeadline,
            SessionsCompleted = completed,
            SessionsTotal = total,
            IsInterviewPendingAtQuote = projection?.IsInterviewPendingAtQuote ?? false,
            ProjectedTeacherSharePct = projection?.ProjectedTeacherSharePct ?? 0,
            ProjectedTeacherEarningsDue = projection?.ProjectedTeacherEarningsDue ?? 0,
            ProjectedFreeSessionTeacherDeduction = projection?.ProjectedFreeSessionTeacherDeduction ?? 0,
            ProjectedPerSessionTeacherValue = projection?.ProjectedPerSessionTeacherValue ?? 0,
        };
    }

    private async Task<decimal> ResolveStarterSharePctAsync(CancellationToken cancellationToken)
    {
        var starter = await _teacherLevelRepository.GetStarterLevelAsync(cancellationToken);
        return starter?.TeacherSharePct ?? 0m;
    }

    private async Task<Dictionary<int, string>> LoadTeacherNamesAsync(
        IReadOnlyList<int> teacherIds,
        CancellationToken cancellationToken)
    {
        if (teacherIds.Count == 0)
            return new Dictionary<int, string>();

        return await _db.Teachers
            .AsNoTracking()
            .Where(t => teacherIds.Contains(t.Id))
            .Select(t => new
            {
                t.Id,
                Name = t.User == null
                    ? ""
                    : ((t.User.FirstName ?? "") + " " + (t.User.LastName ?? "")).Trim()
            })
            .ToDictionaryAsync(
                x => x.Id,
                x => string.IsNullOrWhiteSpace(x.Name) ? $"#{x.Id}" : x.Name,
                cancellationToken);
    }

    private static string? FormatStudentName(Data.Entity.Student.Student? student)
    {
        if (student?.User == null)
            return student == null ? null : $"#{student.Id}";
        var name = $"{student.User.FirstName} {student.User.LastName}".Trim();
        return string.IsNullOrWhiteSpace(name) ? $"#{student.Id}" : name;
    }
}
