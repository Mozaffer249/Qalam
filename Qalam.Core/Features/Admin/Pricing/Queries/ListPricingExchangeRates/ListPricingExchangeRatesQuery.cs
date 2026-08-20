using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Pricing;

namespace Qalam.Core.Features.Admin.Pricing.Queries.ListPricingExchangeRates;

public class ListPricingExchangeRatesQuery : IRequest<Response<List<PricingExchangeRateAdminDto>>>
{
}
