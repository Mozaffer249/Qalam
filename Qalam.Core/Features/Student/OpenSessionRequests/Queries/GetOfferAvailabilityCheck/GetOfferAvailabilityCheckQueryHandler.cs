using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Features.Student.OpenSessionRequests.Services;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.OpenSessionRequests;
using Qalam.Infrastructure.context;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Student.OpenSessionRequests.Queries.GetOfferAvailabilityCheck;

public class GetOfferAvailabilityCheckQueryHandler
    : ResponseHandler, IRequestHandler<GetOfferAvailabilityCheckQuery, Response<List<SessionAvailabilityMatchDto>>>
{
    private readonly ApplicationDBContext _db;
    private readonly IOpenSessionRequestAccessGuard _accessGuard;
    private readonly ISessionAvailabilityMatchService _matchService;

    public GetOfferAvailabilityCheckQueryHandler(
        IStringLocalizer<SharedResources> sharedLocalizer,
        ApplicationDBContext db,
        IOpenSessionRequestAccessGuard accessGuard,
        ISessionAvailabilityMatchService matchService) : base(sharedLocalizer)
    {
        _db = db;
        _accessGuard = accessGuard;
        _matchService = matchService;
    }

    public async Task<Response<List<SessionAvailabilityMatchDto>>> Handle(
        GetOfferAvailabilityCheckQuery request,
        CancellationToken cancellationToken)
    {
        var offer = await _db.OpenSessionOffers
            .AsNoTracking()
            .Include(o => o.OpenSessionRequest)
            .FirstOrDefaultAsync(o => o.Id == request.OfferId, cancellationToken);

        if (offer?.OpenSessionRequest == null)
            return NotFound<List<SessionAvailabilityMatchDto>>("العرض غير موجود");

        if (!await _accessGuard.CanActOnRequestAsync(request.UserId, offer.OpenSessionRequest, cancellationToken))
            return Unauthorized<List<SessionAvailabilityMatchDto>>("Forbidden");

        var result = await _matchService.MatchAsync(
            offer.TeacherId, offer.SessionRequestId, cancellationToken);

        return Success(entity: result);
    }
}
