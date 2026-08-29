using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Payment;
using Qalam.Data.Entity.Teacher;
using Qalam.Infrastructure.context;
using Qalam.Service.Abstracts;
using Qalam.Service.Mappers;

namespace Qalam.Service.Implementations;

public class TeacherEarningService : ITeacherEarningService
{
    private readonly ApplicationDBContext _db;
    private readonly ILogger<TeacherEarningService> _logger;

    public TeacherEarningService(
        ApplicationDBContext db,
        ILogger<TeacherEarningService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task AccrueForCompletedScheduleAsync(
        int courseScheduleId,
        TeacherEarningLineStatus initialStatus = TeacherEarningLineStatus.Pending,
        CancellationToken cancellationToken = default)
    {
        var exists = await _db.TeacherEarningLines
            .AnyAsync(l => l.CourseScheduleId == courseScheduleId, cancellationToken);
        if (exists)
            return;

        var schedule = await _db.CourseSchedules
            .Include(s => s.Enrollment)
                .ThenInclude(e => e!.PricingSnapshot)
            .Include(s => s.Enrollment)
                .ThenInclude(e => e!.Course)
            .FirstOrDefaultAsync(s => s.Id == courseScheduleId, cancellationToken);

        if (schedule?.Enrollment == null)
            return;

        if (schedule.Status != ScheduleStatus.Completed)
            return;

        var enrollment = schedule.Enrollment;
        var teacherId = enrollment.ApprovedByTeacherId;
        if (teacherId <= 0 && enrollment.Course != null)
            teacherId = enrollment.Course.TeacherId;
        if (teacherId <= 0)
            return;

        var snapshot = enrollment.PricingSnapshot;
        var currency = snapshot?.Currency ?? "SAR";
        var packageEarnings = snapshot?.TeacherEarnings ?? 0m;
        if (packageEarnings <= 0m && snapshot != null)
        {
            var starterSharePct = await _db.Set<TeacherLevel>()
                .AsNoTracking()
                .Where(l => l.IsActive)
                .OrderBy(l => l.OrderIndex)
                .Select(l => l.TeacherSharePct)
                .FirstOrDefaultAsync(cancellationToken);
            packageEarnings = EnrollmentEarningsProjectionHelper.ResolvePackageEarningsForAccrual(
                enrollment, snapshot, starterSharePct);
        }

        var siblingSchedules = await _db.CourseSchedules
            .AsNoTracking()
            .Where(s => s.EnrollmentId == enrollment.Id
                        && s.Status != ScheduleStatus.Cancelled
                        && s.Status != ScheduleStatus.Rescheduled)
            .OrderBy(s => s.Date)
            .ThenBy(s => s.Id)
            .Select(s => new { s.Id, s.DurationMinutes, s.Date })
            .ToListAsync(cancellationToken);

        if (enrollment.IsFreeTrial && siblingSchedules.Count > 0)
        {
            var freeId = siblingSchedules[0].Id;
            if (schedule.Id == freeId)
            {
                _logger.LogInformation(
                    "Skipping teacher earning for free-trial first CourseSchedule {ScheduleId}.",
                    courseScheduleId);
                return;
            }
        }

        var totalMinutes = snapshot?.TotalMinutes ?? 0;
        if (totalMinutes <= 0)
            totalMinutes = siblingSchedules.Sum(s => s.DurationMinutes);

        var earnableMinutes = totalMinutes;
        if (enrollment.IsFreeTrial && siblingSchedules.Count > 0)
        {
            var freeMinutes = siblingSchedules[0].DurationMinutes;
            if (freeMinutes <= 0 && snapshot != null)
            {
                freeMinutes = FreeSessionPolicyService.ResolveFirstSessionMinutes(
                    siblingSchedules[0].DurationMinutes > 0 ? siblingSchedules[0].DurationMinutes : null,
                    enrollment.Course?.SessionDurationMinutes,
                    totalMinutes > 0 ? totalMinutes : null,
                    siblingSchedules.Count);
            }
            if (freeMinutes <= 0)
                freeMinutes = 60;
            earnableMinutes = Math.Max(0, totalMinutes - freeMinutes);
        }

        decimal amount = 0;
        if (packageEarnings > 0 && earnableMinutes > 0 && schedule.DurationMinutes > 0)
            amount = Math.Round(packageEarnings * schedule.DurationMinutes / (decimal)earnableMinutes, 2);
        else if (packageEarnings > 0 && earnableMinutes <= 0)
            amount = Math.Round(packageEarnings, 2);

        if (amount <= 0)
        {
            _logger.LogInformation(
                "No teacher earning accrued for CourseSchedule {ScheduleId} (amount 0 / free trial / interview).",
                courseScheduleId);
            return;
        }

        _db.TeacherEarningLines.Add(new TeacherEarningLine
        {
            TeacherId = teacherId,
            EnrollmentId = enrollment.Id,
            CourseScheduleId = courseScheduleId,
            Amount = amount,
            Currency = currency,
            Source = TeacherEarningSource.SessionCompleted,
            Status = initialStatus,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "Accrued TeacherEarningLine {Amount} {Currency} for CourseSchedule {ScheduleId}.",
            amount, currency, courseScheduleId);
    }
}
