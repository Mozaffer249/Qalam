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
                         && cs.Enrollment.Course != null
                         && cs.Enrollment.Course.TeacherId == teacherId
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
            .Include(cs => cs.Attendances)
            .Include(cs => cs.TeachingMode)
            .Include(cs => cs.TeacherAvailability).ThenInclude(ta => ta.TimeSlot)
            .FirstOrDefaultAsync(cs => cs.Id == id, cancellationToken);
    }

    public async Task<List<CourseSchedule>> GetOverdueForAutoCompleteAsync(
        DateTime utcNow,
        int graceMinutes,
        CancellationToken cancellationToken = default)
    {
        // Rough pre-filter: anything whose calendar date is after "today + tiny buffer" cannot be overdue yet.
        var maxCandidateDate = DateOnly.FromDateTime(utcNow);

        var candidates = await _context.CourseSchedules
            .Include(cs => cs.Enrollment).ThenInclude(e => e.Participants)
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
                return endUtc.AddMinutes(graceMinutes) < utcNow;
            })
            .ToList();
    }
}
