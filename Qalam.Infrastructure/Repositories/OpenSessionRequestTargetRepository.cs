using Microsoft.EntityFrameworkCore;
using Qalam.Data.DTOs.OpenSessionRequests;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.OpenSessionRequests;
using Qalam.Data.Results;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Repositories;

public class OpenSessionRequestTargetRepository : GenericRepositoryAsync<OpenSessionRequestTarget>, IOpenSessionRequestTargetRepository
{
    private readonly ApplicationDBContext _context;

    public OpenSessionRequestTargetRepository(ApplicationDBContext context) : base(context)
    {
        _context = context;
    }

    public async Task<List<int>> GetTargetedTeacherIdsAsync(int requestId, CancellationToken cancellationToken = default)
    {
        return await _context.OpenSessionRequestTargets
            .AsNoTracking()
            .Where(t => t.SessionRequestId == requestId)
            .Select(t => t.TeacherId)
            .ToListAsync(cancellationToken);
    }

    public async Task BulkInsertAsync(IEnumerable<OpenSessionRequestTarget> targets, CancellationToken cancellationToken = default)
    {
        await _context.OpenSessionRequestTargets.AddRangeAsync(targets, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<OpenSessionRequestTarget?> GetByRequestAndTeacherAsync(int requestId, int teacherId, CancellationToken cancellationToken = default)
    {
        return await _context.OpenSessionRequestTargets
            .FirstOrDefaultAsync(t => t.SessionRequestId == requestId && t.TeacherId == teacherId, cancellationToken);
    }

    public async Task<bool> SetStatusAsync(int requestId, int teacherId, OpenSessionRequestTargetStatus newStatus, CancellationToken cancellationToken = default)
    {
        var target = await _context.OpenSessionRequestTargets
            .FirstOrDefaultAsync(t => t.SessionRequestId == requestId && t.TeacherId == teacherId, cancellationToken);

        if (target == null) return false;

        target.Status = newStatus;
        if (newStatus == OpenSessionRequestTargetStatus.Viewed && target.ViewedAt == null)
            target.ViewedAt = DateTime.UtcNow;
        target.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<PaginatedResult<TeacherAvailableRequestListItemDto>> GetTeacherInboxAsync(
        int teacherId,
        TeacherInboxFilters filters,
        CancellationToken cancellationToken = default)
    {
        var query =
            from t in _context.OpenSessionRequestTargets.AsNoTracking()
            where t.TeacherId == teacherId
            join r in _context.OpenSessionRequests.AsNoTracking() on t.SessionRequestId equals r.Id
            join u in _context.Users.AsNoTracking() on r.RequestedByUserId equals u.Id
            join st in _context.Students.AsNoTracking() on u.Id equals st.UserId into studentJoin
            from student in studentJoin.DefaultIfEmpty()
            select new { Target = t, Request = r, User = u, Student = student };

        if (filters.Status.HasValue)
            query = query.Where(x => x.Target.Status == filters.Status.Value);

        if (filters.SubjectId.HasValue)
            query = query.Where(x => x.Request.SubjectId == filters.SubjectId.Value);

        if (filters.DateFrom.HasValue)
        {
            var from = filters.DateFrom.Value;
            query = query.Where(x => x.Request.Sessions.Any(s => s.PreferredDate >= from));
        }

        if (filters.DateTo.HasValue)
        {
            var to = filters.DateTo.Value;
            query = query.Where(x => x.Request.Sessions.Any(s => s.PreferredDate <= to));
        }

        query = query.Where(x => x.Request.Status == OpenSessionRequestStatus.Active
                                 || x.Request.Status == OpenSessionRequestStatus.ReceivingOffers);

        query = filters.SortBy switch
        {
            TeacherInboxSort.ExpiringSoon => query.OrderBy(x => x.Request.ExpiresAt),
            TeacherInboxSort.MostOffers => query.OrderByDescending(x =>
                x.Request.Offers.Count(o => o.Status != OpenSessionOfferStatus.Withdrawn)),
            _ => query.OrderByDescending(x => x.Target.MatchedAt),
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((filters.PageNumber - 1) * filters.PageSize)
            .Take(filters.PageSize)
            .Select(x => new TeacherAvailableRequestListItemDto
            {
                Id = x.Request.Id,
                SubjectId = x.Request.SubjectId,
                SubjectNameEn = x.Request.Subject != null ? x.Request.Subject.NameEn : null,
                SubjectNameAr = x.Request.Subject != null ? x.Request.Subject.NameAr : null,
                LevelId = x.Request.LevelId,
                LevelNameEn = x.Request.Level != null ? x.Request.Level.NameEn : null,
                LevelNameAr = x.Request.Level != null ? x.Request.Level.NameAr : null,
                StudentId = x.Student != null ? x.Student.Id : 0,
                StudentDisplayName =
                    ((x.User.FirstName ?? "") + " " + (x.User.LastName ?? "")).Trim(),
                SessionsCount = x.Request.TotalSessionsCount,
                TeachingModeId = x.Request.TeachingModeId,
                TeachingModeNameEn = x.Request.TeachingMode != null ? x.Request.TeachingMode.NameEn : null,
                GroupType = x.Request.GroupType,
                PreferredDates = x.Request.Sessions
                    .Where(s => s.PreferredDate.HasValue)
                    .OrderBy(s => s.SequenceNumber)
                    .Select(s => s.PreferredDate!.Value)
                    .ToList(),
                CurrentOffersCount = x.Request.Offers.Count(o => o.Status != OpenSessionOfferStatus.Withdrawn),
                ExpiresAt = x.Request.ExpiresAt ?? DateTime.MinValue,
                TargetStatus = x.Target.Status,
                MatchedAt = x.Target.MatchedAt,
                ViewedAt = x.Target.ViewedAt,
            })
            .ToListAsync(cancellationToken);

        return new PaginatedResult<TeacherAvailableRequestListItemDto>(items, totalCount, filters.PageNumber, filters.PageSize);
    }

    public async Task<TeacherInboxCountsDto> GetTeacherInboxCountsAsync(
        int teacherId,
        CancellationToken cancellationToken = default)
    {
        var query = _context.OpenSessionRequestTargets
            .AsNoTracking()
            .Where(t => t.TeacherId == teacherId)
            .Where(t => t.OpenSessionRequest.Status == OpenSessionRequestStatus.Active
                         || t.OpenSessionRequest.Status == OpenSessionRequestStatus.ReceivingOffers);

        var grouped = await query
            .GroupBy(t => t.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var counts = new TeacherInboxCountsDto
        {
            All = grouped.Sum(x => x.Count),
            Notified = grouped.FirstOrDefault(x => x.Status == OpenSessionRequestTargetStatus.Notified)?.Count ?? 0,
            Viewed = grouped.FirstOrDefault(x => x.Status == OpenSessionRequestTargetStatus.Viewed)?.Count ?? 0,
            OfferSubmitted = grouped.FirstOrDefault(x => x.Status == OpenSessionRequestTargetStatus.OfferSubmitted)?.Count ?? 0,
            Skipped = grouped.FirstOrDefault(x => x.Status == OpenSessionRequestTargetStatus.Skipped)?.Count ?? 0,
        };

        return counts;
    }
}
