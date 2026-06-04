namespace Qalam.Service.Abstracts;

/// <summary>
/// Orchestrates P3 matching + target-row creation + notification dispatch. Called from the
/// student-side handlers (Create / RespondToInvitation) at the moment a request transitions to Active.
/// Idempotent — safe to call more than once on the same request; only NEW matched teachers are notified.
/// </summary>
public interface IOpenSessionRequestTargetingService
{
    /// <summary>
    /// Runs matching for the request, writes OpenSessionRequestTarget rows for any newly-matched
    /// teachers, and enqueues notifications to them. Returns the count of new targets created.
    /// </summary>
    Task<int> RunMatchingAndNotifyAsync(int requestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Direct-target variant — no broadcast matching. Creates a single OpenSessionRequestTarget row
    /// for the chosen teacher and enqueues a notification email to them. Used when the student picked
    /// a specific teacher up front (<c>OpenSessionRequest.TargetedTeacherId</c> set). Idempotent —
    /// returns 0 if a Target row for this teacher already exists.
    /// </summary>
    Task<int> NotifyTargetedTeacherAsync(int requestId, int teacherId, CancellationToken cancellationToken = default);
}
