using MediatR;
using Qalam.Core.Bases;

namespace Qalam.Core.Features.Teaching.Commands.DeleteTimeSlot;

public class DeleteTimeSlotCommand : IRequest<Response<string>>
{
    public int Id { get; set; }
}
