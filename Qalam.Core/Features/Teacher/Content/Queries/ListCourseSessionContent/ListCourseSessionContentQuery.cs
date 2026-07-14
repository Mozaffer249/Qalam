using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Core.Features.Teacher.Content.Queries.ListCourseSessionContent;

public class ListCourseSessionContentQuery : IRequest<Response<List<TeacherSessionContentLinkDto>>>, IAuthenticatedRequest
{
    public int CourseId { get; set; }
    public int SessionId { get; set; }

    [BindNever]
    public int UserId { get; set; }
}
