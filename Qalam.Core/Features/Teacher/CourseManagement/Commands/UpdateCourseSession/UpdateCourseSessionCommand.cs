using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Course;

namespace Qalam.Core.Features.Teacher.CourseManagement.Commands.UpdateCourseSession;

public class UpdateCourseSessionCommand : IRequest<Response<CourseSessionDto>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    public int CourseId { get; set; }
    public int SessionId { get; set; }
    public UpdateCourseSessionDto Data { get; set; } = null!;
}
