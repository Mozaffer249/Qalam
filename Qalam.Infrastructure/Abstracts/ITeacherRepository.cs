using Qalam.Data.DTOs.Admin;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Teacher;
using Qalam.Data.Results;
using Qalam.Infrastructure.InfrastructureBases;
using StudentEntity = Qalam.Data.Entity.Student.Student;

namespace Qalam.Infrastructure.Abstracts;

public interface ITeacherRepository : IGenericRepositoryAsync<Teacher>
{
    Task<Teacher?> GetByUserIdAsync(int userId);
    Task UpdateStatusAsync(int teacherId, TeacherStatus status);
    Task UpdateLocationAsync(int teacherId, TeacherLocation location);

    // Admin operations
    IQueryable<Teacher> GetPendingTeachersQueryable();
    Task<int> CountAsync(IQueryable<Teacher> query);
    Task<List<PendingTeacherDto>> GetPendingTeachersDtoAsync(int pageNumber, int pageSize);
    Task<TeacherDetailsDto?> GetTeacherDetailsAsync(int teacherId);

    /// <summary>
    /// Admin paginated teacher browse with optional filters (status, location, subject, search).
    /// </summary>
    Task<PaginatedResult<AdminTeacherListItemDto>> SearchForAdminAsync(
        AdminTeacherListFilters filters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Batch projection used by the targeting service after matching: returns (TeacherId, Email)
    /// pairs for the supplied teacher ids whose User.Email is non-empty. Single query, narrow SELECT.
    /// </summary>
    Task<List<(int TeacherId, string Email)>> GetEmailsByTeacherIdsAsync(IReadOnlyCollection<int> teacherIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Student-facing recommendation: top <paramref name="take"/> teachers narrowed by the student's
    /// profile (DomainId / LevelId / GradeId, when present). Filters: Teacher.Status == Active and
    /// IsActive. Ordered by RatingAverage DESC, approved-reviews count DESC, CreatedAt DESC.
    /// Single SQL round-trip — projects straight to <see cref="TeacherCardDto"/>.
    /// </summary>
    Task<List<TeacherCardDto>> GetRecommendedForStudentAsync(
        StudentEntity student,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Student-facing paginated browse with optional filters. Every filter is AND-combined when supplied.
    /// Used by the scenario-2 "Find a Teacher" picker before setting <c>TargetedTeacherId</c>.
    /// </summary>
    Task<PaginatedResult<TeacherCardDto>> SearchAsync(
        TeacherSearchFilters filters,
        CancellationToken cancellationToken = default);
}

/// <summary>Filter + paging + sort inputs for <see cref="ITeacherRepository.SearchAsync"/>.</summary>
public record TeacherSearchFilters(
    int? SubjectId,
    int? DomainId,
    int? LevelId,
    int? GradeId,
    int? QuranContentTypeId,
    int? QuranLevelId,
    TeacherLocation? Location,
    decimal? MinRating,
    string? Search,
    TeacherSearchSort SortBy,
    int PageNumber,
    int PageSize);

public enum TeacherSearchSort
{
    Rating = 1,
    Newest = 2,
    NameAsc = 3
}

/// <summary>Filter + paging + sort inputs for <see cref="ITeacherRepository.SearchForAdminAsync"/>.</summary>
public record AdminTeacherListFilters(
    TeacherStatus? Status,
    TeacherLocation? Location,
    int? SubjectId,
    string? Search,
    AdminTeacherListSort SortBy,
    int PageNumber,
    int PageSize);

public enum AdminTeacherListSort
{
    Newest = 1,
    NameAsc = 2,
    Status = 3
}
