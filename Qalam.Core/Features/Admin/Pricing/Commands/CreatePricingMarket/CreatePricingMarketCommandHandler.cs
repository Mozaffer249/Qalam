using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Pricing;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Pricing.Commands.CreatePricingMarket;

public class CreatePricingMarketCommandHandler : ResponseHandler,
    IRequestHandler<CreatePricingMarketCommand, Response<PricingMarketAdminDto>>
{
    private readonly IPricingAdminService _pricingAdminService;

    public CreatePricingMarketCommandHandler(
        IPricingAdminService pricingAdminService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _pricingAdminService = pricingAdminService;
    }

    public async Task<Response<PricingMarketAdminDto>> Handle(
        CreatePricingMarketCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _pricingAdminService.CreatePricingMarketAsync(request.Data, cancellationToken);
            return Success("Pricing market created.", entity: result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest<PricingMarketAdminDto>(ex.Message);
        }
    }
}
