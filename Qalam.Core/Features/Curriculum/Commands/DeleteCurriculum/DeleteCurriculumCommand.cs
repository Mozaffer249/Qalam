using MediatR;
using Qalam.Core.Bases;

namespace Qalam.Core.Features.Curriculum.Commands.DeleteCurriculum;

public class DeleteCurriculumCommand : IRequest<Response<bool>>
{
    public int Id { get; set; }
}
