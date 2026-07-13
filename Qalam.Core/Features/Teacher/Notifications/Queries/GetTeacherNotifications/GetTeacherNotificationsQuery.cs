using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Core.Features.Teacher.Notifications.Queries.GetTeacherNotifications;

public class GetTeacherNotificationsQuery : IRequest<Response<TeacherNotificationsPageDto>>, IAuthenticatedRequest
{
    public bool UnreadOnly { get; set; }

    [BindNever]
    public int UserId { get; set; }
}
