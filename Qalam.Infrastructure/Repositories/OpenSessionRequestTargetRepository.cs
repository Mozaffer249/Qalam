using Microsoft.EntityFrameworkCore;
using Qalam.Data.DTOs.OpenSessionRequests;
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
        var query = _context.OpenSessionRequestTargets
            .AsNoTracking()
            .Where(t => t.TeacherId == teacherId);

        if (filters.Status.HasValue)
            query = query.Where(t => t.Status == filters.Status.Value);

        if (filters.SubjectId.HasValue)
            query = query.Where(t => t.OpenSessionRequest.SubjectId == filters.SubjectId.Value);

        if (filters.DateFrom.HasValue)
        {
            var from = filters.DateFrom.Value;
            query = query.Where(t => t.OpenSessionRequest.Sessions.Any(s => s.PreferredDate >= from));
        }

        if (filters.DateTo.HasValue)
        {
            var to = filters.DateTo.Value;
            query = query.Where(t => t.OpenSessionRequest.Sessions.Any(s => s.PreferredDate <= to));
        }

        // Always exclude requests that have already exited the offer-accepting window.
        query = query.Where(t => t.OpenSessionRequest.Status == OpenSessionRequestStatus.Active
                                 || t.OpenSessionRequest.Status == OpenSessionRequestStatus.ReceivingOffers);

        query = filters.SortBy switch
        {
            TeacherInboxSort.ExpiringSoon => query.OrderBy(t => t.OpenSessionRequest.ExpiresAt),
            TeacherInboxSort.MostOffers => query.OrderByDescending(t => t.OpenSessionRequest.Offers.Count(o => o.Status != OpenSessionOfferStatus.Withdrawn)),
            _ => query.OrderByDescending(t => t.MatchedAt),
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((filters.PageNumber - 1) * filters.PageSize)
            .Take(filters.PageSize)
            .Select(t => new TeacherAvailableRequestListItemDto
            {
                Id = t.OpenSessionRequest.Id,
                SubjectId = t.OpenSessionRequest.SubjectId,
                SubjectNameEn = t.OpenSessionRequest.Subject != null ? t.OpenSessionRequest.Subject.NameEn : null,
                SubjectNameAr = t.OpenSessionRequest.Subject != null ? t.OpenSessionRequest.Subject.NameAr : null,
                LevelId = t.OpenSessionRequest.LevelId,
                LevelNameEn = t.OpenSessionRequest.Level != null ? t.OpenSessionRequest.Level.NameEn : null,
                LevelNameAr = t.OpenSessionRequest.Level != null ? t.OpenSessionRequest.Level.NameAr : null,
                StudentId = t.OpenSessionRequest.StudentId,
                StudentDisplayName =
                    t.OpenSessionRequest.Student != null && t.OpenSessionRequest.Student.User != null
                        ? (t.OpenSessionRequest.Student.User.FirstName ?? "") + " " + (t.OpenSessionRequest.Student.User.LastName ?? "")
                        : null,
                SessionsCount = t.OpenSessionRequest.TotalSessionsCount,
                TeachingModeId = t.OpenSessionRequest.TeachingModeId,
                TeachingModeNameEn = t.OpenSessionRequest.TeachingMode != null ? t.OpenSessionRequest.TeachingMode.NameEn : null,
                GroupType = t.OpenSessionRequest.GroupType,
                PreferredDates = t.OpenSessionRequest.Sessions
                    .Where(s => s.PreferredDate.HasValue)
                    .OrderBy(s => s.SequenceNumber)
                    .Select(s => s.PreferredDate!.Value)
                    .ToList(),
                CurrentOffersCount = t.OpenSessionRequest.Offers.Count(o => o.Status != OpenSessionOfferStatus.Withdrawn),
                ExpiresAt = t.OpenSessionRequest.ExpiresAt ?? DateTime.MinValue,
                TargetStatus = t.Status,
                MatchedAt = t.MatchedAt,
                ViewedAt = t.ViewedAt,
            })
            .ToListAsync(cancellationToken);

        return new PaginatedResult<TeacherAvailableRequestListItemDto>(items, totalCount, filters.PageNumber, filters.PageSize);
    }
}
