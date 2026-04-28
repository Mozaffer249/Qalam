using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
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
}
