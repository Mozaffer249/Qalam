using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Pricing;

namespace Qalam.Core.Features.Admin.Pricing.Queries.ListDomainSessionPrices;

public class ListDomainSessionPricesQuery : IRequest<Response<List<DomainSessionPriceAdminDto>>>
{
    public int? DomainId { get; set; }
    public string? SessionTypeCode { get; set; }
    public bool IncludeHistory { get; set; }
}
