using MediatR;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Pricing;

namespace Qalam.Core.Features.Common.Pricing.Commands.SetMyPricingMarket;

public class SetMyPricingMarketCommand : IRequest<Response<MyPricingMarketDto>>, IAuthenticatedRequest
{
    public int UserId { get; set; }
    public SetMyPricingMarketDto Data { get; set; } = default!;
}
