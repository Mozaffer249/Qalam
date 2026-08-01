using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Features.Student.OpenSessionRequests.Services;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.OpenSessionRequests;
using Qalam.Infrastructure.context;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Student.OpenSessionRequests.Commands.AcceptSessionOffer;

public class AcceptSessionOfferCommandHandler
    : ResponseHandler, IRequestHandler<AcceptSessionOfferCommand, Response<AcceptSessionOfferResultDto>>
{
    private readonly ApplicationDBContext _db;
    private readonly IOpenSessionRequestAccessGuard _accessGuard;
    private readonly IOpenSessionOfferAcceptanceService _acceptanceService;

    public AcceptSessionOfferCommandHandler(
        IStringLocalizer<SharedResources> sharedLocalizer,
        ApplicationDBContext db,
        IOpenSessionRequestAccessGuard accessGuard,
        IOpenSessionOfferAcceptanceService acceptanceService) : base(sharedLocalizer)
    {
        _db = db;
        _accessGuard = accessGuard;
        _acceptanceService = acceptanceService;
    }

    public async Task<Response<AcceptSessionOfferResultDto>> Handle(
        AcceptSessionOfferCommand request,
        CancellationToken cancellationToken)
    {
        var offer = await _db.OpenSessionOffers
            .AsNoTracking()
            .Include(o => o.OpenSessionRequest)
            .FirstOrDefaultAsync(o => o.Id == request.OfferId, cancellationToken);

        if (offer?.OpenSessionRequest == null)
            return NotFound<AcceptSessionOfferResultDto>("العرض غير موجود");

        if (!await _accessGuard.CanActOnRequestAsync(request.UserId, offer.OpenSessionRequest, cancellationToken))
            return Unauthorized<AcceptSessionOfferResultDto>("Forbidden");

        try
        {
            var result = await _acceptanceService.AcceptAsync(request.OfferId, request.UserId, cancellationToken);
            return Success(entity: result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest<AcceptSessionOfferResultDto>(ex.Message);
        }
    }
}
