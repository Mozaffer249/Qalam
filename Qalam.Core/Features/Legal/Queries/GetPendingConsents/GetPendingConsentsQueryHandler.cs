using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Legal;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Legal.Queries.GetPendingConsents;

public class GetPendingConsentsQueryHandler : ResponseHandler,
    IRequestHandler<GetPendingConsentsQuery, Response<List<PendingConsentDocumentDto>>>
{
    private readonly ILegalConsentService _consentService;

    public GetPendingConsentsQueryHandler(
        ILegalConsentService consentService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _consentService = consentService;
    }

    public async Task<Response<List<PendingConsentDocumentDto>>> Handle(
        GetPendingConsentsQuery request,
        CancellationToken cancellationToken)
    {
        var pending = await _consentService.GetPendingAsync(request.UserId, cancellationToken);
        return Success(entity: pending);
    }
}
