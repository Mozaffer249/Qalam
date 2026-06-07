using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Teacher;

namespace Qalam.Service.Abstracts;

/// <summary>
/// Owns the transactional persistence side of <c>POST /Authentication/Teacher/SubmitRegistrationRequirements</c>:
/// wipes any orphan submissions/documents left from a prior partial attempt, then re-inserts a
/// fresh set driven by the active <c>TeacherRegistrationRequirement</c> rows.
///
/// The handler stays thin: it does auth, status guards, requirement-driven validation, and identity
/// business rules — then hands a typed <see cref="TeacherRegistrationSubmissionInput"/> here.
/// </summary>
public interface ITeacherRegistrationSubmitService
{
    /// <summary>
    /// Persist all submissions for the teacher in a single transaction. Rolls back on any failure and
    /// re-throws with the SQL inner-exception unwrapped, so the handler can surface a useful 400.
    /// </summary>
    Task SubmitAsync(
        Teacher teacher,
        TeacherRegistrationSubmissionInput input,
        List<TeacherRegistrationRequirement> activeRequirements,
        CancellationToken cancellationToken);
}
