using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Course;

namespace Qalam.Core.Features.Teacher.CourseManagement.Commands.PauseCourse;

public class PauseCourseCommand : IRequest<Response<CourseDetailDto>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }
    public int Id { get; set; }
}
