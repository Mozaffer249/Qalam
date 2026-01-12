using Microsoft.EntityFrameworkCore;
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
            .Include(at => at.Curriculum)
            .OrderBy(at => at.OrderIndex);
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
}
