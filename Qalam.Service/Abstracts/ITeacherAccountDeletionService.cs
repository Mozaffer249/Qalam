using Qalam.Data.Entity.Identity;

namespace Qalam.Service.Abstracts;

/// <summary>
/// Hard-deletes a teacher, their Identity user, and teacher-owned related data.
/// Shared/student-owned parents are detached, not destroyed.
/// </summary>
public interface ITeacherAccountDeletionService
{
    /// <returns>Success flag and user-facing message.</returns>
    Task<(bool Success, string Message)> DeleteTeacherAccountAsync(
        int teacherId,
        int adminId,
        string? reason,
        CancellationToken cancellationToken = default);
}
