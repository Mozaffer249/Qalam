using MediatR;
using Qalam.Core.Bases;

namespace Qalam.Core.Features.Education.Commands.DeleteEducationDomain;

public class DeleteEducationDomainCommand : IRequest<Response<bool>>
{
    public int Id { get; set; }
}
