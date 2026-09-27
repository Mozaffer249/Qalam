using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Data.Entity.Pricing;
using Qalam.Data.Entity.Student;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public interface IFreeSessionPolicyService
{
    bool IsEligiblePackage(bool isGroup, int sessionCount);

    Task<bool> IsStudentEligibleForFreeTrialAsync(int studentId, CancellationToken cancellationToken = default);

    /// <summary>Legacy — prefer <see cref="ReserveStudentFreeTrialAsync"/>.</summary>
    Task MarkStudentFreeTrialUsedAsync(int studentId, CancellationToken cancellationToken = default);

    Task ReserveStudentFreeTrialAsync(
        int studentId,
        Enrollment enrollment,
        FreeTrialConsumptionSource source,
        int teacherId,
        int domainId,
        int? openSessionRequestId = null,
        CancellationToken cancellationToken = default);

    Task MarkConsumptionConsumedAsync(
        int enrollmentId,
        int courseScheduleId,
        CancellationToken cancellationToken = default);

    Task CancelConsumptionBeforeStartAsync(
        int enrollmentId,
        int? cancelledByUserId,
        string? reason,
        CancellationToken cancellationToken = default);

    Task TryRevertTeacherInterviewFromEnrollmentAsync(
        int enrollmentId,
        CancellationToken cancellationToken = default);

    Task TryCompleteTeacherInterviewAsync(
        int teacherId,
        int domainId,
        int? enrollmentId = null,
        int? courseScheduleId = null,
        CancellationToken cancellationToken = default);
}

public class FreeSessionPolicyService : IFreeSessionPolicyService
{
    private readonly ApplicationDBContext _db;
    private readonly IStudentRepository _studentRepository;
    private readonly ITeacherRepository _teacherRepository;
    private readonly ITeacherLevelRepository _teacherLevelRepository;
    private readonly ITeacherDomainPricingRepository _domainPricingRepository;

    public FreeSessionPolicyService(
        ApplicationDBContext db,
        IStudentRepository studentRepository,
        ITeacherRepository teacherRepository,
        ITeacherLevelRepository teacherLevelRepository,
        ITeacherDomainPricingRepository domainPricingRepository)
    {
        _db = db;
        _studentRepository = studentRepository;
        _teacherRepository = teacherRepository;
        _teacherLevelRepository = teacherLevelRepository;
        _domainPricingRepository = domainPricingRepository;
    }

    public bool IsEligiblePackage(bool isGroup, int sessionCount) =>
        sessionCount >= 1;

    /// <summary>
    /// First-session student credit: minutes/60 × pricePerHour, capped at package total.
    /// </summary>
    public static decimal ComputeFreeSessionCredit(
        decimal pricePerHour,
        int firstSessionMinutes,
        decimal packageTotal)
    {
        if (pricePerHour <= 0 || firstSessionMinutes <= 0 || packageTotal <= 0)
            return 0m;
        var raw = Math.Round(
            pricePerHour * firstSessionMinutes / 60m,
            2,
            MidpointRounding.AwayFromZero);
        return Math.Min(packageTotal, raw);
    }

    /// <summary>
    /// Resolve first-session minutes for teaser/credit: first fixed session, else
    /// course session duration, else equal split of total package minutes.
    /// </summary>
    public static int ResolveFirstSessionMinutes(
        int? firstSessionDurationMinutes,
        int? sessionDurationMinutes,
        int? totalMinutes,
        int? sessionCount)
    {
        if (firstSessionDurationMinutes is > 0)
            return firstSessionDurationMinutes.Value;
        if (sessionDurationMinutes is > 0)
            return sessionDurationMinutes.Value;
        if (totalMinutes is > 0 && sessionCount is > 0)
            return totalMinutes.Value / sessionCount.Value;
        return 0;
    }

