using Microsoft.EntityFrameworkCore;
using Qalam.Data.DTOs.OpenSessionRequests;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.OpenSessionRequests;
using Qalam.Data.Results;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Repositories;

public class OpenSessionOfferRepository : GenericRepositoryAsync<OpenSessionOffer>, IOpenSessionOfferRepository
{
    private readonly ApplicationDBContext _context;

    public OpenSessionOfferRepository(ApplicationDBContext context) : base(context)
    {
        _context = context;
    }

    public async Task<(int OfferId, OpenSessionOfferStatus Status)?> GetExistingActiveOfferAsync(int requestId, int teacherId, CancellationToken cancellationToken = default)
    {
        var row = await _context.OpenSessionOffers
            .AsNoTracking()
            .Where(o => o.SessionRequestId == requestId
                        && o.TeacherId == teacherId
                        && o.Status != OpenSessionOfferStatus.Withdrawn)
            .Select(o => new { o.Id, o.Status })
            .FirstOrDefaultAsync(cancellationToken);

        return row == null ? null : (row.Id, row.Status);
    }

    public async Task<OpenSessionOffer?> GetByIdForOwnerActionAsync(int offerId, int teacherId, CancellationToken cancellationToken = default)
    {
        return await _context.OpenSessionOffers
            .Where(o => o.Id == offerId && o.TeacherId == teacherId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<TeacherOfferDetailDto?> GetTeacherDetailDtoAsync(int offerId, int teacherId, CancellationToken cancellationToken = default)
    {
        return await _context.OpenSessionOffers
            .AsNoTracking()
            .Where(o => o.Id == offerId && o.TeacherId == teacherId)
            .Select(o => new TeacherOfferDetailDto
            {
                Id = o.Id,
                SessionRequestId = o.SessionRequestId,
                TeacherId = o.TeacherId,
                Price = o.Price,
                TeacherNotes = o.TeacherNotes,
                Status = o.Status,
                Version = o.Version,
                CreatedAt = o.CreatedAt,
                ExpiresAt = o.ExpiresAt,
                AcceptedAt = o.AcceptedAt,
                RejectedAt = o.RejectedAt,
                WithdrawnAt = o.WithdrawnAt,
                ExpiredAt = o.ExpiredAt,
                RejectionReason = o.RejectionReason,
                ConversationId = o.Conversation != null ? (int?)o.Conversation.Id : null,
                Request = null
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PaginatedResult<TeacherOfferListItemDto>> GetTeacherMyOffersAsync(
        int teacherId,
        TeacherMyOffersFilters filters,
        CancellationToken cancellationToken = default)
    {
        var query =
            from o in _context.OpenSessionOffers.AsNoTracking()
            where o.TeacherId == teacherId
            join r in _context.OpenSessionRequests.AsNoTracking() on o.SessionRequestId equals r.Id
            join u in _context.Users.AsNoTracking() on r.RequestedByUserId equals u.Id
            join st in _context.Students.AsNoTracking() on u.Id equals st.UserId into studentJoin
            from student in studentJoin.DefaultIfEmpty()
            select new { Offer = o, Request = r, User = u, Student = student };

        if (filters.Status.HasValue)
            query = query.Where(x => x.Offer.Status == filters.Status.Value);

        if (filters.DateFrom.HasValue)
        {
            var fromUtc = filters.DateFrom.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(x => x.Offer.CreatedAt >= fromUtc);
        }

        if (filters.DateTo.HasValue)
        {
            var toUtc = filters.DateTo.Value.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
            query = query.Where(x => x.Offer.CreatedAt <= toUtc);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.Offer.CreatedAt)
            .Skip((filters.PageNumber - 1) * filters.PageSize)
            .Take(filters.PageSize)
            .Select(x => new TeacherOfferListItemDto
            {
                Id = x.Offer.Id,
                SessionRequestId = x.Offer.SessionRequestId,
                SubjectId = x.Request.SubjectId,
                SubjectNameEn = x.Request.Subject != null ? x.Request.Subject.NameEn : null,
                SubjectNameAr = x.Request.Subject != null ? x.Request.Subject.NameAr : null,
                StudentId = x.Student != null ? x.Student.Id : 0,
                StudentDisplayName = ((x.User.FirstName ?? "") + " " + (x.User.LastName ?? "")).Trim(),
                Price = x.Offer.Price,
                SessionsCount = x.Request.TotalSessionsCount,
                Status = x.Offer.Status,
                Version = x.Offer.Version,
                CreatedAt = x.Offer.CreatedAt,
                ExpiresAt = x.Offer.ExpiresAt,
                UnreadMessagesCount = x.Offer.Conversation != null
                    ? x.Offer.Conversation.Messages.Count(m =>
                        m.SenderUserId != null
                        && (x.Offer.Conversation.TeacherLastReadAt == null || m.SentAt > x.Offer.Conversation.TeacherLastReadAt))
                    : 0,
            })
            .ToListAsync(cancellationToken);

        return new PaginatedResult<TeacherOfferListItemDto>(items, totalCount, filters.PageNumber, filters.PageSize);
    }

    public async Task<List<int>> ExpirePendingOffersAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default)
    {
        var expiring = await _context.OpenSessionOffers
            .Where(o => o.Status == OpenSessionOfferStatus.Pending && o.ExpiresAt < cutoffUtc)
            .ToListAsync(cancellationToken);

        if (expiring.Count == 0)
            return new List<int>();

        var now = DateTime.UtcNow;
        foreach (var o in expiring)
        {
            o.Status = OpenSessionOfferStatus.Expired;
            o.ExpiredAt = now;
            o.UpdatedAt = now;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return expiring.Select(o => o.Id).ToList();
    }
}
