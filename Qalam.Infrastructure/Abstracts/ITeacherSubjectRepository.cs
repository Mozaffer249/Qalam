using Qalam.Data.DTOs.Admin;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Teacher;
using Qalam.Data.Results;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface ITeacherSubjectRepository : IGenericRepositoryAsync<TeacherSubject>
{
    /// <summary>
    /// Get all teacher subjects with their units
    /// </summary>
    Task<List<TeacherSubject>> GetTeacherSubjectsWithUnitsAsync(int teacherId);
    
    /// <summary>
    /// Get a specific teacher subject with units
    /// </summary>
    Task<TeacherSubject?> GetTeacherSubjectWithUnitsAsync(int teacherSubjectId);
    
    /// <summary>
    /// Save teacher subjects (replaces existing)
    /// </summary>
    Task<List<TeacherSubject>> SaveTeacherSubjectsAsync(int teacherId, List<TeacherSubjectItemDto> subjects);
    
    /// <summary>
    /// Check if teacher has a specific subject
    /// </summary>
    Task<bool> TeacherHasSubjectAsync(int teacherId, int subjectId);
    
    /// <summary>
    /// Check if teacher has any approved active subjects (optimized - doesn't retrieve data)
    /// </summary>
    Task<bool> HasAnySubjectsAsync(int teacherId);

    /// <summary>
    /// Check if teacher has any subject offerings (any verification status)
    /// </summary>
    Task<bool> HasAnySubjectOfferingsAsync(int teacherId);

    /// <summary>
    /// Counts by verification status for activation gating
    /// </summary>
    Task<TeacherSubjectActivationSnapshot> GetSubjectActivationSnapshotAsync(int teacherId);
    
    /// <summary>
    /// Remove all subjects for a teacher
    /// </summary>
    Task RemoveAllTeacherSubjectsAsync(int teacherId);
    
    /// <summary>
    /// Get only SubjectIds for a teacher (optimized - no full data)
    /// </summary>
    Task<HashSet<int>> GetExistingSubjectIdsAsync(int teacherId);
    
    /// <summary>
    /// Add only new subjects (skip existing ones)
    /// </summary>
    Task<List<TeacherSubject>> AddNewSubjectsAsync(int teacherId, List<TeacherSubjectItemDto> subjects);

    /// <summary>
    /// Matching engine: return the IDs of teachers who teach <paramref name="subjectId"/> via an
    /// active TeacherSubject and whose own Teacher.Status is Active. Distinct, no duplicates.
    /// </summary>
    Task<List<int>> GetActiveTeacherIdsBySubjectAsync(int subjectId, CancellationToken cancellationToken = default);

    /// <summary>All subjects for a teacher (active, inactive, rejected) with units.</summary>
    Task<List<TeacherSubject>> GetAllByTeacherIdForAdminAsync(int teacherId, CancellationToken cancellationToken = default);

    /// <summary>Owned teacher subject with units, or null.</summary>
    Task<TeacherSubject?> GetByIdForTeacherAsync(int teacherId, int teacherSubjectId, CancellationToken cancellationToken = default);

    /// <summary>Paginated cross-teacher list for admin.</summary>
    Task<PaginatedResult<TeacherSubject>> GetPagedForAdminAsync(
        int pageNumber,
        int pageSize,
        int? teacherId = null,
        int? subjectId = null,
        bool? isActive = null,
        DocumentVerificationStatus? verificationStatus = null,
        CancellationToken cancellationToken = default);

    /// <summary>Distinct education domain IDs from the teacher's subject offerings.</summary>
    Task<List<int>> GetDistinctDomainIdsForTeacherAsync(int teacherId, CancellationToken cancellationToken = default);
}
