using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Core.Features.Teacher.Sessions.Commands.RescheduleMySession;

public class RescheduleMySessionCommand : IRequest<Response<RescheduleMySessionResultDto>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    public int Id { get; set; }
    public DateOnly NewDate { get; set; }
    public int TeacherAvailabilityId { get; set; }
}
