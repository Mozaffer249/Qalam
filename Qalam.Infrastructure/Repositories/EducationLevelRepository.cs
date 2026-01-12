using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Education;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Repositories;

public class EducationLevelRepository : GenericRepositoryAsync<EducationLevel>, IEducationLevelRepository
{
    private readonly ApplicationDBContext _context;

    public EducationLevelRepository(ApplicationDBContext context) : base(context)
    {
        _context = context;
    }

    public IQueryable<EducationLevel> GetLevelsQueryable()
    {
        return _context.EducationLevels
            .AsNoTracking()
            .Include(el => el.Domain)
            .Include(el => el.Curriculum)
            .OrderBy(el => el.OrderIndex);
    }

    public IQueryable<EducationLevel> GetLevelsByDomainId(int domainId)
    {
        return _context.EducationLevels
            .AsNoTracking()
            .Include(el => el.Curriculum)
            .Where(el => el.DomainId == domainId)
            .OrderBy(el => el.OrderIndex);
    }

    public IQueryable<EducationLevel> GetLevelsByCurriculumId(int curriculumId)
    {
        return _context.EducationLevels
            .AsNoTracking()
            .Include(el => el.Domain)
            .Where(el => el.CurriculumId == curriculumId)
            .OrderBy(el => el.OrderIndex);
    }

    public async Task<EducationLevel> GetLevelWithGradesAsync(int id)
    {
        return await _context.EducationLevels
            .Include(el => el.Domain)
            .Include(el => el.Curriculum)
            .Include(el => el.Grades.OrderBy(g => g.OrderIndex))
            .FirstOrDefaultAsync(el => el.Id == id);
    }

    public async Task<bool> IsLevelCodeUniqueAsync(string code, int? excludeId = null)
    {
        var query = _context.EducationLevels.Where(el => el.NameEn == code);
        if (excludeId.HasValue)
            query = query.Where(el => el.Id != excludeId.Value);
        return !await query.AnyAsync();
    }
}
