using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Pricing;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Pricing.Commands.SetDefaultPricingMarket;

public class SetDefaultPricingMarketCommandHandler : ResponseHandler,
    IRequestHandler<SetDefaultPricingMarketCommand, Response<PricingMarketAdminDto>>
{
    private readonly IPricingAdminService _pricingAdminService;

    public SetDefaultPricingMarketCommandHandler(
        IPricingAdminService pricingAdminService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _pricingAdminService = pricingAdminService;
    }

    public async Task<Response<PricingMarketAdminDto>> Handle(
        SetDefaultPricingMarketCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _pricingAdminService.SetDefaultPricingMarketAsync(
                request.Code,
                cancellationToken);
            if (result == null)
                return NotFound<PricingMarketAdminDto>("Pricing market not found.");

            return Success("Default pricing market updated.", entity: result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest<PricingMarketAdminDto>(ex.Message);
        }
    }
}
