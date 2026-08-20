using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Pricing;

namespace Qalam.Core.Features.Common.Pricing.Queries.ListPricingMarkets;

public class ListPricingMarketsQuery : IRequest<Response<List<PricingMarketDto>>>
{
}
