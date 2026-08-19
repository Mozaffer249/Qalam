using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Pricing;

namespace Qalam.Core.Features.Admin.Pricing.Commands.BackfillStarterTeacherLevels;

public class BackfillStarterTeacherLevelsCommand : IRequest<Response<BackfillStarterTeacherLevelsResultDto>>
{
}
