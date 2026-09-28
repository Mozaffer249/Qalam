using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Common;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Repositories;

public class TimeSlotRepository : GenericRepositoryAsync<TimeSlot>, ITimeSlotRepository
{
    private readonly ApplicationDBContext _context;

    public TimeSlotRepository(ApplicationDBContext context) : base(context)
    {
        _context = context;
    }

    public IQueryable<TimeSlot> GetTimeSlotsQueryable()
    {
        return _context.TimeSlots
            .AsNoTracking()
            .OrderBy(ts => ts.StartTime);
    }

    public IQueryable<TimeSlot> GetActiveTimeSlotsQueryable()
    {
        return _context.TimeSlots
            .AsNoTracking()
            .Where(ts => ts.IsActive)
            .OrderBy(ts => ts.StartTime);
    }

    public IQueryable<TimeSlot> GetTimeSlotsByDayOfWeek(int dayOfWeek)
    {
        // TimeSlots are not day-specific in current implementation
        return _context.TimeSlots
            .AsNoTracking()
            .OrderBy(ts => ts.StartTime);
    }

    public async Task<bool> IsTimeSlotOverlappingAsync(int dayOfWeek, TimeSpan startTime, TimeSpan endTime, int? excludeId = null)
    {
        // Slots may cross midnight (end <= start), which SQL range comparisons can't express; the catalog is small.
        var existing = await _context.TimeSlots
            .AsNoTracking()
            .Where(ts => !excludeId.HasValue || ts.Id != excludeId.Value)
            .Select(ts => new { ts.StartTime, ts.EndTime })
            .ToListAsync();

        var candidate = ToDaySegments(startTime, endTime);
        return existing.Any(ts => SegmentsOverlap(candidate, ToDaySegments(ts.StartTime, ts.EndTime)));
    }

    private static List<(TimeSpan Start, TimeSpan End)> ToDaySegments(TimeSpan start, TimeSpan end)
    {
        if (!TimeSlot.EndsNextDayFor(start, end))
            return new() { (start, end) };

        var segments = new List<(TimeSpan Start, TimeSpan End)> { (start, TimeSpan.FromDays(1)) };
        if (end > TimeSpan.Zero)
            segments.Add((TimeSpan.Zero, end));
        return segments;
    }

    private static bool SegmentsOverlap(
        List<(TimeSpan Start, TimeSpan End)> a,
        List<(TimeSpan Start, TimeSpan End)> b) =>
        a.Any(x => b.Any(y => x.Start < y.End && y.Start < x.End));

    public async Task<bool> IsTimeSlotInUseAsync(int timeSlotId, CancellationToken cancellationToken = default)
    {
        if (await _context.TeacherAvailabilities.AnyAsync(ta => ta.TimeSlotId == timeSlotId, cancellationToken))
            return true;
        if (await _context.TeacherAvailabilityExceptions.AnyAsync(e => e.TimeSlotId == timeSlotId, cancellationToken))
            return true;
        if (await _context.ScheduledSessions.AnyAsync(s => s.TimeSlotId == timeSlotId, cancellationToken))
            return true;
        if (await _context.OpenSessionRequestSessions.AnyAsync(
                s => s.TimeSlotId == timeSlotId, cancellationToken))
            return true;

        return false;
    }
}
