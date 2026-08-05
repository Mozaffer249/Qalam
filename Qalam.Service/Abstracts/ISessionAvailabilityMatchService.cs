using Qalam.Data.DTOs.OpenSessionRequests;

namespace Qalam.Service.Abstracts;

/// <summary>
/// Per-session availability vs teacher weekly slots + booked CourseSchedule.
/// Shared by GET availability-match and POST /Offers gating.
/// </summary>
public interface ISessionAvailabilityMatchService
{
    Task<List<SessionAvailabilityMatchDto>> MatchAsync(
        int teacherId,
        int sessionRequestId,
        CancellationToken cancellationToken = default);
}
