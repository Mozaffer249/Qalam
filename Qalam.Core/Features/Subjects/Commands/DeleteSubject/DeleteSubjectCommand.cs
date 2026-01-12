using MediatR;
using Qalam.Core.Bases;

namespace Qalam.Core.Features.Subjects.Commands.DeleteSubject;

public class DeleteSubjectCommand : IRequest<Response<bool>>
{
    public int Id { get; set; }
}
