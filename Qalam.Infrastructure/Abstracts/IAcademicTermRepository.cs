using Qalam.Data.DTOs;
using Qalam.Data.Entity.Education;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface IAcademicTermRepository : IGenericRepositoryAsync<AcademicTerm>
{
    IQueryable<AcademicTerm> GetTermsQueryable();
    IQueryable<AcademicTermDto> GetTermsDtoQueryable();
    IQueryable<AcademicTerm> GetTermsByCurriculumId(int curriculumId);
    IQueryable<AcademicTerm> GetTermsByAcademicProgramId(int academicProgramId);
    Task<AcademicTerm> GetCurrentTermAsync(int curriculumId);
    Task<AcademicTermDto?> GetTermDtoByIdAsync(int id);

    // Filter options
    Task<List<FilterOptionDto>> GetAcademicTermsAsOptionsAsync(int curriculumId);
    Task<List<FilterOptionDto>> GetAcademicTermsByProgramAsOptionsAsync(int academicProgramId);

    /// <summary>
    /// Nulls TermId on Subjects, ContentUnits, and OpenSessionRequests that reference the term, then deletes it.
    /// </summary>
    /// <returns>false if the term does not exist.</returns>
    Task<bool> DeleteClearingReferencesAsync(int id);
}
