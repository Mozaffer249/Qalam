using MediatR;
using Qalam.Core.Bases;

namespace Qalam.Core.Features.Education.Commands.DeleteEducationLevel;

public class DeleteEducationLevelCommand : IRequest<Response<bool>>
{
    public int Id { get; set; }
}
