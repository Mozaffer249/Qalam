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
                CurriculumNameAr = at.Curriculum != null ? at.Curriculum.NameAr : null,
                CurriculumNameEn = at.Curriculum != null ? at.Curriculum.NameEn : null,
                AcademicProgramId = at.AcademicProgramId,
                AcademicProgramNameAr = at.AcademicProgram != null ? at.AcademicProgram.NameAr : null,
                AcademicProgramNameEn = at.AcademicProgram != null ? at.AcademicProgram.NameEn : null,
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
                CurriculumNameAr = at.Curriculum != null ? at.Curriculum.NameAr : null,
                CurriculumNameEn = at.Curriculum != null ? at.Curriculum.NameEn : null,
                AcademicProgramId = at.AcademicProgramId,
                AcademicProgramNameAr = at.AcademicProgram != null ? at.AcademicProgram.NameAr : null,
                AcademicProgramNameEn = at.AcademicProgram != null ? at.AcademicProgram.NameEn : null,
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

    public IQueryable<AcademicTerm> GetTermsByAcademicProgramId(int academicProgramId)
    {
        return _context.AcademicTerms
            .AsNoTracking()
            .Where(at => at.AcademicProgramId == academicProgramId)
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
                NameEn = at.NameEn,
                CanDelete = true
            })
            .ToListAsync();
    }

    public async Task<List<FilterOptionDto>> GetAcademicTermsByProgramAsOptionsAsync(int academicProgramId)
    {
        return await _context.AcademicTerms
            .AsNoTracking()
            .Where(at => at.AcademicProgramId == academicProgramId && at.IsActive)
            .OrderBy(at => at.OrderIndex)
            .Select(at => new FilterOptionDto
            {
                Id = at.Id,
                NameAr = at.NameAr,
                NameEn = at.NameEn,
                CanDelete = true
            })
            .ToListAsync();
    }

    public async Task<bool> DeleteClearingReferencesAsync(int id)
    {
        var term = await _context.AcademicTerms.FirstOrDefaultAsync(t => t.Id == id);
        if (term == null)
            return false;

        await _context.Subjects
            .Where(s => s.TermId == id)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.TermId, (int?)null));

        await _context.ContentUnits
            .Where(cu => cu.TermId == id)
            .ExecuteUpdateAsync(cu => cu.SetProperty(x => x.TermId, (int?)null));

        await _context.OpenSessionRequests
            .Where(r => r.TermId == id)
            .ExecuteUpdateAsync(r => r.SetProperty(x => x.TermId, (int?)null));

        _context.AcademicTerms.Remove(term);
        await _context.SaveChangesAsync();
        return true;
    }
}
