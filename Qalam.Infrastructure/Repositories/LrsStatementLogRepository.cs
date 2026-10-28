using Microsoft.EntityFrameworkCore;
using Qalam.Data.DTOs.Admin;
using Qalam.Data.Entity.LRS;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;

namespace Qalam.Infrastructure.Repositories;

public class LrsStatementLogRepository : ILrsStatementLogRepository
{
    private readonly ApplicationDBContext _db;

    public LrsStatementLogRepository(ApplicationDBContext db)
    {
        _db = db;
    }

    public async Task<(List<AdminLrsStatementListItemDto> Items, int Total)> GetPagedAsync(
        AdminLrsStatementFilter filter, CancellationToken ct = default)
    {
        var query = _db.LrsStatementLogs.AsNoTracking().AsQueryable();

        if (filter.Status.HasValue)
            query = query.Where(l => l.Status == filter.Status.Value);

        if (!string.IsNullOrEmpty(filter.Verb))
            query = query.Where(l => l.Verb == filter.Verb);

        if (filter.DateFrom.HasValue)
            query = query.Where(l => l.CreatedAt >= filter.DateFrom.Value);

        if (filter.DateTo.HasValue)
            query = query.Where(l => l.CreatedAt <= filter.DateTo.Value);

        if (filter.CourseScheduleId.HasValue)
            query = query.Where(l => l.SourceEntityType == "CourseSchedule" && l.SourceEntityId == filter.CourseScheduleId.Value);

        if (filter.EnrollmentId.HasValue)
            query = query.Where(l => l.SourceEntityType == "Enrollment" && l.SourceEntityId == filter.EnrollmentId.Value);

        var total = await query.CountAsync(ct);

        var logs = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(ct);

        var items = new List<AdminLrsStatementListItemDto>();

        foreach (var log in logs)
        {
            var dto = MapToListItem(log);
            await EnrichWithRelatedData(dto, log, ct);
            items.Add(dto);
        }

        return (items, total);
    }

    public async Task<AdminLrsStatementDetailDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var log = await _db.LrsStatementLogs.AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == id, ct);

        if (log == null) return null;

        var dto = new AdminLrsStatementDetailDto
        {
            Id = log.Id,
            StatementId = log.StatementId,
            Verb = log.Verb,
            ActorIdentifier = log.ActorIdentifier,
            ObjectIri = log.ObjectIri,
            Status = log.Status,
            HttpStatusCode = log.HttpStatusCode,
            ErrorMessage = log.ErrorMessage,
            RetryCount = log.RetryCount,
            CreatedAt = log.CreatedAt,
            SentAt = log.SentAt,
            SourceEntityType = log.SourceEntityType,
            SourceEntityId = log.SourceEntityId,
            StatementJson = log.StatementJson
        };

        await EnrichWithRelatedData(dto, log, ct);
        return dto;
    }

    public async Task<AdminLrsDashboardDto> GetDashboardAsync(CancellationToken ct = default)
    {
        var stats = await _db.LrsStatementLogs.AsNoTracking()
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Sent = g.Count(l => l.Status == LrsStatementStatus.Sent),
                Failed = g.Count(l => l.Status == LrsStatementStatus.Failed),
                Queued = g.Count(l => l.Status == LrsStatementStatus.Queued),
                DeadLettered = g.Count(l => l.Status == LrsStatementStatus.DeadLettered),
                LastSent = g.Where(l => l.SentAt != null).Max(l => (DateTime?)l.SentAt)
            })
            .FirstOrDefaultAsync(ct);

        var byVerb = await _db.LrsStatementLogs.AsNoTracking()
            .GroupBy(l => l.Verb)
            .Select(g => new { Verb = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Verb, g => g.Count, ct);

        var total = stats?.Total ?? 0;
        var failed = (stats?.Failed ?? 0) + (stats?.DeadLettered ?? 0);

        return new AdminLrsDashboardDto
        {
            TotalStatements = total,
            SentCount = stats?.Sent ?? 0,
            FailedCount = stats?.Failed ?? 0,
            QueuedCount = stats?.Queued ?? 0,
            DeadLetteredCount = stats?.DeadLettered ?? 0,
            LastSentAt = stats?.LastSent,
            StatementsByVerb = byVerb,
            FailureRate = total > 0 ? Math.Round((decimal)failed / total * 100, 2) : 0
        };
    }

    public async Task<LrsStatementLog?> GetEntityByIdAsync(int id, CancellationToken ct = default)
    {
        return await _db.LrsStatementLogs.FirstOrDefaultAsync(l => l.Id == id, ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
    {
        return _db.SaveChangesAsync(ct);
    }

    private static AdminLrsStatementListItemDto MapToListItem(LrsStatementLog log)
    {
        return new AdminLrsStatementListItemDto
        {
            Id = log.Id,
            StatementId = log.StatementId,
            Verb = log.Verb,
            ActorIdentifier = log.ActorIdentifier,
            ObjectIri = log.ObjectIri,
            Status = log.Status,
            HttpStatusCode = log.HttpStatusCode,
            ErrorMessage = log.ErrorMessage,
            RetryCount = log.RetryCount,
            CreatedAt = log.CreatedAt,
            SentAt = log.SentAt,
            SourceEntityType = log.SourceEntityType,
            SourceEntityId = log.SourceEntityId
        };
    }

    private async Task EnrichWithRelatedData(AdminLrsStatementListItemDto dto, LrsStatementLog log, CancellationToken ct)
    {
        if (log.SourceEntityType == "CourseSchedule")
        {
            var schedule = await _db.CourseSchedules
                .AsNoTracking()
                .Include(s => s.Enrollment).ThenInclude(e => e.Course)
                .FirstOrDefaultAsync(s => s.Id == log.SourceEntityId, ct);

            if (schedule != null)
            {
                dto.SessionDate = schedule.Date;
                dto.EnrollmentId = schedule.EnrollmentId;
                dto.CourseName = schedule.Enrollment?.Course?.Title;
            }
        }
        else if (log.SourceEntityType == "Enrollment")
        {
            var enrollment = await _db.Enrollments
                .AsNoTracking()
                .Include(e => e.Course)
                .FirstOrDefaultAsync(e => e.Id == log.SourceEntityId, ct);

            if (enrollment != null)
            {
                dto.EnrollmentId = enrollment.Id;
                dto.CourseName = enrollment.Course?.Title;
            }
        }

        if (!string.IsNullOrEmpty(log.ActorIdentifier) && log.ActorIdentifier.StartsWith("student-"))
        {
            if (int.TryParse(log.ActorIdentifier.AsSpan("student-".Length), out var userId))
            {
                var user = await _db.Users.AsNoTracking()
                    .Where(u => u.Id == userId)
                    .Select(u => new { u.FirstName, u.LastName })
                    .FirstOrDefaultAsync(ct);

                if (user != null)
                    dto.StudentName = $"{user.FirstName} {user.LastName}".Trim();
            }
        }
    }
}
