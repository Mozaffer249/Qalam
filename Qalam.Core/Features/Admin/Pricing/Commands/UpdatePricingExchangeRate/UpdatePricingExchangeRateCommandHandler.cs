using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Pricing;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Pricing.Commands.UpdatePricingExchangeRate;

public class UpdatePricingExchangeRateCommandHandler : ResponseHandler,
    IRequestHandler<UpdatePricingExchangeRateCommand, Response<PricingExchangeRateAdminDto>>
{
    private readonly IPricingAdminService _pricingAdminService;

    public UpdatePricingExchangeRateCommandHandler(
        IPricingAdminService pricingAdminService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _pricingAdminService = pricingAdminService;
    }

    public async Task<Response<PricingExchangeRateAdminDto>> Handle(
        UpdatePricingExchangeRateCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _pricingAdminService.UpdatePricingExchangeRateAsync(
                request.Code,
                request.Data,
                cancellationToken);
            if (result == null)
                return NotFound<PricingExchangeRateAdminDto>("Pricing market not found.");

            return Success("Exchange rate updated.", entity: result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest<PricingExchangeRateAdminDto>(ex.Message);
        }
    }
}
