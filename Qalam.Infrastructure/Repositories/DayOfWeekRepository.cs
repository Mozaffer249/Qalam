using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Common;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Repositories;

public class DayOfWeekRepository : GenericRepositoryAsync<DayOfWeekMaster>, IDayOfWeekRepository
{
    private readonly ApplicationDBContext _context;

    public DayOfWeekRepository(ApplicationDBContext context) : base(context)
    {
        _context = context;
    }

    public IQueryable<DayOfWeekMaster> GetDaysOfWeekQueryable()
    {
        return _context.DaysOfWeek
            .AsNoTracking()
            .OrderBy(d => d.OrderIndex);
    }

    public IQueryable<DayOfWeekMaster> GetActiveDaysOfWeekQueryable()
    {
        return _context.DaysOfWeek
            .AsNoTracking()
            .Where(d => d.IsActive)
            .OrderBy(d => d.OrderIndex);
    }
}
