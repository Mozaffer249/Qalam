using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;

namespace Qalam.Service.Abstracts;

public interface ISessionAuditService
{
    Task LogAsync(
        int courseScheduleId,
        int actorUserId,
        string actorRole,
        SessionAuditActionType actionType,
        object? payload = null,
        CancellationToken cancellationToken = default);

    Task<List<SessionAuditLog>> ListForScheduleAsync(
        int courseScheduleId,
        CancellationToken cancellationToken = default);
}
