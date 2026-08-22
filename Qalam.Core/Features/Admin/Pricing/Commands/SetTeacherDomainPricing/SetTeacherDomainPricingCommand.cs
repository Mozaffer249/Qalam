using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Pricing;

namespace Qalam.Core.Features.Admin.Pricing.Commands.SetTeacherDomainPricing;

public class SetTeacherDomainPricingCommand : IRequest<Response<TeacherDomainPricingAdminDto>>
{
    public int TeacherId { get; set; }
    public SetTeacherDomainPricingDto Data { get; set; } = null!;
}
