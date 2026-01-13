using Qalam.Data.DTOs;
using Qalam.Data.Entity.Education;
using Qalam.Data.Results;

namespace Qalam.Service.Abstracts;

public interface ISubjectService
{
    // Query Operations
    IQueryable<Subject> GetSubjectsQueryable();
    IQueryable<Subject> GetSubjectsByDomainIdQueryable(int domainId);
    IQueryable<Subject> GetSubjectsByCurriculumIdQueryable(int curriculumId);
    IQueryable<Subject> GetSubjectsByLevelIdQueryable(int levelId);
    IQueryable<Subject> GetSubjectsByGradeIdQueryable(int gradeId);
    IQueryable<Subject> GetSubjectsByTermIdQueryable(int termId);
    Task<Subject> GetSubjectByIdAsync(int id);
    Task<SubjectDto?> GetSubjectDtoByIdAsync(int id);
    Task<Subject> GetSubjectWithDetailsAsync(int id);

    // Command Operations
    Task<Subject> CreateSubjectAsync(Subject subject);
    Task<Subject> UpdateSubjectAsync(Subject subject);
    Task<bool> DeleteSubjectAsync(int id);
    Task<bool> ToggleSubjectStatusAsync(int id);

    // Pagination
    Task<PaginatedResult<SubjectDto>> GetPaginatedSubjectsAsync(
        int pageNumber, int pageSize, int? domainId = null, int? curriculumId = null,
        int? levelId = null, int? gradeId = null, int? termId = null, string? search = null);
}
