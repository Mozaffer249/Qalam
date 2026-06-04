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
        var query = _context.OpenSessionOffers
            .AsNoTracking()
            .Where(o => o.TeacherId == teacherId);

        if (filters.Status.HasValue)
            query = query.Where(o => o.Status == filters.Status.Value);

        if (filters.DateFrom.HasValue)
        {
            var fromUtc = filters.DateFrom.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(o => o.CreatedAt >= fromUtc);
        }

        if (filters.DateTo.HasValue)
        {
            var toUtc = filters.DateTo.Value.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
            query = query.Where(o => o.CreatedAt <= toUtc);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((filters.PageNumber - 1) * filters.PageSize)
            .Take(filters.PageSize)
            .Select(o => new TeacherOfferListItemDto
            {
                Id = o.Id,
                SessionRequestId = o.SessionRequestId,
                SubjectId = o.OpenSessionRequest.SubjectId,
                SubjectNameEn = o.OpenSessionRequest.Subject != null ? o.OpenSessionRequest.Subject.NameEn : null,
                SubjectNameAr = o.OpenSessionRequest.Subject != null ? o.OpenSessionRequest.Subject.NameAr : null,
                StudentId = o.OpenSessionRequest.StudentId,
                StudentDisplayName =
                    o.OpenSessionRequest.Student != null && o.OpenSessionRequest.Student.User != null
                        ? (o.OpenSessionRequest.Student.User.FirstName ?? "") + " " + (o.OpenSessionRequest.Student.User.LastName ?? "")
                        : null,
                Price = o.Price,
                SessionsCount = o.OpenSessionRequest.TotalSessionsCount,
                Status = o.Status,
                Version = o.Version,
                CreatedAt = o.CreatedAt,
                ExpiresAt = o.ExpiresAt,
                UnreadMessagesCount = o.Conversation != null
                    ? o.Conversation.Messages.Count(m =>
                        m.SenderUserId != null
                        && (o.Conversation.TeacherLastReadAt == null || m.SentAt > o.Conversation.TeacherLastReadAt))
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
