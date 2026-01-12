using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Teaching;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Repositories;

public class TeachingModeRepository : GenericRepositoryAsync<TeachingMode>, ITeachingModeRepository
{
    private readonly ApplicationDBContext _context;

    public TeachingModeRepository(ApplicationDBContext context) : base(context)
    {
        _context = context;
    }

    public IQueryable<TeachingMode> GetTeachingModesQueryable()
    {
        return _context.TeachingModes
            .AsNoTracking()
            .OrderBy(tm => tm.NameEn);
    }

    public IQueryable<TeachingMode> GetActiveTeachingModesQueryable()
    {
        // TeachingMode doesn't have IsActive property
        return _context.TeachingModes
            .AsNoTracking()
            .OrderBy(tm => tm.NameEn);
    }

    public async Task<TeachingMode> GetTeachingModeByCodeAsync(string code)
    {
        return await _context.TeachingModes
            .AsNoTracking()
            .FirstOrDefaultAsync(tm => tm.Code == code);
    }

    public async Task<bool> IsTeachingModeCodeUniqueAsync(string code, int? excludeId = null)
    {
        var query = _context.TeachingModes.Where(tm => tm.Code == code);
        if (excludeId.HasValue)
            query = query.Where(tm => tm.Id != excludeId.Value);
        return !await query.AnyAsync();
    }
}
