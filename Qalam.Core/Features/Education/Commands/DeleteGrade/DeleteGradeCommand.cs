using MediatR;
using Qalam.Core.Bases;

namespace Qalam.Core.Features.Education.Commands.DeleteGrade;

public class DeleteGradeCommand : IRequest<Response<bool>>
{
    public int Id { get; set; }
}
