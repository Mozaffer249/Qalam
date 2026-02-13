using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Course;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Teacher.CourseManagement.Commands.CreateCourse;

public class CreateCourseCommandHandler : ResponseHandler,
    IRequestHandler<CreateCourseCommand, Response<CourseDetailDto>>
{
    private readonly ITeacherCourseService _teacherCourseService;

    public CreateCourseCommandHandler(
        ITeacherCourseService teacherCourseService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _teacherCourseService = teacherCourseService;
    }

    public async Task<Response<CourseDetailDto>> Handle(
        CreateCourseCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var dto = await _teacherCourseService.CreateCourseAsync(request.UserId, request.Data, cancellationToken);
            return Success(entity: dto);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest<CourseDetailDto>(ex.Message);
        }
    }
}
