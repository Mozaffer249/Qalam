using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Teaching;

namespace Qalam.Core.Features.Teaching.Commands.SetTimeSlotActive;

public class SetTimeSlotActiveCommand : IRequest<Response<TimeSlotDto>>
{
    public int Id { get; set; }
    public SetTimeSlotActiveDto Data { get; set; } = null!;
}
