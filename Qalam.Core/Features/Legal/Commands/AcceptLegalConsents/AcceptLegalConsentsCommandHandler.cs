using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Helpers;
using Qalam.Core.Resources.Shared;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Legal.Commands.AcceptLegalConsents;

public class AcceptLegalConsentsCommandHandler : ResponseHandler,
    IRequestHandler<AcceptLegalConsentsCommand, Response<string>>
{
    private readonly ILegalConsentService _consentService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AcceptLegalConsentsCommandHandler(
        ILegalConsentService consentService,
        IHttpContextAccessor httpContextAccessor,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _consentService = consentService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Response<string>> Handle(
        AcceptLegalConsentsCommand request,
        CancellationToken cancellationToken)
    {
        var ctx = _httpContextAccessor.HttpContext;
        await _consentService.AcceptAsync(
            request.UserId,
            request.Data.DocumentCodes,
            request.Data.Source ?? "accept-terms",
            ClientIpHelper.GetClientIpAddress(ctx),
            ClientIpHelper.GetUserAgent(ctx),
            cancellationToken);

        return Success<string>("Consents recorded");
    }
}
