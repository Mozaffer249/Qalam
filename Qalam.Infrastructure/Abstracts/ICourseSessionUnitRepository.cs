using Qalam.Data.DTOs.Course;
using Qalam.Data.Entity.Course;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface ICourseSessionUnitRepository : IGenericRepositoryAsync<CourseSessionUnit>
{
    /// <summary>
    /// Returns the SubjectId of the course this session belongs to, but only if the session lives
    /// under the given course and that course is owned by the given teacher. Returns null otherwise.
    /// Used by the update-session-units endpoint to authorize + resolve the subject in one round-trip.
    /// </summary>
    Task<int?> GetSubjectIdForOwnedSessionAsync(int sessionId, int courseId, int teacherId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically replaces all CourseSessionUnit rows for the session: bulk-deletes existing rows
    /// then inserts the new set. Single SaveChanges. Returns the inserted row count.
    /// </summary>
    Task<int> ReplaceUnitsAsync(int sessionId, IEnumerable<CourseSessionUnit> newUnits, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the units back as response DTOs via .Select() projection (no entity materialization).
    /// </summary>
    Task<List<CourseSessionUnitDto>> GetHydratedDtosBySessionAsync(int sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that every supplied ContentUnit / Lesson belongs to <paramref name="subjectId"/>.
    /// Throws InvalidOperationException with the appropriate Arabic-or-English message when a mismatch is found.
    /// </summary>
    Task ValidateUnitsBelongToSubjectAsync(
        IReadOnlyCollection<int> contentUnitIds,
        IReadOnlyCollection<int> lessonIds,
        int subjectId,
        CancellationToken cancellationToken = default);
}
