using Microsoft.EntityFrameworkCore;
using Qalam.Data.DTOs;
using Qalam.Data.Entity.Education;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Repositories;

public class AcademicTermRepository : GenericRepositoryAsync<AcademicTerm>, IAcademicTermRepository
{
    private readonly ApplicationDBContext _context;

    public AcademicTermRepository(ApplicationDBContext context) : base(context)
    {
        _context = context;
    }

    public IQueryable<AcademicTerm> GetTermsQueryable()
    {
        return _context.AcademicTerms
            .AsNoTracking()
            .OrderBy(at => at.OrderIndex);
    }

    public IQueryable<AcademicTermDto> GetTermsDtoQueryable()
    {
        return _context.AcademicTerms
            .AsNoTracking()
            .Select(at => new AcademicTermDto
            {
                Id = at.Id,
                CurriculumId = at.CurriculumId,
                CurriculumNameAr = at.Curriculum.NameAr,
                CurriculumNameEn = at.Curriculum.NameEn,
                NameAr = at.NameAr,
                NameEn = at.NameEn,
                OrderIndex = at.OrderIndex,
                IsMandatory = at.IsMandatory,
                IsActive = at.IsActive,
                CreatedAt = at.CreatedAt
            })
            .OrderBy(at => at.OrderIndex);
    }

    public async Task<AcademicTermDto?> GetTermDtoByIdAsync(int id)
    {
        return await _context.AcademicTerms
            .AsNoTracking()
            .Where(at => at.Id == id)
            .Select(at => new AcademicTermDto
            {
                Id = at.Id,
                CurriculumId = at.CurriculumId,
                CurriculumNameAr = at.Curriculum.NameAr,
                CurriculumNameEn = at.Curriculum.NameEn,
                NameAr = at.NameAr,
                NameEn = at.NameEn,
                OrderIndex = at.OrderIndex,
                IsMandatory = at.IsMandatory,
                IsActive = at.IsActive,
                CreatedAt = at.CreatedAt
            })
            .FirstOrDefaultAsync();
    }

    public IQueryable<AcademicTerm> GetTermsByCurriculumId(int curriculumId)
    {
        return _context.AcademicTerms
            .AsNoTracking()
            .Where(at => at.CurriculumId == curriculumId)
            .OrderBy(at => at.OrderIndex);
    }

    public async Task<AcademicTerm> GetCurrentTermAsync(int curriculumId)
    {
        return await _context.AcademicTerms
            .AsNoTracking()
            .Where(at => at.CurriculumId == curriculumId && at.IsActive)
            .OrderByDescending(at => at.OrderIndex)
            .FirstOrDefaultAsync();
    }

    public async Task<List<FilterOptionDto>> GetAcademicTermsAsOptionsAsync(int curriculumId)
    {
        return await _context.AcademicTerms
            .AsNoTracking()
            .Where(at => at.CurriculumId == curriculumId && at.IsActive)
            .OrderBy(at => at.OrderIndex)
            .Select(at => new FilterOptionDto
            {
                Id = at.Id,
                NameAr = at.NameAr,
                NameEn = at.NameEn
            })
            .ToListAsync();
    }
}
