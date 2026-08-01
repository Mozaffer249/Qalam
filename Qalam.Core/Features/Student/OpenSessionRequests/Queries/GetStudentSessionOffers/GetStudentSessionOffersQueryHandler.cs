using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Features.Student.OpenSessionRequests.Services;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.OpenSessionRequests;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Infrastructure.context;

namespace Qalam.Core.Features.Student.OpenSessionRequests.Queries.GetStudentSessionOffers;

public class GetStudentSessionOffersQueryHandler
    : ResponseHandler, IRequestHandler<GetStudentSessionOffersQuery, Response<List<StudentOfferListItemDto>>>
{
    private readonly ApplicationDBContext _db;
    private readonly IOpenSessionRequestAccessGuard _accessGuard;

    public GetStudentSessionOffersQueryHandler(
        IStringLocalizer<SharedResources> sharedLocalizer,
        ApplicationDBContext db,
        IOpenSessionRequestAccessGuard accessGuard) : base(sharedLocalizer)
    {
        _db = db;
        _accessGuard = accessGuard;
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

        var offers = await _db.OpenSessionOffers
            .AsNoTracking()
            .Where(o => o.SessionRequestId == request.RequestId
                        && o.Status != OpenSessionOfferStatus.Withdrawn)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new StudentOfferListItemDto
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
                ConversationId = o.Conversation != null ? o.Conversation.Id : null
            })
            .ToListAsync(cancellationToken);

        return Success(entity: offers);
    }
}
