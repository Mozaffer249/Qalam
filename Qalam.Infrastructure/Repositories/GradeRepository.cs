using Microsoft.EntityFrameworkCore;
using Qalam.Data.DTOs;
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
            .OrderBy(g => g.OrderIndex);
    }

    public IQueryable<GradeDto> GetGradesDtoQueryable()
    {
        return _context.Grades
            .AsNoTracking()
            .Select(g => new GradeDto
            {
                Id = g.Id,
                LevelId = g.LevelId,
                NameAr = g.NameAr,
                NameEn = g.NameEn,
                OrderIndex = g.OrderIndex,
                IsActive = g.IsActive,
                CreatedAt = g.CreatedAt
            });
    }

    public async Task<GradeDto?> GetGradeDtoByIdAsync(int id)
    {
        return await _context.Grades
            .AsNoTracking()
            .Where(g => g.Id == id)
            .Select(g => new GradeDto
            {
                Id = g.Id,
                LevelId = g.LevelId,
                NameAr = g.NameAr,
                NameEn = g.NameEn,
                OrderIndex = g.OrderIndex,
                IsActive = g.IsActive,
                CreatedAt = g.CreatedAt
            })
            .FirstOrDefaultAsync();
    }

    public IQueryable<GradeDto> GetGradesDtoByLevelId(int levelId)
    {
        return _context.Grades
            .AsNoTracking()
            .Where(g => g.LevelId == levelId)
            .Select(g => new GradeDto
            {
                Id = g.Id,
                LevelId = g.LevelId,
                NameAr = g.NameAr,
                NameEn = g.NameEn,
                OrderIndex = g.OrderIndex,
                IsActive = g.IsActive,
                CreatedAt = g.CreatedAt
            })
            .OrderBy(g => g.OrderIndex);
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
            .Where(g => g.Level.CurriculumId == curriculumId)
            .OrderBy(g => g.OrderIndex);
    }

    public async Task<Grade> GetGradeWithSubjectsAsync(int id)
    {
        return await _context.Grades
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

    public async Task<List<FilterOptionDto>> GetGradesAsOptionsAsync(int levelId)
    {
        return await _context.Grades
            .AsNoTracking()
            .Where(g => g.LevelId == levelId && g.IsActive)
            .OrderBy(g => g.OrderIndex)
            .Select(g => new FilterOptionDto
            {
                Id = g.Id,
                NameAr = g.NameAr,
                NameEn = g.NameEn
            })
            .ToListAsync();
    }
}
