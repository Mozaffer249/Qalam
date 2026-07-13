using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;

namespace Qalam.Core.Features.Teacher.Notifications.Commands.MarkAllTeacherNotificationsRead;

public class MarkAllTeacherNotificationsReadCommand : IRequest<Response<int>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }
}
