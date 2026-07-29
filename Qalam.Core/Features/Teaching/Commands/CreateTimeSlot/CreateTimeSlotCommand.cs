using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Teaching;

namespace Qalam.Core.Features.Teaching.Commands.CreateTimeSlot;

public class CreateTimeSlotCommand : IRequest<Response<TimeSlotDto>>
{
    public CreateTimeSlotDto Data { get; set; } = null!;
}
