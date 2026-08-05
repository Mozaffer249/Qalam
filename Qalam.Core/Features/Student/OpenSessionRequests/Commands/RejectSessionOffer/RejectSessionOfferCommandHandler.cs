using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Features.Student.OpenSessionRequests.Services;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Infrastructure.context;

namespace Qalam.Core.Features.Student.OpenSessionRequests.Commands.RejectSessionOffer;

public class RejectSessionOfferCommandHandler
    : ResponseHandler, IRequestHandler<RejectSessionOfferCommand, Response<string>>
{
    private readonly ApplicationDBContext _db;
    private readonly IOpenSessionRequestAccessGuard _accessGuard;

    public RejectSessionOfferCommandHandler(
        IStringLocalizer<SharedResources> sharedLocalizer,
        ApplicationDBContext db,
        IOpenSessionRequestAccessGuard accessGuard) : base(sharedLocalizer)
    {
        _db = db;
        _accessGuard = accessGuard;
    }

    public async Task<Response<string>> Handle(
        RejectSessionOfferCommand request,
        CancellationToken cancellationToken)
    {
        var offer = await _db.OpenSessionOffers
            .Include(o => o.OpenSessionRequest)
            .FirstOrDefaultAsync(o => o.Id == request.OfferId, cancellationToken);

        if (offer?.OpenSessionRequest == null)
            return NotFound<string>("العرض غير موجود");

        if (!await _accessGuard.CanActOnRequestAsync(request.UserId, offer.OpenSessionRequest, cancellationToken))
            return Unauthorized<string>("Forbidden");

        if (offer.Status != OpenSessionOfferStatus.Pending)
            return BadRequest<string>("يمكن رفض العروض المعلقة فقط");

        if (offer.OpenSessionRequest.Status is not (
                OpenSessionRequestStatus.Active or OpenSessionRequestStatus.ReceivingOffers))
        {
            return BadRequest<string>(
                $"لا يمكن رفض عرض على طلب في الحالة {offer.OpenSessionRequest.Status}");
        }

        var now = DateTime.UtcNow;
        if (offer.ExpiresAt < now)
            return BadRequest<string>("انتهت صلاحية العرض");

        offer.Status = OpenSessionOfferStatus.Rejected;
        offer.RejectedAt = now;
        offer.RejectionReason = string.IsNullOrWhiteSpace(request.Data?.Reason)
            ? null
            : request.Data!.Reason.Trim();
        offer.UpdatedAt = now;

        // Return target to Viewed so the request leaves the "Offered" inbox tab and re-offer is discoverable.
        var target = await _db.OpenSessionRequestTargets
            .FirstOrDefaultAsync(
                t => t.SessionRequestId == offer.SessionRequestId && t.TeacherId == offer.TeacherId,
                cancellationToken);
        if (target is { Status: OpenSessionRequestTargetStatus.OfferSubmitted })
        {
            target.Status = OpenSessionRequestTargetStatus.Viewed;
            target.UpdatedAt = now;
        }

        // Request stays Active / ReceivingOffers while other pending offers remain.
        var remainingPending = await _db.OpenSessionOffers
            .CountAsync(o => o.SessionRequestId == offer.SessionRequestId
                             && o.Id != offer.Id
                             && o.Status == OpenSessionOfferStatus.Pending, cancellationToken);

        if (remainingPending == 0
            && offer.OpenSessionRequest.Status == OpenSessionRequestStatus.ReceivingOffers)
        {
            offer.OpenSessionRequest.Status = OpenSessionRequestStatus.Active;
            offer.OpenSessionRequest.UpdatedAt = now;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Success(entity: "تم رفض العرض");
    }
}
