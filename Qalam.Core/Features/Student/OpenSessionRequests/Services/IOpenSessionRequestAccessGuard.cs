using Qalam.Data.Entity.OpenSessionRequests;

namespace Qalam.Core.Features.Student.OpenSessionRequests.Services;

/// <summary>
/// Guardian-aware authorization for Open Session Requests.
/// A student can act for themselves (if not minor); a guardian can act on behalf of a minor child.
/// </summary>
public interface IOpenSessionRequestAccessGuard
{
    /// <summary>
    /// Can the current user create a new request targeting the given student?
    /// True if (a) the student's User.Id matches currentUserId and they're not a minor,
    /// or (b) currentUserId is the guardian of that student.
    /// </summary>
    Task<AccessGuardResult> CanCreateForStudentAsync(int currentUserId, int targetStudentId, CancellationToken ct);

    /// <summary>
    /// Can the current user act on (cancel, modify, respond to) an existing request?
    /// True if currentUserId equals RequestedByUserId, or the request was created by a guardian
    /// whose User.Id matches currentUserId.
    /// </summary>
    Task<bool> CanActOnRequestAsync(int currentUserId, OpenSessionRequest request, CancellationToken ct);

    /// <summary>
    /// Can the current user respond to an invitation for the given invited student?
    /// True if the invited student is the current user (and not a minor), or the current user
    /// is that student's guardian.
    /// </summary>
    Task<bool> CanRespondToInvitationAsync(int currentUserId, int invitedStudentId, CancellationToken ct);
}

/// <summary>
/// Result of CanCreateForStudentAsync — includes the guardian id when applicable so the handler
/// can populate Enrollment.CreatedByGuardianId without re-querying.
/// </summary>
public record AccessGuardResult(bool Allowed, int? GuardianId, string? Reason)
{
    public static AccessGuardResult Allow() => new(true, null, null);
    public static AccessGuardResult AllowAsGuardian(int guardianId) => new(true, guardianId, null);
    public static AccessGuardResult Deny(string reason) => new(false, null, reason);
}
