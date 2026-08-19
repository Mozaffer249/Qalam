using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Pricing;

namespace Qalam.Core.Features.Admin.Pricing.Commands.SetTeacherLevelTier;

public class SetTeacherLevelTierCommand : IRequest<Response<TeacherLevelTierAdminDto>>
{
    public int Id { get; set; }
    public SetTeacherLevelTierDto Data { get; set; } = null!;
}
