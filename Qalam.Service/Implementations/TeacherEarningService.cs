using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Payment;
using Qalam.Infrastructure.context;
using Qalam.Service.Abstracts;

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
        var totalMinutes = snapshot?.TotalMinutes ?? 0;
        if (totalMinutes <= 0)
        {
            totalMinutes = await _db.CourseSchedules
                .Where(s => s.EnrollmentId == enrollment.Id
                            && s.Status != ScheduleStatus.Cancelled
                            && s.Status != ScheduleStatus.Rescheduled)
                .SumAsync(s => (int?)s.DurationMinutes, cancellationToken) ?? 0;
        }

        decimal amount = 0;
        if (packageEarnings > 0 && totalMinutes > 0 && schedule.DurationMinutes > 0)
            amount = Math.Round(packageEarnings * schedule.DurationMinutes / totalMinutes, 2);
        else if (packageEarnings > 0 && totalMinutes <= 0)
            amount = Math.Round(packageEarnings, 2);

        if (amount <= 0)
        {
            _logger.LogInformation(
                "No teacher earning accrued for CourseSchedule {ScheduleId} (amount 0 / free trial / interview).",
                courseScheduleId);
            return;
        }

        var source = enrollment.IsFreeTrial
            ? TeacherEarningSource.FreeTrialPlatform
            : TeacherEarningSource.SessionCompleted;

        _db.TeacherEarningLines.Add(new TeacherEarningLine
        {
            TeacherId = teacherId,
            EnrollmentId = enrollment.Id,
            CourseScheduleId = courseScheduleId,
            Amount = amount,
            Currency = currency,
            Source = source,
            Status = TeacherEarningLineStatus.Pending,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "Accrued TeacherEarningLine {Amount} {Currency} for CourseSchedule {ScheduleId}.",
            amount, currency, courseScheduleId);
    }
}
