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
using Qalam.Service.Implementations;

namespace Qalam.Core.Features.Student.OpenSessionRequests.Queries.GetStudentSessionOffers;

public class GetStudentSessionOffersQueryHandler
    : ResponseHandler, IRequestHandler<GetStudentSessionOffersQuery, Response<List<StudentOfferListItemDto>>>
{
    private readonly ApplicationDBContext _db;
    private readonly IOpenSessionRequestAccessGuard _accessGuard;
    private readonly IMediaUrlResolver _mediaUrlResolver;
    private readonly IFreeSessionPolicyService _freeSessionPolicy;

    public GetStudentSessionOffersQueryHandler(
        IStringLocalizer<SharedResources> sharedLocalizer,
        ApplicationDBContext db,
        IOpenSessionRequestAccessGuard accessGuard,
        IMediaUrlResolver mediaUrlResolver,
        IFreeSessionPolicyService freeSessionPolicy) : base(sharedLocalizer)
    {
        _db = db;
        _accessGuard = accessGuard;
        _mediaUrlResolver = mediaUrlResolver;
        _freeSessionPolicy = freeSessionPolicy;
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
        var isGroup = entity.GroupType is OfferGroupType.OpenGroup or OfferGroupType.InviteOnly
            || await _db.OpenSessionRequestInvitations.AsNoTracking()
                .AnyAsync(i => i.SessionRequestId == request.RequestId
                    && i.Status == OpenSessionRequestInvitationStatus.Accepted, cancellationToken);
        var freeTrialEligible = _freeSessionPolicy.IsEligiblePackage(isGroup, sessionCount)
            && await _freeSessionPolicy.IsStudentEligibleForFreeTrialAsync(entity.StudentId, cancellationToken);

        var firstSessionMinutes = await _db.OpenSessionRequestSessions
            .AsNoTracking()
            .Where(s => s.SessionRequestId == request.RequestId)
            .OrderBy(s => s.SequenceNumber)
            .Select(s => s.DurationMinutes)
            .FirstOrDefaultAsync(cancellationToken);
        var totalMinutes = await _db.OpenSessionRequestSessions
            .AsNoTracking()
            .Where(s => s.SessionRequestId == request.RequestId)
            .SumAsync(s => (int?)s.DurationMinutes, cancellationToken) ?? 0;

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

            var firstMinutes = FreeSessionPolicyService.ResolveFirstSessionMinutes(
                firstSessionMinutes > 0 ? firstSessionMinutes : null,
                null,
                totalMinutes > 0 ? totalMinutes : null,
                sessionCount);
            var hourly = FreeSessionPolicyService.DerivePricePerHour(offer.Price, totalMinutes);
            var (credit, due) = FreeSessionPolicyService.BuildTeaserAmounts(
                freeTrialEligible, offer.Price, hourly, firstMinutes);
            offer.FreeSessionCredit = credit;
            offer.AmountDue = due;
        }

        return Success(entity: offers);
    }
}
