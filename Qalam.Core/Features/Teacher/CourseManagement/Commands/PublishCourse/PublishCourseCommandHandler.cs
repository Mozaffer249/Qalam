using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Course;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Teacher.CourseManagement.Commands.PublishCourse;

public class PublishCourseCommandHandler : ResponseHandler,
    IRequestHandler<PublishCourseCommand, Response<CourseDetailDto>>
{
    private readonly ITeacherCourseService _teacherCourseService;

    public PublishCourseCommandHandler(
        ITeacherCourseService teacherCourseService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _teacherCourseService = teacherCourseService;
    }

    public async Task<Response<CourseDetailDto>> Handle(
        PublishCourseCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _teacherCourseService.PublishCourseAsync(
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
