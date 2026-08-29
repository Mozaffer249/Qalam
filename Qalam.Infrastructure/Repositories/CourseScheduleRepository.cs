using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Repositories;

public class CourseScheduleRepository : GenericRepositoryAsync<CourseSchedule>, ICourseScheduleRepository
{
    private readonly ApplicationDBContext _context;

    public CourseScheduleRepository(ApplicationDBContext context) : base(context)
    {
        _context = context;
    }

    public async Task<HashSet<(DateOnly Date, int TeacherAvailabilityId)>> GetScheduledSlotsAsync(
        DateOnly fromDate,
        DateOnly toDate,
        IReadOnlyCollection<int> teacherAvailabilityIds,
        CancellationToken ct)
    {
        if (teacherAvailabilityIds.Count == 0)
            return new HashSet<(DateOnly, int)>();

        var ids = teacherAvailabilityIds.ToList();

        var rows = await _context.CourseSchedules
            .AsNoTracking()
            .Where(cs => cs.Status == ScheduleStatus.Scheduled
                      && cs.Date >= fromDate
                      && cs.Date <= toDate
                      && ids.Contains(cs.TeacherAvailabilityId))
            .Select(cs => new { cs.Date, cs.TeacherAvailabilityId })
            .ToListAsync(ct);

        return rows.Select(r => (r.Date, r.TeacherAvailabilityId)).ToHashSet();
    }

    public async Task<List<(DateOnly Date, TimeSpan Start, TimeSpan End)>> GetTeacherBookedSlotsInRangeAsync(
        int teacherId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default)
    {
        var rows = await _context.CourseSchedules
            .AsNoTracking()
            .Where(cs => cs.Status == ScheduleStatus.Scheduled
                         && cs.Date >= fromDate
                         && cs.Date <= toDate
                         && ((cs.Enrollment.Course != null && cs.Enrollment.Course.TeacherId == teacherId)
                             || (cs.Enrollment.Course == null && cs.Enrollment.ApprovedByTeacherId == teacherId))
                         && cs.TeacherAvailability != null
                         && cs.TeacherAvailability.TimeSlot != null)
            .Select(cs => new
            {
                cs.Date,
                Start = cs.TeacherAvailability.TimeSlot!.StartTime,
                End = cs.TeacherAvailability.TimeSlot!.EndTime
            })
            .ToListAsync(cancellationToken);

        return rows.Select(r => (r.Date, r.Start, r.End)).ToList();
    }

    public async Task<CourseSchedule?> GetByIdForLifecycleAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.CourseSchedules
            .Include(cs => cs.Enrollment).ThenInclude(e => e.Participants)
            .Include(cs => cs.Enrollment).ThenInclude(e => e.Course)
                .ThenInclude(c => c.TeacherSubject).ThenInclude(ts => ts.Subject)
            .Include(cs => cs.Enrollment).ThenInclude(e => e.OpenSessionRequest)
            .Include(cs => cs.Enrollment).ThenInclude(e => e.PricingSnapshot)
            .Include(cs => cs.Attendances)
            .Include(cs => cs.TeachingMode)
            .Include(cs => cs.TeacherAvailability).ThenInclude(ta => ta.TimeSlot)
            .FirstOrDefaultAsync(cs => cs.Id == id, cancellationToken);
    }

    public async Task<List<CourseSchedule>> GetOverdueForAutoCompleteAsync(
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        var now = PlatformTime.ToUtcInstant(utcNow);
        var maxCandidateDate = PlatformTime.ToPlatformDate(now);

        var candidates = await _context.CourseSchedules
            .Include(cs => cs.Enrollment).ThenInclude(e => e.Participants)
            .Include(cs => cs.Enrollment).ThenInclude(e => e.Course)
                .ThenInclude(c => c.TeacherSubject).ThenInclude(ts => ts.Subject)
            .Include(cs => cs.Attendances)
            .Include(cs => cs.TeacherAvailability).ThenInclude(ta => ta.TimeSlot)
            .Where(cs =>
                (cs.Status == ScheduleStatus.Scheduled || cs.Status == ScheduleStatus.InProgress)
                && cs.Date <= maxCandidateDate
                && cs.TeacherAvailability != null
                && cs.TeacherAvailability.TimeSlot != null)
            .ToListAsync(cancellationToken);

        return candidates
            .Where(cs =>
            {
                var end = cs.TeacherAvailability.TimeSlot!.EndTime;
                var endUtc = PlatformTime.ToUtc(cs.Date, end);
                return endUtc <= now;
            })
            .ToList();
    }

    public async Task<List<CourseSchedule>> GetDueForAutoStartAsync(
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        var now = PlatformTime.ToUtcInstant(utcNow);
        var maxCandidateDate = PlatformTime.ToPlatformDate(now);

        var candidates = await _context.CourseSchedules
            .Include(cs => cs.TeacherAvailability).ThenInclude(ta => ta.TimeSlot)
            .Where(cs =>
                cs.Status == ScheduleStatus.Scheduled
                && cs.Date <= maxCandidateDate
                && cs.TeacherAvailability != null
                && cs.TeacherAvailability.TimeSlot != null)
            .ToListAsync(cancellationToken);

        return candidates
            .Where(cs =>
            {
                var slot = cs.TeacherAvailability!.TimeSlot!;
                var startUtc = PlatformTime.ToUtc(cs.Date, slot.StartTime);
                var endUtc = PlatformTime.ToUtc(cs.Date, slot.EndTime);
                return startUtc <= now && endUtc > now;
            })
            .ToList();
    }

    public async Task<CourseSchedule?> GetByIdForAdminActionAsync(int id, CancellationToken cancellationToken = default) =>
        await _context.CourseSchedules
            .Include(s => s.Enrollment)
                .ThenInclude(e => e!.Participants)
            .Include(s => s.Attendances)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<CourseSchedule?> GetWithParticipantsForComplaintAsync(int id, CancellationToken cancellationToken = default) =>
        await _context.CourseSchedules
            .Include(s => s.Enrollment)
                .ThenInclude(e => e!.Participants)
            .Include(s => s.Enrollment)
                .ThenInclude(e => e!.Course)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<int?> GetEnrollmentIdByScheduleIdAsync(int scheduleId, CancellationToken cancellationToken = default) =>
        await _context.CourseSchedules.AsNoTracking()
            .Where(s => s.Id == scheduleId)
            .Select(s => (int?)s.EnrollmentId)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<bool> IsCompletedAsync(int scheduleId, CancellationToken cancellationToken = default) =>
        await _context.CourseSchedules.AsNoTracking()
            .AnyAsync(s => s.Id == scheduleId && s.Status == ScheduleStatus.Completed, cancellationToken);

    public async Task<int> GetCourseTeacherIdAsync(int courseId, CancellationToken cancellationToken = default) =>
        await _context.Courses.AsNoTracking()
            .Where(c => c.Id == courseId)
            .Select(c => c.TeacherId)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<CourseSchedule?> GetByIdNoTrackingAsync(int id, CancellationToken cancellationToken = default) =>
        _context.CourseSchedules.AsNoTracking()
            .FirstOrDefaultAsync(cs => cs.Id == id, cancellationToken);
}
