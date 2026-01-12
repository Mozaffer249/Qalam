using Qalam.Data.Entity.Education;
using Qalam.Data.Results;

namespace Qalam.Service.Abstracts;

public interface IContentManagementService
{
    // Content Unit Operations
    IQueryable<ContentUnit> GetContentUnitsQueryable();
    IQueryable<ContentUnit> GetContentUnitsBySubjectIdQueryable(int subjectId);
    IQueryable<ContentUnit> GetContentUnitsByTermIdQueryable(int termId);
    Task<ContentUnit> GetContentUnitByIdAsync(int id);
    Task<ContentUnit> GetContentUnitWithLessonsAsync(int id);
    Task<ContentUnit> CreateContentUnitAsync(ContentUnit contentUnit);
    Task<ContentUnit> UpdateContentUnitAsync(ContentUnit contentUnit);
    Task<bool> DeleteContentUnitAsync(int id);
    Task<bool> ToggleContentUnitStatusAsync(int id);
    Task<bool> ReorderContentUnitsAsync(int subjectId, int termId, List<int> orderedIds);

    // Lesson Operations
    IQueryable<Lesson> GetLessonsQueryable();
    IQueryable<Lesson> GetLessonsByContentUnitIdQueryable(int contentUnitId);
    IQueryable<Lesson> GetLessonsBySubjectIdQueryable(int subjectId);
    Task<Lesson> GetLessonByIdAsync(int id);
    Task<Lesson> GetLessonWithDetailsAsync(int id);
    Task<Lesson> CreateLessonAsync(Lesson lesson);
    Task<Lesson> UpdateLessonAsync(Lesson lesson);
    Task<bool> DeleteLessonAsync(int id);
    Task<bool> ToggleLessonStatusAsync(int id);
    Task<bool> ReorderLessonsAsync(int contentUnitId, List<int> orderedIds);

    // Pagination
    Task<PaginatedResult<ContentUnit>> GetPaginatedContentUnitsAsync(
        int pageNumber, int pageSize, int? subjectId = null, int? termId = null, string? search = null);
    Task<PaginatedResult<Lesson>> GetPaginatedLessonsAsync(
        int pageNumber, int pageSize, int? contentUnitId = null, int? subjectId = null, string? search = null);
}
