using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Course;

namespace Qalam.Core.Features.Teacher.CourseManagement.Commands.UpdateCourseSessionUnits;

public class UpdateCourseSessionUnitsCommand : IRequest<Response<List<CourseSessionUnitDto>>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    public int CourseId { get; set; }
    public int SessionId { get; set; }
    public UpdateCourseSessionUnitsDto Data { get; set; } = null!;
}
