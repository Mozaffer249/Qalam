using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;

namespace Qalam.Core.Features.Teacher.Notifications.Commands.MarkTeacherNotificationRead;

public class MarkTeacherNotificationReadCommand : IRequest<Response<bool>>, IAuthenticatedRequest
{
    public int Id { get; set; }

    [BindNever]
    public int UserId { get; set; }
}
