using Qalam.Data.DTOs.Admin;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Teacher;
using Qalam.Data.Results;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface ITeacherSubjectRepository : IGenericRepositoryAsync<TeacherSubject>
{
    Task<List<TeacherSubject>> GetTeacherSubjectsWithUnitsAsync(int teacherId);

    Task<TeacherSubject?> GetTeacherSubjectWithUnitsAsync(int teacherSubjectId);

    Task<List<TeacherSubject>> SaveTeacherSubjectsAsync(int teacherId, List<TeacherSubjectItemDto> subjects);

    Task<bool> TeacherHasSubjectAsync(int teacherId, int subjectId);

    /// <summary>Check if teacher has any active subjects.</summary>
    Task<bool> HasAnySubjectsAsync(int teacherId);

    /// <summary>Check if teacher has any subject offerings (active or inactive).</summary>
    Task<bool> HasAnySubjectOfferingsAsync(int teacherId);

    Task<TeacherSubjectActivationSnapshot> GetSubjectActivationSnapshotAsync(int teacherId);

    Task RemoveAllTeacherSubjectsAsync(int teacherId);

    Task<HashSet<int>> GetExistingSubjectIdsAsync(int teacherId);

    /// <summary>
    /// Add new subjects or update existing rows for the same SubjectId (unique per teacher).
    /// Returns the saved subjects (new or updated).
    /// </summary>
    Task<List<TeacherSubject>> AddNewSubjectsAsync(int teacherId, List<TeacherSubjectItemDto> subjects);

    /// <summary>
    /// Matching engine: active TeacherSubject + Active teacher, optionally filtered by Quran coverage.
    /// Empty required type/level sets mean no Quran filter.
    /// </summary>
    Task<List<int>> GetActiveTeacherIdsBySubjectAsync(
        int subjectId,
        IReadOnlyCollection<int>? requiredQuranContentTypeIds = null,
        IReadOnlyCollection<int>? requiredQuranLevelIds = null,
        CancellationToken cancellationToken = default);

    Task<List<TeacherSubject>> GetAllByTeacherIdForAdminAsync(int teacherId, CancellationToken cancellationToken = default);

    Task<TeacherSubject?> GetByIdForTeacherAsync(int teacherId, int teacherSubjectId, CancellationToken cancellationToken = default);

    Task<TeacherSubject?> GetBySubjectIdForTeacherAsync(int teacherId, int subjectId, CancellationToken cancellationToken = default);

    Task<bool> DeleteOwnedAsync(int teacherId, int teacherSubjectId, CancellationToken cancellationToken = default);

    /// <summary>Replace units / CanTeachFullSubject / Quran coverage for an owned subject.</summary>
    Task<TeacherSubject?> ReplaceUnitsAsync(
        int teacherId,
        int teacherSubjectId,
        bool canTeachFullSubject,
        List<TeacherSubjectUnitItemDto> units,
        IReadOnlyList<int>? quranContentTypeIds = null,
        IReadOnlyList<int>? quranLevelIds = null,
        IReadOnlyList<int>? educationLevelIds = null,
        CancellationToken cancellationToken = default);

    Task<PaginatedResult<TeacherSubject>> GetPagedForAdminAsync(
        int pageNumber,
        int pageSize,
        int? teacherId = null,
        int? subjectId = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default);

    Task<List<int>> GetDistinctDomainIdsForTeacherAsync(int teacherId, CancellationToken cancellationToken = default);

    Task<List<TeacherSubject>> GetTeacherSubjectsInDomainAsync(
        int teacherId,
        int domainId,
        CancellationToken cancellationToken = default);

    /// <summary>Active subjects in a domain (for cascade deactivate).</summary>
    Task<List<TeacherSubject>> GetActiveSubjectsInDomainAsync(
        int teacherId,
        int domainId,
        CancellationToken cancellationToken = default);

    /// <summary>Inactive subjects in a domain (for cascade reactivate).</summary>
    Task<List<TeacherSubject>> GetInactiveSubjectsInDomainAsync(
        int teacherId,
        int domainId,
        CancellationToken cancellationToken = default);

    /// <summary>Active subjects with units for student teacher profile / wizard.</summary>
    Task<List<TeacherSubject>> GetActiveSubjectsWithUnitsAsync(
        int teacherId,
        CancellationToken cancellationToken = default);
}
