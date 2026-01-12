using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Quran;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Repositories;

public class QuranLevelRepository : GenericRepositoryAsync<QuranLevel>, IQuranLevelRepository
{
    private readonly ApplicationDBContext _context;

    public QuranLevelRepository(ApplicationDBContext context) : base(context)
    {
        _context = context;
    }

    public IQueryable<QuranLevel> GetQuranLevelsQueryable()
    {
        return _context.QuranLevels
            .AsNoTracking()
            .OrderBy(ql => ql.OrderIndex);
    }

    public IQueryable<QuranLevel> GetActiveQuranLevelsQueryable()
    {
        return _context.QuranLevels
            .AsNoTracking()
            .Where(ql => ql.IsActive)
            .OrderBy(ql => ql.OrderIndex);
    }

    public async Task<QuranLevel> GetQuranLevelWithSubjectsAsync(int id)
    {
        return await _context.QuranLevels
            .FirstOrDefaultAsync(ql => ql.Id == id);
    }

    public async Task<QuranLevel> GetQuranLevelByCodeAsync(string code)
    {
        return await _context.QuranLevels
            .AsNoTracking()
            .FirstOrDefaultAsync(ql => ql.NameEn == code);
    }
}
