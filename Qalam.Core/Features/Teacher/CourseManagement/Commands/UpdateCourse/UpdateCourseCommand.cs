using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Course;

namespace Qalam.Core.Features.Teacher.CourseManagement.Commands.UpdateCourse;

public class UpdateCourseCommand : IRequest<Response<CourseDetailDto>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }
    public int Id { get; set; }
    public UpdateCourseDto Data { get; set; } = null!;
}
