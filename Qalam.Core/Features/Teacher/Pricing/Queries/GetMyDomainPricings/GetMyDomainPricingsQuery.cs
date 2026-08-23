using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Pricing;

namespace Qalam.Core.Features.Teacher.Pricing.Queries.GetMyDomainPricings;

public class GetMyDomainPricingsQuery : IRequest<Response<List<TeacherMyDomainPricingDto>>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }
}
