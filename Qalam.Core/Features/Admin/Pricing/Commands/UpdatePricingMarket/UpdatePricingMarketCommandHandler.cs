using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Pricing;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Pricing.Commands.UpdatePricingMarket;

public class UpdatePricingMarketCommandHandler : ResponseHandler,
    IRequestHandler<UpdatePricingMarketCommand, Response<PricingMarketAdminDto>>
{
    private readonly IPricingAdminService _pricingAdminService;

    public UpdatePricingMarketCommandHandler(
        IPricingAdminService pricingAdminService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _pricingAdminService = pricingAdminService;
    }

    public async Task<Response<PricingMarketAdminDto>> Handle(
        UpdatePricingMarketCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _pricingAdminService.UpdatePricingMarketAsync(
                request.Code,
                request.Data,
                cancellationToken);
            if (result == null)
                return NotFound<PricingMarketAdminDto>("Pricing market not found.");

            return Success("Pricing market updated.", entity: result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest<PricingMarketAdminDto>(ex.Message);
        }
    }
}
