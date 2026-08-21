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

namespace Qalam.Core.Features.Student.OpenSessionRequests.Queries.GetStudentSessionOfferById;

public class GetStudentSessionOfferByIdQueryHandler
    : ResponseHandler, IRequestHandler<GetStudentSessionOfferByIdQuery, Response<StudentOfferDetailDto>>
{
    private readonly ApplicationDBContext _db;
    private readonly IOpenSessionRequestAccessGuard _accessGuard;
    private readonly IMediaUrlResolver _mediaUrlResolver;

    public GetStudentSessionOfferByIdQueryHandler(
        IStringLocalizer<SharedResources> sharedLocalizer,
        ApplicationDBContext db,
        IOpenSessionRequestAccessGuard accessGuard,
        IMediaUrlResolver mediaUrlResolver) : base(sharedLocalizer)
    {
        _db = db;
        _accessGuard = accessGuard;
        _mediaUrlResolver = mediaUrlResolver;
    }

    public async Task<Response<StudentOfferDetailDto>> Handle(
        GetStudentSessionOfferByIdQuery request,
        CancellationToken cancellationToken)
    {
        var entity = await _db.OpenSessionRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.RequestId, cancellationToken);

        if (entity == null)
            return NotFound<StudentOfferDetailDto>("الطلب غير موجود");

        if (!await _accessGuard.CanActOnRequestAsync(request.UserId, entity, cancellationToken))
            return Unauthorized<StudentOfferDetailDto>("Forbidden");

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

        var offer = await _db.OpenSessionOffers
            .AsNoTracking()
            .Where(o => o.Id == request.OfferId
                        && o.SessionRequestId == request.RequestId)
            .Select(o => new StudentOfferDetailDto
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
                ConversationId = o.Conversation != null ? o.Conversation.Id : null,
                AcceptedAt = o.AcceptedAt,
                RejectedAt = o.RejectedAt,
                WithdrawnAt = o.WithdrawnAt,
                ExpiredAt = o.ExpiredAt,
                RejectionReason = o.RejectionReason,
                SubjectId = o.OpenSessionRequest.SubjectId,
                SubjectName = o.OpenSessionRequest.Subject != null
                    ? o.OpenSessionRequest.Subject.NameAr ?? o.OpenSessionRequest.Subject.NameEn
                    : null,
                TotalSessionsCount = o.OpenSessionRequest.TotalSessionsCount,
                Bio = o.Teacher != null ? o.Teacher.Bio : null,
                SessionDurationMinutes = o.OpenSessionRequest.Sessions
                    .OrderBy(s => s.SequenceNumber)
                    .Select(s => s.DurationMinutes)
                    .FirstOrDefault(),
                RecentReviews = o.Teacher != null
                    ? o.Teacher.TeacherReviews
                        .Where(r => r.IsApproved)
                        .OrderByDescending(r => r.CreatedAt)
                        .Take(2)
                        .Select(r => new StudentOfferReviewPreviewDto
                        {
                            Id = r.Id,
                            Rating = r.Rating,
                            Feedback = r.Feedback,
                            StudentDisplayName = r.Student != null && r.Student.User != null
                                ? (r.Student.User.FirstName ?? "Student")
                                : "Student",
                            CreatedAt = r.CreatedAt
                        })
                        .ToList()
                    : new List<StudentOfferReviewPreviewDto>()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (offer == null)
            return NotFound<StudentOfferDetailDto>("العرض غير موجود");

        if (!string.IsNullOrWhiteSpace(offer.ProfilePictureUrl))
            offer.ProfilePictureUrl = _mediaUrlResolver.ToPublicUrl(offer.ProfilePictureUrl);

        var teacherSubjectNames = await _db.TeacherSubjects
            .AsNoTracking()
            .Where(ts => ts.TeacherId == offer.TeacherId && ts.IsActive && ts.Subject != null)
            .OrderBy(ts => ts.Id)
            .Select(ts => ts.Subject!.NameAr ?? ts.Subject.NameEn)
            .Take(5)
            .ToListAsync(cancellationToken);

        var tags = new List<string>();
        if (!string.IsNullOrWhiteSpace(offer.SubjectName))
            tags.Add(offer.SubjectName.Trim());
        foreach (var name in teacherSubjectNames)
        {
            if (string.IsNullOrWhiteSpace(name))
                continue;
            var trimmed = name.Trim();
            if (tags.Exists(t => string.Equals(t, trimmed, StringComparison.OrdinalIgnoreCase)))
                continue;
            tags.Add(trimmed);
            if (tags.Count >= 3)
                break;
        }

        offer.SubjectTags = tags;

        return Success(entity: offer);
    }
}
