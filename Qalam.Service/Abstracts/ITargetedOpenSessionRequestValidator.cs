using Qalam.Data.DTOs.OpenSessionRequests;

namespace Qalam.Service.Abstracts;

/// <summary>
/// Validates that an Open Session Request targeted at a specific teacher is consistent with that
/// teacher's offerings:
///  - teacher exists and is active,
///  - teacher has an active <c>TeacherSubject</c> row for the requested subject,
///  - every per-session <c>Units[]</c> entry refers to a unit / lesson inside that teacher's repertoire,
///  - per-row invariants hold (exactly-one-of contentUnitId/lessonId, and the
///    includesAllLessons + lessonId pair is forbidden).
///
/// Pure validation — does not mutate state. Returns a single error message on first failure,
/// or <c>null</c> on success. All DB access lives here so handlers stay clean.
/// </summary>
public interface ITargetedOpenSessionRequestValidator
{
    Task<string?> ValidateAsync(
        int targetedTeacherId,
        int subjectId,
        IReadOnlyList<CreateOpenSessionRequestSessionDto> sessions,
        CancellationToken cancellationToken = default);
}
