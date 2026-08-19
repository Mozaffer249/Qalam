using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Pricing;

namespace Qalam.Core.Features.Admin.Pricing.Commands.SetDomainSessionPrice;

public class SetDomainSessionPriceCommand : IRequest<Response<DomainSessionPriceAdminDto>>
{
    public SetDomainSessionPriceDto Data { get; set; } = null!;
}