    /// <summary>
    /// Derive student hourly rate from a gross package total and total minutes.
    /// </summary>
    public static decimal DerivePricePerHour(decimal grossPackageTotal, int totalMinutes)
    {
        if (grossPackageTotal <= 0 || totalMinutes <= 0)
            return 0m;
        return Math.Round(grossPackageTotal * 60m / totalMinutes, 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Preview amounts for APIs: when eligible apply first-session credit; else credit 0 and due = gross.
    /// </summary>
    public static (decimal FreeSessionCredit, decimal AmountDue) BuildTeaserAmounts(
        bool eligible,
        decimal grossPackageTotal,
        decimal pricePerHour,
        int firstSessionMinutes)
    {
        if (!eligible || grossPackageTotal <= 0)
            return (0m, Math.Max(0m, grossPackageTotal));

        var credit = ComputeFreeSessionCredit(pricePerHour, firstSessionMinutes, grossPackageTotal);
        var amountDue = Math.Max(0m, Math.Round(grossPackageTotal - credit, 2, MidpointRounding.AwayFromZero));
        return (credit, amountDue);
    }

    /// <summary>
    /// Post-commit read breakdown: NetDue is always <see cref="Enrollment.AmountDue"/> when free trial
    /// (or positive AmountDue); reconstruct gross + first-session credit for UI hints.
    /// </summary>
    public static (decimal Gross, decimal Credit, decimal NetDue) ResolveFreeTrialBreakdown(Enrollment enrollment)
    {
        var netDue = enrollment.IsFreeTrial || enrollment.AmountDue > 0
            ? enrollment.AmountDue
            : enrollment.EnrollmentRequest?.EstimatedTotalPrice ?? 0m;

        if (!enrollment.IsFreeTrial)
            return (netDue, 0m, netDue);

        var snap = enrollment.PricingSnapshot;
        var schedules = enrollment.CourseSchedules ?? [];
        var scheduleMinutes = schedules.Sum(s => s.DurationMinutes);
        var totalMinutes = snap?.TotalMinutes > 0
            ? snap.TotalMinutes
            : scheduleMinutes;

        var firstMinutes = ResolveFirstSessionMinutes(
            schedules.OrderBy(s => s.Date).Select(s => (int?)s.DurationMinutes).FirstOrDefault(),
            enrollment.Course?.SessionDurationMinutes,
            totalMinutes > 0 ? totalMinutes : null,
            schedules.Count > 0 ? schedules.Count : enrollment.Course?.SessionsCount);
        if (firstMinutes <= 0)
            firstMinutes = 60;

        // 1) Prefer snapshot hourly + package minutes → engine gross.
        if (snap?.PricePerHour > 0 && totalMinutes > 0)
        {
            var engineGross = Math.Round(
                snap.PricePerHour * totalMinutes / 60m,
                2,
                MidpointRounding.AwayFromZero);
            if (engineGross > 0)
            {
                var credit = Math.Max(0m, Math.Round(engineGross - netDue, 2, MidpointRounding.AwayFromZero));
                return (engineGross, credit, netDue);
            }
        }

        // 2) Request estimate when it exceeds net due (common when snapshot was not loaded).
        var estimate = enrollment.EnrollmentRequest?.EstimatedTotalPrice ?? 0m;
        if (estimate > netDue)
        {
            var credit = Math.Round(estimate - netDue, 2, MidpointRounding.AwayFromZero);
            return (estimate, credit, netDue);
        }

        // 3) Reconstruct from course hourly rate + schedule/package minutes.
        var courseHourly = enrollment.Course?.Price ?? 0m;
        if (courseHourly > 0 && totalMinutes > 0)
        {
            var engineGross = Math.Round(
                courseHourly * totalMinutes / 60m,
                2,
                MidpointRounding.AwayFromZero);
            if (engineGross > 0)
            {
                var credit = Math.Max(0m, Math.Round(engineGross - netDue, 2, MidpointRounding.AwayFromZero));
                return (engineGross, credit, netDue);
            }
        }

        // 4) Last resort: first-session credit from available hourly (do not derive from netDue alone).
        var fallbackHourly = snap?.PricePerHour > 0
            ? snap.PricePerHour
            : courseHourly;
        if (fallbackHourly <= 0 && totalMinutes > 0 && estimate > 0)
            fallbackHourly = DerivePricePerHour(estimate, totalMinutes);

        var creditOnly = ComputeFreeSessionCredit(
            fallbackHourly, firstMinutes, Math.Max(netDue, fallbackHourly));
        return (netDue + creditOnly, creditOnly, netDue);
    }

    /// <summary>
    /// Student free trial: reduce only the student payable by the first-session credit.
    /// Teacher earnings stay the full package — the platform covers the credit
    /// (<c>PlatformShare = AmountDue − TeacherEarnings</c>, may be negative).
    /// Returns net amount due and the credit applied.
    /// </summary>
    public static (decimal AmountDue, decimal FreeSessionCredit) ApplyFreeTrialToSnapshot(
        PricingSnapshot snapshot,
        decimal grossPackageTotal,
        int firstSessionMinutes)
    {
        var pricePerHour = snapshot.PricePerHour;
        var credit = ComputeFreeSessionCredit(pricePerHour, firstSessionMinutes, grossPackageTotal);
        var amountDue = Math.Max(0m, Math.Round(grossPackageTotal - credit, 2, MidpointRounding.AwayFromZero));

        snapshot.TotalPrice = amountDue;
        snapshot.PlatformShare = Math.Round(
            amountDue - snapshot.TeacherEarnings,
            2,
            MidpointRounding.AwayFromZero);
        snapshot.UpdatedAt = DateTime.UtcNow;
        return (amountDue, credit);
    }

    public async Task<bool> IsStudentEligibleForFreeTrialAsync(
        int studentId,
        CancellationToken cancellationToken = default)
    {
        var student = await _studentRepository.GetByIdAsync(studentId);
        return student is { HasUsedFreeTrialSession: false };
    }

    public async Task MarkStudentFreeTrialUsedAsync(int studentId, CancellationToken cancellationToken = default)
    {
        var student = await _studentRepository.GetByIdAsync(studentId);
        if (student == null || student.HasUsedFreeTrialSession)
            return;

        student.HasUsedFreeTrialSession = true;
        student.UpdatedAt = DateTime.UtcNow;
        await _studentRepository.UpdateAsync(student);
        await _studentRepository.SaveChangesAsync();
    }

    public async Task ReserveStudentFreeTrialAsync(
        int studentId,
        Enrollment enrollment,
        FreeTrialConsumptionSource source,
        int teacherId,
        int domainId,
        int? openSessionRequestId = null,
        CancellationToken cancellationToken = default)
    {
        var student = await _studentRepository.GetByIdAsync(studentId);
        if (student == null)
            throw new InvalidOperationException($"Student {studentId} not found.");

        var now = DateTime.UtcNow;
        var consumption = new StudentFreeTrialConsumption
        {
            StudentId = studentId,
            Source = source,
            Enrollment = enrollment,
            OpenSessionRequestId = openSessionRequestId,
            TeacherId = teacherId,
            DomainId = domainId,
            Status = FreeTrialConsumptionStatus.Reserved,
            ReservedAt = now,
            CreatedAt = now
        };
        _db.StudentFreeTrialConsumptions.Add(consumption);

        if (!student.HasUsedFreeTrialSession)
        {
            student.HasUsedFreeTrialSession = true;
            student.UpdatedAt = now;
            await _studentRepository.UpdateAsync(student);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkConsumptionConsumedAsync(
        int enrollmentId,
        int courseScheduleId,
        CancellationToken cancellationToken = default)
    {
        var consumption = await _db.StudentFreeTrialConsumptions
            .FirstOrDefaultAsync(
                c => c.EnrollmentId == enrollmentId
                     && c.Status == FreeTrialConsumptionStatus.Reserved,
                cancellationToken);
        if (consumption == null)
            return;

        var now = DateTime.UtcNow;
        consumption.Status = FreeTrialConsumptionStatus.Consumed;
        consumption.ConsumedAt = now;
        consumption.CourseScheduleId = courseScheduleId;
        consumption.UpdatedAt = now;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task CancelConsumptionBeforeStartAsync(
        int enrollmentId,
        int? cancelledByUserId,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        var consumption = await _db.StudentFreeTrialConsumptions
            .FirstOrDefaultAsync(
                c => c.EnrollmentId == enrollmentId
                     && (c.Status == FreeTrialConsumptionStatus.Reserved
                         || c.Status == FreeTrialConsumptionStatus.Consumed),
                cancellationToken);
        if (consumption == null)
            return;

        var now = DateTime.UtcNow;
        if (consumption.Status == FreeTrialConsumptionStatus.Reserved)
        {
            consumption.Status = FreeTrialConsumptionStatus.CancelledBeforeStart;
            consumption.CancelledAt = now;
            consumption.RestoredEligibility = true;
            consumption.CancelReason = reason;
            consumption.CancelledByUserId = cancelledByUserId;
            consumption.UpdatedAt = now;

            var student = await _studentRepository.GetByIdAsync(consumption.StudentId);
            if (student != null && student.HasUsedFreeTrialSession)
            {
                var hasOtherActive = await _db.StudentFreeTrialConsumptions
                    .AnyAsync(
                        c => c.StudentId == consumption.StudentId
                             && c.Id != consumption.Id
                             && c.Status == FreeTrialConsumptionStatus.Reserved,
                        cancellationToken);
                if (!hasOtherActive)
                {
                    student.HasUsedFreeTrialSession = false;
                    student.UpdatedAt = now;
                    await _studentRepository.UpdateAsync(student);
                }
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task TryRevertTeacherInterviewFromEnrollmentAsync(
        int enrollmentId,
        CancellationToken cancellationToken = default)
    {
        var enrollment = await _db.Enrollments
            .AsNoTracking()
            .Include(e => e.Course)
                .ThenInclude(c => c!.TeacherSubject)
                .ThenInclude(ts => ts!.Subject)
            .Include(e => e.OpenSessionRequest)
            .Include(e => e.PricingSnapshot)
            .FirstOrDefaultAsync(e => e.Id == enrollmentId, cancellationToken);
        if (enrollment == null)
            return;

        var teacherId = enrollment.ApprovedByTeacherId;
        if (teacherId <= 0)
            return;

        var teacher = await _teacherRepository.GetByIdAsync(teacherId);
        if (teacher == null
            || teacher.InterviewUnlockSource != InterviewUnlockSource.AutoFromSession
            || teacher.InterviewUnlockEnrollmentId != enrollmentId)
            return;

        var hasOtherCompleted = await HasOtherCompletedSessionsForTeacherAsync(
            teacherId, enrollmentId, cancellationToken);
        if (hasOtherCompleted)
            return;

        var starterLevel = await _teacherLevelRepository.GetStarterLevelAsync(cancellationToken);
        var now = DateTime.UtcNow;

        teacher.HasCompletedInterviewSession = false;
        teacher.InterviewUnlockSource = InterviewUnlockSource.None;
        teacher.InterviewUnlockEnrollmentId = null;
        teacher.InterviewUnlockCourseScheduleId = null;
        teacher.InterviewUnlockedAt = null;
        if (starterLevel != null && teacher.TeacherLevelId == starterLevel.Id)
            teacher.TeacherLevelId = null;
        teacher.UpdatedAt = now;
        await _teacherRepository.UpdateAsync(teacher);

        var domainId = ResolveDomainId(enrollment);
        if (domainId > 0)
        {
            var pricing = await _domainPricingRepository.GetOrCreateAsync(teacherId, domainId, cancellationToken);
            if (pricing.InterviewUnlockSource == InterviewUnlockSource.AutoFromSession
                && pricing.InterviewUnlockEnrollmentId == enrollmentId)
            {
                pricing.HasCompletedInterviewSession = false;
                if (starterLevel != null && pricing.TeacherLevelId == starterLevel.Id)
                    pricing.TeacherLevelId = null;
                pricing.InterviewUnlockSource = InterviewUnlockSource.None;
                pricing.InterviewUnlockEnrollmentId = null;
                pricing.InterviewUnlockCourseScheduleId = null;
                pricing.InterviewRevertedAt = now;
                pricing.UpdatedAt = now;
                await _domainPricingRepository.UpdateAsync(pricing);
            }
        }

        await _domainPricingRepository.SaveChangesAsync();
    }

    public async Task TryCompleteTeacherInterviewAsync(
        int teacherId,
        int domainId,
        int? enrollmentId = null,
        int? courseScheduleId = null,
        CancellationToken cancellationToken = default)
    {
        if (domainId <= 0)
            return;

        var teacher = await _teacherRepository.GetByIdAsync(teacherId);
        if (teacher == null)
            return;

        var pricing = await _domainPricingRepository.GetOrCreateAsync(teacherId, domainId, cancellationToken);
        var isAccountInterview = !teacher.HasCompletedInterviewSession;
        if (!isAccountInterview && pricing.HasCompletedInterviewSession && pricing.TeacherLevelId.HasValue)
            return;

        var minLevel = await _teacherLevelRepository.GetStarterLevelAsync(cancellationToken);
        if (minLevel == null)
            throw new InvalidOperationException("No active teacher level configured.");

        var now = DateTime.UtcNow;
        if (!pricing.HasCompletedInterviewSession || !pricing.TeacherLevelId.HasValue)
        {
            pricing.HasCompletedInterviewSession = true;
            pricing.TeacherLevelId ??= minLevel.Id;
            if (pricing.InterviewUnlockSource == InterviewUnlockSource.None)
            {
                pricing.InterviewUnlockSource = InterviewUnlockSource.AutoFromSession;
                pricing.InterviewUnlockEnrollmentId = enrollmentId;
                pricing.InterviewUnlockCourseScheduleId = isAccountInterview ? courseScheduleId : null;
                pricing.InterviewUnlockedAt = now;
            }
            pricing.InterviewRevertedAt = null;
            pricing.UpdatedAt = now;
            await _domainPricingRepository.UpdateAsync(pricing);
        }

        if (isAccountInterview)
        {
            teacher.HasCompletedInterviewSession = true;
            teacher.InterviewUnlockSource = InterviewUnlockSource.AutoFromSession;
            teacher.InterviewUnlockEnrollmentId = enrollmentId;
            teacher.InterviewUnlockCourseScheduleId = courseScheduleId;
            teacher.InterviewUnlockedAt = now;
            teacher.TeacherLevelId ??= pricing.TeacherLevelId;
            teacher.UpdatedAt = now;
            await _teacherRepository.UpdateAsync(teacher);
        }

        await _teacherRepository.SaveChangesAsync();
    }

    private static int ResolveDomainId(Enrollment enrollment)
    {
        if (enrollment.Course?.TeacherSubject?.Subject?.DomainId is > 0)
            return enrollment.Course.TeacherSubject.Subject.DomainId;
        if (enrollment.PricingSnapshot?.DomainId is > 0)
            return enrollment.PricingSnapshot.DomainId;
        if (enrollment.OpenSessionRequest?.DomainId is > 0)
            return enrollment.OpenSessionRequest.DomainId;
        return 0;
    }

    private async Task<bool> HasOtherCompletedSessionsForTeacherAsync(
        int teacherId,
        int excludeEnrollmentId,
        CancellationToken cancellationToken)
    {
        return await _db.CourseSchedules
            .AsNoTracking()
            .AnyAsync(
                cs => cs.Status == ScheduleStatus.Completed
                      && cs.EnrollmentId != excludeEnrollmentId
                      && cs.Enrollment.ApprovedByTeacherId == teacherId,
                cancellationToken);
    }
}
