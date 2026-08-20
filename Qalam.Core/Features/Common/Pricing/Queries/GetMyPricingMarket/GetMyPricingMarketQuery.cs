using MediatR;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Pricing;

namespace Qalam.Core.Features.Common.Pricing.Queries.GetMyPricingMarket;

public class GetMyPricingMarketQuery : IRequest<Response<MyPricingMarketDto>>, IAuthenticatedRequest
{
    public int UserId { get; set; }
}
