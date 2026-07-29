using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Teaching;

namespace Qalam.Core.Features.Teaching.Commands.UpdateTimeSlot;

public class UpdateTimeSlotCommand : IRequest<Response<TimeSlotDto>>
{
    public int Id { get; set; }
    public UpdateTimeSlotDto Data { get; set; } = null!;
}
