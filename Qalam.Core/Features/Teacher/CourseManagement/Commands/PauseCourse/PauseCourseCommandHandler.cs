using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Course;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Teacher.CourseManagement.Commands.PauseCourse;

public class PauseCourseCommandHandler : ResponseHandler,
    IRequestHandler<PauseCourseCommand, Response<CourseDetailDto>>
{
    private readonly ITeacherCourseService _teacherCourseService;

    public PauseCourseCommandHandler(
        ITeacherCourseService teacherCourseService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _teacherCourseService = teacherCourseService;
    }

    public async Task<Response<CourseDetailDto>> Handle(
        PauseCourseCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _teacherCourseService.PauseCourseAsync(
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
