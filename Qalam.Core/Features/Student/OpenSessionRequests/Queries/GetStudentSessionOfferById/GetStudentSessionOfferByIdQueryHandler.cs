using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Features.Student.OpenSessionRequests.Services;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.OpenSessionRequests;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Infrastructure.context;

namespace Qalam.Core.Features.Student.OpenSessionRequests.Queries.GetStudentSessionOfferById;

public class GetStudentSessionOfferByIdQueryHandler
    : ResponseHandler, IRequestHandler<GetStudentSessionOfferByIdQuery, Response<StudentOfferDetailDto>>
{
    private readonly ApplicationDBContext _db;
    private readonly IOpenSessionRequestAccessGuard _accessGuard;

    public GetStudentSessionOfferByIdQueryHandler(
        IStringLocalizer<SharedResources> sharedLocalizer,
        ApplicationDBContext db,
        IOpenSessionRequestAccessGuard accessGuard) : base(sharedLocalizer)
    {
        _db = db;
        _accessGuard = accessGuard;
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

        var offer = await _db.OpenSessionOffers
            .AsNoTracking()
            .Where(o => o.Id == request.OfferId
                        && o.SessionRequestId == request.RequestId
                        && o.Status != OpenSessionOfferStatus.Withdrawn)
            .Select(o => new StudentOfferDetailDto
            {
                Id = o.Id,
                SessionRequestId = o.SessionRequestId,
                TeacherId = o.TeacherId,
                TeacherName = o.Teacher != null && o.Teacher.User != null
                    ? ((o.Teacher.User.FirstName ?? "") + " " + (o.Teacher.User.LastName ?? "")).Trim()
                    : null,
                Price = o.Price,
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
                TotalSessionsCount = o.OpenSessionRequest.TotalSessionsCount
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (offer == null)
            return NotFound<StudentOfferDetailDto>("العرض غير موجود");

        return Success(entity: offer);
    }
}
