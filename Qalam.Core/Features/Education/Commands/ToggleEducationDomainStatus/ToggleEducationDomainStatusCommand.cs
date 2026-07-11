using MediatR;
using Qalam.Core.Bases;

namespace Qalam.Core.Features.Education.Commands.ToggleEducationDomainStatus;

public class ToggleEducationDomainStatusCommand : IRequest<Response<bool>>
{
    public int Id { get; set; }
}
