using Qalam.Data.Entity.Course;

namespace Qalam.Infrastructure.Abstracts;

public interface ISessionAuditLogRepository
{
    Task AddAsync(SessionAuditLog log, CancellationToken cancellationToken = default);

    Task<List<SessionAuditLog>> ListForScheduleAsync(
        int courseScheduleId,
        CancellationToken cancellationToken = default);
}
