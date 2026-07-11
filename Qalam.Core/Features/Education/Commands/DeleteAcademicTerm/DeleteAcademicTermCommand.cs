using MediatR;
using Qalam.Core.Bases;

namespace Qalam.Core.Features.Education.Commands.DeleteAcademicTerm;

public class DeleteAcademicTermCommand : IRequest<Response<bool>>
{
    public int Id { get; set; }
}
