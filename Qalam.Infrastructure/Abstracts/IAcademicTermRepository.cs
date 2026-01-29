using Qalam.Data.DTOs;
using Qalam.Data.Entity.Education;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface IAcademicTermRepository : IGenericRepositoryAsync<AcademicTerm>
{
    IQueryable<AcademicTerm> GetTermsQueryable();
    IQueryable<AcademicTermDto> GetTermsDtoQueryable();
    IQueryable<AcademicTerm> GetTermsByCurriculumId(int curriculumId);
    Task<AcademicTerm> GetCurrentTermAsync(int curriculumId);
    Task<AcademicTermDto?> GetTermDtoByIdAsync(int id);

    // Filter options
    Task<List<FilterOptionDto>> GetAcademicTermsAsOptionsAsync(int curriculumId);
}
