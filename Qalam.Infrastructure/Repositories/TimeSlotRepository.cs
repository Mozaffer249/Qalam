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
        var query = _context.TimeSlots.Where(ts =>
            (ts.StartTime < endTime && ts.EndTime > startTime));

        if (excludeId.HasValue)
            query = query.Where(ts => ts.Id != excludeId.Value);

        return await query.AnyAsync();
    }
}
