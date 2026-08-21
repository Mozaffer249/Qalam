using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Features.Student.OpenSessionRequests.Services;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.OpenSessionRequests;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Infrastructure.context;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Student.OpenSessionRequests.Queries.GetStudentSessionOffers;

public class GetStudentSessionOffersQueryHandler
    : ResponseHandler, IRequestHandler<GetStudentSessionOffersQuery, Response<List<StudentOfferListItemDto>>>
{
    private readonly ApplicationDBContext _db;
    private readonly IOpenSessionRequestAccessGuard _accessGuard;
    private readonly IMediaUrlResolver _mediaUrlResolver;

    public GetStudentSessionOffersQueryHandler(
        IStringLocalizer<SharedResources> sharedLocalizer,
        ApplicationDBContext db,
        IOpenSessionRequestAccessGuard accessGuard,
        IMediaUrlResolver mediaUrlResolver) : base(sharedLocalizer)
    {
        _db = db;
        _accessGuard = accessGuard;
        _mediaUrlResolver = mediaUrlResolver;
    }

    public async Task<Response<List<StudentOfferListItemDto>>> Handle(
        GetStudentSessionOffersQuery request,
        CancellationToken cancellationToken)
    {
        var entity = await _db.OpenSessionRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.RequestId, cancellationToken);

        if (entity == null)
            return NotFound<List<StudentOfferListItemDto>>("الطلب غير موجود");

        if (!await _accessGuard.CanActOnRequestAsync(request.UserId, entity, cancellationToken))
            return Unauthorized<List<StudentOfferListItemDto>>("Forbidden");

        var sessionCount = await _db.OpenSessionRequestSessions
            .AsNoTracking()
            .CountAsync(s => s.SessionRequestId == request.RequestId, cancellationToken);
        var student = await _db.Students.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == entity.StudentId, cancellationToken);
        var isGroup = entity.GroupType is OfferGroupType.OpenGroup or OfferGroupType.InviteOnly
            || await _db.OpenSessionRequestInvitations.AsNoTracking()
                .AnyAsync(i => i.SessionRequestId == request.RequestId
                    && i.Status == OpenSessionRequestInvitationStatus.Accepted, cancellationToken);
        var freeTrialEligible = !isGroup
            && sessionCount == 1
            && student is { HasUsedFreeTrialSession: false };

        var offers = await _db.OpenSessionOffers
            .AsNoTracking()
            .Where(o => o.SessionRequestId == request.RequestId)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new StudentOfferListItemDto
            {
                Id = o.Id,
                SessionRequestId = o.SessionRequestId,
                TeacherId = o.TeacherId,
                TeacherName = o.Teacher != null && o.Teacher.User != null
                    ? ((o.Teacher.User.FirstName ?? "") + " " + (o.Teacher.User.LastName ?? "")).Trim()
                    : null,
                ProfilePictureUrl = o.Teacher != null && o.Teacher.User != null
                    ? o.Teacher.User.ProfilePictureUrl
                    : null,
                RatingAverage = o.Teacher != null ? o.Teacher.RatingAverage : 0m,
                ReviewsCount = o.Teacher != null
                    ? o.Teacher.TeacherReviews.Count(r => r.IsApproved)
                    : 0,
                IsVerified = o.Teacher != null && o.Teacher.Status == TeacherStatus.Active,
                Price = o.Price,
                IsFreeTrialEligible = freeTrialEligible,
                Status = o.Status,
                Version = o.Version,
                TeacherNotes = o.TeacherNotes,
                ExpiresAt = o.ExpiresAt,
                CreatedAt = o.CreatedAt,
                ConversationId = o.Conversation != null ? o.Conversation.Id : null
            })
            .ToListAsync(cancellationToken);

        foreach (var offer in offers)
        {
            if (!string.IsNullOrWhiteSpace(offer.ProfilePictureUrl))
                offer.ProfilePictureUrl = _mediaUrlResolver.ToPublicUrl(offer.ProfilePictureUrl);
        }

        return Success(entity: offers);
    }
}
