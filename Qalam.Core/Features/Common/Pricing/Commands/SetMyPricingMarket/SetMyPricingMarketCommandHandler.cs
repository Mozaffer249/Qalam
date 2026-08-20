using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Pricing;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Common.Pricing.Commands.SetMyPricingMarket;

public class SetMyPricingMarketCommandHandler : ResponseHandler,
    IRequestHandler<SetMyPricingMarketCommand, Response<MyPricingMarketDto>>
{
    private readonly IPricingMarketService _pricingMarketService;

    public SetMyPricingMarketCommandHandler(
        IPricingMarketService pricingMarketService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _pricingMarketService = pricingMarketService;
    }

    public async Task<Response<MyPricingMarketDto>> Handle(
        SetMyPricingMarketCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var dto = await _pricingMarketService.SetMyMarketAsync(
                request.UserId,
                request.Data,
                cancellationToken);
            return Success(entity: dto);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest<MyPricingMarketDto>(ex.Message);
        }
    }
}
