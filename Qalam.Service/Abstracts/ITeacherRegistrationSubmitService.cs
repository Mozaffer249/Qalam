using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Teacher;

namespace Qalam.Service.Abstracts;

/// <summary>
/// Owns the transactional persistence side of <c>POST /Authentication/Teacher/SubmitRegistrationRequirements</c>.
/// Fresh Incomplete submits wipe orphan rows then insert a full set. Completion submits
/// (missing catalog fields while prior submissions exist) preserve approved rows and only insert missing codes.
///
/// The handler stays thin: it does auth, status guards, requirement-driven validation, and identity
/// business rules — then hands a typed <see cref="TeacherRegistrationSubmissionInput"/> here.
/// </summary>
public interface ITeacherRegistrationSubmitService
{
    /// <summary>
    /// Persist submissions for the teacher in a single transaction. Rolls back on any failure and
    /// re-throws with the SQL inner-exception unwrapped, so the handler can surface a useful 400.
    /// </summary>
    /// <param name="alreadySubmittedCodes">Requirement codes that already have a submission row.</param>
    /// <param name="preserveExistingSubmissions">
    /// When true, do not delete existing submission rows; only insert codes not in
    /// <paramref name="alreadySubmittedCodes"/>. Used for PendingVerification completion of missing fields.
    /// </param>
    Task SubmitAsync(
        Teacher teacher,
        TeacherRegistrationSubmissionInput input,
        List<TeacherRegistrationRequirement> activeRequirements,
        IReadOnlySet<string> alreadySubmittedCodes,
        bool preserveExistingSubmissions,
        CancellationToken cancellationToken);
}
