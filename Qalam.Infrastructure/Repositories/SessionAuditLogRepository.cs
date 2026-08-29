using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Course;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;

namespace Qalam.Infrastructure.Repositories;

public class SessionAuditLogRepository : ISessionAuditLogRepository
{
    private readonly ApplicationDBContext _context;

    public SessionAuditLogRepository(ApplicationDBContext context)
    {
        _context = context;
    }

    public async Task AddAsync(SessionAuditLog log, CancellationToken cancellationToken = default)
    {
        _context.SessionAuditLogs.Add(log);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<List<SessionAuditLog>> ListForScheduleAsync(
        int courseScheduleId,
        CancellationToken cancellationToken = default) =>
        _context.SessionAuditLogs
            .AsNoTracking()
            .Where(l => l.CourseScheduleId == courseScheduleId)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(cancellationToken);
}
