using Qalam.Data.Entity.Education;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface IAcademicTermRepository : IGenericRepositoryAsync<AcademicTerm>
{
    IQueryable<AcademicTerm> GetTermsQueryable();
    IQueryable<AcademicTerm> GetTermsByCurriculumId(int curriculumId);
    Task<AcademicTerm> GetCurrentTermAsync(int curriculumId);
}
