using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Pricing;

namespace Qalam.Core.Features.Admin.Pricing.Queries.ListTeacherDomainPricings;

public class ListTeacherDomainPricingsQuery : IRequest<Response<List<TeacherDomainPricingAdminDto>>>
{
    public int? DomainId { get; set; }
    public int? TeacherId { get; set; }
}
