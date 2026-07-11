using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Education.Commands.ToggleEducationDomainStatus;

public class ToggleEducationDomainStatusCommandHandler : ResponseHandler,
    IRequestHandler<ToggleEducationDomainStatusCommand, Response<bool>>
{
    private readonly IEducationDomainService _domainService;

    public ToggleEducationDomainStatusCommandHandler(
        IStringLocalizer<SharedResources> localizer,
        IEducationDomainService domainService) : base(localizer)
    {
        _domainService = domainService;
    }

    public async Task<Response<bool>> Handle(
        ToggleEducationDomainStatusCommand request,
        CancellationToken cancellationToken)
    {
        var result = await _domainService.ToggleDomainStatusAsync(request.Id);

        if (!result)
            return NotFound<bool>("Education domain not found");

        return Success(entity: true, Message: "Domain status toggled successfully");
    }
}
