using Qalam.Data.Entity.Education;
using Qalam.Data.Results;

namespace Qalam.Service.Abstracts;

public interface IGradeService
{
    // Education Level Operations
    IQueryable<EducationLevel> GetLevelsQueryable();
    IQueryable<EducationLevel> GetLevelsByDomainIdQueryable(int domainId);
    IQueryable<EducationLevel> GetLevelsByCurriculumIdQueryable(int curriculumId);
    Task<EducationLevel> GetLevelByIdAsync(int id);
    Task<EducationLevel> GetLevelWithGradesAsync(int id);
    Task<EducationLevel> CreateLevelAsync(EducationLevel level);
    Task<EducationLevel> UpdateLevelAsync(EducationLevel level);
    Task<bool> DeleteLevelAsync(int id);

    // Grade Operations
    IQueryable<Grade> GetGradesQueryable();
    IQueryable<Grade> GetGradesByLevelIdQueryable(int levelId);
    IQueryable<Grade> GetGradesByCurriculumIdQueryable(int curriculumId);
    Task<Grade> GetGradeByIdAsync(int id);
    Task<Grade> GetGradeWithSubjectsAsync(int id);
    Task<Grade> CreateGradeAsync(Grade grade);
    Task<Grade> UpdateGradeAsync(Grade grade);
    Task<bool> DeleteGradeAsync(int id);

    // Academic Term Operations
    IQueryable<AcademicTerm> GetTermsQueryable();
    IQueryable<AcademicTerm> GetTermsByCurriculumIdQueryable(int curriculumId);
    Task<AcademicTerm> GetTermByIdAsync(int id);
    Task<AcademicTerm> GetCurrentTermAsync(int curriculumId);
    Task<AcademicTerm> CreateTermAsync(AcademicTerm term);
    Task<AcademicTerm> UpdateTermAsync(AcademicTerm term);
    Task<bool> DeleteTermAsync(int id);

    // Pagination
    Task<PaginatedResult<EducationLevel>> GetPaginatedLevelsAsync(
        int pageNumber, int pageSize, int? domainId = null, int? curriculumId = null, string? search = null);
    Task<PaginatedResult<Grade>> GetPaginatedGradesAsync(
        int pageNumber, int pageSize, int? levelId = null, int? curriculumId = null, string? search = null);
    Task<PaginatedResult<AcademicTerm>> GetPaginatedTermsAsync(
        int pageNumber, int pageSize, int? curriculumId = null);

    // Validation
    Task<bool> IsLevelCodeUniqueAsync(string code, int? excludeId = null);
    Task<bool> IsGradeCodeUniqueAsync(string code, int? excludeId = null);
}
