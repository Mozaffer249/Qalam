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

    /// <summary>Session share of the package earnings: <c>packageEarnings × scheduleMinutes / totalMinutes</c>.</summary>
    public static decimal ComputeScheduleEarning(decimal packageEarnings, int totalMinutes, int scheduleMinutes)
    {
        if (packageEarnings <= 0m)
            return 0m;
        if (totalMinutes <= 0 || scheduleMinutes <= 0)
            return Math.Round(packageEarnings, 2, MidpointRounding.AwayFromZero);
        return Math.Round(
            packageEarnings * Math.Min(scheduleMinutes, totalMinutes) / totalMinutes,
            2,
            MidpointRounding.AwayFromZero);
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

        var interviewScheduleId = await _db.Teachers
            .AsNoTracking()
            .Where(t => t.Id == teacherId)
            .Select(t => t.InterviewUnlockCourseScheduleId)
            .FirstOrDefaultAsync(cancellationToken);
        if (interviewScheduleId == schedule.Id)
        {
            _logger.LogInformation(
                "Skipping teacher earning for the teacher's unpaid interview CourseSchedule {ScheduleId}.",
                courseScheduleId);
            return;
        }

        var snapshot = enrollment.PricingSnapshot;
        var currency = snapshot?.Currency ?? "SAR";
        var packageEarnings = snapshot?.TeacherEarnings ?? 0m;

        var totalMinutes = snapshot?.TotalMinutes ?? 0;
        if (totalMinutes <= 0)
        {
            totalMinutes = await _db.CourseSchedules
                .AsNoTracking()
                .Where(s => s.EnrollmentId == enrollment.Id
                            && s.Status != ScheduleStatus.Cancelled
                            && s.Status != ScheduleStatus.Rescheduled)
                .SumAsync(s => s.DurationMinutes, cancellationToken);
        }

        var amount = ComputeScheduleEarning(packageEarnings, totalMinutes, schedule.DurationMinutes);
        if (amount <= 0)
        {
            _logger.LogInformation(
                "No teacher earning accrued for CourseSchedule {ScheduleId} (amount 0).",
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
