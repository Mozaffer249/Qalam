using MediatR;
using Qalam.Core.Bases;

namespace Qalam.Core.Features.Content.Commands.DeleteLesson;

public class DeleteLessonCommand : IRequest<Response<bool>>
{
    public int Id { get; set; }
}
