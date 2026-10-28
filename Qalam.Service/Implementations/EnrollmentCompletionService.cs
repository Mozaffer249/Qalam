using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Infrastructure.context;
using Qalam.Service.Abstracts;
using Qalam.Service.Helpers;

namespace Qalam.Service.Implementations;

public class EnrollmentCompletionService : IEnrollmentCompletionService
{
    private readonly ApplicationDBContext _db;
    private readonly ILrsDispatchService _lrsDispatch;
    private readonly ILogger<EnrollmentCompletionService> _logger;

    public EnrollmentCompletionService(
        ApplicationDBContext db,
        ILrsDispatchService lrsDispatch,
        ILogger<EnrollmentCompletionService> logger)
    {
        _db = db;
        _lrsDispatch = lrsDispatch;
        _logger = logger;
    }

    public async Task TryCompleteEnrollmentIfFinishedAsync(
        int enrollmentId,
        CancellationToken cancellationToken = default)
    {
        var enrollment = await _db.Enrollments
            .Include(e => e.CourseSchedules)
            .FirstOrDefaultAsync(e => e.Id == enrollmentId, cancellationToken);

        if (enrollment == null)
            return;

        if (!EnrollmentLifecycleRules.ShouldMarkEnrollmentCompleted(enrollment))
            return;

        enrollment.EnrollmentStatus = EnrollmentStatus.Completed;
        enrollment.CompletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Marked Enrollment {EnrollmentId} Completed (all sessions finished).", enrollmentId);

        try
        {
            await _lrsDispatch.DispatchCourseCompletedAsync(enrollment, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to dispatch LRS course-completed for Enrollment {EnrollmentId}",
                enrollmentId);
        }
    }

    public async Task<int> SweepFinishedEnrollmentsAsync(CancellationToken cancellationToken = default)
    {
        var candidates = await _db.Enrollments
            .Include(e => e.CourseSchedules)
            .Where(e => e.EnrollmentStatus == EnrollmentStatus.Active)
            .Where(e => e.CourseSchedules.Any(s => s.Status == ScheduleStatus.Completed))
            .Where(e => !e.CourseSchedules.Any(s =>
                s.Status == ScheduleStatus.Scheduled || s.Status == ScheduleStatus.InProgress))
            .ToListAsync(cancellationToken);

        var count = 0;
        foreach (var enrollment in candidates)
        {
            if (!EnrollmentLifecycleRules.ShouldMarkEnrollmentCompleted(enrollment))
                continue;

            enrollment.EnrollmentStatus = EnrollmentStatus.Completed;
            enrollment.CompletedAt = DateTime.UtcNow;
            count++;
        }

        if (count > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);

            foreach (var enrollment in candidates.Where(e => e.EnrollmentStatus == EnrollmentStatus.Completed))
            {
                try
                {
                    await _lrsDispatch.DispatchCourseCompletedAsync(enrollment, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Failed to dispatch LRS course-completed for Enrollment {EnrollmentId}",
                        enrollment.Id);
                }
            }
        }

        return count;
    }
}
