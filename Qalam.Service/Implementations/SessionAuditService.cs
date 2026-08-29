using System.Text.Json;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class SessionAuditService : ISessionAuditService
{
    private readonly ISessionAuditLogRepository _auditLogs;

    public SessionAuditService(ISessionAuditLogRepository auditLogs)
    {
        _auditLogs = auditLogs;
    }

    public async Task LogAsync(
        int courseScheduleId,
        int actorUserId,
        string actorRole,
        SessionAuditActionType actionType,
        object? payload = null,
        CancellationToken cancellationToken = default)
    {
        await _auditLogs.AddAsync(new SessionAuditLog
        {
            CourseScheduleId = courseScheduleId,
            ActorUserId = actorUserId,
            ActorRole = actorRole,
            ActionType = actionType,
            PayloadJson = payload == null ? null : JsonSerializer.Serialize(payload),
            CreatedAt = DateTime.UtcNow,
        }, cancellationToken);
    }

    public Task<List<SessionAuditLog>> ListForScheduleAsync(
        int courseScheduleId,
        CancellationToken cancellationToken = default) =>
        _auditLogs.ListForScheduleAsync(courseScheduleId, cancellationToken);
}
