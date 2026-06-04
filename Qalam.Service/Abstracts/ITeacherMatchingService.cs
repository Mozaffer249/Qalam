namespace Qalam.Service.Abstracts;

/// <summary>
/// P3 matching engine — given a published OpenSessionRequest, finds which teachers should
/// receive it in their inbox. Pure read; returns teacher IDs. Target-row writes + notifications
/// happen in the orchestrating IOpenSessionRequestTargetingService.
/// </summary>
public interface ITeacherMatchingService
{
    /// <summary>
    /// Returns the IDs of active teachers whose TeacherSubject covers the request's SubjectId.
    /// Skips teachers already targeted on this request (idempotent — safe to call multiple times).
    /// </summary>
    Task<List<int>> FindMatchingTeacherIdsAsync(int requestId, CancellationToken cancellationToken = default);
}
