using MediatR;
using Qalam.Core.Bases;

namespace Qalam.Core.Features.Curriculum.Commands.ToggleCurriculumStatus;

public class ToggleCurriculumStatusCommand : IRequest<Response<bool>>
{
    public int Id { get; set; }
}
