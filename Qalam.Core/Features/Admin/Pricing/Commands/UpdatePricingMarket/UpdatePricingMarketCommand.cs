using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Pricing;

namespace Qalam.Core.Features.Admin.Pricing.Commands.UpdatePricingMarket;

public class UpdatePricingMarketCommand : IRequest<Response<PricingMarketAdminDto>>
{
    public string Code { get; set; } = default!;
    public UpdatePricingMarketDto Data { get; set; } = null!;
}
