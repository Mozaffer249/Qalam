using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Education;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Repositories;

public class GradeRepository : GenericRepositoryAsync<Grade>, IGradeRepository
{
    private readonly ApplicationDBContext _context;

    public GradeRepository(ApplicationDBContext context) : base(context)
    {
        _context = context;
    }

    public IQueryable<Grade> GetGradesQueryable()
    {
        return _context.Grades
            .AsNoTracking()
            .Include(g => g.Level)
            .ThenInclude(l => l.Curriculum)
            .OrderBy(g => g.Level.OrderIndex)
            .ThenBy(g => g.OrderIndex);
    }

    public IQueryable<Grade> GetGradesByLevelId(int levelId)
    {
        return _context.Grades
            .AsNoTracking()
            .Where(g => g.LevelId == levelId)
            .OrderBy(g => g.OrderIndex);
    }

    public IQueryable<Grade> GetGradesByCurriculumId(int curriculumId)
    {
        return _context.Grades
            .AsNoTracking()
            .Include(g => g.Level)
            .Where(g => g.Level.CurriculumId == curriculumId)
            .OrderBy(g => g.Level.OrderIndex)
            .ThenBy(g => g.OrderIndex);
    }

    public async Task<Grade> GetGradeWithSubjectsAsync(int id)
    {
        return await _context.Grades
            .Include(g => g.Level)
            .ThenInclude(l => l.Curriculum)
            .Include(g => g.Subjects.OrderBy(s => s.NameEn))
            .FirstOrDefaultAsync(g => g.Id == id);
    }

    public async Task<bool> IsGradeCodeUniqueAsync(string code, int? excludeId = null)
    {
        var query = _context.Grades.Where(g => g.NameEn == code);
        if (excludeId.HasValue)
            query = query.Where(g => g.Id != excludeId.Value);
        return !await query.AnyAsync();
    }
}
