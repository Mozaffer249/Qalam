using Qalam.Data.DTOs.Course;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Results;

namespace Qalam.Service.Abstracts;

public interface ITeacherCourseService
{
    Task<CourseDetailDto?> GetCourseByIdForTeacherAsync(int userId, int courseId, CancellationToken cancellationToken = default);

    Task<PaginatedResult<CourseListItemDto>> GetCoursesForTeacherAsync(
        int userId,
        int pageNumber,
        int pageSize,
        int? domainId,
        CourseStatus? status,
        int? subjectId,
        CancellationToken cancellationToken = default);

    Task<CourseDetailDto> CreateCourseAsync(int userId, CreateCourseDto dto, CancellationToken cancellationToken = default);

    Task<CourseDetailDto?> UpdateCourseAsync(int userId, int courseId, UpdateCourseDto dto, CancellationToken cancellationToken = default);

    Task<(bool Success, string Message)> DeleteCourseAsync(int userId, int courseId, CancellationToken cancellationToken = default);
}
