using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Pricing;

namespace Qalam.Core.Features.Admin.Pricing.Commands.UpdatePricingExchangeRate;

public class UpdatePricingExchangeRateCommand : IRequest<Response<PricingExchangeRateAdminDto>>
{
    public string Code { get; set; } = default!;
    public UpdatePricingExchangeRateDto Data { get; set; } = null!;
}
