using Microsoft.AspNetCore.Http;
using Qalam.Data.DTOs.Student;

namespace Qalam.Service.Abstracts;

public interface IGuardianChildrenService
{
    /// <summary>
    /// Returns the guardian's children (plus self-student if present), enriched with next session/progress.
    /// Null when the user has no guardian profile.
    /// </summary>
    Task<List<ChildStudentDto>?> GetMyChildrenAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a child owned by the guardian. Returns updated DTO, or an error/not-found flag.
    /// </summary>
    Task<GuardianChildUpdateResult> UpdateChildAsync(
        int userId,
        int studentId,
        UpdateChildDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Queues a profile picture upload for a guardian-owned child. Replaces any existing picture
    /// (previous OSS object is deleted by MessagingApi after successful upload).
    /// </summary>
    Task<GuardianChildUpdateResult> UpdateProfilePictureAsync(
        int userId,
        int studentId,
        IFormFile file,
        CancellationToken cancellationToken = default);

    /// <summary>Student ids the user may act on (own student + guardian children).</summary>
    Task<HashSet<int>> GetOwnedStudentIdsAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves the target student for list queries. When <paramref name="studentId"/> is set,
    /// requires ownership; otherwise uses the caller's own student row.
    /// </summary>
    Task<int?> ResolveTargetStudentIdAsync(
        int userId,
        int? studentId,
        CancellationToken cancellationToken = default);
}

public sealed class GuardianChildUpdateResult
{
    public bool Succeeded { get; init; }
    public bool NotFound { get; init; }
    public string? Error { get; init; }
    public ChildStudentDto? Child { get; init; }

    public static GuardianChildUpdateResult Ok(ChildStudentDto child) => new()
    {
        Succeeded = true,
        Child = child,
    };

    public static GuardianChildUpdateResult FailNotFound(string message) => new()
    {
        NotFound = true,
        Error = message,
    };

    public static GuardianChildUpdateResult Fail(string message) => new()
    {
        Error = message,
    };
}
