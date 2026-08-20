using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Pricing;

namespace Qalam.Core.Features.Admin.Pricing.Commands.SetDefaultPricingMarket;

public class SetDefaultPricingMarketCommand : IRequest<Response<PricingMarketAdminDto>>
{
    public string Code { get; set; } = default!;
}
