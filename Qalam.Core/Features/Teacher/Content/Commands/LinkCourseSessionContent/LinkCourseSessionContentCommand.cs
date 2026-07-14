using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Core.Features.Teacher.Content.Commands.LinkCourseSessionContent;

public class LinkCourseSessionContentCommand : IRequest<Response<TeacherSessionContentLinkDto>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    public int CourseId { get; set; }
    public int SessionId { get; set; }
    public int ContentItemId { get; set; }
}
