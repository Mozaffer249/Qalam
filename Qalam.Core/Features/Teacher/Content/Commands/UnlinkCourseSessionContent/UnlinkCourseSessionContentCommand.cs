using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;

namespace Qalam.Core.Features.Teacher.Content.Commands.UnlinkCourseSessionContent;

public class UnlinkCourseSessionContentCommand : IRequest<Response<string>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    public int CourseId { get; set; }
    public int SessionId { get; set; }
    public int LinkId { get; set; }
}
