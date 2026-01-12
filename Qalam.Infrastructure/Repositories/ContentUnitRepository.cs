using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Education;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Repositories;

public class ContentUnitRepository : GenericRepositoryAsync<ContentUnit>, IContentUnitRepository
{
    private readonly ApplicationDBContext _context;

    public ContentUnitRepository(ApplicationDBContext context) : base(context)
    {
        _context = context;
    }

    public IQueryable<ContentUnit> GetContentUnitsQueryable()
    {
        return _context.ContentUnits
            .AsNoTracking()
            .Include(cu => cu.Subject)
            .OrderBy(cu => cu.OrderIndex);
    }

    public IQueryable<ContentUnit> GetContentUnitsBySubjectId(int subjectId)
    {
        return _context.ContentUnits
            .AsNoTracking()
            .Where(cu => cu.SubjectId == subjectId)
            .OrderBy(cu => cu.OrderIndex);
    }

    public IQueryable<ContentUnit> GetContentUnitsByTermId(int termId)
    {
        return _context.ContentUnits
            .AsNoTracking()
            .Include(cu => cu.Subject)
            .Where(cu => cu.Subject.TermId == termId)
            .OrderBy(cu => cu.OrderIndex);
    }

    public async Task<ContentUnit> GetContentUnitWithLessonsAsync(int id)
    {
        return await _context.ContentUnits
            .Include(cu => cu.Subject)
            .Include(cu => cu.Lessons.OrderBy(l => l.OrderIndex))
            .FirstOrDefaultAsync(cu => cu.Id == id);
    }

    public async Task<int> GetNextOrderIndexAsync(int subjectId, int termId)
    {
        var maxOrder = await _context.ContentUnits
            .Where(cu => cu.SubjectId == subjectId)
            .MaxAsync(cu => (int?)cu.OrderIndex) ?? 0;
        return maxOrder + 1;
    }

    public async Task UpdateRangeAsync(IEnumerable<ContentUnit> entities)
    {
        _context.ContentUnits.UpdateRange(entities);
        await _context.SaveChangesAsync();
    }
}
