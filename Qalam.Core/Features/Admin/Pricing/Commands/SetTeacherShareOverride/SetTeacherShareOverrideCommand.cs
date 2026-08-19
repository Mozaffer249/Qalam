using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Pricing;

namespace Qalam.Core.Features.Admin.Pricing.Commands.SetTeacherShareOverride;

public class SetTeacherShareOverrideCommand : IRequest<Response<string>>
{
    public int TeacherId { get; set; }
    public SetTeacherShareOverrideDto Data { get; set; } = null!;
}
