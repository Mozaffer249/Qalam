using MediatR;
using Qalam.Core.Bases;

namespace Qalam.Core.Features.Content.Commands.DeleteContentUnit;

public class DeleteContentUnitCommand : IRequest<Response<bool>>
{
    public int Id { get; set; }
}
