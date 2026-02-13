using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Course;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Teacher.CourseManagement.Commands.UpdateCourse;

public class UpdateCourseCommandHandler : ResponseHandler,
    IRequestHandler<UpdateCourseCommand, Response<CourseDetailDto>>
{
    private readonly ITeacherCourseService _teacherCourseService;

    public UpdateCourseCommandHandler(
        ITeacherCourseService teacherCourseService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _teacherCourseService = teacherCourseService;
    }

    public async Task<Response<CourseDetailDto>> Handle(
        UpdateCourseCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _teacherCourseService.UpdateCourseAsync(request.UserId, request.Id, request.Data, cancellationToken);
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
