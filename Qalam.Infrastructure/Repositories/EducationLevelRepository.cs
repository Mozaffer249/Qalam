using Microsoft.EntityFrameworkCore;
using Qalam.Data.DTOs;
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
            .OrderBy(el => el.OrderIndex);
    }

    public IQueryable<EducationLevelDto> GetLevelsDtoQueryable()
    {
        return _context.EducationLevels
            .AsNoTracking()
            .Select(el => new EducationLevelDto
            {
                Id = el.Id,
                DomainId = el.DomainId,
                DomainNameAr = el.Domain.NameAr,
                DomainNameEn = el.Domain.NameEn,
                CurriculumId = el.CurriculumId,
                CurriculumNameAr = el.Curriculum != null ? el.Curriculum.NameAr : null,
                CurriculumNameEn = el.Curriculum != null ? el.Curriculum.NameEn : null,
                NameAr = el.NameAr,
                NameEn = el.NameEn,
                OrderIndex = el.OrderIndex,
                IsActive = el.IsActive,
                CreatedAt = el.CreatedAt
            })
            .OrderBy(el => el.OrderIndex);
    }

    public async Task<EducationLevelDto?> GetLevelDtoByIdAsync(int id)
    {
        return await _context.EducationLevels
            .AsNoTracking()
            .Where(el => el.Id == id)
            .Select(el => new EducationLevelDto
            {
                Id = el.Id,
                DomainId = el.DomainId,
                DomainNameAr = el.Domain.NameAr,
                DomainNameEn = el.Domain.NameEn,
                CurriculumId = el.CurriculumId,
                CurriculumNameAr = el.Curriculum != null ? el.Curriculum.NameAr : null,
                CurriculumNameEn = el.Curriculum != null ? el.Curriculum.NameEn : null,
                NameAr = el.NameAr,
                NameEn = el.NameEn,
                OrderIndex = el.OrderIndex,
                IsActive = el.IsActive,
                CreatedAt = el.CreatedAt
            })
            .FirstOrDefaultAsync();
    }

    public IQueryable<EducationLevel> GetLevelsByDomainId(int domainId)
    {
        return _context.EducationLevels
            .AsNoTracking()
            .Where(el => el.DomainId == domainId)
            .OrderBy(el => el.OrderIndex);
    }

    public IQueryable<EducationLevel> GetLevelsByCurriculumId(int curriculumId)
    {
        return _context.EducationLevels
            .AsNoTracking()
            .Where(el => el.CurriculumId == curriculumId)
            .OrderBy(el => el.OrderIndex);
    }

    public async Task<EducationLevel> GetLevelWithGradesAsync(int id)
    {
        return await _context.EducationLevels
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
