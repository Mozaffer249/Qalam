using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Pricing;

namespace Qalam.Core.Features.Admin.Pricing.Commands.CreateTeacherLevelTier;

public class CreateTeacherLevelTierCommand : IRequest<Response<TeacherLevelTierAdminDto>>
{
    public CreateTeacherLevelTierDto Data { get; set; } = null!;
}
