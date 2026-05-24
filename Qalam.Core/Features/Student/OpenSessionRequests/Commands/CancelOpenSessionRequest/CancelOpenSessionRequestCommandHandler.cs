using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Features.Student.OpenSessionRequests.Services;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Infrastructure.context;

namespace Qalam.Core.Features.Student.OpenSessionRequests.Commands.CancelOpenSessionRequest;

public class CancelOpenSessionRequestCommandHandler
    : ResponseHandler, IRequestHandler<CancelOpenSessionRequestCommand, Response<string>>
{
    private static readonly OpenSessionRequestStatus[] CancellableStatuses =
    {
        OpenSessionRequestStatus.Draft,
        OpenSessionRequestStatus.PendingInvitations,
        OpenSessionRequestStatus.Active,
        OpenSessionRequestStatus.ReceivingOffers,
    };

    private readonly ApplicationDBContext _db;
    private readonly IOpenSessionRequestAccessGuard _accessGuard;

    public CancelOpenSessionRequestCommandHandler(
        IStringLocalizer<SharedResources> sharedLocalizer,
        ApplicationDBContext db,
        IOpenSessionRequestAccessGuard accessGuard) : base(sharedLocalizer)
    {
        _db = db;
        _accessGuard = accessGuard;
    }

    public async Task<Response<string>> Handle(CancelOpenSessionRequestCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.OpenSessionRequests
            .Include(r => r.Offers)
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

        if (entity is null)
            return NotFound<string>("الطلب غير موجود");

        if (!await _accessGuard.CanActOnRequestAsync(request.UserId, entity, cancellationToken))
            return Unauthorized<string>("غير مصرح لك بإلغاء هذا الطلب");

        if (!CancellableStatuses.Contains(entity.Status))
            return BadRequest<string>($"لا يمكن إلغاء الطلب في الحالة الحالية ({entity.Status})");

        entity.Status = OpenSessionRequestStatus.Cancelled;
        entity.CancelledAt = DateTime.UtcNow;
        entity.CancellationReason = request.Data.Reason;

        // Withdraw all pending offers so teachers stop seeing this request as actionable.
        foreach (var offer in entity.Offers.Where(o => o.Status == OpenSessionOfferStatus.Pending))
        {
            offer.Status = OpenSessionOfferStatus.Withdrawn;
            offer.WithdrawnAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);

        // TODO (P3/P5): publish RabbitMQ notifications to affected teachers + invited students.

        return Success(entity: "تم إلغاء الطلب");
    }
}
