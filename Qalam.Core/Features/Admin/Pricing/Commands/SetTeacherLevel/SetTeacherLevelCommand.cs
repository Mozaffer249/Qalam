using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Pricing;

namespace Qalam.Core.Features.Admin.Pricing.Commands.SetTeacherLevel;

public class SetTeacherLevelCommand : IRequest<Response<string>>
{
    public int TeacherId { get; set; }
    public SetTeacherLevelDto Data { get; set; } = null!;
}
