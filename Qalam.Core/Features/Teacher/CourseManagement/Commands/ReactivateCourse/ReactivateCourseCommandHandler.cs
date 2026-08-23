using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Course;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Teacher.CourseManagement.Commands.ReactivateCourse;

public class ReactivateCourseCommandHandler : ResponseHandler,
    IRequestHandler<ReactivateCourseCommand, Response<CourseDetailDto>>
{
    private readonly ITeacherCourseService _teacherCourseService;

    public ReactivateCourseCommandHandler(
        ITeacherCourseService teacherCourseService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _teacherCourseService = teacherCourseService;
    }

    public async Task<Response<CourseDetailDto>> Handle(
        ReactivateCourseCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _teacherCourseService.ReactivateCourseAsync(
                request.UserId, request.Id, cancellationToken);
            if (result == null)
                return NotFound<CourseDetailDto>("Course not found.");
            return Success(entity: result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest<CourseDetailDto>(ex.Message);
        }
    }
}
