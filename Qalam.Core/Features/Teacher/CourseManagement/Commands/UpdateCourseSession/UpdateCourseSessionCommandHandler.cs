using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Course;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Teacher.CourseManagement.Commands.UpdateCourseSession;

public class UpdateCourseSessionCommandHandler : ResponseHandler,
    IRequestHandler<UpdateCourseSessionCommand, Response<CourseSessionDto>>
{
    private readonly ITeacherCourseService _teacherCourseService;

    public UpdateCourseSessionCommandHandler(
        ITeacherCourseService teacherCourseService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _teacherCourseService = teacherCourseService;
    }

    public async Task<Response<CourseSessionDto>> Handle(
        UpdateCourseSessionCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _teacherCourseService.UpdateSessionAsync(
                request.UserId,
                request.CourseId,
                request.SessionId,
                request.Data,
                cancellationToken);

            if (result == null)
                return NotFound<CourseSessionDto>("Course or session not found.");

            return Success(entity: result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest<CourseSessionDto>(ex.Message);
        }
    }
}
