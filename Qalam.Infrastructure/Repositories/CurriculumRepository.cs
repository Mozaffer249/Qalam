using Microsoft.EntityFrameworkCore;
using Qalam.Data.DTOs;
using Qalam.Data.Entity.Education;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Repositories;

public class CurriculumRepository : GenericRepositoryAsync<Curriculum>, ICurriculumRepository
{
    private readonly ApplicationDBContext _context;

    public CurriculumRepository(ApplicationDBContext context) : base(context)
    {
        _context = context;
    }

    public IQueryable<Curriculum> GetCurriculumsQueryable()
    {
        return _context.Curriculums
            .AsNoTracking()
            .OrderBy(c => c.NameEn);
    }

    public IQueryable<Curriculum> GetActiveCurriculumsQueryable()
    {
        return _context.Curriculums
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.NameEn);
    }

    public async Task<Curriculum> GetCurriculumWithLevelsAsync(int id)
    {
        return await _context.Curriculums
            .Include(c => c.EducationLevels.OrderBy(el => el.OrderIndex))
            .Include(c => c.AcademicTerms.OrderBy(at => at.OrderIndex))
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Curriculum> GetCurriculumByCodeAsync(string code)
    {
        return await _context.Curriculums
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.NameEn == code);
    }

    public async Task<bool> IsCurriculumCodeUniqueAsync(string code, int? excludeId = null)
    {
        var query = _context.Curriculums.Where(c => c.NameEn == code);
        if (excludeId.HasValue)
            query = query.Where(c => c.Id != excludeId.Value);
        return !await query.AnyAsync();
    }

    #region DTO Projection Methods

    public IQueryable<CurriculumDto> GetCurriculumsDtoQueryable()
    {
        return _context.Curriculums
            .AsNoTracking()
            .Select(c => new CurriculumDto
            {
                Id = c.Id,
                NameAr = c.NameAr,
                NameEn = c.NameEn,
                Country = c.Country,
                DescriptionAr = c.DescriptionAr,
                DescriptionEn = c.DescriptionEn,
                IsActive = c.IsActive,
                CreatedAt = c.CreatedAt,
        
            })
            .OrderBy(c => c.NameEn);
    }

    public async Task<CurriculumDto?> GetCurriculumDtoByIdAsync(int id)
    {
        return await _context.Curriculums
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CurriculumDto
            {
                Id = c.Id,
                NameAr = c.NameAr,
                NameEn = c.NameEn,
                Country = c.Country,
                DescriptionAr = c.DescriptionAr,
                DescriptionEn = c.DescriptionEn,
                IsActive = c.IsActive,
                CreatedAt = c.CreatedAt,
         
            })
            .FirstOrDefaultAsync();
    }

    #endregion
}
