using Qalam.Data.DTOs.OpenSessionRequests;

namespace Qalam.Service.Abstracts;

public interface IOpenSessionRequestPublishService
{
    /// <summary>
    /// Publishes a Draft OSR: validates sessions/Quran/lead/targeted teacher, auto-accepts owned
    /// invitees, transitions to PendingInvitations or Active, and dispatches matching when Active.
    /// Caller must authorize the acting user before invoking.
    /// </summary>
    Task<OpenSessionRequestPublishResultDto> PublishAsync(
        int requestId,
        int actingUserId,
        CancellationToken cancellationToken = default);
}
