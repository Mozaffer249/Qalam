using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Pricing;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Pricing.Commands.SetDomainSessionPrice;

public class SetDomainSessionPriceCommandHandler : ResponseHandler,
    IRequestHandler<SetDomainSessionPriceCommand, Response<DomainSessionPriceAdminDto>>
{
    private readonly IPricingAdminService _pricingAdminService;

    public SetDomainSessionPriceCommandHandler(
        IPricingAdminService pricingAdminService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _pricingAdminService = pricingAdminService;
    }

    public async Task<Response<DomainSessionPriceAdminDto>> Handle(
        SetDomainSessionPriceCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _pricingAdminService.SetDomainSessionPriceAsync(request.Data, cancellationToken);
            if (result == null)
                return NotFound<DomainSessionPriceAdminDto>("Domain not found.");

            return Success("Domain session price updated.", entity: result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest<DomainSessionPriceAdminDto>(ex.Message);
        }
    }
}
