using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.context;
using Qalam.Service.Abstracts;
using Qalam.Service.Helpers;
using Qalam.Service.Models.Policy;

namespace Qalam.Service.Implementations;

public class PolicyContextBuilder : IPolicyContextBuilder
{
    private readonly ApplicationDBContext _db;
    private readonly IPolicyResolver _resolver;

    public PolicyContextBuilder(ApplicationDBContext db, IPolicyResolver resolver)
    {
        _db = db;
        _resolver = resolver;
    }

    public Task<int?> GetEnrollmentIdForScheduleAsync(int scheduleId, CancellationToken cancellationToken = default)
        => _db.CourseSchedules.AsNoTracking()
            .Where(s => s.Id == scheduleId)
            .Select(s => (int?)s.EnrollmentId)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<PolicyContextBundle?> BuildAsync(
        int enrollmentId,
        PolicyCaseKind kind,
        int? targetScheduleId = null,
        PolicyStudentChoice choice = PolicyStudentChoice.None,
        CancellationToken cancellationToken = default)
    {
        var enrollment = await _db.Enrollments
            .Include(e => e.Participants)
            .Include(e => e.EnrollmentRequest)
            .Include(e => e.PricingSnapshot)
            .Include(e => e.PolicyVersion)
            .Include(e => e.CourseSchedules).ThenInclude(cs => cs.Attendances)
            .Include(e => e.CourseSchedules).ThenInclude(cs => cs.TeacherAvailability).ThenInclude(a => a.TimeSlot)
            .AsSplitQuery()
            .FirstOrDefaultAsync(e => e.Id == enrollmentId, cancellationToken);
        if (enrollment == null)
            return null;

        var paymentRows = await _db.EnrollmentPayments.AsNoTracking()
            .Where(ep => ep.EnrollmentParticipant.EnrollmentId == enrollmentId
                         && (ep.Payment.Status == PaymentStatus.Succeeded || ep.Payment.Status == PaymentStatus.Refunded))
            .Select(ep => new
            {
                ep.PaymentId,
                ep.Payment.TotalAmount,
                ep.Payment.Currency,
                Refunded = ep.Payment.Refunds
                    .Where(r => r.Status == RefundStatus.Succeeded)
                    .Sum(r => (decimal?)r.Amount) ?? 0m
            })
            .ToListAsync(cancellationToken);
        var payments = paymentRows
            .GroupBy(p => p.PaymentId)
            .Select(g => g.First())
            .Select(p => new PolicyPaymentInfo { PaymentId = p.PaymentId, Paid = p.TotalAmount, AlreadyRefunded = p.Refunded })
            .ToList();

        // A rescheduled session is represented by its successor; replacements for cancelled sessions are extra.
        var rescheduledIds = enrollment.CourseSchedules
            .Where(s => s.Status == ScheduleStatus.Rescheduled)
            .Select(s => s.Id)
            .ToHashSet();
        var packageSchedules = enrollment.CourseSchedules
            .Where(s => s.Status != ScheduleStatus.Rescheduled
                        && (s.ReplacesScheduleId == null || rescheduledIds.Contains(s.ReplacesScheduleId.Value)))
            .ToList();
        var sessions = enrollment.CourseSchedules
            .Select(s => new PolicySessionInfo
            {
                ScheduleId = s.Id,
                DurationMinutes = s.DurationMinutes,
                Usage = UsageOf(s),
                StartUtc = SessionAttendanceRules.ResolveStartUtc(s),
                IsReplacement = s.ReplacesScheduleId != null
            })
            .ToList();

        var snapshot = enrollment.PricingSnapshot;
        var shareRatio = snapshot == null
            ? 0m
            : snapshot.TotalPrice > 0
                ? Math.Clamp(snapshot.TeacherEarnings / snapshot.TotalPrice, 0m, 1m)
                : Math.Clamp(snapshot.TeacherSharePct / 100m, 0m, 1m);

        var ctx = new PolicyContext
        {
            Kind = kind,
            NowUtc = DateTime.UtcNow,
            Currency = paymentRows.Select(p => p.Currency).FirstOrDefault(c => !string.IsNullOrWhiteSpace(c))
                       ?? snapshot?.Currency ?? "SAR",
            IsFreeTrial = enrollment.IsFreeTrial,
            IsGroup = enrollment.Kind == EnrollmentKind.Group,
            HasStarted = EnrollmentLifecycleRules.HasSessionStarted(enrollment),
            FirstSessionStartUtc = sessions
                .Where(s => s.Usage is PolicySessionUsage.Upcoming or PolicySessionUsage.InProgress or PolicySessionUsage.Completed)
                .Select(s => s.StartUtc)
                .Where(s => s != null)
                .Min(),
            Payments = payments,
            Sessions = sessions,
            PackageSessionCount = packageSchedules.Count,
            PackageMinutes = snapshot?.TotalMinutes > 0 ? snapshot.TotalMinutes : packageSchedules.Sum(s => s.DurationMinutes),
            TeacherShareRatio = shareRatio,
            TargetScheduleId = targetScheduleId,
            Choice = choice
        };

        var policy = await _resolver.ForEnrollmentAsync(enrollment, cancellationToken);
        return new PolicyContextBundle { Enrollment = enrollment, Context = ctx, Policy = policy };
    }

    private static PolicySessionUsage UsageOf(CourseSchedule s) => s.Status switch
    {
        ScheduleStatus.Scheduled => PolicySessionUsage.Upcoming,
        ScheduleStatus.InProgress => PolicySessionUsage.InProgress,
        ScheduleStatus.Completed when s.Attendances.Count > 0
                                      && s.Attendances.All(a => a.Status == SessionAttendanceStatus.Absent)
            => PolicySessionUsage.StudentNoShow,
        ScheduleStatus.Completed => PolicySessionUsage.Completed,
        ScheduleStatus.Cancelled when s.CancellationReason == ScheduleCancellationReason.TeacherNoShow
            => PolicySessionUsage.TeacherNoShow,
        _ => PolicySessionUsage.Cancelled
    };
}
