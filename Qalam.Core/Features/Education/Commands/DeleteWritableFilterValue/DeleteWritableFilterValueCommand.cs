using MediatR;
using Qalam.Core.Bases;

namespace Qalam.Core.Features.Education.Commands.DeleteWritableFilterValue;

public class DeleteWritableFilterValueCommand : IRequest<Response<bool>>
{
    public int Id { get; set; }
}
