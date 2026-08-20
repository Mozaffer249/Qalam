using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Pricing;

namespace Qalam.Core.Features.Admin.Pricing.Queries.ListPricingMarkets;

public class ListAdminPricingMarketsQuery : IRequest<Response<List<PricingMarketDto>>>
{
}
