using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Pricing;

namespace Qalam.Core.Features.Admin.Pricing.Commands.CreatePricingMarket;

public class CreatePricingMarketCommand : IRequest<Response<PricingMarketAdminDto>>
{
    public CreatePricingMarketDto Data { get; set; } = null!;
}
