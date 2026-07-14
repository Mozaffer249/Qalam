using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Core.Features.Teacher.Content.Commands.LinkSessionContent;

public class LinkSessionContentCommand : IRequest<Response<TeacherSessionContentLinkDto>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    public int ScheduleId { get; set; }
    public int ContentItemId { get; set; }
}
